
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public abstract class SpeechVerifierBase : ISpeechVerifier
    {
        #region Life & Death

        protected SpeechVerifierBase(SpeechApi.Configuration configuration)
        {
            Configuration = configuration;
            Profiles = new Dictionary<uint, SpeechVerifier.Profile>();
            VerifyResults = new Dictionary<string, SpeechVerifier.InstanceResult>(StringComparer.OrdinalIgnoreCase);
            MetaData = new Dictionary<string, string>();
            ExtraData = new Dictionary<string, object>();
            Content = new SpeechContent();
            ResetVerifier();
        }

        public abstract void Dispose();

        #endregion

        #region Public Properties

        public string InteractionId { get; set; }

        public string InteractionTag { get; set; }

        public string ClientId { get; set; }
        
        public string AuthToken { get; set; }

        #endregion

        #region Public Getters

        public SpeechApi.Configuration Configuration { get; }

        public SpeechContent Content { get; private set; }

        public CookieContainer CookieContainer { get; private set; }

        public string Server { get { return Configuration.Server; } }

        public string SessionId { get; protected set; }

        public string InteractionSource { get { return Configuration.ApplicationSource; } }

        public string InteractionAgent { get { return Configuration.ApplicationUserAgent; } }

        public Dictionary<string, string> MetaData { get; private set; }

        public Dictionary<string, object> ExtraData { get; private set; }

        public bool IsSessionOpen { get; protected set; }

        public bool IsSessionClosing { get; protected set; }

        public bool IsOverridable { get; private set; }

        public bool IsAuthorized { get; private set; }

        public Dictionary<uint, SpeechVerifier.Profile> Profiles { get; protected set; }

        public WaveFormatEncoding Codec { get; protected set; }

        public double VerifyScore { get; protected set; }

        public double SpeechRequired { get; protected set; }

        public double SpeechExtracted { get; protected set; }

        public int SpeechProgress
        {
            get
            {
                if (SpeechRequired <= 0) return 0;
                int progress = (int)System.Math.Round((SpeechExtracted / SpeechRequired) * 100);
                return (progress < 0) ? 0 : ((progress >= 100) ? (HasEnoughSpeech ? 100 : 99) : progress);
            }
        }

        public bool HasEnoughSpeech { get { return SpeechExtracted >= SpeechRequired; } }

        public bool IsTooSoft { get; protected set; }

        public bool IsTooLoud { get; protected set; }

        public SpeechVerifier.Result VerifyRawResult { get; protected set; }

        public SpeechVerifier.Result VerifyResult
        {
            get
            {
                if (IsLivenessRequired)
                {
                    switch (VerifyRawResult)
                    {
                        case SpeechVerifier.Result.Pass: return IsAlive ? SpeechVerifier.Result.PassIsAlive : SpeechVerifier.Result.PassNotAlive;
                        case SpeechVerifier.Result.Ambiguous: return IsAlive ? SpeechVerifier.Result.AmbiguousIsAlive : SpeechVerifier.Result.AmbiguousNotAlive;
                        case SpeechVerifier.Result.Fail: return IsAlive ? SpeechVerifier.Result.FailIsAlive : SpeechVerifier.Result.FailNotAlive;
                        case SpeechVerifier.Result.NeedMore:
                            switch (LivenessResult)
                            {
                                case SpeechVerifier.AliveResult.Untested: return SpeechVerifier.Result.NeedMore;
                                case SpeechVerifier.AliveResult.Alive: return SpeechVerifier.Result.NeedMoreIsAlive;
                                case SpeechVerifier.AliveResult.NotAlive: return SpeechVerifier.Result.NeedMoreNotAlive;
                            }
                            break;
                    }
                }
                return VerifyRawResult;
            }
        }

        public Dictionary<string, SpeechVerifier.InstanceResult> VerifyResults { get; protected set; }

        public uint TotalProcessCalls { get; protected set; }

        public uint TotalSnippetsSent { get; protected set; }

        public long TotalAudioBytesSent { get; protected set; }

        public bool IsLivenessRequired { get; protected set; }

        public bool IsVerified
        {
            get
            {
                return (VerifyRawResult == SpeechVerifier.Result.Pass && !IsLivenessRequired || IsAlive);
            }
        }

        public SpeechVerifier.AliveResult LivenessResult { get; protected set; }

        public bool IsAlive { get { return LivenessResult == SpeechVerifier.AliveResult.Alive; } }

        public bool HasResult
        {
            get
            {
                return (VerifyRawResult == SpeechVerifier.Result.Pass ||
                        VerifyRawResult == SpeechVerifier.Result.Ambiguous ||
                        VerifyRawResult == SpeechVerifier.Result.Fail);
            }
        }

        public bool HasRawResult
        {
            get
            {
                return VerifyRawResult == SpeechVerifier.Result.Pass ||
                    VerifyRawResult == SpeechVerifier.Result.Ambiguous ||
                    VerifyRawResult == SpeechVerifier.Result.Fail;
            }
        }

        public ISpeechLogger Logger { get { return Configuration.Logger; } }

        #endregion

        #region Public Methods

        public void SetFeedback(bool isBreakAttempt, bool isRecording, bool isBackgroundNoise, string comments)
        {
            comments = string.IsNullOrEmpty(comments) ? "N/A" : comments.Substring(0, System.Math.Min(comments.Length, 256));

            // Populate the MetaData using custom interface (old way of storing feedback)
            MetaData[SpeechApi.FEEDBACK_BREAK_ATTEMPT] = isBreakAttempt ? "1" : "0";
            MetaData[SpeechApi.FEEDBACK_RECORDING] = isRecording ? "1" : "0";
            MetaData[SpeechApi.FEEDBACK_BACKGROUND_NOISE] = isRecording ? "1" : "0";
            MetaData[SpeechApi.FEEDBACK_COMMENTS] = comments;

            // Populate the MetaData using the MetaData interface (new way of adding meta information)
            SetMetaData(SpeechApi.FEEDBACK_BREAK_ATTEMPT, isBreakAttempt);
            SetMetaData(SpeechApi.FEEDBACK_RECORDING, isRecording);
            SetMetaData(SpeechApi.FEEDBACK_BACKGROUND_NOISE, isBackgroundNoise);
            SetMetaData(SpeechApi.FEEDBACK_COMMENTS, comments);
        }

        public void SetMetaData(string name, bool value)
        {
            SetMetaData(name, value ? "1" : "0");
        }

        public void SetMetaData(string name, int value)
        {
            SetMetaData(name, Convert.ToString(value));
        }

        public void SetMetaData(string name, double value)
        {
            SetMetaData(name, Convert.ToString(value));
        }

        public void SetMetaData(string name, string value)
        {
            MetaData["Meta-" + name] = value;
        }

        public bool Append(string filename)
        {
            if (IsSessionOpen)
            {
                return Append(new WaveFileReader(filename));
            }
            return false;
        }

        public bool Append(string filename, SpeechContexts contexts)
        {
            if (IsSessionOpen)
            {
                var waveStream = new WaveFileReader(filename);
                if (Append(waveStream))
                {
                    return AppendContexts(contexts);
                }
            }
            return false;
        }

        public virtual bool Append(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                Logger?.LogDebug("SpeechVerifierBase.Append(): In-Length: " + stream.Length);
                Logger?.LogDebug("SpeechVerifierBase.Append(): In-WaveFormat: " + stream.WaveFormat);

                SpeechAudio speechAudio;
                if (Codec == WaveFormatEncoding.ALaw)
                {
                    speechAudio = new SpeechAudio(stream, WaveFormat.CreateALawFormat(8000, 1));
                }
                else if (Codec == WaveFormatEncoding.Pcm)
                {
                    speechAudio = new SpeechAudio(stream, new WaveFormat(8000, 16, 1));
                }
                else
                {
                    return false;
                }

                Logger?.LogDebug("SpeechVerifierBase.Append(): Append-Length: " + speechAudio.Stream.Length);
                Logger?.LogDebug("SpeechVerifierBase.Append(): Append-WaveFormat: " + speechAudio.Stream.WaveFormat);

                speechAudio.FileName = BuildAudioName();

                Content.Add("data", speechAudio);

                TotalSnippetsSent++;
                TotalAudioBytesSent += speechAudio.Stream.Length;

                return true;
            }
            return false;
        }

        public bool Append(WaveStream stream, SpeechContexts contexts)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    return AppendContexts(contexts);
                }
            }
            return false;
        }

        public virtual bool AppendStereo(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                var waveStream = new WaveFileReader(filename);
                Logger?.LogDebug("SpeechVerifierBase.AppendStereo(): Filename: " + filename);
                return AppendStereo(waveStream, channel);
            }
            return false;
        }

        public virtual bool AppendStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                Logger?.LogDebug("SpeechVerifierBase.AppendStereo(): In-Channel: " + channel);
                Logger?.LogDebug("SpeechVerifierBase.AppendStereo(): In-Length: " + stream.Length);
                Logger?.LogDebug("SpeechVerifierBase.AppendStereo(): In-WaveFormat: " + stream.WaveFormat);

                SpeechAudio speechAudio;
                if (Codec == WaveFormatEncoding.Pcm)
                {
                    speechAudio = new SpeechAudio(stream, new WaveFormat(8000, 16, 2), channel);
                }
                else
                {
                    return false;
                }

                Logger?.LogDebug("SpeechVerifierBase.AppendStereo(): Append-Length: " + speechAudio.Stream.Length);
                Logger?.LogDebug("SpeechVerifierBase.AppendStereo(): Append-WaveFormat: " + speechAudio.Stream.WaveFormat);

                speechAudio.FileName = BuildAudioName();

                switch (channel)
                {
                    case SpeechAudioChannel.Mono:
                        Content.Add("data", speechAudio);
                        break;
                    case SpeechAudioChannel.StereoLeft:
                        Content.Add("left", speechAudio);
                        break;
                    case SpeechAudioChannel.StereoRight:
                        Content.Add("right", speechAudio);
                        break;
                }

                TotalSnippetsSent++;
                TotalAudioBytesSent += speechAudio.Stream.Length;

                return true;
            }
            return false;
        }

        public virtual bool AppendStereo(string filename, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                var waveStream = new WaveFileReader(filename);
                if (AppendStereo(waveStream, channel))
                {
                    return AppendContexts(contexts);
                }
            }
            return false;
        }

        public virtual bool AppendStereo(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return AppendContexts(contexts);
                }
            }
            return false;
        }

        public virtual bool AppendContexts(SpeechContexts contexts)
        {
            if (IsSessionOpen)
            {
                if (contexts != null)
                {
                    IsLivenessRequired = true;
                    foreach (var ctx in contexts)
                    {
                        Content.Add("speech", ctx);
                    }
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Public Synchronous Methods

        public abstract SpeechVerifier.Profile PrefetchProfile();

        public abstract bool Start();

        public abstract SpeechVerifier.Result Post();

        public abstract SpeechVerifier.Result Post(string languageCode, string livenessText);

        public virtual SpeechVerifier.Result Post(string filename)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result Post(string filename, SpeechContexts contexts)
        {
            if (IsSessionOpen)
            {
                if (Append(filename, contexts))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result Post(string filename, string languageCode, string livenessText)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    IsLivenessRequired = true;
                    return Post(languageCode, livenessText);
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result Post(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result Post(WaveStream stream, SpeechContexts contexts)
        {
            if (IsSessionOpen)
            {
                if (Append(stream, contexts))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result Post(WaveStream stream, string languageCode, string livenessText)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    IsLivenessRequired = true;
                    return Post(languageCode, livenessText);
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result PostStereo(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result PostStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result PostStereo(string filename, string languageCode, string livenessText, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return Post(languageCode, livenessText);
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result PostStereo(WaveStream stream, string languageCode, string livenessText, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return Post(languageCode, livenessText);
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result PostStereo(string filename, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, contexts, channel))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public virtual SpeechVerifier.Result PostStereo(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, contexts, channel))
                {
                    return Post();
                }
            }
            return SpeechVerifier.Result.Invalid;
        }

        public abstract SpeechVerifier.Result Summarize();

        public abstract bool Cancel(string reason);

        #endregion

        #region Public Callback Methods

        public abstract void PrefetchProfile(SpeechVerifier.ProfileCallback profileCallback);

        public abstract void Start(SpeechVerifier.StartCallback callback);

        public abstract void Post(SpeechVerifier.PostCallback callback);

        public abstract void Post(string languageCode, string livenessText, SpeechVerifier.PostCallback callback);

        public virtual void Post(string filename, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    Post(callback);
                }
            }
        }

        public virtual void Post(string filename, SpeechContexts contexts, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(filename, contexts))
                {
                    Post(callback);
                }
            }
        }

        public virtual void Post(string filename, string languageCode, string livenessText, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    Post(languageCode, livenessText, callback);
                }
            }
        }

        public virtual void Post(WaveStream stream, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    Post(callback);
                }
            }
        }

        public virtual void Post(WaveStream stream, SpeechContexts contexts, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(stream, contexts))
                {
                    Post(callback);
                }
            }
        }

        public virtual void Post(WaveStream stream, string languageCode, string livenessText, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    Post(languageCode, livenessText, callback);
                }
            }
        }

        public virtual void PostStereo(string filename, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    Post(callback);
                }
            }
        }

        public virtual void PostStereo(WaveStream stream, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    Post(callback);
                }
            }
        }

        public virtual void PostStereo(string filename, string languageCode, string livenessText, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    Post(languageCode, livenessText, callback);
                }
            }
        }

        public virtual void PostStereo(WaveStream stream, string languageCode, string livenessText, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    Post(languageCode, livenessText, callback);
                }
            }
        }

        public virtual void PostStereo(string filename, SpeechContexts contexts, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, contexts, channel))
                {
                    Post(callback);
                }
            }
        }

        public virtual void PostStereo(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, contexts, channel))
                {
                     Post(callback);
                }
            }
        }

        public abstract void Summarize(SpeechVerifier.SummarizeCallback callback);

        public abstract void Cancel(string reason, SpeechVerifier.CancelCallback callback);

        #endregion

        #region Public Asynchronous Methods

        public abstract Task<SpeechVerifier.Profile> PrefetchProfileAsync();

        public abstract Task<bool> StartAsync();

        public abstract Task<SpeechVerifier.Result> PostAsync();

        public abstract Task<SpeechVerifier.Result> PostAsync(string languageCode, string livenessText);

        public virtual Task<SpeechVerifier.Result> PostAsync(string filename)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostAsync(string filename, SpeechContexts contexts)
        {
            if (IsSessionOpen)
            {
                if (Append(filename, contexts))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostAsync(string filename, string languageCode, string livenessText)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    IsLivenessRequired = true;
                    return PostAsync(languageCode, livenessText);
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostAsync(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostAsync(WaveStream stream, SpeechContexts contexts)
        {
            if (IsSessionOpen)
            {
                if (Append(stream, contexts))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostAsync(WaveStream stream, string languageCode, string livenessText)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    IsLivenessRequired = true;
                    return PostAsync(languageCode, livenessText);
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostStereoAsync(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostStereoAsync(string filename, string languageCode, string livenessText, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return PostAsync(languageCode, livenessText);
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostStereoAsync(WaveStream stream, string languageCode, string livenessText, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return PostAsync(languageCode, livenessText);
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostStereoAsync(string filename, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, contexts, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public virtual Task<SpeechVerifier.Result> PostStereoAsync(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, contexts, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechVerifier.Result.Invalid);
        }

        public abstract Task<SpeechVerifier.Result> SummarizeAsync();

        public abstract Task<bool> CancelAsync(string reason);

        #endregion

        #region Protected Methods

        protected void ResetVerifier()
        {
            SessionId = "";
            Codec = WaveFormatEncoding.Unknown;
            Profiles.Clear();
            VerifyResults.Clear();
            VerifyRawResult = SpeechVerifier.Result.NeedMore;
            MetaData.Clear();
            ExtraData.Clear();
            Content.Clear();
            CookieContainer = new CookieContainer();
            TotalSnippetsSent = 0;
            TotalAudioBytesSent = 0;
            TotalProcessCalls = 0;
            SpeechRequired = 0;
            SpeechExtracted = 0;
            VerifyScore = 0;
            IsSessionOpen = false;
            IsSessionClosing = false;
            IsAuthorized = false;
            IsOverridable = false;
            IsLivenessRequired = false;
            LivenessResult = SpeechVerifier.AliveResult.Untested;
            IsTooSoft = false;
            IsTooLoud = false;
        }
        
        protected uint AddProfile(uint index, SpeechVerifier.Profile profile)
        {
            if(!profile.MinimumSecondsOfSpeech.HasValue)
            { 
                profile.MinimumSecondsOfSpeech = 0;
            }
            SpeechRequired = profile.MinimumSecondsOfSpeech.Value;

            Profiles.Add(index, profile);

            if (Codec == WaveFormatEncoding.Unknown)
            {
                Codec = profile.Codec;
            }

            Logger?.LogDebug("SpeechVerifierBase.AddProfile(): Type: {0}", profile.Type);
            Logger?.LogDebug("SpeechVerifierBase.AddProfile(): Codec: {0}", profile.Codec);
            Logger?.LogDebug("SpeechVerifierBase.AddProfile(): MinimumSecondsOfSpeech: {0}", profile.MinimumSecondsOfSpeech);
            Logger?.LogDebug("SpeechVerifierBase.AddProfile(): Pass Threshold: {0}", profile.PassThreshold);
            Logger?.LogDebug("SpeechVerifierBase.AddProfile(): Fail Threshold: {0}", profile.FailThreshold);
            Logger?.LogDebug("SpeechVerifierBase.AddProfile(): Index: {0}", index);

            return index;
        }
        
        protected void AddResult(uint index, string name, SpeechVerifier.InstanceResult result)
        {
            SpeechVerifier.Profile profile = Profiles[index];

            if(result.Result == SpeechVerifier.Result.Unknown)
            {
                if (result.Score == 0.0)
                {
                    result.Result = SpeechVerifier.Result.NotScored;
                }
                else if (result.Score >= profile.PassThreshold)
                {
                    result.Result = SpeechVerifier.Result.Pass;
                }
                else if (result.Score <= profile.FailThreshold)
                {
                    result.Result = SpeechVerifier.Result.Fail;
                }
                else
                {
                    result.Result = SpeechVerifier.Result.Ambiguous;
                }
            }

            Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - Index: {1}", name, index);
            Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - Error: {1}", name, result.ErrorCode);
            Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - Score: {1}", name, result.Score);
            Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - SpeechExtracted: {1}", name, result.SpeechExtracted);
            Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - Result: {1}", name, result.Result);

            if(result.IsOverridable.HasValue)
            {
                IsOverridable = result.IsOverridable.Value;
                Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - IsOverridable: {1}", name, IsOverridable);
            }

            if (result.IsAuthorized.HasValue)
            {
                IsAuthorized = result.IsAuthorized.Value;
                Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - IsAuthorized: {1}", name, IsAuthorized);
            }

            foreach (var extra in result.Extra)
            {
                Logger?.LogDebug("SpeechVerifierBase.AddResult(): {0} - {1}: {2}", name, extra.Key, extra.Value);
            }

            VerifyResults.Add(name, result);
        }

        protected SpeechVerifier.Result GetErrorResult(int errorCode)
        {
            errorCode = errorCode >= 0 ? errorCode : -errorCode;
            switch (errorCode)
            {
                case 104: return SpeechVerifier.Result.LimitReached;
                case 110: return SpeechVerifier.Result.Unauthorized;
                case 403: return SpeechVerifier.Result.NeedMore;
                case 410: return SpeechVerifier.Result.NotFound;
                case 412: return SpeechVerifier.Result.LimitReached;
                case 420: return SpeechVerifier.Result.BadEnrollment;
                case 425: return SpeechVerifier.Result.TooSoft;
                case 426: return SpeechVerifier.Result.TooLoud;
                default: return SpeechVerifier.Result.Error;
            }
        }

        #endregion

        #region Private Methods

        private string BuildAudioName()
        {
            StringBuilder builder = new StringBuilder();
            string iid = InteractionId;
            if (!string.IsNullOrEmpty(iid))
            {
                builder.Append(iid).Append("-");
            }
            return builder.Append(ClientId).Append("-")
                    .Append(TotalProcessCalls).Append("-")
                    .Append(Content.Count).Append(".raw").ToString();
        }

        #endregion

    }
}

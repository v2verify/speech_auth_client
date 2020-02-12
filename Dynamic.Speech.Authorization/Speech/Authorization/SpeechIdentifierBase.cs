
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public abstract class SpeechIdentifierBase : ISpeechIdentifier
    {
        #region Life & Death

        protected SpeechIdentifierBase(SpeechApi.Configuration configuration)
        {
            Configuration = configuration;
            PossibleClientIdList = new List<string>();
            Profiles = new Dictionary<uint, SpeechIdentifier.Profile>();
            IdentifyResults = new Dictionary<string, SpeechIdentifier.InstanceResult>(StringComparer.OrdinalIgnoreCase);
            MetaData = new Dictionary<string, string>();
            ExtraData = new Dictionary<string, object>();
            Content = new SpeechContent();
            ResetIdentifier();
        }

        public abstract void Dispose();

        #endregion

        #region Public Properties

        public string InteractionId { get; set; }

        public string InteractionTag { get; set; }

        public string ProbableClientId { get; set; }

        public string PossibleClientIds
        {
            get
            {
                return string.Join(",", PossibleClientIdList);
            }
            set
            {
                if(string.IsNullOrEmpty(value))
                {
                    PossibleClientIdList.Clear();
                }
                else
                {
                    PossibleClientIdList.Clear();
                    PossibleClientIdList.AddRange(value.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        public List<string> PossibleClientIdList { get; private set; }

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
        
        public Dictionary<uint, SpeechIdentifier.Profile> Profiles { get; protected set; }

        public WaveFormatEncoding Codec { get; protected set; }

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

        public uint TotalProcessCalls { get; protected set; }

        public uint TotalSnippetsSent { get; protected set; }

        public long TotalAudioBytesSent { get; protected set; }

        public SpeechIdentifier.Result IdentifyResult { get; protected set; }

        public Dictionary<string, SpeechIdentifier.InstanceResult> IdentifyResults { get; protected set; }

        public bool HasResult
        {
            get
            {
                return (IdentifyResult == SpeechIdentifier.Result.Pass ||
                        IdentifyResult == SpeechIdentifier.Result.Ambiguous ||
                        IdentifyResult == SpeechIdentifier.Result.Fail);
            }
        }

        public bool IsIdentified { get { return (IdentifyResult == SpeechIdentifier.Result.Pass); } }

        public string IdentifiedClientId { get; protected set; }

        public double IdentifiedScore { get; protected set; }

        public bool IsAuthorized { get; private set; }

        public ISpeechLogger Logger { get { return Configuration.Logger; } }

        #endregion

        #region Public Methods

        public void SetFeedback(bool isRecording, bool isBackgroundNoise, string comments)
        {
            comments = string.IsNullOrEmpty(comments) ? "N/A" : comments.Substring(0, System.Math.Min(comments.Length, 256));

            // Populate the MetaData using custom interface (old way of storing feedback)
            MetaData[SpeechApi.FEEDBACK_RECORDING] = isRecording ? "1" : "0";
            MetaData[SpeechApi.FEEDBACK_BACKGROUND_NOISE] = isRecording ? "1" : "0";
            MetaData[SpeechApi.FEEDBACK_COMMENTS] = comments;

            // Populate the MetaData using the MetaData interface (new way of adding meta information)
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
        
        public virtual bool Append(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                Logger?.LogDebug("SpeechIdentifierBase.Append(): In-Length: " + stream.Length);
                Logger?.LogDebug("SpeechIdentifierBase.Append(): In-WaveFormat: " + stream.WaveFormat);

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

                Logger?.LogDebug("SpeechIdentifierBase.Append(): Append-Length: " + speechAudio.Stream.Length);
                Logger?.LogDebug("SpeechIdentifierBase.Append(): Append-WaveFormat: " + speechAudio.Stream.WaveFormat);

                speechAudio.FileName = BuildAudioName();

                Content.Add("data", speechAudio);

                TotalSnippetsSent++;
                TotalAudioBytesSent += speechAudio.Stream.Length;

                return true;
            }
            return false;
        }

        public virtual bool AppendStereo(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                var waveStream = new WaveFileReader(filename);
                Logger?.LogDebug("SpeechIdentifierBase.AppendStereo(): Filename: " + filename);
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

                Logger?.LogDebug("SpeechIdentifierBase.AppendStereo(): In-Channel: " + channel);
                Logger?.LogDebug("SpeechIdentifierBase.AppendStereo(): In-Length: " + stream.Length);
                Logger?.LogDebug("SpeechIdentifierBase.AppendStereo(): In-WaveFormat: " + stream.WaveFormat);

                SpeechAudio speechAudio;
                if (Codec == WaveFormatEncoding.Pcm)
                {
                    speechAudio = new SpeechAudio(stream, new WaveFormat(8000, 16, 2), channel);
                }
                else
                {
                    return false;
                }

                Logger?.LogDebug("SpeechIdentifierBase.AppendStereo(): Append-Length: " + speechAudio.Stream.Length);
                Logger?.LogDebug("SpeechIdentifierBase.AppendStereo(): Append-WaveFormat: " + speechAudio.Stream.WaveFormat);

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

        #endregion

        #region Public Synchronous Methods

        public abstract SpeechIdentifier.Profile PrefetchProfile();

        public abstract bool Start();

        public abstract SpeechIdentifier.Result Post();
        
        public virtual SpeechIdentifier.Result Post(string filename)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    return Post();
                }
            }
            return SpeechIdentifier.Result.Invalid;
        }
        
        public virtual SpeechIdentifier.Result Post(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    return Post();
                }
            }
            return SpeechIdentifier.Result.Invalid;
        }

        public virtual SpeechIdentifier.Result PostStereo(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return Post();
                }
            }
            return SpeechIdentifier.Result.Invalid;
        }

        public virtual SpeechIdentifier.Result PostStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return Post();
                }
            }
            return SpeechIdentifier.Result.Invalid;
        }

        public abstract SpeechIdentifier.Result Summarize();

        public abstract bool Cancel(string reason);

        #endregion

        #region Public Callback Methods

        public abstract void PrefetchProfile(SpeechIdentifier.ProfileCallback profileCallback);

        public abstract void Start(SpeechIdentifier.StartCallback callback);

        public abstract void Post(SpeechIdentifier.PostCallback callback);
        
        public virtual void Post(string filename, SpeechIdentifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    Post(callback);
                }
            }
        }
        
        public virtual void Post(WaveStream stream, SpeechIdentifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    Post(callback);
                }
            }
        }

        public virtual void PostStereo(string filename, SpeechAudioChannel channel, SpeechIdentifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    Post(callback);
                }
            }
        }

        public virtual void PostStereo(WaveStream stream, SpeechAudioChannel channel, SpeechIdentifier.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    Post(callback);
                }
            }
        }

        public abstract void Summarize(SpeechIdentifier.SummarizeCallback callback);

        public abstract void Cancel(string reason, SpeechIdentifier.CancelCallback callback);

        #endregion

        #region Public Asynchronous Methods

        public abstract Task<SpeechIdentifier.Profile> PrefetchProfileAsync();

        public abstract Task<bool> StartAsync();

        public abstract Task<SpeechIdentifier.Result> PostAsync();
        
        public virtual Task<SpeechIdentifier.Result> PostAsync(string filename)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechIdentifier.Result.Invalid);
        }
        
        public virtual Task<SpeechIdentifier.Result> PostAsync(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechIdentifier.Result.Invalid);
        }

        public virtual Task<SpeechIdentifier.Result> PostStereoAsync(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechIdentifier.Result.Invalid);
        }

        public virtual Task<SpeechIdentifier.Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechIdentifier.Result.Invalid);
        }

        public abstract Task<SpeechIdentifier.Result> SummarizeAsync();

        public abstract Task<bool> CancelAsync(string reason);

        #endregion

        #region Protected Methods

        protected void ResetIdentifier()
        {
            SessionId = "";
            Codec = WaveFormatEncoding.Unknown;
            Profiles.Clear();
            IdentifyResults.Clear();
            IdentifyResult = SpeechIdentifier.Result.NeedMore;
            MetaData.Clear();
            ExtraData.Clear();
            Content.Clear();
            CookieContainer = new CookieContainer();
            TotalSnippetsSent = 0;
            TotalAudioBytesSent = 0;
            TotalProcessCalls = 0;
            SpeechRequired = 0;
            SpeechExtracted = 0;
            IdentifiedClientId = "";
            IdentifiedScore = 0;
            IsSessionOpen = false;
            IsSessionClosing = false;
            IsAuthorized = false;
            IsTooSoft = false;
            IsTooLoud = false;
        }

        protected uint AddProfile(uint index, SpeechIdentifier.Profile profile)
        {
            if (!profile.MinimumSecondsOfSpeech.HasValue)
            {
                profile.MinimumSecondsOfSpeech = 0;
            }
            SpeechRequired = profile.MinimumSecondsOfSpeech.Value;

            Profiles.Add(index, profile);

            if (Codec == WaveFormatEncoding.Unknown)
            {
                Codec = profile.Codec;
            }

            Logger?.LogDebug("SpeechIdentifierBase.AddProfile(): Type: {0}", profile.Type);
            Logger?.LogDebug("SpeechIdentifierBase.AddProfile(): Codec: {0}", profile.Codec);
            Logger?.LogDebug("SpeechIdentifierBase.AddProfile(): MinimumSecondsOfSpeech: {0}", profile.MinimumSecondsOfSpeech);
            Logger?.LogDebug("SpeechIdentifierBase.AddProfile(): Pass Threshold: {0}", profile.PassThreshold);
            Logger?.LogDebug("SpeechIdentifierBase.AddProfile(): Fail Threshold: {0}", profile.FailThreshold);
            Logger?.LogDebug("SpeechIdentifierBase.AddProfile(): Index: {0}", index);

            return index;
        }

        protected void AddResult(uint index, string name, SpeechIdentifier.InstanceResult result)
        {
            SpeechIdentifier.Profile profile = Profiles[index];

            if (result.Result == SpeechIdentifier.Result.Unknown)
            {
                if (result.Score == 0.0)
                {
                    result.Result = SpeechIdentifier.Result.NotScored;
                }
                else if (result.Score >= profile.PassThreshold)
                {
                    result.Result = SpeechIdentifier.Result.Pass;
                }
                else if (result.Score <= profile.FailThreshold)
                {
                    result.Result = SpeechIdentifier.Result.Fail;
                }
                else
                {
                    result.Result = SpeechIdentifier.Result.Ambiguous;
                }
            }

            Logger?.LogDebug("SpeechIdentifierBase.AddResult(): {0} - Index: {1}", name, index);
            Logger?.LogDebug("SpeechIdentifierBase.AddResult(): {0} - Error: {1}", name, result.ErrorCode);
            Logger?.LogDebug("SpeechIdentifierBase.AddResult(): {0} - Score: {1}", name, result.Score);
            Logger?.LogDebug("SpeechIdentifierBase.AddResult(): {0} - SpeechExtracted: {1}", name, result.SpeechExtracted);
            Logger?.LogDebug("SpeechIdentifierBase.AddResult(): {0} - Result: {1}", name, result.Result);
            
            if (result.IsAuthorized.HasValue)
            {
                IsAuthorized = result.IsAuthorized.Value;
                Logger?.LogDebug("SpeechIdentifierBase.AddResult(): {0} - IsAuthorized: {1}", name, IsAuthorized);
            }

            foreach (var extra in result.Extra)
            {
                Logger?.LogDebug("SpeechIdentifierBase.AddResult(): {0} - {1}: {2}", name, extra.Key, extra.Value);
            }

            IdentifyResults.Add(name, result);
        }

        protected SpeechIdentifier.Result GetErrorResult(int errorCode)
        {
            errorCode = errorCode >= 0 ? errorCode : -errorCode;
            switch (errorCode)
            {
                case 104: return SpeechIdentifier.Result.LimitReached;
                case 110: return SpeechIdentifier.Result.Unauthorized;
                case 403: return SpeechIdentifier.Result.NeedMore;
                case 410: return SpeechIdentifier.Result.NotFound;
                case 412: return SpeechIdentifier.Result.LimitReached;
                case 420: return SpeechIdentifier.Result.BadEnrollment;
                case 425: return SpeechIdentifier.Result.TooSoft;
                case 426: return SpeechIdentifier.Result.TooLoud;
                default: return SpeechIdentifier.Result.Error;
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
            return builder.Append(PossibleClientIds).Append("-")
                    .Append(TotalProcessCalls).Append("-")
                    .Append(Content.Count).Append(".raw").ToString();
        }

        #endregion

    }
}

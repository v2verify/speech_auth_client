
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public abstract class SpeechEnrollerBase : ISpeechEnroller
    {
        #region Life & Death

        protected SpeechEnrollerBase(SpeechApi.Configuration configuration)
        {
            Configuration = configuration;
            Profiles = new Dictionary<uint, SpeechEnroller.Profile>();
            EnrollResults = new Dictionary<string, SpeechEnroller.InstanceResult>(StringComparer.OrdinalIgnoreCase);
            MetaData = new Dictionary<string, string>();
            ExtraData = new Dictionary<string, object>();
            Content = new SpeechContent();
            ResetEnroller();
        }

        public abstract void Dispose();

        #endregion

        #region Public Properties

        public string InteractionId { get; set; }

        public string InteractionTag { get; set; }

        public SpeechEnroller.Gender SubPopulation { get; set; }

        public string ClientId { get; set; }

        public string AuthToken { get; set; }

        #endregion

        #region Public Getters

        public SpeechApi.Configuration Configuration { get; private set; }

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

        public Dictionary<uint, SpeechEnroller.Profile> Profiles { get; protected set; }

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

        public double SpeechTrained { get; protected set; }

        public SpeechEnroller.Result EnrollResult { get; protected set; }

        public Dictionary<string, SpeechEnroller.InstanceResult> EnrollResults { get; protected set; }

        public uint TotalProcessCalls { get; protected set; }

        public uint TotalSnippetsSent { get; protected set; }

        public long TotalAudioBytesSent { get; protected set; }

        public bool HasEnoughSpeech { get { return SpeechExtracted >= SpeechRequired; } }

        public bool IsTooSoft { get; protected set; }

        public bool IsTooLoud { get; protected set; }

        public bool IsTrained { get; protected set; }

        public ISpeechLogger Logger { get { return Configuration.Logger; } }

        #endregion

        #region Public Methods

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
            // For now, double up on these fields, until
            // they can be properly coded for server side
            MetaData[name] = value;
            MetaData["Meta-" + name] = value;
        }

        public virtual bool Append(string filename)
        {
            if (IsSessionOpen)
            {
                var waveStream = new WaveFileReader(filename);
                Logger?.LogDebug("SpeechEnrollerBase.Append(): Filename: " + filename);
                return Append(waveStream);
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

                Logger?.LogDebug("SpeechEnrollerBase.Append(): In-Length: " + stream.Length);
                Logger?.LogDebug("SpeechEnrollerBase.Append(): In-WaveFormat: " + stream.WaveFormat);

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

                Logger?.LogDebug("SpeechEnrollerBase.Append(): Append-Length: " + speechAudio.Stream.Length);
                Logger?.LogDebug("SpeechEnrollerBase.Append(): Append-WaveFormat: " + speechAudio.Stream.WaveFormat);

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
                Logger?.LogDebug("SpeechEnrollerBase.AppendStereo(): Filename: " + filename);
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

                Logger?.LogDebug("SpeechEnrollerBase.AppendStereo(): In-Channel: " + channel);
                Logger?.LogDebug("SpeechEnrollerBase.AppendStereo(): In-Length: " + stream.Length);
                Logger?.LogDebug("SpeechEnrollerBase.AppendStereo(): In-WaveFormat: " + stream.WaveFormat);

                SpeechAudio speechAudio;
                if (Codec == WaveFormatEncoding.Pcm)
                {
                    speechAudio = new SpeechAudio(stream, new WaveFormat(8000, 16, 2), channel);
                }
                else
                {
                    return false;
                }

                Logger?.LogDebug("SpeechEnrollerBase.AppendStereo(): Append-Length: " + speechAudio.Stream.Length);
                Logger?.LogDebug("SpeechEnrollerBase.AppendStereo(): Append-WaveFormat: " + speechAudio.Stream.WaveFormat);

                speechAudio.FileName = BuildAudioName();

                switch(channel)
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

        public abstract SpeechEnroller.Profile PrefetchProfile();

        public abstract bool Start();

        public abstract SpeechEnroller.Result Post();

        public virtual SpeechEnroller.Result Post(string filename)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    return Post();
                }
            }
            return SpeechEnroller.Result.Invalid;
        }

        public virtual SpeechEnroller.Result Post(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    return Post();
                }
            }
            return SpeechEnroller.Result.Invalid;
        }

        public virtual SpeechEnroller.Result PostStereo(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return Post();
                }
            }
            return SpeechEnroller.Result.Invalid;
        }

        public virtual SpeechEnroller.Result PostStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return Post();
                }
            }
            return SpeechEnroller.Result.Invalid;
        }

        public abstract SpeechEnroller.Result Train();

        public abstract bool Cancel(string reason);

        #endregion

        #region Public Callback Methods

        public abstract void PrefetchProfile(SpeechEnroller.ProfileCallback profileCallback);

        public abstract void Start(SpeechEnroller.StartCallback callback);

        public abstract void Post(SpeechEnroller.PostCallback callback);

        public virtual void Post(string filename, SpeechEnroller.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    Post(callback);
                }
            }
        }

        public virtual void Post(WaveStream stream, SpeechEnroller.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    Post(callback);
                }
            }
        }

        public virtual void PostStereo(string filename, SpeechAudioChannel channel, SpeechEnroller.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    Post(callback);
                }
            }
        }

        public virtual void PostStereo(WaveStream stream, SpeechAudioChannel channel, SpeechEnroller.PostCallback callback)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    Post(callback);
                }
            }
        }

        public abstract void Train(SpeechEnroller.TrainCallback callback);

        public abstract void Cancel(string reason, SpeechEnroller.CancelCallback callback);

        #endregion

        #region Public Asynchronous Methods

        public abstract Task<SpeechEnroller.Profile> PrefetchProfileAsync();

        public abstract Task<bool> StartAsync();

        public abstract Task<SpeechEnroller.Result> PostAsync();

        public virtual Task<SpeechEnroller.Result> PostAsync(string filename)
        {
            if (IsSessionOpen)
            {
                if (Append(filename))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechEnroller.Result.Invalid);
        }

        public virtual Task<SpeechEnroller.Result> PostAsync(WaveStream stream)
        {
            if (IsSessionOpen)
            {
                if (Append(stream))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechEnroller.Result.Invalid);
        }

        public virtual Task<SpeechEnroller.Result> PostStereoAsync(string filename, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(filename, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechEnroller.Result.Invalid);
        }

        public virtual Task<SpeechEnroller.Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel)
        {
            if (IsSessionOpen)
            {
                if (AppendStereo(stream, channel))
                {
                    return PostAsync();
                }
            }
            return Task.FromResult(SpeechEnroller.Result.Invalid);
        }

        public abstract Task<SpeechEnroller.Result> TrainAsync();

        public abstract Task<bool> CancelAsync(string reason);

        #endregion

        #region Protected Methods

        protected void ResetEnroller()
        {
            SessionId = "";
            Codec = WaveFormatEncoding.Unknown;
            Profiles.Clear();
            EnrollResults.Clear();
            EnrollResult = SpeechEnroller.Result.NeedMore;
            MetaData.Clear();
            ExtraData.Clear();
            Content.Clear();
            CookieContainer = new CookieContainer();
            TotalSnippetsSent = 0;
            TotalAudioBytesSent = 0;
            TotalProcessCalls = 0;
            SpeechRequired = 0;
            SpeechExtracted = 0;
            IsSessionOpen = false;
            IsSessionClosing = false;
            IsTooSoft = false;
            IsTooLoud = false;
        }
        
        protected uint AddProfile(uint index, SpeechEnroller.Profile profile)
        {
            Profiles.Add(index, profile);

            if (Codec == WaveFormatEncoding.Unknown)
            {
                Codec = profile.Codec;
            }

            Logger?.LogDebug("SpeechEnrollerBase.AddProfile(): Codec: {0}", profile.Codec);
            Logger?.LogDebug("SpeechEnrollerBase.AddProfile(): MinimumSecondsOfSpeech: {0}", profile.MinimumSecondsOfSpeech);
            Logger?.LogDebug("SpeechEnrollerBase.AddProfile(): Index: {0}", index);

            SpeechRequired = profile.MinimumSecondsOfSpeech;

            return index;
        }
        
        protected void AddResult(uint index, string name, SpeechEnroller.InstanceResult result)
        {
            SpeechEnroller.Profile profile = Profiles[index];

            if (result.SpeechExtracted.HasValue)
            {
                result.SpeechTrained = 0.0;
                if (result.SpeechExtracted.Value >= profile.MinimumSecondsOfSpeech)
                {
                    result.Result = SpeechEnroller.Result.Success;
                }
                else
                {
                    result.Result = SpeechEnroller.Result.NeedMore;
                }
            }
            else if (result.SpeechTrained.HasValue)
            {
                result.Result = SpeechEnroller.Result.Success;
                result.SpeechExtracted = 0.0;
            }
            else
            {
                // Error
                result.Result = SpeechEnroller.Result.Invalid;
            }

            Logger?.LogDebug("SpeechEnrollerBase.AddResult(): {0} - ErrorCode: {1}", name, result.ErrorCode);
            Logger?.LogDebug("SpeechEnrollerBase.AddResult(): {0} - SpeechExtracted: {1}", name, result.SpeechExtracted);
            Logger?.LogDebug("SpeechEnrollerBase.AddResult(): {0} - SpeechTrained: {1}", name, result.SpeechTrained);
            Logger?.LogDebug("SpeechEnrollerBase.AddResult(): {0} - Result: {1}", name, result.Result);

            foreach (var extra in result.Extra)
            {
                Logger?.LogDebug("SpeechEnrollerBase.AddResult(): {0} - {1}: {2}", name, extra.Key, extra.Value);
            }

            EnrollResults.Add(name, result);
        }

        protected SpeechEnroller.Result GetErrorResult(int errorCode)
        {
            errorCode = errorCode >= 0 ? errorCode : -errorCode;
            switch (errorCode)
            {
                case 104: return SpeechEnroller.Result.LimitReached;
                case 110: return SpeechEnroller.Result.Unauthorized;
                case 303: return SpeechEnroller.Result.NeedMore;
                case 311: return SpeechEnroller.Result.LimitReached;
                case 318: return SpeechEnroller.Result.AlreadyEnrolled;
                case 320: return SpeechEnroller.Result.TokenExists;
                case 321: return SpeechEnroller.Result.TokenRequired;
                case 325: return SpeechEnroller.Result.TooSoft;
                case 326: return SpeechEnroller.Result.TooLoud;
                default: return SpeechEnroller.Result.Error;
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

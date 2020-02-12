
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public class SpeechIdentifier : IDisposable
    {
        #region Public Enum's, Classes, Interfaces, and Delegates

        public enum Result
        {
            [Description("Pass")]
            Pass,

            [Description("Ambiguous")]
            Ambiguous,

            [Description("Fail")]
            Fail,

            [Description("NeedMore")]
            NeedMore,

            [Description("Not Scored")]
            NotScored,

            [Description("Too Soft")]
            TooSoft,

            [Description("Too Loud")]
            TooLoud,

            [Description("Limit Reached")]
            LimitReached,

            [Description("Unauthorized")]
            Unauthorized,

            [Description("Not Found")]
            NotFound,

            [Description("Bad Enrollment")]
            BadEnrollment,

            [Description("Timeout")]
            Timeout,

            [Description("Invalid")]
            Invalid,

            [Description("Error")]
            Error,

            [Description("Unknown")]
            Unknown
        }

        public enum ProfileType
        {
            [Description("Unknown")]
            Unknown = -1,

            [Description("[Multi-Verify] Identify")]
            Identify = 6
        }

        public class Profile
        {
            public WaveFormatEncoding Codec { get; internal set; }

            public ProfileType Type { get; internal set; }

            public double? MinimumSecondsOfSpeech { get; internal set; }

            public double PassThreshold { get; internal set; }

            public double FailThreshold { get; internal set; }
        }

        public class InstanceResult
        {
            internal InstanceResult()
            {
                Extra = new Dictionary<string, object>();
            }

            public Result Result { get; internal set; }
            public double Score { get; internal set; }
            public double SpeechExtracted { get; internal set; }
            public uint ErrorCode { get; internal set; }
            public bool? IsOverridable { get; internal set; }
            public bool? IsAuthorized { get; internal set; }
            public Dictionary<string, object> Extra { get; private set; }
        }

        public delegate void ProfileCallback(Profile profile);

        public delegate void StartCallback(bool started);

        public delegate void PostCallback(Result result);

        public delegate void SummarizeCallback(Result result);

        public delegate void CancelCallback();

        #endregion

        #region Private

        private ISpeechIdentifier mSpeechIdentifier;

        #endregion

        #region Life & Death

        public SpeechIdentifier()
            : this(SpeechApi.DefaultConfiguration)
        {
        }

        public SpeechIdentifier(SpeechApi.Configuration configuration)
        {
            if (configuration.ServerTransport != SpeechApi.SpeechTransport.Rest)
            {
                throw new NotSupportedException("ServerTransport not supported");
            }
            else if (configuration.ServerVersion == SpeechApi.SpeechVersion.Version_1)
            {
                if (configuration.ServerTransportObject is HttpClient)
                {
                    mSpeechIdentifier = new Internal.SpeechIdentifier_HttpClient_Version1(configuration);
                }
                else
                {
                    throw new NotSupportedException("ServerTransport not supported");
                }
            }
            else
            {
                throw new NotSupportedException("ServerVersion not supported");
            }
        }

        public void Dispose()
        {
            if (mSpeechIdentifier != null)
            {
                mSpeechIdentifier.Dispose();
                mSpeechIdentifier = null;
            }
        }

        ~SpeechIdentifier()
        {
            Dispose();
        }

        #endregion

        #region Public Properties

        public string InteractionId
        {
            get { return mSpeechIdentifier.InteractionId; }
            set { mSpeechIdentifier.InteractionId = value; }
        }

        public string InteractionTag
        {
            get { return mSpeechIdentifier.InteractionTag; }
            set { mSpeechIdentifier.InteractionTag = value; }
        }

        public string MostLikelyClientId
        {
            get { return mSpeechIdentifier.ProbableClientId; }
            set { mSpeechIdentifier.ProbableClientId = value; }
        }

        public string PossibleClientIds
        {
            get { return mSpeechIdentifier.PossibleClientIds; }
            set { mSpeechIdentifier.PossibleClientIds = value; }
        }

        public List<string> PossibleClientIdList
        {
            get { return mSpeechIdentifier.PossibleClientIdList; }
        }

        #endregion

        #region Public Getters

        public string Server
        {
            get { return mSpeechIdentifier.Server; }
        }

        public string SessionId
        {
            get { return mSpeechIdentifier.SessionId; }
        }

        public string InteractionSource
        {
            get { return mSpeechIdentifier.InteractionSource; }
        }

        public string InteractionAgent
        {
            get { return mSpeechIdentifier.InteractionAgent; }
        }

        public bool IsSessionOpen
        {
            get { return mSpeechIdentifier.IsSessionOpen; }
        }

        public bool IsSessionClosing
        {
            get { return mSpeechIdentifier.IsSessionClosing; }
        }

        public Dictionary<uint, Profile> Profiles
        {
            get { return mSpeechIdentifier.Profiles; }
        }

        public WaveFormatEncoding Codec
        {
            get { return mSpeechIdentifier.Codec; }
        }

        public double SpeechRequired
        {
            get { return mSpeechIdentifier.SpeechRequired; }
        }

        public double SpeechExtracted
        {
            get { return mSpeechIdentifier.SpeechExtracted; }
        }

        public int SpeechProgress
        {
            get { return mSpeechIdentifier.SpeechProgress; }
        }

        public bool HasEnoughSpeech
        {
            get { return mSpeechIdentifier.HasEnoughSpeech; }
        }

        public bool IsTooSoft
        {
            get { return mSpeechIdentifier.IsTooSoft; }
        }

        public bool IsTooLoud
        {
            get { return mSpeechIdentifier.IsTooLoud; }
        }

        public Result IdentifyResult
        {
            get { return mSpeechIdentifier.IdentifyResult; }
        }

        public Dictionary<string, InstanceResult> IdentifyResults
        {
            get { return mSpeechIdentifier.IdentifyResults; }
        }

        public uint TotalProcessCalls
        {
            get { return mSpeechIdentifier.TotalProcessCalls; }
        }

        public uint TotalSnippetsSent
        {
            get { return mSpeechIdentifier.TotalSnippetsSent; }
        }

        public long TotalAudioBytesSent
        {
            get { return mSpeechIdentifier.TotalAudioBytesSent; }
        }

        public bool HasResult
        {
            get { return mSpeechIdentifier.HasResult; }
        }

        public bool IsIdentified
        {
            get { return mSpeechIdentifier.IsIdentified; }
        }

        public string IdentifiedClientId
        {
            get { return mSpeechIdentifier.IdentifiedClientId; }
        }

        public double IdentifiedScore
        {
            get { return mSpeechIdentifier.IdentifiedScore; }
        }

        public bool IsAuthorized
        {
            get { return mSpeechIdentifier.IsAuthorized; }
        }

        public IReadOnlyDictionary<string, object> ExtraData
        {
            get { return mSpeechIdentifier.ExtraData; }
        }

        public ISpeechLogger Logger
        {
            get { return mSpeechIdentifier.Logger; }
        }

        #endregion

        #region Public Methods

        public void SetFeedback(bool isRecording, bool isBackgroundNoise, string comments)
        {
            mSpeechIdentifier.SetFeedback(isRecording, isBackgroundNoise, comments);
        }

        public void SetMetaData(string name, bool value)
        {
            mSpeechIdentifier.SetMetaData(name, value);
        }

        public void SetMetaData(string name, int value)
        {
            mSpeechIdentifier.SetMetaData(name, value);
        }

        public void SetMetaData(string name, double value)
        {
            mSpeechIdentifier.SetMetaData(name, value);
        }

        public void SetMetaData(string name, string value)
        {
            mSpeechIdentifier.SetMetaData(name, value);
        }

        public bool Append(string filename)
        {
            return mSpeechIdentifier.Append(filename);
        }

        public bool Append(WaveStream stream)
        {
            return mSpeechIdentifier.Append(stream);
        }

        public bool AppendStereo(string filename, SpeechAudioChannel channel)
        {
            return mSpeechIdentifier.AppendStereo(filename, channel);
        }

        public bool AppendStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            return mSpeechIdentifier.AppendStereo(stream, channel);
        }

        #endregion

        #region Public Synchronous Methods

        public Profile PrefetchProfile()
        {
            return mSpeechIdentifier.PrefetchProfile();
        }

        public bool Start()
        {
            return mSpeechIdentifier.Start();
        }

        public Result Post()
        {
            return mSpeechIdentifier.Post();
        }
        
        public Result Post(string filename)
        {
            return mSpeechIdentifier.Post(filename);
        }
        
        public Result Post(WaveStream stream)
        {
            return mSpeechIdentifier.Post(stream);
        }

        public Result PostStereo(string filename, SpeechAudioChannel channel)
        {
            return mSpeechIdentifier.PostStereo(filename, channel);
        }

        public Result PostStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            return mSpeechIdentifier.PostStereo(stream, channel);
        }

        public Result Summarize()
        {
            return mSpeechIdentifier.Summarize();
        }

        public bool Cancel(string reason)
        {
            return mSpeechIdentifier.Cancel(reason);
        }

        #endregion

        #region Public Callback Methods

        public void PrefetchProfile(ProfileCallback profileCallback)
        {
            mSpeechIdentifier.PrefetchProfile(profileCallback);
        }

        public void Start(StartCallback callback)
        {
            mSpeechIdentifier.Start(callback);
        }

        public void Post(PostCallback callback)
        {
            mSpeechIdentifier.Post(callback);
        }
        
        public void Post(string filename, PostCallback callback)
        {
            mSpeechIdentifier.Post(filename, callback);
        }
        
        public void Post(WaveStream stream, PostCallback callback)
        {
            mSpeechIdentifier.Post(stream, callback);
        }

        public void PostStereo(string filename, SpeechAudioChannel channel, PostCallback callback)
        {
            mSpeechIdentifier.PostStereo(filename, channel, callback);
        }

        public void PostStereo(WaveStream stream, SpeechAudioChannel channel, PostCallback callback)
        {
            mSpeechIdentifier.PostStereo(stream, channel, callback);
        }

        public void Summarize(SummarizeCallback callback)
        {
            mSpeechIdentifier.Summarize(callback);
        }

        public void Cancel(string reason, CancelCallback callback)
        {
            mSpeechIdentifier.Cancel(reason, callback);
        }

        #endregion

        #region Public Asynchronous Methods

        public Task<Profile> PrefetchProfileAsync()
        {
            return mSpeechIdentifier.PrefetchProfileAsync();
        }

        public Task<bool> StartAsync()
        {
            return mSpeechIdentifier.StartAsync();
        }

        public Task<Result> PostAsync()
        {
            return mSpeechIdentifier.PostAsync();
        }
        
        public Task<Result> PostAsync(string filename)
        {
            return mSpeechIdentifier.PostAsync(filename);
        }
        
        public Task<Result> PostAsync(WaveStream stream)
        {
            return mSpeechIdentifier.PostAsync(stream);
        }

        public Task<Result> PostStereoAsync(string filename, SpeechAudioChannel channel)
        {
            return mSpeechIdentifier.PostStereoAsync(filename, channel);
        }

        public Task<Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel)
        {
            return mSpeechIdentifier.PostStereoAsync(stream, channel);
        }

        public Task<Result> SummarizeAsync()
        {
            return mSpeechIdentifier.SummarizeAsync();
        }

        public Task<bool> CancelAsync(string reason)
        {
            return mSpeechIdentifier.CancelAsync(reason);
        }

        #endregion

    }

    public static class SpeechIdentifierExtensions
    {
        #region Public Utility Methods

        public static string GetDescrption(this SpeechIdentifier.Result result)
        {
            switch (result)
            {
                case SpeechIdentifier.Result.Pass: return "Pass";
                case SpeechIdentifier.Result.Ambiguous: return "Ambiguous";
                case SpeechIdentifier.Result.Fail: return "Fail";
                case SpeechIdentifier.Result.NeedMore: return "NeedMore";
                case SpeechIdentifier.Result.NotScored: return "NotScored";
                case SpeechIdentifier.Result.TooSoft: return "TooSoft";
                case SpeechIdentifier.Result.TooLoud: return "TooLoud";
                case SpeechIdentifier.Result.LimitReached: return "LimitReached";
                case SpeechIdentifier.Result.Unauthorized: return "Unauthorized";
                case SpeechIdentifier.Result.NotFound: return "NotFound";
                case SpeechIdentifier.Result.BadEnrollment: return "BadEnrollment";
                case SpeechIdentifier.Result.Timeout: return "Timeout";
                case SpeechIdentifier.Result.Invalid: return "Invalid";
                case SpeechIdentifier.Result.Error: return "Error";
                case SpeechIdentifier.Result.Unknown: return "Unknown";
            }
            return "Unknown";
        }

        public static SpeechIdentifier.ProfileType GetProfileType(this uint profileType)
        {
            switch (profileType)
            {
                case 6:
                case 7: return SpeechIdentifier.ProfileType.Identify;

            }
            return SpeechIdentifier.ProfileType.Unknown;
        }

        public static int GetProfileType(this SpeechIdentifier.ProfileType profileType)
        {
            switch (profileType)
            {
                case SpeechIdentifier.ProfileType.Identify: return 6;
            }
            return -1;
        }

        #endregion
    }
}

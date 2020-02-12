
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public class SpeechVerifier : IDisposable
    {
        #region Public Enum's, Classes, Interfaces, and Delegates

        public enum Result
        {
            [Description("Pass")]
            Pass,

            [Description("Pass - Is Alive")]
            PassIsAlive,

            [Description("Pass - Not Alive")]
            PassNotAlive,

            [Description("Ambiguous")]
            Ambiguous,

            [Description("Ambiguous - Is Alive")]
            AmbiguousIsAlive,

            [Description("Ambiguous - Not Alive")]
            AmbiguousNotAlive,

            [Description("Fail")]
            Fail,

            [Description("Fail - Is Alive")]
            FailIsAlive,

            [Description("Fail - Not Alive")]
            FailNotAlive,

            [Description("NeedMore")]
            NeedMore,

            [Description("NeedMore - Is Alive")]
            NeedMoreIsAlive,

            [Description("NeedMore - Not Alive")]
            NeedMoreNotAlive,

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

        public enum AliveResult
        {
            [Description("Untested")]
            Untested,

            [Description("Not Alive")]
            NotAlive,

            [Description("Alive")]
            Alive
        }

        public enum ProfileType
        {
            [Description("Unknown")]
            Unknown = -1,

            [Description("Single")]
            Single = 2,

            [Description("[Multi-Verify] Single Liveness")]
            SingleLivness = 3,
            
            [Description("[Multi-Verify] Drop One")]
            DropOne = 10,
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

        private ISpeechVerifier mSpeechVerifier;

        #endregion

        #region Life & Death

        public SpeechVerifier()
            : this(SpeechApi.DefaultConfiguration)
        {
        }

        public SpeechVerifier(SpeechApi.Configuration configuration)
        {
            if (configuration.ServerTransport != SpeechApi.SpeechTransport.Rest)
            {
                throw new NotSupportedException("ServerTransport not supported");
            }
            else if (configuration.ServerVersion == SpeechApi.SpeechVersion.Version_1)
            {
                if (configuration.ServerTransportObject is HttpClient)
                {
                    mSpeechVerifier = new Internal.SpeechVerifier_HttpClient_Version1(configuration);
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
            if (mSpeechVerifier != null)
            {
                mSpeechVerifier.Dispose();
                mSpeechVerifier = null;
            }
        }

        ~SpeechVerifier()
        {
            Dispose();
        }

        #endregion

        #region Public Properties

        public string InteractionId
        {
            get { return mSpeechVerifier.InteractionId; }
            set { mSpeechVerifier.InteractionId = value; }
        }

        public string InteractionTag
        {
            get { return mSpeechVerifier.InteractionTag; }
            set { mSpeechVerifier.InteractionTag = value; }
        }

        public string ClientId
        {
            get { return mSpeechVerifier.ClientId; }
            set { mSpeechVerifier.ClientId = value; }
        }

        public string AuthToken
        {
            get { return mSpeechVerifier.AuthToken; }
            set { mSpeechVerifier.AuthToken = value; }
        }

        #endregion

        #region Public Getters

        public string Server
        {
            get { return mSpeechVerifier.Server; }
        }

        public string SessionId
        {
            get { return mSpeechVerifier.SessionId; }
        }

        public string InteractionSource
        {
            get { return mSpeechVerifier.InteractionSource; }
        }

        public string InteractionAgent
        {
            get { return mSpeechVerifier.InteractionAgent; }
        }

        public bool IsSessionOpen
        {
            get { return mSpeechVerifier.IsSessionOpen; }
        }

        public bool IsSessionClosing
        {
            get { return mSpeechVerifier.IsSessionClosing; }
        }

        public Dictionary<uint, Profile> Profiles
        {
            get { return mSpeechVerifier.Profiles; }
        }

        public WaveFormatEncoding Codec
        {
            get { return mSpeechVerifier.Codec; }
        }

        public uint TotalProcessCalls
        {
            get { return mSpeechVerifier.TotalProcessCalls; }
        }

        public uint TotalSnippetsSent
        {
            get { return mSpeechVerifier.TotalSnippetsSent; }
        }

        public long TotalAudioBytesSent
        {
            get { return mSpeechVerifier.TotalAudioBytesSent; }
        }

        public double SpeechRequired
        {
            get { return mSpeechVerifier.SpeechRequired; }
        }

        public double SpeechExtracted
        {
            get { return mSpeechVerifier.SpeechExtracted; }
        }

        public int SpeechProgress
        {
            get { return mSpeechVerifier.SpeechProgress; }
        }

        public bool HasEnoughSpeech
        {
            get { return mSpeechVerifier.HasEnoughSpeech; }
        }

        public bool IsTooSoft
        {
            get { return mSpeechVerifier.IsTooSoft; }
        }

        public bool IsTooLoud
        {
            get { return mSpeechVerifier.IsTooLoud; }
        }

        public Result VerifyRawResult
        {
            get { return mSpeechVerifier.VerifyRawResult; }
        }

        public Result VerifyResult
        {
            get { return mSpeechVerifier.VerifyResult; }
        }

        public Dictionary<string, InstanceResult> VerifyResults
        {
            get { return mSpeechVerifier.VerifyResults; }
        }

        public bool IsOverridable
        {
            get { return mSpeechVerifier.IsOverridable; }
        }

        public bool IsAuthorized
        {
            get { return mSpeechVerifier.IsAuthorized; }
        }

        public double VerifyScore
        {
            get { return mSpeechVerifier.VerifyScore; }
        }

        public bool IsLivenessRequired
        {
            get { return mSpeechVerifier.IsLivenessRequired; }
        }

        public bool IsVerified
        {
            get { return mSpeechVerifier.IsVerified; }
        }

        public AliveResult LivenessResult
        {
            get { return mSpeechVerifier.LivenessResult; }
        }

        public bool IsAlive
        {
            get { return mSpeechVerifier.IsAlive; }
        }

        public bool HasResult
        {
            get { return mSpeechVerifier.HasResult; }
        }

        public bool HasRawResult
        {
            get { return mSpeechVerifier.HasRawResult; }
        }

        public IReadOnlyDictionary<string, object> ExtraData
        {
            get { return mSpeechVerifier.ExtraData; }
        }

        public ISpeechLogger Logger
        {
            get { return mSpeechVerifier.Logger; }
        }

        #endregion

        #region Public Methods

        public void SetFeedback(bool isBreakAttempt, bool isRecording, bool isBackgroundNoise, string comments)
        {
            mSpeechVerifier.SetFeedback(isBreakAttempt, isRecording, isBackgroundNoise, comments);
        }

        public void SetMetaData(string name, bool value)
        {
            mSpeechVerifier.SetMetaData(name, value);
        }

        public void SetMetaData(string name, int value)
        {
            mSpeechVerifier.SetMetaData(name, value);
        }

        public void SetMetaData(string name, double value)
        {
            mSpeechVerifier.SetMetaData(name, value);
        }

        public void SetMetaData(string name, string value)
        {
            mSpeechVerifier.SetMetaData(name, value);
        }

        public bool Append(string filename)
        {
            return mSpeechVerifier.Append(filename);
        }

        public bool Append(WaveStream stream)
        {
            return mSpeechVerifier.Append(stream);
        }

        public bool Append(string filename, SpeechContexts contexts)
        {
            return mSpeechVerifier.Append(filename, contexts);
        }

        public bool Append(WaveStream stream, SpeechContexts contexts)
        {
            return mSpeechVerifier.Append(stream, contexts);
        }

        public bool AppendStereo(string filename, SpeechAudioChannel channel)
        {
            return mSpeechVerifier.AppendStereo(filename, channel);
        }

        public bool AppendStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            return mSpeechVerifier.AppendStereo(stream, channel);
        }

        public bool AppendStereo(string filename, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            return mSpeechVerifier.AppendStereo(filename, contexts, channel);
        }

        public bool AppendStereo(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel)
        {
            return mSpeechVerifier.AppendStereo(stream, contexts, channel);
        }

        #endregion

        #region Public Synchronous Methods

        public Profile PrefetchProfile()
        {
            return mSpeechVerifier.PrefetchProfile();
        }

        public bool Start()
        {
            return mSpeechVerifier.Start();
        }

        public Result Post()
        {
            return mSpeechVerifier.Post();
        }

        public Result Post(string languageCode, string livenessText)
        {
            return mSpeechVerifier.Post(languageCode, livenessText);
        }

        public Result Post(string filename)
        {
            return mSpeechVerifier.Post(filename);
        }

        public Result Post(string filename, SpeechContexts contexts)
        {
            return mSpeechVerifier.Post(filename, contexts);
        }

        public Result Post(string filename, string languageCode, string livenessText)
        {
            return mSpeechVerifier.Post(filename, languageCode, livenessText);
        }

        public Result Post(WaveStream stream)
        {
            return mSpeechVerifier.Post(stream);
        }

        public Result Post(WaveStream stream, SpeechContexts contexts)
        {
            return mSpeechVerifier.Post(stream, contexts);
        }

        public Result Post(WaveStream stream, string languageCode, string livenessText)
        {
            return mSpeechVerifier.Post(stream, languageCode, livenessText);
        }

        public Result Summarize()
        {
            return mSpeechVerifier.Summarize();
        }

        public bool Cancel(string reason)
        {
            return mSpeechVerifier.Cancel(reason);
        }

        #endregion
        
        #region Public Callback Methods

        public void PrefetchProfile(ProfileCallback profileCallback)
        {
            mSpeechVerifier.PrefetchProfile(profileCallback);
        }

        public void Start(StartCallback callback)
        {
            mSpeechVerifier.Start(callback);
        }

        public void Post(PostCallback callback)
        {
            mSpeechVerifier.Post(callback);
        }

        public void Post(string languageCode, string livenessText, PostCallback callback)
        {
            mSpeechVerifier.Post(languageCode, livenessText, callback);
        }

        public void Post(string filename, PostCallback callback)
        {
            mSpeechVerifier.Post(filename, callback);
        }

        public void Post(string filename, SpeechContexts contexts, PostCallback callback)
        {
            mSpeechVerifier.Post(filename, contexts, callback);
        }

        public void Post(string filename, string languageCode, string livenessText, PostCallback callback)
        {
            mSpeechVerifier.Post(filename, languageCode, livenessText, callback);
        }

        public void Post(WaveStream stream, PostCallback callback)
        {
            mSpeechVerifier.Post(stream, callback);
        }

        public void Post(WaveStream stream, SpeechContexts contexts, PostCallback callback)
        {
            mSpeechVerifier.Post(stream, contexts, callback);
        }

        public void Post(WaveStream stream, string languageCode, string livenessText, PostCallback callback)
        {
            mSpeechVerifier.Post(stream, languageCode, livenessText, callback);
        }

        public void Summarize(SummarizeCallback callback)
        {
            mSpeechVerifier.Summarize(callback);
        }

        public void Cancel(string reason, CancelCallback callback)
        {
            mSpeechVerifier.Cancel(reason, callback);
        }
        
        #endregion

        #region Public Asynchronous Methods

        public Task<Profile> PrefetchProfileAsync()
        {
            return mSpeechVerifier.PrefetchProfileAsync();
        }

        public Task<bool> StartAsync()
        {
            return mSpeechVerifier.StartAsync();
        }

        public Task<Result> PostAsync()
        {
            return mSpeechVerifier.PostAsync();
        }

        public Task<Result> PostAsync(string languageCode, string livenessText)
        {
            return mSpeechVerifier.PostAsync(languageCode, livenessText);
        }

        public Task<Result> PostAsync(string filename)
        {
            return mSpeechVerifier.PostAsync(filename);
        }

        public Task<Result> PostAsync(string filename, SpeechContexts contexts)
        {
            return mSpeechVerifier.PostAsync(filename, contexts);
        }

        public Task<Result> PostAsync(string filename, string languageCode, string livenessText)
        {
            return mSpeechVerifier.PostAsync(filename, languageCode, livenessText);
        }

        public Task<Result> PostAsync(WaveStream stream)
        {
            return mSpeechVerifier.PostAsync(stream);
        }

        public Task<Result> PostAsync(WaveStream stream, SpeechContexts contexts)
        {
            return mSpeechVerifier.PostAsync(stream, contexts);
        }

        public Task<Result> PostAsync(WaveStream stream, string languageCode, string livenessText)
        {
            return mSpeechVerifier.PostAsync(stream, languageCode, livenessText);
        }

        public Task<Result> SummarizeAsync()
        {
            return mSpeechVerifier.SummarizeAsync();
        }

        public Task<bool> CancelAsync(string reason)
        {
            return mSpeechVerifier.CancelAsync(reason);
        }

        #endregion

    }

    public static class SpeechVerifierExtensions
    {
        #region Public Utility Methods

        public static string GetDescrption(this SpeechVerifier.Result result)
        {
            switch (result)
            {
                case SpeechVerifier.Result.Pass: return "Pass";
                case SpeechVerifier.Result.PassIsAlive: return "PassIsAlive";
                case SpeechVerifier.Result.PassNotAlive: return "PassNotAlive";
                case SpeechVerifier.Result.Ambiguous: return "Ambiguous";
                case SpeechVerifier.Result.AmbiguousIsAlive: return "AmbiguousIsAlive";
                case SpeechVerifier.Result.AmbiguousNotAlive: return "AmbiguousNotAlive";
                case SpeechVerifier.Result.Fail: return "Fail";
                case SpeechVerifier.Result.FailIsAlive: return "FailIsAlive";
                case SpeechVerifier.Result.FailNotAlive: return "FailNotAlive";
                case SpeechVerifier.Result.NeedMore: return "NeedMore";
                case SpeechVerifier.Result.NeedMoreIsAlive: return "NeedMoreIsAlive";
                case SpeechVerifier.Result.NeedMoreNotAlive: return "NeedMoreNotAlive";
                case SpeechVerifier.Result.NotScored: return "NotScored";
                case SpeechVerifier.Result.TooSoft: return "TooSoft";
                case SpeechVerifier.Result.TooLoud: return "TooLoud";
                case SpeechVerifier.Result.LimitReached: return "LimitReached";
                case SpeechVerifier.Result.Unauthorized: return "Unauthorized";
                case SpeechVerifier.Result.NotFound: return "NotFound";
                case SpeechVerifier.Result.BadEnrollment: return "BadEnrollment";
                case SpeechVerifier.Result.Timeout: return "Timeout";
                case SpeechVerifier.Result.Invalid: return "Invalid";
                case SpeechVerifier.Result.Error: return "Error";
                case SpeechVerifier.Result.Unknown: return "Unknown";
            }
            return "Unknown";
        }

        public static SpeechVerifier.ProfileType GetProfileType(this uint profileType)
        {
            switch (profileType)
            {
                case 2: return SpeechVerifier.ProfileType.Single;
                case 3: return SpeechVerifier.ProfileType.SingleLivness;
                case 10:
                case 11: return SpeechVerifier.ProfileType.DropOne;

            }
            return SpeechVerifier.ProfileType.Unknown;
        }

        public static int GetProfileType(this SpeechVerifier.ProfileType profileType)
        {
            switch (profileType)
            {
                case SpeechVerifier.ProfileType.Single: return 2;
                case SpeechVerifier.ProfileType.SingleLivness: return 3;
                case SpeechVerifier.ProfileType.DropOne: return 10;
            }
            return -1;
        }

        #endregion
    }
}

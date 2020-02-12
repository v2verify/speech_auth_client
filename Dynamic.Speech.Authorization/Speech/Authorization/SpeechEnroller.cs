
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public class SpeechEnroller : IDisposable
    {
        #region Public Enum's, Classes, Interfaces and Delegates

        public enum Result
        {
            [Description("Success")]
            Success,

            [Description("Need More Speech")]
            NeedMore,

            [Description("Already Enrolled")]
            AlreadyEnrolled,

            [Description("Token Exists")]
            TokenExists,

            [Description("Token Required")]
            TokenRequired,

            [Description("Too Soft")]
            TooSoft,

            [Description("Too Loud")]
            TooLoud,

            [Description("Limit Reached")]
            LimitReached,

            [Description("Unauthorized")]
            Unauthorized,

            [Description("Timeout")]
            Timeout,

            [Description("Invalid")]
            Invalid,

            [Description("Error")]
            Error,

            [Description("Unknown")]
            Unknown
        }

        public enum Gender
        {
            [Description("Unknown")]
            Unknown = -1,

            [Description("Male")]
            Male = 0,

            [Description("Female")]
            Female = 1
        }

        public class Profile
        {
            public WaveFormatEncoding Codec { get; internal set; }

            public double MinimumSecondsOfSpeech { get; internal set; }
        }

        public class InstanceResult
        {
            internal InstanceResult()
            {
                Extra = new Dictionary<string, object>();
            }

            public Result Result { get; internal set; }
            public double? SpeechExtracted { get; internal set; }
            public double? SpeechTrained { get; internal set; }
            public uint ErrorCode { get; internal set; }
            public Dictionary<string, object> Extra { get; private set; }
        }

        public delegate void ProfileCallback(Profile profile);

        public delegate void StartCallback(bool started);

        public delegate void PostCallback(Result result);

        public delegate void TrainCallback(Result result);

        public delegate void CancelCallback();

        #endregion

        #region Private

        private ISpeechEnroller mSpeechEnroller;

        #endregion

        #region Life & Death

        public SpeechEnroller()
            : this(SpeechApi.DefaultConfiguration)
        {
        }

        public SpeechEnroller(SpeechApi.Configuration configuration)
        {
            if (configuration.ServerTransport != SpeechApi.SpeechTransport.Rest)
            {
                throw new NotSupportedException("ServerTransport not supported");
            }
            else if (configuration.ServerVersion == SpeechApi.SpeechVersion.Version_1)
            {
                if (configuration.ServerTransportObject is HttpClient)
                {
                    mSpeechEnroller = new Internal.SpeechEnroller_HttpClient_Version1(configuration);
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
            if (mSpeechEnroller != null)
            {
                mSpeechEnroller.Dispose();
                mSpeechEnroller = null;
            }
        }

        ~SpeechEnroller()
        {
            Dispose();
        }

        #endregion

        #region Public Properties

        public string InteractionId
        {
            get { return mSpeechEnroller.InteractionId; }
            set { mSpeechEnroller.InteractionId = value; }
        }

        public string InteractionTag
        {
            get { return mSpeechEnroller.InteractionTag; }
            set { mSpeechEnroller.InteractionTag = value; }
        }

        public Gender SubPopulation
        {
            get { return mSpeechEnroller.SubPopulation; }
            set { mSpeechEnroller.SubPopulation = value; }
        }

        public string ClientId
        {
            get { return mSpeechEnroller.ClientId; }
            set { mSpeechEnroller.ClientId = value; }
        }

        public string AuthToken
        {
            get { return mSpeechEnroller.AuthToken; }
            set { mSpeechEnroller.AuthToken = value; }
        }

        #endregion

        #region Public Getters

        public string Server
        {
            get { return mSpeechEnroller.Server; }
        }

        public string SessionId
        {
            get { return mSpeechEnroller.SessionId; }
        }

        public string InteractionSource
        {
            get { return mSpeechEnroller.InteractionSource; }
        }

        public string InteractionAgent
        {
            get { return mSpeechEnroller.InteractionAgent; }
        }

        public bool IsSessionOpen
        {
            get { return mSpeechEnroller.IsSessionOpen; }
        }

        public bool IsSessionClosing
        {
            get { return mSpeechEnroller.IsSessionClosing; }
        }

        public Dictionary<uint, Profile> Profiles
        {
            get { return mSpeechEnroller.Profiles; }
        }

        public WaveFormatEncoding Codec
        {
            get { return mSpeechEnroller.Codec; }
        }

        public double SpeechRequired
        {
            get { return mSpeechEnroller.SpeechRequired; }
        }

        public double SpeechExtracted
        {
            get { return mSpeechEnroller.SpeechExtracted; }
        }

        public int SpeechProgress
        {
            get { return mSpeechEnroller.SpeechProgress; }
        }

        public double SpeechTrained
        {
            get { return mSpeechEnroller.SpeechTrained; }
        }

        public Result EnrollResult
        {
            get { return mSpeechEnroller.EnrollResult; }
        }

        public Dictionary<string, InstanceResult> EnrollResults
        {
            get { return mSpeechEnroller.EnrollResults; }
        }

        public uint TotalProcessCalls
        {
            get { return mSpeechEnroller.TotalProcessCalls; }
        }
        
        public uint TotalSnippetsSent
        {
            get { return mSpeechEnroller.TotalSnippetsSent; }
        }

        public long TotalAudioBytesSent
        {
            get { return mSpeechEnroller.TotalAudioBytesSent; }
        }

        public bool HasEnoughSpeech
        {
            get { return mSpeechEnroller.HasEnoughSpeech; }
        }

        public bool IsTooSoft
        {
            get { return mSpeechEnroller.IsTooSoft; }
        }

        public bool IsTooLoud
        {
            get { return mSpeechEnroller.IsTooLoud; }
        }

        public bool IsTrained
        {
            get { return mSpeechEnroller.IsTrained; }
        }

        public IReadOnlyDictionary<string, object> ExtraData
        {
            get { return mSpeechEnroller.ExtraData; }
        }

        public ISpeechLogger Logger
        {
            get { return mSpeechEnroller.Logger; }
        }

        #endregion

        #region Public Methods

        public void SetMetaData(string name, bool value)
        {
            mSpeechEnroller.SetMetaData(name, value);
        }

        public void SetMetaData(string name, int value)
        {
            mSpeechEnroller.SetMetaData(name, value);
        }

        public void SetMetaData(string name, double value)
        {
            mSpeechEnroller.SetMetaData(name, value);
        }

        public void SetMetaData(string name, string value)
        {
            mSpeechEnroller.SetMetaData(name, value);
        }

        public bool Append(string filename)
        {
            return mSpeechEnroller.Append(filename);
        }

        public bool Append(WaveStream stream)
        {
            return mSpeechEnroller.Append(stream);
        }

        public bool AppendStereo(string filename, SpeechAudioChannel channel)
        {
            return mSpeechEnroller.AppendStereo(filename, channel);
        }

        public bool AppendStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            return mSpeechEnroller.AppendStereo(stream, channel);
        }

        #endregion

        #region Public Synchronous Methods

        public Profile PrefetchProfile()
        {
            return mSpeechEnroller.PrefetchProfile();
        }

        public bool Start()
        {
            return mSpeechEnroller.Start();
        }

        public Result Post()
        {
            return mSpeechEnroller.Post();
        }

        public Result Post(string filename)
        {
            return mSpeechEnroller.Post(filename);
        }

        public Result Post(WaveStream stream)
        {
            return mSpeechEnroller.Post(stream);
        }

        public Result PostStereo(string filename, SpeechAudioChannel channel)
        {
            return mSpeechEnroller.PostStereo(filename, channel);
        }

        public Result PostStereo(WaveStream stream, SpeechAudioChannel channel)
        {
            return mSpeechEnroller.PostStereo(stream, channel);
        }

        public Result Train()
        {
            return mSpeechEnroller.Train();
        }

        public bool Cancel(string reason)
        {
            return mSpeechEnroller.Cancel(reason);
        }

        #endregion

        #region Public Callback Methods

        public void PrefetchProfile(ProfileCallback callback)
        {
            mSpeechEnroller.PrefetchProfile(callback);
        }

        public void Start(StartCallback callback)
        {
            mSpeechEnroller.Start(callback);
        }

        public void Post(PostCallback callback)
        {
            mSpeechEnroller.Post(callback);
        }

        public void Post(string filename, PostCallback callback)
        {
            mSpeechEnroller.Post(filename, callback);
        }

        public void Post(WaveStream stream, PostCallback callback)
        {
            mSpeechEnroller.Post(stream, callback);
        }

        public void PostStereo(string filename, SpeechAudioChannel channel, PostCallback callback)
        {
            mSpeechEnroller.PostStereo(filename, channel, callback);
        }

        public void PostStereo(WaveStream stream, SpeechAudioChannel channel, PostCallback callback)
        {
            mSpeechEnroller.PostStereo(stream, channel, callback);
        }

        public void Train(TrainCallback callback)
        {
            mSpeechEnroller.Train(callback);
        }

        public void Cancel(string reason, CancelCallback callback)
        {
            mSpeechEnroller.Cancel(reason, callback);
        }

        #endregion

        #region Public Asynchronous Methods

        public Task<Profile> PrefetchProfileAsync()
        {
            return mSpeechEnroller.PrefetchProfileAsync();
        }

        public Task<bool> StartAsync()
        {
            return mSpeechEnroller.StartAsync();
        }

        public Task<Result> PostAsync()
        {
            return mSpeechEnroller.PostAsync();
        }

        public Task<Result> PostAsync(string filename)
        {
            return mSpeechEnroller.PostAsync(filename);
        }

        public Task<Result> PostAsync(WaveStream stream)
        {
            return mSpeechEnroller.PostAsync(stream);
        }

        public Task<Result> PostStereoAsync(string filename, SpeechAudioChannel channel)
        {
            return mSpeechEnroller.PostStereoAsync(filename, channel);
        }

        public Task<Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel)
        {
            return mSpeechEnroller.PostStereoAsync(stream, channel);
        }

        public Task<Result> TrainAsync()
        {
            return mSpeechEnroller.TrainAsync();
        }

        public Task<bool> CancelAsync(string reason)
        {
            return mSpeechEnroller.CancelAsync(reason);
        }

        #endregion
        
    }

    public static class SpeechEnrollerExtensions
    {
        #region Public Utility Methods

        public static string GetDescrption(this SpeechEnroller.Result result)
        {
            switch (result)
            {
                case SpeechEnroller.Result.Unknown: return "Unknown";
                case SpeechEnroller.Result.Invalid: return "Invalid";
                case SpeechEnroller.Result.Error: return "Error";
                case SpeechEnroller.Result.Timeout: return "Timeout";
                case SpeechEnroller.Result.AlreadyEnrolled: return "AlreadyEnrolled";
                case SpeechEnroller.Result.NeedMore: return "NeedMore";
                case SpeechEnroller.Result.Success: return "Success";
                case SpeechEnroller.Result.TooSoft: return "TooSoft";
                case SpeechEnroller.Result.TooLoud: return "TooLoud";
                case SpeechEnroller.Result.LimitReached: return "LimitReached";
                case SpeechEnroller.Result.Unauthorized: return "Unauthorized";
            }
            return "Unknown";
        }

        public static SpeechEnroller.Gender GetGender(this string gender)
        {
            switch (gender.ToLower())
            {
                case "u":
                case "unknown":
                case "m":
                case "male": return SpeechEnroller.Gender.Male;
                case "f":
                case "female": return SpeechEnroller.Gender.Female;
            }
            return SpeechEnroller.Gender.Unknown;
        }

        public static string GetGender(this SpeechEnroller.Gender gender)
        {
            switch (gender)
            {
                case SpeechEnroller.Gender.Male: return "m";
                case SpeechEnroller.Gender.Female: return "f";
            }
            return "u";
        }

        #endregion
    }
}

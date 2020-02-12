
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public interface ISpeechEnroller : IDisposable
    {
        #region Public Properties

        string InteractionId { get; set; }

        string InteractionTag { get; set; }

        SpeechEnroller.Gender SubPopulation { get; set; }

        string ClientId { get; set; }

        string AuthToken { get; set; }

        #endregion

        #region Public Getters

        SpeechApi.Configuration Configuration { get; }

        CookieContainer CookieContainer { get; }

        string Server { get; }

        string SessionId { get; }

        string InteractionSource { get; }

        string InteractionAgent { get; }

        Dictionary<string, string> MetaData { get; }

        Dictionary<string, object> ExtraData { get; }

        bool IsSessionOpen { get; }

        bool IsSessionClosing { get; }

        Dictionary<uint, SpeechEnroller.Profile> Profiles { get; }

        WaveFormatEncoding Codec { get; }

        double SpeechRequired { get; }

        double SpeechExtracted { get; }

        int SpeechProgress { get; }

        double SpeechTrained { get; }

        SpeechEnroller.Result EnrollResult { get; }

        Dictionary<string, SpeechEnroller.InstanceResult> EnrollResults { get; }

        uint TotalProcessCalls { get; }

        uint TotalSnippetsSent { get; }

        long TotalAudioBytesSent { get; }

        bool HasEnoughSpeech { get; }

        bool IsTooSoft { get; }

        bool IsTooLoud { get; }

        bool IsTrained { get; }

        ISpeechLogger Logger { get; }

        #endregion

        #region Public Methods

        void SetMetaData(string name, bool value);

        void SetMetaData(string name, int value);

        void SetMetaData(string name, double value);

        void SetMetaData(string name, string value);

        bool Append(string filename);

        bool Append(WaveStream stream);

        bool AppendStereo(string filename, SpeechAudioChannel channel);

        bool AppendStereo(WaveStream stream, SpeechAudioChannel channel);

        #endregion

        #region Public Synchronous Methods

        SpeechEnroller.Profile PrefetchProfile();

        bool Start();

        SpeechEnroller.Result Post();

        SpeechEnroller.Result Post(string filename);

        SpeechEnroller.Result Post(WaveStream stream);

        SpeechEnroller.Result PostStereo(string filename, SpeechAudioChannel channel);

        SpeechEnroller.Result PostStereo(WaveStream stream, SpeechAudioChannel channel);

        SpeechEnroller.Result Train();

        bool Cancel(string reason);

        #endregion

        #region Public Callback Methods

        void PrefetchProfile(SpeechEnroller.ProfileCallback profileCallback);

        void Start(SpeechEnroller.StartCallback callback);

        void Post(SpeechEnroller.PostCallback callback);

        void Post(string filename, SpeechEnroller.PostCallback callback);

        void Post(WaveStream stream, SpeechEnroller.PostCallback callback);

        void PostStereo(string filename, SpeechAudioChannel channel, SpeechEnroller.PostCallback callback);

        void PostStereo(WaveStream stream, SpeechAudioChannel channel, SpeechEnroller.PostCallback callback);

        void Train(SpeechEnroller.TrainCallback callback);

        void Cancel(string reason, SpeechEnroller.CancelCallback callback);

        #endregion

        #region Public Asynchronous Methods

        Task<SpeechEnroller.Profile> PrefetchProfileAsync();

        Task<bool> StartAsync();

        Task<SpeechEnroller.Result> PostAsync();

        Task<SpeechEnroller.Result> PostAsync(string filename);

        Task<SpeechEnroller.Result> PostAsync(WaveStream stream);

        Task<SpeechEnroller.Result> PostStereoAsync(string filename, SpeechAudioChannel channel);

        Task<SpeechEnroller.Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel);

        Task<SpeechEnroller.Result> TrainAsync();

        Task<bool> CancelAsync(string reason);

        #endregion
    }
}

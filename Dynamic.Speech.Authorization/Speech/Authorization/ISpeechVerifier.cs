
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public interface ISpeechVerifier : IDisposable
    {
        #region Public Properties

        string InteractionId { get; set; }

        string InteractionTag { get; set; }

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

        bool IsOverridable { get; }

        bool IsAuthorized { get; }

        Dictionary<uint, SpeechVerifier.Profile> Profiles { get; }

        WaveFormatEncoding Codec { get; }

        double VerifyScore { get; }

        double SpeechRequired { get; }

        double SpeechExtracted { get; }

        int SpeechProgress { get; }

        bool HasEnoughSpeech { get; }

        bool IsTooSoft { get; }

        bool IsTooLoud { get; }

        SpeechVerifier.Result VerifyRawResult { get; }

        SpeechVerifier.Result VerifyResult { get; }

        Dictionary<string, SpeechVerifier.InstanceResult> VerifyResults { get; }

        uint TotalProcessCalls { get; }

        uint TotalSnippetsSent { get; }

        long TotalAudioBytesSent { get; }

        bool IsLivenessRequired { get; }

        bool IsVerified { get; }

        SpeechVerifier.AliveResult LivenessResult { get; }

        bool IsAlive { get; }

        bool HasResult { get; }

        bool HasRawResult { get; }

        ISpeechLogger Logger { get; }

        #endregion

        #region Public Methods

        void SetFeedback(bool isBreakAttempt, bool isRecording, bool isBackgroundNoise, string comments);

        void SetMetaData(string name, bool value);

        void SetMetaData(string name, int value);

        void SetMetaData(string name, double value);

        void SetMetaData(string name, string value);

        bool Append(string filename);

        bool Append(string filename, SpeechContexts contexts);

        bool Append(WaveStream stream);

        bool Append(WaveStream stream, SpeechContexts contexts);

        bool AppendStereo(string filename, SpeechAudioChannel channel);

        bool AppendStereo(WaveStream stream, SpeechAudioChannel channel);

        bool AppendStereo(string filename, SpeechContexts contexts, SpeechAudioChannel channel);

        bool AppendStereo(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel);

        #endregion

        #region Public Synchronous Methods

        SpeechVerifier.Profile PrefetchProfile();

        bool Start();

        SpeechVerifier.Result Post();

        SpeechVerifier.Result Post(string languageCode, string livenessText);

        SpeechVerifier.Result Post(string filename);

        SpeechVerifier.Result Post(string filename, SpeechContexts contexts);

        SpeechVerifier.Result Post(string filename, string languageCode, string livenessText);

        SpeechVerifier.Result Post(WaveStream stream);

        SpeechVerifier.Result Post(WaveStream stream, SpeechContexts contexts);

        SpeechVerifier.Result Post(WaveStream stream, string languageCode, string livenessText);

        SpeechVerifier.Result PostStereo(string filename, SpeechAudioChannel channel);

        SpeechVerifier.Result PostStereo(string filename, SpeechContexts contexts, SpeechAudioChannel channel);

        SpeechVerifier.Result PostStereo(string filename, string languageCode, string livenessText, SpeechAudioChannel channel);

        SpeechVerifier.Result PostStereo(WaveStream stream, SpeechAudioChannel channel);

        SpeechVerifier.Result PostStereo(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel);

        SpeechVerifier.Result PostStereo(WaveStream stream, string languageCode, string livenessText, SpeechAudioChannel channel);

        SpeechVerifier.Result Summarize();

        bool Cancel(string reason);

        #endregion

        #region Public Callback Methods

        void PrefetchProfile(SpeechVerifier.ProfileCallback callback);

        void Start(SpeechVerifier.StartCallback callback);

        void Post(SpeechVerifier.PostCallback callback);

        void Post(string languageCode, string livenessText, SpeechVerifier.PostCallback callback);

        void Post(string filename, SpeechVerifier.PostCallback callback);

        void Post(string filename, SpeechContexts contexts, SpeechVerifier.PostCallback callback);

        void Post(string filename, string languageCode, string livenessText, SpeechVerifier.PostCallback callback);

        void Post(WaveStream stream, SpeechVerifier.PostCallback callback);

        void Post(WaveStream stream, SpeechContexts contexts, SpeechVerifier.PostCallback callback);

        void Post(WaveStream stream, string languageCode, string livenessText, SpeechVerifier.PostCallback callback);

        void PostStereo(string filename, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback);

        void PostStereo(string filename, SpeechContexts contexts, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback);

        void PostStereo(string filename, string languageCode, string livenessText, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback);

        void PostStereo(WaveStream stream, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback);

        void PostStereo(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback);

        void PostStereo(WaveStream stream, string languageCode, string livenessText, SpeechAudioChannel channel, SpeechVerifier.PostCallback callback);

        void Summarize(SpeechVerifier.SummarizeCallback callback);

        void Cancel(string reason, SpeechVerifier.CancelCallback callback);
        
        #endregion

        #region Public Asynchronous Methods

        Task<SpeechVerifier.Profile> PrefetchProfileAsync();

        Task<bool> StartAsync();

        Task<SpeechVerifier.Result> PostAsync();

        Task<SpeechVerifier.Result> PostAsync(string languageCode, string livenessText);

        Task<SpeechVerifier.Result> PostAsync(string filename);

        Task<SpeechVerifier.Result> PostAsync(string filename, SpeechContexts contexts);

        Task<SpeechVerifier.Result> PostAsync(string filename, string languageCode, string livenessText);

        Task<SpeechVerifier.Result> PostAsync(WaveStream stream);

        Task<SpeechVerifier.Result> PostAsync(WaveStream stream, SpeechContexts contexts);

        Task<SpeechVerifier.Result> PostAsync(WaveStream stream, string languageCode, string livenessText);

        Task<SpeechVerifier.Result> PostStereoAsync(string filename, SpeechAudioChannel channel);

        Task<SpeechVerifier.Result> PostStereoAsync(string filename, SpeechContexts contexts, SpeechAudioChannel channel);

        Task<SpeechVerifier.Result> PostStereoAsync(string filename, string languageCode, string livenessText, SpeechAudioChannel channel);

        Task<SpeechVerifier.Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel);

        Task<SpeechVerifier.Result> PostStereoAsync(WaveStream stream, SpeechContexts contexts, SpeechAudioChannel channel);

        Task<SpeechVerifier.Result> PostStereoAsync(WaveStream stream, string languageCode, string livenessText, SpeechAudioChannel channel);

        Task<SpeechVerifier.Result> SummarizeAsync();

        Task<bool> CancelAsync(string reason);

        #endregion
    }
}

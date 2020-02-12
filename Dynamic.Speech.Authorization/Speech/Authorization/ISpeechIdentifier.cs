
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public interface ISpeechIdentifier : IDisposable
    {
        #region Public Properties

        string InteractionId { get; set; }

        string InteractionTag { get; set; }
        
        string ProbableClientId { get; set; }

        string PossibleClientIds { get; set; }

        List<string> PossibleClientIdList { get; }

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

        bool IsAuthorized { get; }

        Dictionary<uint, SpeechIdentifier.Profile> Profiles { get; }

        WaveFormatEncoding Codec { get; }

        double SpeechRequired { get; }

        double SpeechExtracted { get; }

        int SpeechProgress { get; }

        bool HasEnoughSpeech { get; }

        bool IsTooSoft { get; }

        bool IsTooLoud { get; }

        uint TotalProcessCalls { get; }

        uint TotalSnippetsSent { get; }

        long TotalAudioBytesSent { get; }

        SpeechIdentifier.Result IdentifyResult { get; }

        Dictionary<string, SpeechIdentifier.InstanceResult> IdentifyResults { get; }

        bool HasResult { get; }

        bool IsIdentified { get; }

        string IdentifiedClientId { get; }

        double IdentifiedScore { get; }
        
        ISpeechLogger Logger { get; }

        #endregion

        #region Public Methods

        void SetFeedback(bool isRecording, bool isBackgroundNoise, string comments);

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

        SpeechIdentifier.Profile PrefetchProfile();

        bool Start();

        SpeechIdentifier.Result Post();
        
        SpeechIdentifier.Result Post(string filename);
        
        SpeechIdentifier.Result Post(WaveStream stream);

        SpeechIdentifier.Result PostStereo(string filename, SpeechAudioChannel channel);

        SpeechIdentifier.Result PostStereo(WaveStream stream, SpeechAudioChannel channel);

        SpeechIdentifier.Result Summarize();

        bool Cancel(string reason);

        #endregion

        #region Public Callback Methods

        void PrefetchProfile(SpeechIdentifier.ProfileCallback callback);

        void Start(SpeechIdentifier.StartCallback callback);

        void Post(SpeechIdentifier.PostCallback callback);
        
        void Post(string filename, SpeechIdentifier.PostCallback callback);
        
        void Post(WaveStream stream, SpeechIdentifier.PostCallback callback);

        void PostStereo(string filename, SpeechAudioChannel channel, SpeechIdentifier.PostCallback callback);
        
        void PostStereo(WaveStream stream, SpeechAudioChannel channel, SpeechIdentifier.PostCallback callback);

        void Summarize(SpeechIdentifier.SummarizeCallback callback);

        void Cancel(string reason, SpeechIdentifier.CancelCallback callback);

        #endregion

        #region Public Asynchronous Methods

        Task<SpeechIdentifier.Profile> PrefetchProfileAsync();

        Task<bool> StartAsync();

        Task<SpeechIdentifier.Result> PostAsync();
        
        Task<SpeechIdentifier.Result> PostAsync(string filename);
        
        Task<SpeechIdentifier.Result> PostAsync(WaveStream stream);

        Task<SpeechIdentifier.Result> PostStereoAsync(string filename, SpeechAudioChannel channel);

        Task<SpeechIdentifier.Result> PostStereoAsync(WaveStream stream, SpeechAudioChannel channel);
        
        Task<SpeechIdentifier.Result> SummarizeAsync();

        Task<bool> CancelAsync(string reason);

        #endregion
    }
}

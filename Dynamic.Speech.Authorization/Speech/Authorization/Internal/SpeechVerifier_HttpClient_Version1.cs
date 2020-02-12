
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Net.Http.Headers;

using Newtonsoft.Json;

namespace Dynamic.Speech.Authorization.Internal
{
    internal class SpeechVerifier_HttpClient_Version1 : SpeechVerifierBase
    {
        #region Private

        private const string URI_PATH_PROFILE = "/1/sve/Verification/Profile";
        private const string URI_PATH_START = "/1/sve/Verification/{0}";
        private const string URI_PATH_PROCESS = "/1/sve/Verification";
        private const string URI_PATH_LIVENESS = "/1/sve/Verification/Liveness/{0}/{1}";
        private const string URI_PATH_SUMMARIZE = "/1/sve/Verification";
        private const string URI_PATH_CANCEL = "/1/sve/Cancel/{0}";

        private enum CallMode
        {
            Profile,
            Start,
            Process,
            Summarize,
            Cancel
        };

        private HttpClient mHttpClient;
        private uint mLastProfileIndex;

        #endregion

        #region Life & Death

        internal SpeechVerifier_HttpClient_Version1(SpeechApi.Configuration configuration)
            : base(configuration)
        {
            mHttpClient = (HttpClient)configuration.ServerTransportObject;
        }

        public override void Dispose()
        {
            mHttpClient = null;
        }

        #endregion

        #region Public Synchronous Methods

        public override SpeechVerifier.Profile PrefetchProfile()
        {
            ResetVerifier();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.PreFetchProfile(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Profile, uri))
            {
                try
                {
                    var responseTask = mHttpClient.SendAsync(request);

                    responseTask.RunSynchronously();

                    var resultTask = HandleResponse(CallMode.Profile, request, responseTask.Result);

                    resultTask.RunSynchronously();

                    return resultTask.Result ? Profiles[mLastProfileIndex] : null;
                }
                catch (HttpRequestException ex)
                {
                    VerifyRawResult = SpeechVerifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return null;
                }
            }
        }

        public override bool Start()
        {
            ResetVerifier();
            
            if (string.IsNullOrEmpty(ClientId))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.Start(): Missing ClientId.");
                VerifyRawResult = SpeechVerifier.Result.Invalid;
                return false;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, ClientId);

            Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Start(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Start, uri))
            {
                try
                {
                    var responseTask = mHttpClient.SendAsync(request);

                    responseTask.RunSynchronously();

                    var resultTask = HandleResponse(CallMode.Start, request, responseTask.Result);

                    resultTask.RunSynchronously();

                    return resultTask.Result;
                }
                catch (HttpRequestException ex)
                {
                    VerifyRawResult = SpeechVerifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return false;
                }
            }
        }

        public override SpeechVerifier.Result Post()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                VerifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Post(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    try
                    {
                        var responseTask = mHttpClient.SendAsync(request);

                        responseTask.RunSynchronously();

                        var resultTask = HandleResponse(CallMode.Process, request, responseTask.Result);

                        resultTask.RunSynchronously();
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return VerifyRawResult;
                }
            }
            return (VerifyRawResult = SpeechVerifier.Result.Invalid);
        }

        public override SpeechVerifier.Result Post(string languageCode, string livenessText)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.Post(liveness): Missing Language Code");
                return (VerifyRawResult = SpeechVerifier.Result.Invalid);
            }

            if (string.IsNullOrEmpty(livenessText))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.Post(liveness): Missing Liveness Text");
                return (VerifyRawResult = SpeechVerifier.Result.Invalid);
            }

            if (IsSessionOpen && !IsSessionClosing)
            {
                VerifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_LIVENESS, languageCode, livenessText);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Post(liveness): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    try
                    {
                        var responseTask = mHttpClient.SendAsync(request);

                        responseTask.RunSynchronously();

                        var resultTask = HandleResponse(CallMode.Process, request, responseTask.Result);

                        resultTask.RunSynchronously();
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }

                    return VerifyRawResult;
                }
            }
            return (VerifyRawResult = SpeechVerifier.Result.Invalid);
        }

        public override SpeechVerifier.Result Summarize()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                VerifyResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_SUMMARIZE);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Summarize(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Summarize, uri))
                {
                    try
                    {
                        var responseTask = mHttpClient.SendAsync(request);

                        responseTask.RunSynchronously();

                        var resultTask = HandleResponse(CallMode.Summarize, request, responseTask.Result);

                        resultTask.RunSynchronously();
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }

                    return VerifyRawResult;
                }
            }
            return (VerifyRawResult = SpeechVerifier.Result.Invalid);
        }

        public override bool Cancel(string reason)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                VerifyResults.Clear();
                Content.Clear();

                if (string.IsNullOrEmpty(reason))
                {
                    reason = "Unknown";
                }
                else
                {
                    reason = reason.Replace(' ', '-').Substring(0, System.Math.Min(reason.Length, 64));
                }

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_CANCEL, reason);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Cancel(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Cancel, uri))
                {
                    try
                    {
                        var responseTask = mHttpClient.SendAsync(request);

                        responseTask.RunSynchronously();

                        var resultTask = HandleResponse(CallMode.Cancel, request, responseTask.Result);

                        resultTask.RunSynchronously();

                        return resultTask.Result;
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                }
            }
            return false;
        }

        #endregion

        #region Public Callback Methods

        public override void PrefetchProfile(SpeechVerifier.ProfileCallback callback)
        {
            ResetVerifier();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.PreFetchProfile(callback): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Profile, uri))
            {
                var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                    if (requestTask.IsFaulted)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(requestTask.Exception);
                        callback?.Invoke(null);
                        return;
                    }
                    HandleResponse(CallMode.Profile, request, requestTask.Result).ContinueWith((responseTask) => {
                        if (responseTask.IsFaulted)
                        {
                            VerifyRawResult = SpeechVerifier.Result.Timeout;
                            Logger?.LogError(responseTask.Exception);
                            callback?.Invoke(null);
                            return;
                        }
                        callback?.Invoke(responseTask.Result ? Profiles[mLastProfileIndex] : null);
                    });
                });
                
                if (task.IsFaulted)
                {
                    VerifyRawResult = SpeechVerifier.Result.Timeout;
                    Logger?.LogError(task.Exception);
                    callback?.Invoke(null);
                }
            }
        }

        public override void Start(SpeechVerifier.StartCallback callback)
        {
            ResetVerifier();

            if (string.IsNullOrEmpty(ClientId))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.Start(callback): Missing ClientId.");
                VerifyRawResult = SpeechVerifier.Result.Invalid;
                return;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, ClientId);

            Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Start(callback): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Start, uri))
            {
                var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                    if (requestTask.IsFaulted)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(requestTask.Exception);
                        callback?.Invoke(false);
                        return;
                    }
                    HandleResponse(CallMode.Start, request, requestTask.Result).ContinueWith((responseTask) => {
                        if (responseTask.IsFaulted)
                        {
                            VerifyRawResult = SpeechVerifier.Result.Timeout;
                            Logger?.LogError(responseTask.Exception);
                            callback?.Invoke(false);
                            return;
                        }
                        callback?.Invoke(responseTask.Result);
                    });
                });
                
                if (task.IsFaulted)
                {
                    VerifyRawResult = SpeechVerifier.Result.Timeout;
                    Logger?.LogError(task.Exception);
                    callback?.Invoke(false);
                }
            }
        }

        public override void Post(SpeechVerifier.PostCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                VerifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Post(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            VerifyRawResult = SpeechVerifier.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke(VerifyResult);
                            return;
                        }
                        HandleResponse(CallMode.Process, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                VerifyRawResult = SpeechVerifier.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke(VerifyResult);
                                return;
                            }
                            callback?.Invoke(VerifyRawResult);
                        });
                    });

                    if (task.IsFaulted)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke(VerifyResult);
                    }

                    return;
                }
            }
            callback?.Invoke(VerifyRawResult = SpeechVerifier.Result.Invalid);
        }
        
        public override void Post(string languageCode, string livenessText, SpeechVerifier.PostCallback callback)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.Post(liveness): Missing Language Code");
                callback?.Invoke(VerifyRawResult = SpeechVerifier.Result.Invalid);
                return;
            }

            if (string.IsNullOrEmpty(livenessText))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.Post(liveness): Missing Liveness Text");
                callback?.Invoke(VerifyRawResult = SpeechVerifier.Result.Invalid);
                return;
            }

            if (IsSessionOpen && !IsSessionClosing)
            {
                VerifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_LIVENESS, languageCode, livenessText);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Post(liveness, callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            VerifyRawResult = SpeechVerifier.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke(VerifyResult);
                            return;
                        }
                        HandleResponse(CallMode.Process, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                VerifyRawResult = SpeechVerifier.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke(VerifyResult);
                                return;
                            }
                            callback?.Invoke(VerifyRawResult);
                        });
                    });

                    if (task.IsFaulted)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke(VerifyResult);
                    }

                    return;
                }
            }
            callback?.Invoke(VerifyRawResult = SpeechVerifier.Result.Invalid);
        }

        public override void Summarize(SpeechVerifier.SummarizeCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                VerifyResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_SUMMARIZE);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Summarize(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Summarize, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            VerifyRawResult = SpeechVerifier.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke(VerifyResult);
                            return;
                        }
                        HandleResponse(CallMode.Summarize, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                VerifyRawResult = SpeechVerifier.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke(VerifyResult);
                                return;
                            }
                            callback?.Invoke(VerifyRawResult);
                        });
                    });

                    if (task.IsFaulted)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke(VerifyResult);
                    }

                    return;
                }
            }
            callback?.Invoke(VerifyRawResult = SpeechVerifier.Result.Invalid);
        }

        public override void Cancel(string reason, SpeechVerifier.CancelCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                VerifyResults.Clear();
                Content.Clear();

                if (string.IsNullOrEmpty(reason))
                {
                    reason = "Unknown";
                }
                else
                {
                    reason = reason.Replace(' ', '-').Substring(0, System.Math.Min(reason.Length, 64));
                }

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_CANCEL, reason);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Cancel(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Cancel, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            VerifyRawResult = SpeechVerifier.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke();
                            return;
                        }
                        HandleResponse(CallMode.Cancel, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                VerifyRawResult = SpeechVerifier.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke();
                                return;
                            }
                            callback?.Invoke();
                        });
                    });

                    if (task.IsFaulted)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke();
                    }

                    return;
                }
            }
            VerifyRawResult = SpeechVerifier.Result.Invalid;
            callback?.Invoke();
        }

        #endregion

        #region Public Asynchronous Methods

        public override async Task<SpeechVerifier.Profile> PrefetchProfileAsync()
        {
            ResetVerifier();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.PrefetchProfileAsync(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Profile, uri))
            {
                try
                {
                    return await HandleResponse(CallMode.Profile, request, await mHttpClient.SendAsync(request)) ? Profiles[mLastProfileIndex] : null;
                }
                catch (HttpRequestException ex)
                {
                    VerifyRawResult = SpeechVerifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return null;
                }
            }
        }

        public override async Task<bool> StartAsync()
        {
            ResetVerifier();

            if (string.IsNullOrEmpty(ClientId))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.StartAsync(callback): Missing ClientId.");
                VerifyRawResult = SpeechVerifier.Result.Invalid;
                return false;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, ClientId);

            Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.StartAsync(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Start, uri))
            {
                try
                {
                    return await HandleResponse(CallMode.Start, request, await mHttpClient.SendAsync(request));
                }
                catch (HttpRequestException ex)
                {
                    VerifyRawResult = SpeechVerifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return false;
                }
            }
        }

        public override async Task<SpeechVerifier.Result> PostAsync()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                VerifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.PostAsync(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    try
                    {
                        await HandleResponse(CallMode.Process, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return VerifyRawResult;
                }
            }
            return (VerifyRawResult = SpeechVerifier.Result.Invalid);
        }
        
        public override async Task<SpeechVerifier.Result> PostAsync(string languageCode, string livenessText)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.PostAsync(liveness): Missing Language Code");
                return (VerifyRawResult = SpeechVerifier.Result.Invalid);
            }

            if (string.IsNullOrEmpty(livenessText))
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.PostAsync(liveness): Missing Liveness Text");
                return (VerifyRawResult = SpeechVerifier.Result.Invalid);
            }

            if (IsSessionOpen && !IsSessionClosing)
            {
                VerifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_LIVENESS, languageCode, livenessText);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.PostAsync(liveness): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    try
                    {
                        await HandleResponse(CallMode.Process, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return VerifyRawResult;
                }
            }
            return (VerifyRawResult = SpeechVerifier.Result.Invalid);
        }

        public override async Task<SpeechVerifier.Result> SummarizeAsync()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                VerifyResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_SUMMARIZE);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.SummarizeAsync(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Summarize, uri))
                {
                    try
                    {
                        await HandleResponse(CallMode.Summarize, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return VerifyRawResult;
                }
            }
            return (VerifyRawResult = SpeechVerifier.Result.Invalid);
        }

        public override async Task<bool> CancelAsync(string reason)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                VerifyResults.Clear();
                Content.Clear();

                if (string.IsNullOrEmpty(reason))
                {
                    reason = "Unknown";
                }
                else
                {
                    reason = reason.Replace(' ', '-').Substring(0, System.Math.Min(reason.Length, 64));
                }

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_CANCEL, reason);

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.Cancel(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Cancel, uri))
                {
                    try
                    {
                        return await HandleResponse(CallMode.Cancel, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        VerifyRawResult = SpeechVerifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                }
            }
            return false;
        }

        #endregion

        #region Private
        
        private HttpRequestMessage BuildRequest(CallMode mode, Uri uri)
        {
            HttpRequestMessage request = null;
            switch (mode)
            {
                case CallMode.Profile:
                    request = new HttpRequestMessage(HttpMethod.Post, uri);
                    request.Headers.Add("Developer-Key", Configuration.DeveloperKey);
                    request.Headers.Add("Application-Key", Configuration.ApplicationKey);
                    if (!string.IsNullOrEmpty(InteractionId))
                    {
                        request.Headers.Add(SpeechApi.INTERACTION_ID, InteractionId);
                    }
                    if (!string.IsNullOrEmpty(InteractionTag))
                    {
                        request.Headers.Add(SpeechApi.INTERACTION_TAG, InteractionTag);
                    }
                    if (!string.IsNullOrEmpty(Configuration.ApplicationSource))
                    {
                        request.Headers.Add(SpeechApi.INTERACTION_SOURCE, Configuration.ApplicationSource);
                        request.Headers.Add(SpeechApi.APP_VERSION_ID, Configuration.ApplicationSource);
                    }
                    if (!string.IsNullOrEmpty(Configuration.ApplicationUserAgent))
                    {
                        request.Headers.Add(SpeechApi.INTERACTION_AGENT, Configuration.ApplicationUserAgent);
                    }
                    break;
                case CallMode.Start:
                    request = new HttpRequestMessage(HttpMethod.Post, uri);
                    request.Headers.Add("Developer-Key", Configuration.DeveloperKey);
                    request.Headers.Add("Application-Key", Configuration.ApplicationKey);
                    if (!string.IsNullOrEmpty(InteractionId))
                    {
                        Logger?.LogDebug("{0}: {1}", SpeechApi.INTERACTION_ID, InteractionId);
                        request.Headers.Add(SpeechApi.INTERACTION_ID, InteractionId);
                    }
                    if (!string.IsNullOrEmpty(InteractionTag))
                    {
                        Logger?.LogDebug("{0}: {1}", SpeechApi.INTERACTION_TAG, InteractionTag);
                        request.Headers.Add(SpeechApi.INTERACTION_TAG, InteractionTag);
                    }
                    if (!string.IsNullOrEmpty(Configuration.ApplicationSource))
                    {
                        Logger?.LogDebug("{0}: {1}", SpeechApi.INTERACTION_SOURCE, Configuration.ApplicationSource);
                        Logger?.LogDebug("{0}: {1}", SpeechApi.APP_VERSION_ID, Configuration.ApplicationSource);
                        request.Headers.Add(SpeechApi.INTERACTION_SOURCE, Configuration.ApplicationSource);
                        request.Headers.Add(SpeechApi.APP_VERSION_ID, Configuration.ApplicationSource);
                    }
                    if (!string.IsNullOrEmpty(Configuration.ApplicationUserAgent))
                    {
                        Logger?.LogDebug("{0}: {1}", SpeechApi.INTERACTION_AGENT, Configuration.ApplicationUserAgent);
                        request.Headers.Add(SpeechApi.INTERACTION_AGENT, Configuration.ApplicationUserAgent);
                    }
                    if (MetaData.Count > 0)
                    {
                        foreach (var item in MetaData)
                        {
                            Logger?.LogDebug("{0}: {1}", item.Key, item.Value);
                            request.Headers.Add(item.Key, item.Value);
                        }
                        MetaData.Clear();
                    }
                    break;
                case CallMode.Process:
                    request = new HttpRequestMessage(HttpMethod.Post, uri);
                    Logger?.LogDebug("{0}: {1}", SpeechApi.SESSION_ID, SessionId);
                    request.Headers.Add(SpeechApi.SESSION_ID, SessionId);
                    if (MetaData.Count > 0)
                    {
                        foreach (var item in MetaData)
                        {
                            Logger?.LogDebug("{0}: {1}", item.Key, item.Value);
                            request.Headers.Add(item.Key, item.Value);
                        }
                        MetaData.Clear();
                    }
                    request.Content = BuildContent();
                    break;
                case CallMode.Summarize:
                    request = new HttpRequestMessage(HttpMethod.Delete, uri);
                    Logger?.LogDebug("{0}: {1}", SpeechApi.SESSION_ID, SessionId);
                    request.Headers.Add(SpeechApi.SESSION_ID, SessionId);
                    if (MetaData.Count > 0)
                    {
                        foreach (var item in MetaData)
                        {
                            Logger?.LogDebug("{0}: {1}", item.Key, item.Value);
                            request.Headers.Add(item.Key, item.Value);
                        }
                        MetaData.Clear();
                    }
                    if (!string.IsNullOrEmpty(AuthToken))
                    {
                        Logger?.LogDebug("{0}: {1}", SpeechApi.OVERRIDE_TOKEN, AuthToken);
                        request.Headers.Add(SpeechApi.OVERRIDE_TOKEN, AuthToken);
                    }
                    break;
                case CallMode.Cancel:
                    request = new HttpRequestMessage(HttpMethod.Delete, uri);
                    Logger?.LogDebug("{0}: {1}", SpeechApi.SESSION_ID, SessionId);
                    request.Headers.Add(SpeechApi.SESSION_ID, SessionId);
                    break;
            }

            request.Version = new Version(1, 1);
            request.Headers.ConnectionClose = false;
            string cookies = CookieContainer.GetCookieHeader(request.RequestUri);
            if (!string.IsNullOrEmpty(cookies))
            {
                Logger?.LogDebug("{0}: {1}", SpeechApi.COOKIE, cookies);
                request.Headers.Add(SpeechApi.COOKIE, cookies);
            }

            return request;
        }

        private async Task<bool> HandleResponse(CallMode mode, HttpRequestMessage request, HttpResponseMessage response)
        {
            Content.Clear();
            ExtraData.Clear();

            if (response == null)
            {
                Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Timeout occurred");
                VerifyRawResult = SpeechVerifier.Result.Timeout;
                return false;
            }

            try
            {
                HttpStatusCode code = response.StatusCode;
                string description = response.ReasonPhrase;

                Logger?.LogDebug($"SpeechVerifier_HttpClient_Version1.HandleResponse(): Http Status Code: {code}");
                Logger?.LogDebug($"SpeechVerifier_HttpClient_Version1.HandleResponse(): Http Status Description: {description}");

                if (code == HttpStatusCode.OK)
                {
                    if (mode == CallMode.Start)
                    {
                        if (!response.Headers.TryGetValues(SpeechApi.SESSION_ID, out var sessionValue))
                        {
                            VerifyRawResult = SpeechVerifier.Result.Invalid;
                            Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Invalid start response, no Vv-Session-Id");
                            return false;
                        }

                        SessionId = sessionValue.FirstOrDefault();
                        if (string.IsNullOrEmpty(SessionId))
                        {
                            VerifyRawResult = SpeechVerifier.Result.Invalid;
                            Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Invalid start response, empty Vv-Session-Id");
                            return false;
                        }

                        IsSessionOpen = true;
                    }
                    else if (mode == CallMode.Summarize)
                    {
                        SessionId = "";
                        IsSessionOpen = false;
                    }
                    else if (mode == CallMode.Cancel)
                    {
                        SessionId = "";
                        IsSessionOpen = false;
                        return true;
                    }
                }

                if (response.Headers.TryGetValues(SpeechApi.SET_COOKIE, out var newCookies))
                {
                    foreach (var item in SetCookieHeaderValue.ParseList(newCookies.ToList()))
                    {
                        var uri = new Uri(request.RequestUri, item.Path.Value);
                        Logger?.LogDebug("{0}: {1} - {2} - {3}", SpeechApi.SET_COOKIE, item.Name.Value, item.Value.Value, item.Path.Value);
                        CookieContainer.Add(uri, new Cookie(item.Name.Value, item.Value.Value, item.Path.Value));
                    }
                }

                if (response.Content == null)
                {
                    Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Expected Content Type: 'application/json'. Actual Content Type: 'Empty'");
                    return false;
                }
                else if (!response.Content.Headers.ContentType.MediaType.Equals("application/json"))
                {
                    Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Expected Content Type: 'application/json'. Actual Content Type: '{0}'", response.Content.Headers.ContentType.MediaType);
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (content == null || content.Length == 0)
                {
                    Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Expected Content Data: Length 0");
                    return false;
                }

                Logger?.LogDebug("SpeechVerifier_HttpClient_Version1.HandleResponse(): Http Content: {0}", content);

                SpeechVerifier_Version1_Response version1Response = JsonConvert.DeserializeObject<SpeechVerifier_Version1_Response>(content);

                if (code == HttpStatusCode.OK)
                {
                    switch (mode)
                    {
                        case CallMode.Profile:
                            if (version1Response.VerifyProfile != null)
                            {
                                if (version1Response.VerifyProfile.Profile.Type == SpeechVerifier.ProfileType.Unknown)
                                {
                                    Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Invalid ProfileType");
                                    return false;
                                }
                                mLastProfileIndex = AddProfile(version1Response.VerifyProfile.Index, version1Response.VerifyProfile.Profile);
                            }
                            break;
                        case CallMode.Start:
                            if (version1Response.VerifyProfile != null)
                            {
                                if (version1Response.VerifyProfile.Profile.Type == SpeechVerifier.ProfileType.Unknown)
                                {
                                    Logger?.LogError("SpeechVerifier_HttpClient_Version1.HandleResponse(): Invalid ProfileType");
                                    return false;
                                }
                                mLastProfileIndex = AddProfile(version1Response.VerifyProfile.Index, version1Response.VerifyProfile.Profile);
                            }
                            if (version1Response.ResultData != null)
                            {
                                foreach (var kv in version1Response.ResultData)
                                {
                                    ExtraData.Add(kv.Key, kv.Value);
                                }
                            }
                            break;
                        case CallMode.Process:
                            if (version1Response.VerifyResults != null)
                            {
                                foreach (var kv in version1Response.VerifyResults)
                                {
                                    AddResult(kv.Value.Index, kv.Key, kv.Value.Instance);
                                }

                                if (VerifyResults.Count > 0 && !string.IsNullOrEmpty(ClientId) && VerifyResults.ContainsKey(ClientId))
                                {
                                    SpeechVerifier.InstanceResult result = VerifyResults[ClientId];
                                    VerifyScore = result.Score;
                                    SpeechExtracted = result.SpeechExtracted;
                                    VerifyRawResult = result.Result;
                                }
                            }
                            if(version1Response.LivenessData != null)
                            {
                                LivenessResult = version1Response.LivenessData.Result;
                            }
                            if (version1Response.ResultData != null)
                            {
                                foreach (var kv in version1Response.ResultData)
                                {
                                    ExtraData.Add(kv.Key, kv.Value);
                                }
                            }
                            break;
                        case CallMode.Summarize:
                            if (version1Response.VerifyResults != null)
                            {
                                foreach (var kv in version1Response.VerifyResults)
                                {
                                    AddResult(kv.Value.Index, kv.Key, kv.Value.Instance);
                                }

                                if (VerifyResults.Count > 0 && !string.IsNullOrEmpty(ClientId) && VerifyResults.ContainsKey(ClientId))
                                {
                                    SpeechVerifier.InstanceResult result = VerifyResults[ClientId];
                                    VerifyScore = result.Score;
                                    SpeechExtracted = result.SpeechExtracted;
                                    VerifyRawResult = result.Result;
                                }
                            }
                            if (version1Response.LivenessData != null)
                            {
                                LivenessResult = version1Response.LivenessData.Result;
                            }
                            if (version1Response.ResultData != null)
                            {
                                foreach (var kv in version1Response.ResultData)
                                {
                                    ExtraData.Add(kv.Key, kv.Value);
                                }
                            }
                            break;
                    }

                    return true;
                }
                else
                {
                    if (version1Response.Error.HasValue)
                    {
                        VerifyRawResult = GetErrorResult(version1Response.Error.Value);
                    }
                    else
                    {
                        VerifyRawResult = SpeechVerifier.Result.Unknown;
                    }

                    if (VerifyRawResult == SpeechVerifier.Result.NeedMore)
                    {
                        if (IsTooSoft) VerifyRawResult = SpeechVerifier.Result.TooSoft;
                        else if (IsTooLoud) VerifyRawResult = SpeechVerifier.Result.TooLoud;
                    }
                    else if (VerifyRawResult == SpeechVerifier.Result.TooSoft)
                    {
                        IsTooSoft = true;
                    }
                    else if (VerifyRawResult == SpeechVerifier.Result.TooLoud)
                    {
                        IsTooLoud = true;
                    }

                    Logger?.LogError(
                        "SpeechVerifier_HttpClient_Version1.HandleResponse(): Error detected.\r\n\tUri: {0}\r\n\tResult: {1}\r\n\tCode: {2}\r\n\tDescription: {3}",
                        request.RequestUri,
                        VerifyRawResult,
                        version1Response.Error.HasValue ? "" + version1Response.Error.Value + "" : "?",
                        version1Response.Description);

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private MultipartFormDataContent BuildContent()
        {
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
            var content = new MultipartFormDataContent(boundary);

            foreach (var element in Content)
            {
                if (element.Value is SpeechAudio audio)
                {
                    content.Add(new StreamContent(audio.Stream), element.Name, audio.FileName);
                }
                else if(element.Value is SpeechContext context)
                {
                    content.Add(new StringContent(JsonConvert.SerializeObject(context)), element.Name);
                }
            }

            return content;
        }

        #endregion

    }

    [JsonObject]
    internal class SpeechVerifier_Version1_Response
    {
        [JsonProperty("profile.verify")]
        public SpeechVerifier_Version1_Profile VerifyProfile { get; set; }

        [JsonProperty("result.verify")]
        public Dictionary<string, SpeechVerifier_Version1_Result> VerifyResults { get; set; }

        [JsonProperty("result.liveness")]
        public SpeechVerifier_Version1_Liveness LivenessData { get; set; }

        [JsonProperty("result.data")]
        public Dictionary<string, object> ResultData { get; set; }

        [JsonProperty("error")]
        public int? Error { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    [JsonObject]
    internal class SpeechVerifier_Version1_Profile
    {
        [JsonProperty("index")]
        public uint Index { get; set; }

        [JsonProperty("codec")]
        public string Codec { get; set; }

        [JsonProperty("min_seconds_of_speech")]
        public double? MinimumSecondsOfSpeech { get; set; }

        [JsonProperty("type")]
        public uint ProfileType { get; set; }

        [JsonProperty("pass")]
        public double PassThreshold { get; set; }

        [JsonProperty("fail")]
        public double FailThreshold { get; set; }

        [JsonIgnore]
        public SpeechVerifier.Profile Profile
        {
            get
            {
                var profile = new SpeechVerifier.Profile();
                profile.Type = SpeechVerifierExtensions.GetProfileType(ProfileType);
                profile.Codec = Codec.GetCodec();
                profile.MinimumSecondsOfSpeech = MinimumSecondsOfSpeech;
                profile.PassThreshold = PassThreshold;
                profile.FailThreshold = FailThreshold;
                return profile;
            }
        }
    }

    [JsonObject]
    internal class SpeechVerifier_Version1_Result
    {
        [JsonProperty("error")]
        public uint Error { get; set; }

        [JsonProperty("index")]
        public uint Index { get; set; }

        [JsonProperty("seconds_extracted")]
        public double? SpeechExtracted { get; set; }

        [JsonProperty("score")]
        public double? Score { get; set; }

        [JsonProperty("status")]
        public char? Status { get; set; }

        [JsonProperty("overridable")]
        public bool? IsOverridable { get; set; }

        [JsonProperty("authorized")]
        public bool? IsAuthorized { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData { get; set; }

        [JsonIgnore]
        public SpeechVerifier.InstanceResult Instance
        {
            get
            {
                var instance = new SpeechVerifier.InstanceResult();
                instance.ErrorCode = Error;
                instance.SpeechExtracted = SpeechExtracted ?? 0;
                instance.Score = Score ?? 0;
                if(Status.HasValue)
                {
                    switch (Status.Value)
                    {
                        case 'P': instance.Result = SpeechVerifier.Result.Pass; break;
                        case 'A': instance.Result = SpeechVerifier.Result.Ambiguous; break;
                        case 'F': instance.Result = SpeechVerifier.Result.Fail; break;
                        case 'M': instance.Result = SpeechVerifier.Result.NeedMore; break;
                        case 'N': instance.Result = SpeechVerifier.Result.NotScored; break;
                        default: instance.Result = SpeechVerifier.Result.Unknown; break;
                    }
                }
                else
                {
                    instance.Result = SpeechVerifier.Result.Unknown;
                }
                instance.IsOverridable = IsOverridable;
                instance.IsAuthorized = IsAuthorized;
                if (ExtraData != null && ExtraData.Count > 0)
                {
                    foreach (var kv in ExtraData)
                    {
                        instance.Extra.Add(kv.Key, kv.Value);
                    }
                }
                return instance;
            }
        }
    }

    [JsonObject]
    internal class SpeechVerifier_Version1_Liveness
    {
        [JsonProperty("is_alive")]
        public bool? IsAlive { get; set; }

        [JsonIgnore]
        public SpeechVerifier.AliveResult Result
        {
            get
            {
                return IsAlive.HasValue && IsAlive.Value ? SpeechVerifier.AliveResult.Alive : SpeechVerifier.AliveResult.NotAlive;
            }
        }
    }

}

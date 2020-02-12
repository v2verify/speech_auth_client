
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Net.Http.Headers;

using Newtonsoft.Json;

namespace Dynamic.Speech.Authorization.Internal
{
    internal class SpeechIdentifier_HttpClient_Version1 : SpeechIdentifierBase
    {
        #region Private

        private const string URI_PATH_PROFILE = "/1/sve/Verification/Profile";
        private const string URI_PATH_START = "/1/sve/Verification/{0}";
        private const string URI_PATH_PROCESS = "/1/sve/Verification";
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

        internal SpeechIdentifier_HttpClient_Version1(SpeechApi.Configuration configuration)
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

        public override SpeechIdentifier.Profile PrefetchProfile()
        {
            ResetIdentifier();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.PreFetchProfile(): URI: " + uri.ToString());

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
                    IdentifyResult = SpeechIdentifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return null;
                }
            }
        }

        public override bool Start()
        {
            ResetIdentifier();

            string composedClientId = GetComposedClientIds();

            if (string.IsNullOrEmpty(composedClientId))
            {
                Logger?.LogError("SpeechIdentifier_HttpClient_Version1.Start(): Missing ClientId and/or GroupIds.");
                IdentifyResult = SpeechIdentifier.Result.Invalid;
                return false;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, composedClientId);

            Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Start(): URI: " + uri.ToString());

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
                    IdentifyResult = SpeechIdentifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return false;
                }
            }
        }

        public override SpeechIdentifier.Result Post()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IdentifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Post(): URI: " + uri.ToString());

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
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return IdentifyResult;
                }
            }
            return (IdentifyResult = SpeechIdentifier.Result.Invalid);
        }
        
        public override SpeechIdentifier.Result Summarize()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                IdentifyResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_SUMMARIZE);

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Summarize(): URI: " + uri.ToString());

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
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }

                    return IdentifyResult;
                }
            }
            return (IdentifyResult = SpeechIdentifier.Result.Invalid);
        }

        public override bool Cancel(string reason)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                IdentifyResults.Clear();
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

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Cancel(): URI: " + uri.ToString());

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
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                }
            }
            return false;
        }

        #endregion

        #region Public Callback Methods

        public override void PrefetchProfile(SpeechIdentifier.ProfileCallback callback)
        {
            ResetIdentifier();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.PreFetchProfile(callback): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Profile, uri))
            {
                var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                    if (requestTask.IsFaulted)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(requestTask.Exception);
                        callback?.Invoke(null);
                        return;
                    }
                    HandleResponse(CallMode.Profile, request, requestTask.Result).ContinueWith((responseTask) => {
                        if (responseTask.IsFaulted)
                        {
                            IdentifyResult = SpeechIdentifier.Result.Timeout;
                            Logger?.LogError(responseTask.Exception);
                            callback?.Invoke(null);
                            return;
                        }
                        callback?.Invoke(responseTask.Result ? Profiles[mLastProfileIndex] : null);
                    });
                });

                if (task.IsFaulted)
                {
                    IdentifyResult = SpeechIdentifier.Result.Timeout;
                    Logger?.LogError(task.Exception);
                    callback?.Invoke(null);
                }
            }
        }

        public override void Start(SpeechIdentifier.StartCallback callback)
        {
            ResetIdentifier();

            string composedClientId = GetComposedClientIds();

            if (string.IsNullOrEmpty(composedClientId))
            {
                Logger?.LogError("SpeechIdentifier_HttpClient_Version1.Start(callback): Missing ClientId and/or GroupIds.");
                IdentifyResult = SpeechIdentifier.Result.Invalid;
                return;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, composedClientId);

            Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Start(callback): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Start, uri))
            {
                var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                    if (requestTask.IsFaulted)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(requestTask.Exception);
                        callback?.Invoke(false);
                        return;
                    }
                    HandleResponse(CallMode.Start, request, requestTask.Result).ContinueWith((responseTask) => {
                        if (responseTask.IsFaulted)
                        {
                            IdentifyResult = SpeechIdentifier.Result.Timeout;
                            Logger?.LogError(responseTask.Exception);
                            callback?.Invoke(false);
                            return;
                        }
                        callback?.Invoke(responseTask.Result);
                    });
                });

                if (task.IsFaulted)
                {
                    IdentifyResult = SpeechIdentifier.Result.Timeout;
                    Logger?.LogError(task.Exception);
                    callback?.Invoke(false);
                }
            }
        }

        public override void Post(SpeechIdentifier.PostCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IdentifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Post(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            IdentifyResult = SpeechIdentifier.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke(IdentifyResult);
                            return;
                        }
                        HandleResponse(CallMode.Process, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                IdentifyResult = SpeechIdentifier.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke(IdentifyResult);
                                return;
                            }
                            callback?.Invoke(IdentifyResult);
                        });
                    });

                    if (task.IsFaulted)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke(IdentifyResult);
                    }

                    return;
                }
            }
            callback?.Invoke(IdentifyResult = SpeechIdentifier.Result.Invalid);
        }
        
        public override void Summarize(SpeechIdentifier.SummarizeCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                IdentifyResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_SUMMARIZE);

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Summarize(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Summarize, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            IdentifyResult = SpeechIdentifier.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke(IdentifyResult);
                            return;
                        }
                        HandleResponse(CallMode.Summarize, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                IdentifyResult = SpeechIdentifier.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke(IdentifyResult);
                                return;
                            }
                            callback?.Invoke(IdentifyResult);
                        });
                    });

                    if (task.IsFaulted)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke(IdentifyResult);
                    }

                    return;
                }
            }
            callback?.Invoke(IdentifyResult = SpeechIdentifier.Result.Invalid);
        }

        public override void Cancel(string reason, SpeechIdentifier.CancelCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                IdentifyResults.Clear();
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

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Cancel(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Cancel, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            IdentifyResult = SpeechIdentifier.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke();
                            return;
                        }
                        HandleResponse(CallMode.Cancel, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                IdentifyResult = SpeechIdentifier.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke();
                                return;
                            }
                            callback?.Invoke();
                        });
                    });

                    if (task.IsFaulted)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke();
                    }

                    return;
                }
            }
            IdentifyResult = SpeechIdentifier.Result.Invalid;
            callback?.Invoke();
        }

        #endregion

        #region Public Asynchronous Methods

        public override async Task<SpeechIdentifier.Profile> PrefetchProfileAsync()
        {
            ResetIdentifier();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.PrefetchProfileAsync(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Profile, uri))
            {
                try
                {
                    return await HandleResponse(CallMode.Profile, request, await mHttpClient.SendAsync(request)) ? Profiles[mLastProfileIndex] : null;
                }
                catch (HttpRequestException ex)
                {
                    IdentifyResult = SpeechIdentifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return null;
                }
            }
        }

        public override async Task<bool> StartAsync()
        {
            ResetIdentifier();

            string composedClientId = GetComposedClientIds();

            if (string.IsNullOrEmpty(composedClientId))
            {
                Logger?.LogError("SpeechIdentifier_HttpClient_Version1.StartAsync(callback): Missing ClientId and/or GroupIds.");
                IdentifyResult = SpeechIdentifier.Result.Invalid;
                return false;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, composedClientId);

            Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.StartAsync(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Start, uri))
            {
                try
                {
                    return await HandleResponse(CallMode.Start, request, await mHttpClient.SendAsync(request));
                }
                catch (HttpRequestException ex)
                {
                    IdentifyResult = SpeechIdentifier.Result.Timeout;
                    Logger?.LogError(ex);
                    return false;
                }
            }
        }

        public override async Task<SpeechIdentifier.Result> PostAsync()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IdentifyResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.PostAsync(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    try
                    {
                        await HandleResponse(CallMode.Process, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return IdentifyResult;
                }
            }
            return (IdentifyResult = SpeechIdentifier.Result.Invalid);
        }
        
        public override async Task<SpeechIdentifier.Result> SummarizeAsync()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                IdentifyResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_SUMMARIZE);

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.SummarizeAsync(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Summarize, uri))
                {
                    try
                    {
                        await HandleResponse(CallMode.Summarize, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return IdentifyResult;
                }
            }
            return (IdentifyResult = SpeechIdentifier.Result.Invalid);
        }

        public override async Task<bool> CancelAsync(string reason)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                IdentifyResults.Clear();
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

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.Cancel(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Cancel, uri))
                {
                    try
                    {
                        return await HandleResponse(CallMode.Cancel, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        IdentifyResult = SpeechIdentifier.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                }
            }
            return false;
        }

        #endregion

        #region Private

        private string GetComposedClientIds()
        {
            bool alreadyGrouped = false;
            string composedClientId = "";
            if (!string.IsNullOrEmpty(ProbableClientId))
            {
                composedClientId += ProbableClientId;
                if (ProbableClientId.Contains("$"))
                {
                    alreadyGrouped = true;
                    ProbableClientId = ProbableClientId.Substring(0, ProbableClientId.IndexOf('$'));
                }
                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.GetComposedClientId(): Most-Likely-Client-Id: " + ProbableClientId);
            }

            if (!alreadyGrouped)
            {
                var possibleClientIds = PossibleClientIds;
                if (!string.IsNullOrEmpty(possibleClientIds))
                {
                    if (composedClientId != string.Empty)
                    {
                        composedClientId += "$";
                    }
                    composedClientId += possibleClientIds;
                    Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.GetComposedClientId(): Possible-Client-Ids: " + possibleClientIds);
                }
            }

            return composedClientId;
        }

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
                Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Timeout occurred");
                IdentifyResult = SpeechIdentifier.Result.Timeout;
                return false;
            }

            try
            {
                HttpStatusCode code = response.StatusCode;
                string description = response.ReasonPhrase;

                Logger?.LogDebug($"SpeechIdentifier_HttpClient_Version1.HandleResponse(): Http Status Code: {code}");
                Logger?.LogDebug($"SpeechIdentifier_HttpClient_Version1.HandleResponse(): Http Status Description: {description}");

                if (code == HttpStatusCode.OK)
                {
                    if (mode == CallMode.Start)
                    {
                        if (!response.Headers.TryGetValues(SpeechApi.SESSION_ID, out var sessionValue))
                        {
                            Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Invalid start response, no Vv-Session-Id");
                            return false;
                        }

                        SessionId = sessionValue.FirstOrDefault();
                        if (string.IsNullOrEmpty(SessionId))
                        {
                            Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Invalid start response, empty Vv-Session-Id");
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
                    Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Expected Content Type: 'application/json'. Actual Content Type: 'Empty'");
                    return false;
                }
                else if (!response.Content.Headers.ContentType.MediaType.Equals("application/json"))
                {
                    Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Expected Content Type: 'application/json'. Actual Content Type: '{0}'", response.Content.Headers.ContentType.MediaType);
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (content == null || content.Length == 0)
                {
                    Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Expected Content Data: Length 0");
                    return false;
                }

                Logger?.LogDebug("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Http Content: {0}", content);

                SpeechIdentifier_Version1_Response version1Response = JsonConvert.DeserializeObject<SpeechIdentifier_Version1_Response>(content);

                if (code == HttpStatusCode.OK)
                {
                    switch (mode)
                    {
                        case CallMode.Profile:
                            if (version1Response.IdentifyProfile != null)
                            {
                                if(version1Response.IdentifyProfile.Profile.Type == SpeechIdentifier.ProfileType.Unknown)
                                {
                                    IdentifyResult = SpeechIdentifier.Result.Invalid;
                                    Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Invalid ProfileType");
                                    return false;
                                }
                                mLastProfileIndex = AddProfile(version1Response.IdentifyProfile.Index, version1Response.IdentifyProfile.Profile);
                            }
                            break;
                        case CallMode.Start:
                            if (version1Response.IdentifyProfile != null)
                            {
                                if (version1Response.IdentifyProfile.Profile.Type == SpeechIdentifier.ProfileType.Unknown)
                                {
                                    IdentifyResult = SpeechIdentifier.Result.Invalid;
                                    Logger?.LogError("SpeechIdentifier_HttpClient_Version1.HandleResponse(): Invalid ProfileType");
                                    return false;
                                }
                                mLastProfileIndex = AddProfile(version1Response.IdentifyProfile.Index, version1Response.IdentifyProfile.Profile);
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
                            if (version1Response.IdentifyResults != null)
                            {
                                foreach (var kv in version1Response.IdentifyResults)
                                {
                                    AddResult(kv.Value.Index, kv.Key, kv.Value.Instance);
                                }

                                if (IdentifyResults.Count == 1)
                                {
                                    var obj = (DictionaryEntry)IdentifyResults.GetFirst();
                                    SpeechIdentifier.InstanceResult result = (SpeechIdentifier.InstanceResult)obj.Value;
                                    IdentifiedClientId = Convert.ToString(obj.Key);
                                    IdentifiedScore = result.Score;
                                    SpeechExtracted = result.SpeechExtracted;
                                    IdentifyResult = result.Result;
                                }
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
                            if (version1Response.IdentifyResults != null)
                            {
                                foreach (var kv in version1Response.IdentifyResults)
                                {
                                    AddResult(kv.Value.Index, kv.Key, kv.Value.Instance);
                                }

                                if (IdentifyResults.Count == 1)
                                {
                                    var obj = (DictionaryEntry)IdentifyResults.GetFirst();
                                    SpeechIdentifier.InstanceResult result = (SpeechIdentifier.InstanceResult)obj.Value;
                                    IdentifiedClientId = Convert.ToString(obj.Key);
                                    IdentifiedScore = result.Score;
                                    SpeechExtracted = result.SpeechExtracted;
                                    IdentifyResult = result.Result;
                                }
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
                        IdentifyResult = GetErrorResult(version1Response.Error.Value);
                    }
                    else
                    {
                        IdentifyResult = SpeechIdentifier.Result.Unknown;
                    }

                    if (IdentifyResult == SpeechIdentifier.Result.NeedMore)
                    {
                        if (IsTooSoft) IdentifyResult = SpeechIdentifier.Result.TooSoft;
                        else if (IsTooLoud) IdentifyResult = SpeechIdentifier.Result.TooLoud;
                    }
                    else if (IdentifyResult == SpeechIdentifier.Result.TooSoft)
                    {
                        IsTooSoft = true;
                    }
                    else if (IdentifyResult == SpeechIdentifier.Result.TooLoud)
                    {
                        IsTooLoud = true;
                    }

                    Logger?.LogError(
                        "SpeechIdentifier_HttpClient_Version1.HandleResponse(): Error detected.\r\n\tUri: {0}\r\n\tResult: {1}\r\n\tCode: {2}\r\n\tDescription: {3}",
                        request.RequestUri,
                        IdentifyResult,
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
            }

            return content;
        }

        #endregion

    }

    [JsonObject]
    internal class SpeechIdentifier_Version1_Response
    {
        [JsonProperty("profile.verify")]
        public SpeechIdentifier_Version1_Profile IdentifyProfile { get; set; }

        [JsonProperty("result.verify")]
        public Dictionary<string, SpeechIdentifier_Version1_Result> IdentifyResults { get; set; }
        
        [JsonProperty("result.data")]
        public Dictionary<string, object> ResultData { get; set; }

        [JsonProperty("error")]
        public int? Error { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    [JsonObject]
    internal class SpeechIdentifier_Version1_Profile
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
        public SpeechIdentifier.Profile Profile
        {
            get
            {
                var profile = new SpeechIdentifier.Profile();
                profile.Type = SpeechIdentifierExtensions.GetProfileType(ProfileType);
                profile.Codec = Codec.GetCodec();
                profile.MinimumSecondsOfSpeech = MinimumSecondsOfSpeech;
                profile.PassThreshold = PassThreshold;
                profile.FailThreshold = FailThreshold;
                return profile;
            }
        }
    }

    [JsonObject]
    internal class SpeechIdentifier_Version1_Result
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

        [JsonProperty("authorized")]
        public bool? IsAuthorized { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData { get; set; }

        [JsonIgnore]
        public SpeechIdentifier.InstanceResult Instance
        {
            get
            {
                var instance = new SpeechIdentifier.InstanceResult();
                instance.ErrorCode = Error;
                instance.SpeechExtracted = SpeechExtracted ?? 0;
                instance.Score = Score ?? 0;
                if (Status.HasValue)
                {
                    switch (Status.Value)
                    {
                        case 'P': instance.Result = SpeechIdentifier.Result.Pass; break;
                        case 'A': instance.Result = SpeechIdentifier.Result.Ambiguous; break;
                        case 'F': instance.Result = SpeechIdentifier.Result.Fail; break;
                        case 'M': instance.Result = SpeechIdentifier.Result.NeedMore; break;
                        case 'N': instance.Result = SpeechIdentifier.Result.NotScored; break;
                        default: instance.Result = SpeechIdentifier.Result.Unknown; break;
                    }
                }
                else
                {
                    instance.Result = SpeechIdentifier.Result.Unknown;
                }
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

}


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
    internal class SpeechEnroller_HttpClient_Version1 : SpeechEnrollerBase
    {
        #region Private

        private const string URI_PATH_PROFILE = "/1/sve/Enrollment/Profile";
        private const string URI_PATH_START = "/1/sve/Enrollment/{0}/{1}";
        private const string URI_PATH_PROCESS = "/1/sve/Enrollment";
        private const string URI_PATH_TRAIN = "/1/sve/Enrollment";
        private const string URI_PATH_CANCEL = "/1/sve/Cancel/{0}";

        private enum CallMode
        {
            Profile,
            Start,
            Process,
            Train,
            Cancel
        };

        private HttpClient mHttpClient;
        private uint mLastProfileIndex;

        #endregion

        #region Life & Death

        internal SpeechEnroller_HttpClient_Version1(SpeechApi.Configuration configuration)
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

        public override SpeechEnroller.Profile PrefetchProfile()
        {
            ResetEnroller();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.PreFetchProfile(): URI: " + uri.ToString());

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
                    EnrollResult = SpeechEnroller.Result.Timeout;
                    Logger?.LogError(ex);
                    return null;
                }
            }
        }

        public override bool Start()
        {
            ResetEnroller();

            if (string.IsNullOrEmpty(ClientId))
            {
                Logger?.LogError("SpeechEnroller_HttpClient_Version1.Start(): Missing Client-Id");
                EnrollResult = SpeechEnroller.Result.Invalid;
                return false;
            }
            
            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, ClientId, SubPopulation.GetGender());

            Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Start(): URI: " + uri.ToString());

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
                catch(HttpRequestException ex)
                {
                    EnrollResult = SpeechEnroller.Result.Timeout;
                    Logger?.LogError(ex);
                    return false;
                }
            }
        }

        public override SpeechEnroller.Result Post()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                EnrollResults.Clear();
                
                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Post(): URI: " + uri.ToString());

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
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return EnrollResult;
                }
            }
            return (EnrollResult = SpeechEnroller.Result.Invalid);
        }

        public override SpeechEnroller.Result Train()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                EnrollResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_TRAIN);

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Train(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Train, uri))
                {
                    try
                    {
                        var responseTask = mHttpClient.SendAsync(request);

                        responseTask.RunSynchronously();

                        var resultTask = HandleResponse(CallMode.Train, request, responseTask.Result);

                        resultTask.RunSynchronously();
                    }
                    catch (HttpRequestException ex)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return EnrollResult;
                }
            }
            return (EnrollResult = SpeechEnroller.Result.Invalid);
        }

        public override bool Cancel(string reason)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                EnrollResults.Clear();
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

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Cancel(): URI: " + uri.ToString());

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
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                }
            }
            return false;
        }

        #endregion

        #region Public Callback Methods

        public override void PrefetchProfile(SpeechEnroller.ProfileCallback callback)
        {
            ResetEnroller();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.PreFetchProfile(callback): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Profile, uri))
            {
                var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                    if (requestTask.IsFaulted)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(requestTask.Exception);
                        callback?.Invoke(null);
                        return;
                    }
                    HandleResponse(CallMode.Profile, request, requestTask.Result).ContinueWith((responseTask) => {
                        if (responseTask.IsFaulted)
                        {
                            EnrollResult = SpeechEnroller.Result.Timeout;
                            Logger?.LogError(responseTask.Exception);
                            callback?.Invoke(null);
                            return;
                        }
                        callback?.Invoke(responseTask.Result ? Profiles[mLastProfileIndex] : null);
                    });
                });

                if(task.IsFaulted)
                {
                    EnrollResult = SpeechEnroller.Result.Timeout;
                    Logger?.LogError(task.Exception);
                    callback?.Invoke(null);
                }
            }
        }

        public override void Start(SpeechEnroller.StartCallback callback)
        {
            ResetEnroller();

            if (string.IsNullOrEmpty(ClientId))
            {
                Logger?.LogError("SpeechEnroller_HttpClient_Version1.Start(callback): Missing Client-Id");
                EnrollResult = SpeechEnroller.Result.Invalid;
                callback?.Invoke(false);
                return;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, ClientId, SubPopulation.GetGender());

            Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Start(callback): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Start, uri))
            {
                var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                    if (requestTask.IsFaulted)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(requestTask.Exception);
                        callback?.Invoke(false);
                        return;
                    }
                    HandleResponse(CallMode.Start, request, requestTask.Result).ContinueWith((responseTask) => {
                        if (responseTask.IsFaulted)
                        {
                            EnrollResult = SpeechEnroller.Result.Timeout;
                            Logger?.LogError(responseTask.Exception);
                            callback?.Invoke(false);
                            return;
                        }
                        callback?.Invoke(responseTask.Result);
                    });
                });

                if (task.IsFaulted)
                {
                    EnrollResult = SpeechEnroller.Result.Timeout;
                    Logger?.LogError(task.Exception);
                    callback?.Invoke(false);
                }
            }
        }

        public override void Post(SpeechEnroller.PostCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                EnrollResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Post(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            EnrollResult = SpeechEnroller.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke(EnrollResult);
                            return;
                        }
                        HandleResponse(CallMode.Process, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                EnrollResult = SpeechEnroller.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke(EnrollResult);
                                return;
                            }
                            callback?.Invoke(EnrollResult);
                        });
                    });
                    
                    if (task.IsFaulted)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke(EnrollResult);
                    }

                    return;
                }
            }
            callback?.Invoke(EnrollResult = SpeechEnroller.Result.Invalid);
        }

        public override void Train(SpeechEnroller.TrainCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                EnrollResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_TRAIN);

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Train(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Train, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            EnrollResult = SpeechEnroller.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke(EnrollResult);
                            return;
                        }
                        HandleResponse(CallMode.Train, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                EnrollResult = SpeechEnroller.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke(EnrollResult);
                                return;
                            }
                            callback?.Invoke(EnrollResult);
                        });
                    });
                    
                    if (task.IsFaulted)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke(EnrollResult);
                    }

                    return;
                }
            }
            callback?.Invoke(EnrollResult = SpeechEnroller.Result.Invalid);
        }

        public override void Cancel(string reason, SpeechEnroller.CancelCallback callback)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                EnrollResults.Clear();
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

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Cancel(callback): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Cancel, uri))
                {
                    var task = mHttpClient.SendAsync(request).ContinueWith((requestTask) => {
                        if (requestTask.IsFaulted)
                        {
                            EnrollResult = SpeechEnroller.Result.Timeout;
                            Logger?.LogError(requestTask.Exception);
                            callback?.Invoke();
                            return;
                        }
                        HandleResponse(CallMode.Cancel, request, requestTask.Result).ContinueWith((responseTask) => {
                            if (responseTask.IsFaulted)
                            {
                                EnrollResult = SpeechEnroller.Result.Timeout;
                                Logger?.LogError(responseTask.Exception);
                                callback?.Invoke();
                                return;
                            }
                            callback?.Invoke();
                        });
                    });
                    
                    if (task.IsFaulted)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(task.Exception);
                        callback?.Invoke();
                    }

                    return;
                }
            }
            EnrollResult = SpeechEnroller.Result.Invalid;
            callback?.Invoke();
        }

        #endregion

        #region Public Asynchronous Methods

        public override async Task<SpeechEnroller.Profile> PrefetchProfileAsync()
        {
            ResetEnroller();

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROFILE);

            Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.PrefetchProfileAsync(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Profile, uri))
            {
                try
                { 
                    return await HandleResponse(CallMode.Profile, request, await mHttpClient.SendAsync(request)) ? Profiles[mLastProfileIndex] : null;
                }
                catch (HttpRequestException ex)
                {
                    EnrollResult = SpeechEnroller.Result.Timeout;
                    Logger?.LogError(ex);
                    return null;
                }
            }
        }

        public override async Task<bool> StartAsync()
        {
            ResetEnroller();

            if (string.IsNullOrEmpty(ClientId))
            {
                Logger?.LogError("SpeechEnroller_HttpClient_Version1.StartAsync(): Missing Client-Id");
                EnrollResult = SpeechEnroller.Result.Invalid;
                return false;
            }

            Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_START, ClientId, SubPopulation.GetGender());

            Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.StartAsync(): URI: " + uri.ToString());

            using (var request = BuildRequest(CallMode.Start, uri))
            {
                try
                {
                    return await HandleResponse(CallMode.Start, request, await mHttpClient.SendAsync(request));
                }
                catch(HttpRequestException ex)
                {
                    EnrollResult = SpeechEnroller.Result.Timeout;
                    Logger?.LogError(ex);
                    return false;
                }
            }
        }

        public override async Task<SpeechEnroller.Result> PostAsync()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                EnrollResults.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_PROCESS);

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.PostAsync(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Process, uri))
                {
                    try
                    { 
                        await HandleResponse(CallMode.Process, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return EnrollResult;
                }
            }
            return (EnrollResult = SpeechEnroller.Result.Invalid);
        }

        public override async Task<SpeechEnroller.Result> TrainAsync()
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                EnrollResults.Clear();
                Content.Clear();

                Uri uri = Configuration.Server.BuildEndpoint(URI_PATH_TRAIN);

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.TrainAsync(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Train, uri))
                {
                    try
                    { 
                        await HandleResponse(CallMode.Train, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(ex);
                    }
                    return EnrollResult;
                }
            }
            return (EnrollResult = SpeechEnroller.Result.Invalid);
        }

        public override async Task<bool> CancelAsync(string reason)
        {
            if (IsSessionOpen && !IsSessionClosing)
            {
                IsSessionClosing = true;
                EnrollResults.Clear();
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

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.Cancel(): URI: " + uri.ToString());

                using (var request = BuildRequest(CallMode.Cancel, uri))
                {
                    try
                    { 
                        return await HandleResponse(CallMode.Cancel, request, await mHttpClient.SendAsync(request));
                    }
                    catch (HttpRequestException ex)
                    {
                        EnrollResult = SpeechEnroller.Result.Timeout;
                        Logger?.LogError(ex);
                        return false;
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
                    if (!string.IsNullOrEmpty(AuthToken))
                    {
                        Logger?.LogDebug("{0}: {1}", SpeechApi.OVERRIDE_TOKEN, AuthToken);
                        request.Headers.Add(SpeechApi.OVERRIDE_TOKEN, AuthToken);
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
                case CallMode.Train:
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
                Logger?.LogError("SpeechEnroller_HttpClient_Version1.HandleResponse(): Timeout occurred");
                EnrollResult = SpeechEnroller.Result.Timeout;
                return false;
            }

            try
            {
                HttpStatusCode code = response.StatusCode;
                string description = response.ReasonPhrase;

                Logger?.LogDebug($"SpeechEnroller_HttpClient_Version1.HandleResponse(): Http Status Code: {code}");
                Logger?.LogDebug($"SpeechEnroller_HttpClient_Version1.HandleResponse(): Http Status Description: {description}");

                if (code == HttpStatusCode.OK)
                {
                    if (mode == CallMode.Start)
                    {
                        if (!response.Headers.TryGetValues(SpeechApi.SESSION_ID, out var sessionValue))
                        {
                            Logger?.LogError("SpeechEnroller_HttpClient_Version1.HandleResponse(): Invalid start response, no Vv-Session-Id");
                            return false;
                        }

                        SessionId = sessionValue.FirstOrDefault();
                        if (string.IsNullOrEmpty(SessionId))
                        {
                            Logger?.LogError("SpeechEnroller_HttpClient_Version1.HandleResponse(): Invalid start response, empty Vv-Session-Id");
                            return false;
                        }

                        IsSessionOpen = true;
                    }
                    else if (mode == CallMode.Train)
                    {
                        SessionId = "";
                        IsSessionOpen = false;
                    }
                    else if(mode == CallMode.Cancel)
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
                    Logger?.LogError("SpeechEnroller_HttpClient_Version1.HandleResponse(): Expected Content Type: 'application/json'. Actual Content Type: 'Empty'");
                    return false;
                }
                else if(!response.Content.Headers.ContentType.MediaType.Equals("application/json"))
                {
                    Logger?.LogError("SpeechEnroller_HttpClient_Version1.HandleResponse(): Expected Content Type: 'application/json'. Actual Content Type: '{0}'", response.Content.Headers.ContentType.MediaType);
                    return false;
                }
                
                var content = await response.Content.ReadAsStringAsync();

                if (content == null || content.Length == 0)
                {
                    Logger?.LogError("SpeechEnroller_HttpClient_Version1.HandleResponse(): Expected Content Data: Length 0");
                    return false;
                }

                Logger?.LogDebug("SpeechEnroller_HttpClient_Version1.HandleResponse(): Http Content: {0}", content);

                SpeechEnroller_Version1_Response version1Response = JsonConvert.DeserializeObject<SpeechEnroller_Version1_Response>(content);

                if (code == HttpStatusCode.OK)
                {
                    switch (mode)
                    {
                        case CallMode.Profile:
                            if (version1Response.EnrollProfile != null)
                            {
                                mLastProfileIndex = AddProfile(version1Response.EnrollProfile.Index, version1Response.EnrollProfile.Profile);
                            }
                            break;
                        case CallMode.Start:
                            if (version1Response.EnrollProfile != null)
                            {
                                mLastProfileIndex = AddProfile(version1Response.EnrollProfile.Index, version1Response.EnrollProfile.Profile);
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
                            if (version1Response.EnrollResults != null)
                            {
                                foreach (var kv in version1Response.EnrollResults)
                                {
                                    AddResult(kv.Value.Index, kv.Key, kv.Value.Instance);
                                }

                                if (!string.IsNullOrEmpty(ClientId) && EnrollResults.ContainsKey(ClientId.ToLower()))
                                {
                                    SpeechEnroller.InstanceResult result = EnrollResults[ClientId.ToLower()];
                                    SpeechTrained = 0;
                                    if (result.SpeechExtracted.HasValue)
                                    {
                                        SpeechExtracted = result.SpeechExtracted.Value;
                                    }
                                    EnrollResult = result.Result;
                                }
                                else
                                {
                                    SpeechExtracted = 0;
                                    SpeechTrained = 0;
                                    EnrollResult = SpeechEnroller.Result.Unknown;
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
                        case CallMode.Train:
                            if (version1Response.EnrollResults != null)
                            {
                                foreach (var kv in version1Response.EnrollResults)
                                {
                                    AddResult(kv.Value.Index, kv.Key, kv.Value.Instance);
                                }

                                if (!string.IsNullOrEmpty(ClientId) && EnrollResults.ContainsKey(ClientId.ToLower()))
                                {
                                    SpeechEnroller.InstanceResult result = EnrollResults[ClientId.ToLower()];
                                    SpeechExtracted = 0;
                                    if (result.SpeechTrained.HasValue)
                                    {
                                        SpeechTrained = result.SpeechTrained.Value;
                                    }
                                    EnrollResult = result.Result;
                                }
                                else
                                {
                                    SpeechExtracted = 0;
                                    SpeechTrained = 0;
                                    EnrollResult = SpeechEnroller.Result.Unknown;
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
                        EnrollResult = GetErrorResult(version1Response.Error.Value);
                    }
                    else
                    {
                        EnrollResult = SpeechEnroller.Result.Unknown;
                    }

                    if (EnrollResult == SpeechEnroller.Result.NeedMore)
                    {
                        if (IsTooSoft) EnrollResult = SpeechEnroller.Result.TooSoft;
                        else if (IsTooLoud) EnrollResult = SpeechEnroller.Result.TooLoud;
                    }
                    else if (EnrollResult == SpeechEnroller.Result.TooSoft)
                    {
                        IsTooSoft = true;
                    }
                    else if (EnrollResult == SpeechEnroller.Result.TooLoud)
                    {
                        IsTooLoud = true;
                    }

                    Logger?.LogError(
                        "SpeechEnroller_HttpClient_Version1.HandleResponse(): Error detected.\r\n\tUri: {0}\r\n\tResult: {1}\r\n\tCode: {2}\r\n\tDescription: {3}",
                        request.RequestUri,
                        EnrollResult,
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

            foreach(var element in Content)
            {
                if(element.Value is SpeechAudio audio)
                {
                    content.Add(new StreamContent(audio.Stream), element.Name, audio.FileName);
                }
            }

            return content;
        }

        #endregion

    }

    [JsonObject]
    internal class SpeechEnroller_Version1_Response
    {
        [JsonProperty("profile.enroll")]
        public SpeechEnroller_Version1_Profile EnrollProfile { get; set; }

        [JsonProperty("result.enroll")]
        public Dictionary<string, SpeechEnroller_Version1_Result> EnrollResults { get; set; }

        [JsonProperty("result.data")]
        public Dictionary<string, object> ResultData { get; set; }

        [JsonProperty("error")]
        public int? Error { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
    
    [JsonObject]
    internal class SpeechEnroller_Version1_Profile
    {
        [JsonProperty("index")]
        public uint Index { get; set; }

        [JsonProperty("codec")]
        public string Codec { get; set; }

        [JsonProperty("min_seconds_of_speech")]
        public double MinimumSecondsOfSpeech { get; set; }

        [JsonIgnore]
        public SpeechEnroller.Profile Profile
        {
            get
            {
                var profile = new SpeechEnroller.Profile();
                profile.Codec = Codec.GetCodec();
                profile.MinimumSecondsOfSpeech = MinimumSecondsOfSpeech;
                return profile;
            }
        }
    }

    [JsonObject]
    internal class SpeechEnroller_Version1_Result
    {
        [JsonProperty("error")]
        public uint Error { get; set; }

        [JsonProperty("index")]
        public uint Index { get; set; }
        
        [JsonProperty("seconds_extracted")]
        public double? SecondsExtracted { get; set; }

        [JsonProperty("seconds_trained")]
        public double? SecondsTrained { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData { get; set; }

        [JsonIgnore]
        public SpeechEnroller.InstanceResult Instance
        {
            get
            {
                var instance = new SpeechEnroller.InstanceResult();
                instance.ErrorCode = Error;
                instance.SpeechExtracted = SecondsExtracted;
                instance.SpeechTrained = SecondsTrained;
                if(ExtraData != null && ExtraData.Count > 0)
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

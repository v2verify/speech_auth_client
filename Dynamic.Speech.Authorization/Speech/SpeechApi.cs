using System;
using System.Globalization;
using System.Net.Http;

namespace Dynamic.Speech
{
    public interface ISpeechLogger
    {
        void LogInfo(string msg, params object[] data);
        void LogDebug(string msg, params object[] data);
        void LogError(string msg, params object[] data);
        void LogError(Exception ex);
        void LogError(Exception ex, string msg, params object[] data);
    }

    public class SpeechApi
    {
        public enum SpeechServer
        {
            /// <summary>
            /// 
            /// </summary>
            UserDefined,
            
            /// <summary>
            /// 
            /// </summary>
            Public,
        }

        public enum SpeechVersion
        {
            /// <summary>
            /// A place holder for an undecided Version
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// [Deprecated][Unsupported]
            /// 
            /// Version Number maintained for historical reference
            /// </summary>
            Version_0 = 0,

            /// <summary>
            /// 
            /// </summary>
            Version_1 = 1
        }

        public enum SpeechTransport
        {
            /// <summary>
            /// A place holder for an undecided transport
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// 
            /// </summary>
            Rest = 0
        }

        public const string DEVELOPER_KEY = "Developer-Key";
        public const string APPLICATION_KEY = "Application-Key";
        public const string APP_VERSION_ID = "App-Version-Id";
        public const string INTERACTION_ID = "Interaction-Id";
        public const string INTERACTION_TAG = "Interaction-Tag";
        public const string INTERACTION_SOURCE = "Interaction-Source";
        public const string INTERACTION_AGENT = "Interaction-Agent";
        public const string SESSION_ID = "Vv-Session-Id";
        public const string OVERRIDE_TOKEN = "Vv-Override-Token";
        public const string FEEDBACK_BREAK_ATTEMPT = "Feedback-BreakAttempt";
        public const string FEEDBACK_RECORDING = "Feedback-Recording";
        public const string FEEDBACK_BACKGROUND_NOISE = "Feedback-BackgroundNoise";
        public const string FEEDBACK_COMMENTS = "Feedback-Comments";
        public const string COOKIE = "Cookie";
        public const string SET_COOKIE = "Set-Cookie";

        public const SpeechVersion DefaultVersion = SpeechVersion.Version_1;

        public static void Initialize(Configuration configuration)
        {
            if(configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            mConfiguration = configuration;
        }

        public static string DeveloperKey
        {
            get
            {
                if(mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.DeveloperKey;
            }
        }

        public static string ApplicationKey
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.ApplicationKey;
            }
        }

        public static string ApplicationSource
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.ApplicationSource;
            }
        }

        public static string ApplicationSourceVersion
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.ApplicationUserAgent;
            }
        }

        public static SpeechServer ServerType
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.ServerType;
            }
        }

        public static SpeechTransport ServerTransport
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.ServerTransport;
            }
        }

        public static string Server
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.Server;
            }
        }

        public static SpeechVersion ServerVersion
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.ServerVersion;
            }
        }

        public static CultureInfo Culture
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.Culture;
            }
        }

        public static int SocketReadTimeout
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.SocketReadTimeout;
            }
        }

        public static ISpeechLogger Logger
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration.Logger;
            }
        }

        public static Configuration DefaultConfiguration
        {
            get
            {
                if (mConfiguration == null)
                {
                    throw new Exception("Configuration Not Set");
                }
                return mConfiguration;
            }
        }

        #region

        public sealed class Configuration
        {
            public string DeveloperKey { get; internal set; }
            public string ApplicationKey { get; internal set; }
            public string ApplicationSource { get; internal set; }
            public string ApplicationUserAgent { get; internal set; }
            public SpeechServer ServerType { get; internal set; }
            public string Server { get; internal set; }
            public SpeechTransport ServerTransport { get; internal set; }
            public SpeechVersion ServerVersion { get; internal set; }
            public object ServerTransportObject { get; internal set; }
            public CultureInfo Culture { get; private set; }
            public int SocketReadTimeout { get; internal set; }
            public ISpeechLogger Logger { get; internal set; }

            internal Configuration()
            {
                DeveloperKey = "";
                ApplicationKey = "";
                ApplicationSource = "Dynamic.SpeechApi/2.0";
                ApplicationUserAgent = "";
                ServerType = SpeechServer.Public;
                Server = V2_ON_DEMAND_PUBLIC_SERVER;
                ServerTransport = SpeechTransport.Rest;
                ServerVersion = DefaultVersion;
                ServerTransportObject = null;
                Culture = new CultureInfo("en-US");
                SocketReadTimeout = 0;
                Logger = null;
            }
        }

        public sealed class Builder
        {
            private Configuration mConfiguration;

            public Builder()
            {
                mConfiguration = new Configuration();
            }

            public Builder SetDeveloperKey(string key)
            {
                mConfiguration.DeveloperKey = key;
                return this;
            }

            public Builder SetApplicationKey(string key)
            {
                mConfiguration.ApplicationKey = key;
                return this;
            }

            public Builder SetApplicationSource(string source)
            {
                mConfiguration.ApplicationSource = source;
                return this;
            }

            public Builder SetApplicationUserAgent(string userAgent)
            {
                mConfiguration.ApplicationUserAgent = userAgent;
                return this;
            }

            public Builder SetServer(string server)
            {
                mConfiguration.ServerType = SpeechServer.UserDefined;
                mConfiguration.Server = server;
                return this;
            }

            public Builder SetServerVersion(SpeechVersion version)
            {
                mConfiguration.ServerVersion = version;
                return this;
            }

            public Builder SetSocketReadTimeout(int timeout)
            {
                mConfiguration.SocketReadTimeout = timeout;
                return this;
            }

            public Builder SetServerTransport(SpeechTransport transport)
            {
                mConfiguration.ServerTransport = transport;
                return this;
            }

            public Builder SetTransport(HttpClient httpClient)
            {
                if (httpClient == null)
                {
                    throw new ArgumentNullException("httpClient");
                }
                
                SetServer(httpClient.BaseAddress.AbsoluteUri);
                SetServerTransport(SpeechTransport.Rest);
                mConfiguration.ServerTransportObject = httpClient;
                return this;
            }

            public Builder SetLogger(ISpeechLogger logger)
            {
                mConfiguration.Logger = logger;
                return this;
            }

            public Configuration Build()
            {
                if(string.IsNullOrEmpty(mConfiguration.DeveloperKey))
                {
                    throw new MissingFieldException("DeveloperKey");
                }
                else if (string.IsNullOrEmpty(mConfiguration.ApplicationKey))
                {
                    throw new MissingFieldException("ApplicationKey");
                }
                else if (string.IsNullOrEmpty(mConfiguration.Server))
                {
                    throw new MissingFieldException("Server");
                }
                
                if(mConfiguration.ServerTransportObject == null)
                {
                    if (SpeechApi.mConfiguration != null && SpeechApi.mConfiguration.ServerTransportObject != null)
                    {
                        mConfiguration.ServerTransportObject = SpeechApi.mConfiguration.ServerTransportObject;
                    }
                    else
                    {
                        mConfiguration.ServerTransportObject = new HttpClient();
                    }
                }

                return mConfiguration;
            }
        }

        #endregion

        #region
        
        private const string V2_ON_DEMAND_PUBLIC_SERVER = "https://public.v2ondemandapis.com";

        private static Configuration mConfiguration;

        static SpeechApi()
        {
            mConfiguration = null;
        }

        #endregion
    }
}

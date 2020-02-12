
using System;
using System.Collections;
using System.Text;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    internal static class SpeechExtensions
    {
        #region Internal Static Functions

        internal static WaveFormatEncoding GetCodec(this string algo)
        {
            switch (algo.ToLower())
            {
                case "a": case "alaw": return WaveFormatEncoding.ALaw;
                case "p": case "pcm_little_endian": return WaveFormatEncoding.Pcm;
            }
            return WaveFormatEncoding.Unknown;
        }

        #endregion

        #region Internal Endpoint Builders

        internal static Uri BuildEndpoint(this string address)
        {
            return BuildEndpoint(address, "");
        }

        internal static Uri BuildEndpoint(this string address, string path, params object[] args)
        {
            StringBuilder builder = new StringBuilder(address);
            if (!address.StartsWith("http://") && !address.StartsWith("https://"))
            {
                builder.Insert(0, "http://");
            }
            if (!address.EndsWith("/") && !path.StartsWith("/"))
            {
                if (args == null || args.Length == 0)
                {
                    builder.Append("/").Append(path);
                }
                else
                {
                    builder.Append("/").AppendFormat(path, args);
                }
            }
            else if (address.EndsWith("/") && path.StartsWith("/"))
            {
                if (args == null || args.Length == 0)
                {
                    builder.Append(path.Substring(1));
                }
                else
                {
                    builder.AppendFormat(path.Substring(1), args);
                }
            }
            else
            {
                if (args == null || args.Length == 0)
                {
                    builder.Append(path);
                }
                else
                {
                    builder.AppendFormat(path, args);
                }
            }
            Uri uri;
            if (Uri.TryCreate(builder.Replace("//", "/", 8, builder.Length - 8).ToString(), UriKind.Absolute, out uri))
            {
                return uri;
            }
            return null;
        }

        #endregion

        #region Internal Collection Functions
        
        internal static object GetFirst(this IDictionary dict)
        {
            if (dict.Count == 0)
                return null;

            foreach (var o in dict)
            {
                return o;
            }

            return null;
        }

        #endregion
    }
}

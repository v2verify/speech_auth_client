using System;
using System.Diagnostics;

namespace Dynamic.Speech
{
    public class SpeechLogger : ISpeechLogger
    {
        public void LogDebug(string msg, params object[] data)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                if (data.Length != 0)
                {
                    AppendToLog("[Debug]: " + string.Format(msg, data));
                }
                else
                {
                    AppendToLog("[Debug]: " + msg);
                }
            }
        }

        public void LogError(string msg, params object[] data)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                if (data.Length != 0)
                {
                    AppendToLog("[Error]: " + string.Format(msg, data));
                }
                else
                {
                    AppendToLog("[Error]: " + msg);
                }
            }
        }

        public void LogError(Exception ex)
        {
            AppendToLog("[Exception]: " + ex.Message.ToString().Trim());
            AppendToLog("[StackTrace]: " + ex.StackTrace.Trim());
        }

        public void LogError(Exception ex, string msg, params object[] data)
        {
            AppendToLog("[Exception]: " + ex.Message.ToString().Trim());
            AppendToLog("[StackTrace]: " + ex.StackTrace.Trim());
            if (!string.IsNullOrEmpty(msg))
            {
                if (data.Length != 0)
                {
                    AppendToLog("[Error]: " + string.Format(msg, data));
                }
                else
                {
                    AppendToLog("[Error]: " + msg);
                }
            }
        }

        public void LogInfo(string msg, params object[] data)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                if (data.Length != 0)
                {
                    AppendToLog("[Info]: " + string.Format(msg, data));
                }
                else
                {
                    AppendToLog("[Info]: " + msg);
                }
            }
        }

        private void AppendToLog(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                Console.WriteLine(msg);
                Debug.WriteLine(msg);
            }
        }
    }
}

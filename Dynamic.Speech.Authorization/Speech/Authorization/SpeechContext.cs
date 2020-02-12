
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using Newtonsoft.Json;

namespace Dynamic.Speech.Authorization
{
    [JsonObject]
    public class SpeechContext : ISpeechElement
    {
        public const uint SPEECH_FLAG_NONE = 0;
        public const uint SPEECH_FLAG_LIVENESS = 1;
        public const uint SPEECH_FLAG_IDENTITY = 2;
        public const uint SPEECH_FLAG_PIN = 3;
        public const uint SPEECH_FLAG_IDENTITY_PIN = 4;
        
        [Browsable(true)]
        [Description("The Name of the Speech Context")]
        [DisplayName("Name")]
        [Category("Basic")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [Browsable(true)]
        [Description("The Language Code of the Speech Context")]
        [DisplayName("Language Code")]
        [Category("Basic")]
        [JsonProperty("languageCode")]
        public string LanguageCode { get; set; }

        [Browsable(true)]
        [Description("The Grammar of the Speech Context")]
        [DisplayName("Grammar")]
        [Category("Basic")]
        [JsonProperty("grammar")]
        public string Grammar { get; set; }

        [Browsable(true)]
        [Description("The phrases to test against the language code")]
        [DisplayName("Phrases")]
        [Category("Basic")]
        [JsonProperty("phrases")]
        public List<string> Phrases { get; set; }

        [Browsable(true)]
        [Description("The flag code for the type of speech context this is")]
        [DisplayName("Flags")]
        [Category("Basic")]
        [JsonProperty("flags")]
        public uint Flags { get; set; }

        [Browsable(true)]
        [Description("Determines whether or not to include this Speech Context in the Liveness transaction")]
        [DisplayName("Enabled")]
        [Category("Basic")]
        [JsonIgnore]
        public bool Enabled { get; set; }

        public SpeechContext()
        {
            Name = null;
            Grammar = null;
            LanguageCode = "";
            Phrases = new List<string>();
            Flags = SPEECH_FLAG_NONE;
            Enabled = true;
        }

        public SpeechContext(string languageCode)
        {
            Name = null;
            Grammar = null;
            LanguageCode = languageCode;
            Phrases = new List<string>();
            Flags = SPEECH_FLAG_NONE;
            Enabled = true;
        }

        public SpeechContext(string languageCode, List<string> phrases)
        {
            Name = null;
            Grammar = null;
            LanguageCode = languageCode;
            Phrases = phrases;
            Flags = SPEECH_FLAG_NONE;
            Enabled = true;
        }

        public SpeechContext(string name, string languageCode, List<string> phrases)
        {
            Name = name;
            Grammar = null;
            LanguageCode = languageCode;
            Phrases = phrases;
            Flags = SPEECH_FLAG_NONE;
            Enabled = true;
        }

        public SpeechContext(string name, string grammar, string languageCode, List<string> phrases)
        {
            Name = name;
            Grammar = grammar;
            LanguageCode = languageCode;
            Phrases = phrases;
            Flags = SPEECH_FLAG_NONE;
            Enabled = true;
        }

        public SpeechContext(string name, string grammar, string languageCode, List<string> phrases, uint flags)
        {
            Name = name;
            Grammar = grammar;
            LanguageCode = languageCode;
            Phrases = phrases;
            Flags = flags;
            Enabled = true;
        }

        public SpeechContext Clone()
        {
            SpeechContext context = new SpeechContext();

            context.Name = Name;
            context.Grammar = Grammar;
            context.LanguageCode = LanguageCode;
            context.Phrases = new List<string>(Phrases);
            context.Flags = Flags;
            context.Enabled = Enabled;

            return context;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrEmpty(Name))
            {
                builder.Append("[Name: " + Name + ", ");
            }
            if (!string.IsNullOrEmpty(Grammar))
            {
                if (builder.Length == 0)
                {
                    builder.Append("[Grammar: " + Grammar + ", ");
                }
                else
                {
                    builder.Append("Grammar: " + Grammar + ", ");
                }
            }
            if (builder.Length == 0)
            {
                return "[LanguageCode: " + LanguageCode + ", Phrases: " + string.Join(",", Phrases.ToArray()) + "]";
            }
            else
            {
                builder.Append("LanguageCode: " + LanguageCode + ", Phrases: " + string.Join(",", Phrases.ToArray()) + "]");
                return builder.ToString();
            }
        }

        public void Dispose()
        {
        }
    }
}

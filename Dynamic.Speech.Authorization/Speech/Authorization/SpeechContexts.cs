
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Dynamic.Speech.Authorization
{
    public class SpeechContexts : IEnumerable<SpeechContext>, IEnumerable
    {
        private List<SpeechContext> mSpeechContexts;

        public SpeechContexts()
        {
            mSpeechContexts = new List<SpeechContext>();
        }

        public SpeechContexts(int capacity)
        {
            mSpeechContexts = new List<SpeechContext>(capacity);
        }

        public SpeechContexts(IEnumerable<SpeechContext> collection)
        {
            mSpeechContexts = new List<SpeechContext>(collection);
        }

        public int Count { get { return mSpeechContexts.Count; } }

        public void Add(SpeechContext speechContext)
        {
            mSpeechContexts.Add(speechContext);
        }

        public void Add(string languageCode, string phrase)
        {
            mSpeechContexts.Add(new SpeechContext(languageCode, new List<string> { phrase }));
        }

        public void Add(string languageCode, List<string> phrases)
        {
            mSpeechContexts.Add(new SpeechContext(languageCode, phrases));
        }

        public void Add(string name, string languageCode, List<string> phrases)
        {
            mSpeechContexts.Add(new SpeechContext(name, languageCode, phrases));
        }

        public void Add(string name, string grammar, string languageCode, List<string> phrases)
        {
            mSpeechContexts.Add(new SpeechContext(name, grammar, languageCode, phrases));
        }

        public void Add(string name, string grammar, string languageCode, List<string> phrases, uint flags)
        {
            mSpeechContexts.Add(new SpeechContext(name, grammar, languageCode, phrases, flags));
        }

        public void Clear()
        {
            mSpeechContexts.Clear();
        }

        public SpeechContexts Clone()
        {
            SpeechContexts contexts = new SpeechContexts(Count);

            foreach (var context in this)
            {
                contexts.Add(context.Clone());
            }

            return contexts;
        }
        
        public IEnumerator<SpeechContext> GetEnumerator()
        {
            return mSpeechContexts.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var elem in this)
            {
                builder.Append(elem.ToString());
            }
            return builder.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mSpeechContexts.GetEnumerator();
        }
    }
}

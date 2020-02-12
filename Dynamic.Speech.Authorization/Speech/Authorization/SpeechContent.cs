using System;
using System.Collections;
using System.Collections.Generic;

namespace Dynamic.Speech.Authorization
{
    public interface ISpeechElement : IDisposable { }

    public sealed class SpeechElement : IDisposable
    {
        public string Name { get; private set; }
        public ISpeechElement Value { get; set; }

        public SpeechElement(string name, ISpeechElement value)
        {
            Name = name;
            Value = value;
        }

        public void Dispose()
        {
            Value.Dispose();
        }
    }

    public sealed class SpeechContent : IEnumerable<SpeechElement>, IEnumerable
    {
        private List<SpeechElement> mList;

        public SpeechContent()
        {
            mList = new List<SpeechElement>();
        }

        public SpeechContent(int capacity)
        {
            mList = new List<SpeechElement>(capacity);
        }

        public SpeechContent(IEnumerable<SpeechElement> collection)
        {
            if (collection != null)
            {
                mList = new List<SpeechElement>(collection);
            }
            else
            {
                mList = new List<SpeechElement>();
            }
        }

        public SpeechContent(IDictionary<string, ISpeechElement> elements)
        {
            mList = new List<SpeechElement>();
            if (elements != null && elements.Count != 0)
            {
                foreach (var elem in elements)
                {
                    Add(elem.Key, elem.Value);
                }
            }
        }

        public void Add(string name, ISpeechElement value)
        {
            mList.Add(new SpeechElement(name, value));
        }

        public void Remove(string name)
        {
            mList.RemoveAll(item => item.Name == name);
        }

        public void Remove(string name, object value)
        {
            foreach (var item in mList)
            {
                if (item.Name.Equals(name) && item.Value.Equals(value))
                {
                    mList.Remove(item);
                    return;
                }
            }
        }

        public void Clear()
        {
            foreach(var element in mList)
            {
                element.Dispose();
            }
            mList.Clear();
        }

        public ISpeechElement this[string name]
        {
            get
            {
                foreach (var item in mList)
                {
                    if (item.Name.Equals(name))
                    {
                        return item.Value;
                    }
                }

                // Element wasnt found
                var itm = new SpeechElement(name, null);
                mList.Add(itm);
                return itm.Value;
            }

            set
            {
                foreach (var item in mList)
                {
                    if (item.Name.Equals(name))
                    {
                        item.Value = value;
                        return;
                    }
                }

                // Element wasnt found
                Add(name, value);
            }
        }

        public SpeechElement this[int index]
        {
            get
            {
                return mList[index];
            }

            set
            {
                mList[index] = value;
            }
        }

        public int Count { get { return mList.Count; } }

        public IEnumerator<SpeechElement> GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        public SpeechElement First
        {
            get
            {
                if (mList.Count == 0)
                    return null;

                return mList[0];
            }
        }

        public SpeechElement Last
        {
            get
            {
                if (mList.Count == 0)
                    return null;

                return mList[mList.Count - 1];
            }
        }

        public List<SpeechElement> ToList()
        {
            return mList;
        }

        public SpeechContent Clone()
        {
            return new SpeechContent(mList);
        }

        public IEnumerable ToKeys()
        {
            return new KeyEnumerator(this);
        }

        public IEnumerable ToValues()
        {
            return new ValueEnumerator(this);
        }

        public struct KeyEnumerator : IEnumerator<string>, IDisposable, IEnumerator, IEnumerable
        {
            private SpeechContent mContent;
            private IEnumerator<SpeechElement> mEnumerator;

            internal KeyEnumerator(SpeechContent content)
            {
                mContent = content;
                mEnumerator = content.GetEnumerator();
                Current = null;
            }

            public string Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                mEnumerator = null;
            }

            public IEnumerator GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (mEnumerator.MoveNext())
                {
                    Current = mEnumerator.Current.Name;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                mEnumerator = mContent.GetEnumerator();
                Current = null;
            }
        }

        public struct ValueEnumerator : IEnumerator<ISpeechElement>, IDisposable, IEnumerator, IEnumerable
        {
            private SpeechContent mContent;
            private IEnumerator<SpeechElement> mEnumerator;

            internal ValueEnumerator(SpeechContent content)
            {
                mContent = content;
                mEnumerator = content.GetEnumerator();
                Current = null;
            }

            public ISpeechElement Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                mEnumerator = null;
            }

            public IEnumerator GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (mEnumerator.MoveNext())
                {
                    Current = mEnumerator.Current.Value;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                mEnumerator = mContent.GetEnumerator();
                Current = null;
            }
        }
    }
}

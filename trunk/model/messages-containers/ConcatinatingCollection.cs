using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint.MessagesContainers
{
    public abstract class ConcatinatingCollection : IMessagesCollection
    {
        protected abstract IEnumerable<IMessagesCollection> GetCollectionsToConcat();
        protected abstract IEnumerable<IMessagesCollection> GetCollectionsToConcatReverse();

        static bool MoveToNextNotEmptyCollection(IEnumerator<IMessagesCollection> e)
        {
            for (; ; )
            {
                if (!e.MoveNext())
                    return false;
                if (e.Current.Count == 0)
                    continue;
                return true;
            }
        }

        #region ILinesCollection Members

        public int Count
        {
            get
            {
                int ret = 0;
                foreach (IMessagesCollection c in GetCollectionsToConcat())
                    ret += c.Count;
                return ret;
            }
        }

        public IEnumerable<IndexedMessage> Forward(int startPos, int endPosition)
        {
            using (IEnumerator<IMessagesCollection> e = GetCollectionsToConcat().GetEnumerator())
            {
                if (!MoveToNextNotEmptyCollection(e))
                    yield break;
                startPos = Math.Max(startPos, 0);
                int pos = startPos;
                for (; startPos < endPosition;)
                {
                    for (; pos >= e.Current.Count;)
                    {
                        pos -= e.Current.Count;
                        if (!MoveToNextNotEmptyCollection(e))
                            yield break;
                    }
                    IMessagesCollection currentCollection = e.Current;
                    foreach (IndexedMessage l in currentCollection.Forward(pos, int.MaxValue))
                    {
                        if (startPos >= endPosition)
                            break;
                        yield return new IndexedMessage(startPos, l.Message);
                        startPos++;
                        pos++;
                    }
                }
            }
        }

        public IEnumerable<IndexedMessage> Reverse(int startPos, int endPosition)
        {
            IEnumerable<IMessagesCollection> colls = GetCollectionsToConcatReverse();
            int count = 0;
            foreach (IMessagesCollection c in colls)
                count += c.Count;
            int maxPos = count - 1;
            using (IEnumerator<IMessagesCollection> e = colls.GetEnumerator())
            {
                if (!MoveToNextNotEmptyCollection(e))
                    yield break;

                int revStartPos = maxPos - Math.Min(startPos, count - 1);
                int revEndPosition = maxPos - Math.Max(endPosition, -1);

                int pos = revStartPos;
                for (; revStartPos < revEndPosition;)
                {
                    for (; pos >= e.Current.Count;)
                    {
                        pos -= e.Current.Count;
                        if (!MoveToNextNotEmptyCollection(e))
                            yield break;
                    }
                    foreach (IndexedMessage l in e.Current.Reverse(e.Current.Count - 1 - pos, -1))
                    {
                        if (revStartPos >= revEndPosition)
                            break;
                        yield return new IndexedMessage(maxPos - revStartPos, l.Message);
                        revStartPos++;
                        pos++;
                    }
                }
            }
        }

        #endregion
    };
}

using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.MessagesContainers
{
    public abstract class MergingCollection : IMessagesCollection
    {
        protected abstract IEnumerable<IMessagesCollection> GetCollectionsToMerge();
        protected virtual void Lock()
        {
        }
        protected virtual void Unlock()
        {
        }

        int IMessagesCollection.Count
        {
            get
            {
                Lock();
                try
                {
                    int ret = 0;
                    foreach (IMessagesCollection c in GetCollectionsToMerge())
                        ret += c.Count;
                    return ret;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        class QueueEntriesComparer : IComparer<QueueEntry>
        {
            readonly int directionSign;

            public QueueEntriesComparer(bool reverse) { directionSign = reverse ? -1 : 1; }

            public int Compare(QueueEntry x, QueueEntry y)
            {
                return directionSign * MessagesComparer.Compare(x.enumerator.Current.Message, y.enumerator.Current.Message);
            }
        };

        struct QueueEntry
        {
            public IEnumerator<IndexedMessage> enumerator;
            public IMessagesCollection collection;

            public QueueEntry(IEnumerator<IndexedMessage> enumerator, IMessagesCollection collection)
            {
                this.enumerator = enumerator;
                this.collection = collection;
            }
        };

        IEnumerable<IndexedMessage> IMessagesCollection.Forward(int startPos, int endPosition)
        {
            return Forward(startPos, endPosition).Select(x => x.Message);
        }

        public IEnumerable<MergingCollectionEntry> Forward(int startPos, int endPosition)
        {
            Lock();
            try
            {
                int totalCount = 0;
                var queueComparer = new QueueEntriesComparer(reverse: false);
                var queue = new PriorityQueue<QueueEntry, QueueEntry>(queueComparer);
                try
                {
                    int collectionsCount = 0;
                    foreach (IMessagesCollection l in GetCollectionsToMerge())
                    {
                        ++collectionsCount;
                        int localCount = l.Count;
                        totalCount += localCount;
                        IEnumerator<IndexedMessage> i = l.Forward(0, localCount).GetEnumerator();
                        if (i.MoveNext())
                        {
                            var entry = new QueueEntry(i, l);
                            queue.Enqueue(entry, entry);
                        }
                    }
                    startPos = RangeUtils.PutInRange(0, totalCount, startPos);
                    endPosition = RangeUtils.PutInRange(0, totalCount, endPosition);

                    if (collectionsCount == 1) // optimized version for the case when there is only one collection to merge
                    {
                        if (endPosition > 0)
                        {
                            var entry = queue.Dequeue();
                            using IEnumerator<IndexedMessage> i = entry.enumerator;
                            for (int idx = 0; idx < endPosition; ++idx)
                            {
                                if (idx >= startPos)
                                    yield return new MergingCollectionEntry(
                                        new IndexedMessage(idx, i.Current.Message), entry.collection, i.Current.Index);
                                if (!i.MoveNext())
                                    break;
                            }
                        }
                    }
                    else
                    {
                        for (int idx = 0; idx < endPosition; ++idx)
                        {
                            var entry = queue.Dequeue();
                            var i = entry.enumerator;
                            try
                            {
                                if (idx >= startPos)
                                    yield return new MergingCollectionEntry(
                                        new IndexedMessage(idx, i.Current.Message), entry.collection, i.Current.Index);
                                if (i.MoveNext())
                                {
                                    queue.Enqueue(entry, entry);
                                    i = null;
                                }
                            }
                            finally
                            {
                                if (i != null)
                                    i.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    while (queue.Count != 0)
                        queue.Dequeue().enumerator.Dispose();
                }
            }
            finally
            {
                Unlock();
            }
        }


        IEnumerable<IndexedMessage> IMessagesCollection.Reverse(int startPos, int endPosition)
        {
            return Reverse(startPos, endPosition).Select(x => x.Message);
        }

        public IEnumerable<MergingCollectionEntry> Reverse(int startPos, int endPosition)
        {
            Lock();
            try
            {
                var queueComparer = new QueueEntriesComparer(reverse: true);
                var queue = new PriorityQueue<QueueEntry, QueueEntry>(queueComparer);
                try
                {
                    int collectionsCount = 0;
                    int c = 0;
                    foreach (IMessagesCollection l in GetCollectionsToMerge())
                    {
                        ++collectionsCount;
                        int lc = l.Count;
                        c += lc;
                        IEnumerator<IndexedMessage> i = l.Reverse(lc - 1, -1).GetEnumerator();
                        if (i.MoveNext())
                        {
                            var entry = new QueueEntry(i, l);
                            queue.Enqueue(entry, entry);
                        }
                    }
                    startPos = RangeUtils.PutInRange(-1, c - 1, startPos);
                    endPosition = RangeUtils.PutInRange(-1, c - 1, endPosition);
                    for (int idx = c - 1; idx > endPosition; --idx)
                    {
                        var entry = queue.Dequeue();
                        var i = entry.enumerator;
                        try
                        {
                            if (idx <= startPos)
                                yield return new MergingCollectionEntry(
                                    new IndexedMessage(idx, i.Current.Message), entry.collection, i.Current.Index);
                            if (i.MoveNext())
                            {
                                queue.Enqueue(entry, entry);
                                i = null;
                            }
                        }
                        finally
                        {
                            if (i != null)
                                i.Dispose();
                        }
                    }
                }
                finally
                {
                    while (queue.Count != 0)
                        queue.Dequeue().enumerator.Dispose();
                }
            }
            finally
            {
                Unlock();
            }
        }
    }

    public struct MergingCollectionEntry
    {
        public readonly IndexedMessage Message;
        public readonly IMessagesCollection SourceCollection;
        public readonly int SourceIndex;

        public MergingCollectionEntry(IndexedMessage m, IMessagesCollection source, int sourceIndex)
        {
            this.Message = m;
            this.SourceCollection = source;
            this.SourceIndex = sourceIndex;
        }
    };

    public class SimpleMergingCollection : MergingCollection
    {
        IEnumerable<IMessagesCollection> collections;

        public SimpleMergingCollection(IEnumerable<IMessagesCollection> collections)
        {
            this.collections = collections;
        }

        protected override IEnumerable<IMessagesCollection> GetCollectionsToMerge()
        {
            return collections;
        }
    }

    public class SimpleCollection : IMessagesCollection
    {
        protected List<IMessage> list;

        public SimpleCollection(List<IMessage> list)
        {
            this.list = list;
        }

        IEnumerable<IndexedMessage> IMessagesCollection.Forward(int begin, int end)
        {
            for (var i = begin; i != end; ++i)
            {
                yield return new IndexedMessage(i, list[i]);
            }
        }
        IEnumerable<IndexedMessage> IMessagesCollection.Reverse(int begin, int end)
        {
            for (var i = begin; i != end; --i)
            {
                yield return new IndexedMessage(i, list[i]);
            }
        }
        int IMessagesCollection.Count
        {
            get { return list.Count; }
        }
    };
}

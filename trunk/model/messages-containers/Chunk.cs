using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LogJoint.MessagesContainers
{
    [DebuggerDisplay("{GetHashCode()}")]
    sealed class Chunk : IMessagesCollection
    {
        public const int MaxChunkSize = 256;

        public void Add(IMessage l)
        {
            if (size >= MaxChunkSize)
                throw new InvalidOperationException("Can't add to the chunk because it is full");

            if (l == null)
                throw new ArgumentNullException("l");

            data[size] = l;
            ++size;
        }

        public bool IsFull { get { return size == MaxChunkSize; } }

        public IMessage First { get { return data[0]; } }
        public IMessage Last { get { return data[size - 1]; } }

        public void TrimLeft(int pos)
        {
            IMessage[] tmp = new IMessage[MaxChunkSize];
            int sz = size - pos;
            Array.Copy(data, pos, tmp, 0, sz);
            data = tmp;
            size = (short)sz;
        }

        public void TrimRight(int pos)
        {
            int sz = pos + 1;
            Array.Clear(data, sz, MaxChunkSize - sz);
            size = (short)sz;
        }

        public void SetLast(IMessage msg)
        {
            data[size - 1] = msg;
        }

        IMessage[] data = new IMessage[MaxChunkSize];
        short size;

        internal Chunk prev;
        internal Chunk next;

        #region ILinesCollection Members

        public int Count
        {
            get { return (int)size; }
        }

        public IEnumerable<IndexedMessage> Forward(int begin, int end)
        {
            begin = RangeUtils.PutInRange(0, size, begin);
            end = RangeUtils.PutInRange(0, size, end);
            for (int i = begin; i < end; ++i)
            {
                yield return new IndexedMessage(i, data[i]);
            }
        }

        public IEnumerable<IndexedMessage> Reverse(int begin, int end)
        {
            begin = RangeUtils.PutInRange(-1, size - 1, begin);
            end = RangeUtils.PutInRange(-1, size - 1, end);
            for (int i = begin; i > end; --i)
            {
                yield return new IndexedMessage(i, data[i]);
            }
        }

        #endregion
    }
}

﻿using LogJoint.Postprocessing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint
{
    public struct DejitteringParams
    {
        public int JitterBufferSize;

        public DejitteringParams(XElement configNode)
        {
            if (configNode == null)
                throw new ArgumentNullException("configNode");
            var a = configNode.Attribute("jitter-buffer-size");
            if (a == null || !int.TryParse(a.Value, out JitterBufferSize))
                JitterBufferSize = 16;
        }
        static public DejitteringParams? FromConfigNode(XElement configNode)
        {
            if (configNode == null)
                return null;
            return new DejitteringParams(configNode);
        }
    };

    /// <summary>
    /// Implementation of IAsyncEnumerable<PostprocessedMessage> that mitigates 'partially-sorted-log' problem.
    /// 'partially-sorted-log' problem has to do with logs that are mostly sorded by time but
    /// there might be little defects where several messages have incorrect order. Bad order 
    /// may be a result of bad logic in multithreded log writer. Well known example of a log
    /// having 'partially-sorted-log' problem is Windows Event Log.
    /// DejitteringMessagesParser is a transparent wrapper for the underlying IAsyncEnumerable<PostprocessedMessage>.
    /// Logically DejitteringMessagesParser implements the following idea: when client reads Nth message 
    /// a range of messages is actually read (N - jitterBufferSize/2, N + jitterBufferSize/2). 
    /// This range is sorded by time and the message in the middle of the range is 
    /// returned as Nth message. DejitteringMessagesParser is optimized for sequential reading.
    /// </summary>
    public class DejitteringMessagesParser : IAsyncDisposable
    {
        public static IAsyncEnumerable<PostprocessedMessage> Create(Func<CreateParserParams, IAsyncEnumerable<PostprocessedMessage>> underlyingParserFactory,
            CreateParserParams originalParams, DejitteringParams config)
        {
            return Create(underlyingParserFactory, originalParams, config.JitterBufferSize);
        }

        public static async IAsyncEnumerable<PostprocessedMessage> Create(Func<CreateParserParams, IAsyncEnumerable<PostprocessedMessage>> underlyingParserFactory,
            CreateParserParams originalParams, int jitterBufferSize)
        {
            await using var parser = new DejitteringMessagesParser(originalParams, jitterBufferSize);
            await parser.CreateUnderlyingParserAndInitJitterBuffer(underlyingParserFactory);
            for (; ; )
            {
                PostprocessedMessage message = await parser.ReadNextAndPostprocess();
                if (message.Message == null)
                    break;
                yield return message;
            }
        }


        private DejitteringMessagesParser(CreateParserParams originalParams, int jitterBufferSize)
        {
            if (jitterBufferSize < 1)
                throw new ArgumentException("jitterBufferSize must be equal to or geater than 1");
            if (originalParams.Range == null)
                throw new ArgumentNullException("DejitteringMessagesParser does not support unspecified positions range", "originalParams.Range");

            this.originalParams = originalParams;
            this.originalParams.EnsureStartPositionIsInRange();

            this.jitterBufferSize = jitterBufferSize;
            this.jitterBuffer = new VCSKicksCollection.PriorityQueue<Entry>(new Comparer(originalParams.Direction, jitterBufferSize));
            this.positionsBuffer = new Generic.CircularBuffer<MessagesPositions>(jitterBufferSize + 1);
        }

        private async ValueTask<PostprocessedMessage> ReadNextAndPostprocess()
        {
            CheckDisposed();
            for (; ; )
            {
                var ret = jitterBuffer.Dequeue();
                if (ret.data.Message != null)
                {
                    var positions = positionsBuffer.Pop();
                    ret.data.Message.SetPosition(positions.Position, positions.EndPosition);
                    if (currentIndex - ret.index > jitterBufferSize + 2)
                    {
                        continue;
                    }
                    if (!originalParams.Range.Value.IsInRange(ret.data.Message.Position))
                    {
                        return new PostprocessedMessage();
                    }
                }
                await LoadNextMessage();
                return ret.data;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (disposed)
                return;
            disposed = true;
            await enumerator.Dispose();
        }

        class Comparer : IComparer<Entry>
        {
            readonly int inversionFlag;
            readonly long bufferSize;

            public Comparer(MessagesParserDirection direction, int bufferSize)
            {
                this.inversionFlag = direction == MessagesParserDirection.Forward ? 1 : -1;
                this.bufferSize = bufferSize;
            }

            int IComparer<Entry>.Compare(Entry e1, Entry e2)
            {
                int cmpResult;

                long idxDiff = e1.index - e2.index;
                if (Math.Abs(idxDiff) > bufferSize)
                    return Math.Sign(idxDiff);

                var x = e1.data;
                var y = e2.data;

                cmpResult = inversionFlag * MessageTimestamp.Compare(x.Message.Time, y.Message.Time);
                if (cmpResult != 0)
                    return cmpResult;

                cmpResult = inversionFlag * Math.Sign(x.Message.Position - y.Message.Position);
                return cmpResult;
            }
        };

        void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        static MessagesParserDirection GetOppositeDirection(MessagesParserDirection direction)
        {
            return direction == MessagesParserDirection.Backward ? MessagesParserDirection.Forward : MessagesParserDirection.Backward;
        }

        async Task CreateUnderlyingParserAndInitJitterBuffer(Func<CreateParserParams, IAsyncEnumerable<PostprocessedMessage>> underlyingParserFactory)
        {
            CreateParserParams reversedParserParams = originalParams;
            reversedParserParams.Range = null;
            reversedParserParams.Direction = GetOppositeDirection(originalParams.Direction);
            reversedParserParams.Flags |= MessagesParserFlag.DisableMultithreading;

            int reversedMessagesQueued = 0;

            await using (IAsyncEnumerator<PostprocessedMessage> reversedParser = underlyingParserFactory(reversedParserParams).GetAsyncEnumerator())
            {
                var tmp = new List<PostprocessedMessage>();
                for (int i = 0; i < jitterBufferSize; ++i)
                {
                    if (!await reversedParser.MoveNextAsync())
                        break;
                    tmp.Add(reversedParser.Current);
                }
                tmp.Reverse();
                foreach (var tmpMsg in tmp)
                {
                    jitterBuffer.Enqueue(new Entry() { data = tmpMsg, index = currentIndex++ });
                    positionsBuffer.Push(new MessagesPositions(tmpMsg.Message));
                    ++reversedMessagesQueued;
                }
            };

            enumerator = await ReadAddMessagesFromRangeCompleteJitterBuffer(underlyingParserFactory).GetEnumerator();
            for (int i = 0; i < jitterBufferSize; ++i)
            {
                var tmp = await LoadNextMessage();
                reversedMessagesQueued -= tmp.DequeuedMessages;
                if (tmp.LoadedMessage == null)
                    break;
            }
            for (int i = 0; i < reversedMessagesQueued && jitterBuffer.Count > 0; ++i)
            {
                jitterBuffer.Dequeue();
                positionsBuffer.Pop();
            }
        }

        IEnumerableAsync<PostprocessedMessage> ReadAddMessagesFromRangeCompleteJitterBuffer(
            Func<CreateParserParams, IAsyncEnumerable<PostprocessedMessage>> underlyingParserFactory)
        {
            return EnumerableAsync.Produce<PostprocessedMessage>(async yieldAsync =>
            {
                CreateParserParams mainParserParams = originalParams;
                //mainParserParams.Range = null;
                await foreach (PostprocessedMessage msg in underlyingParserFactory(mainParserParams))
                {
                    if (!await yieldAsync.YieldAsync(msg))
                        break;
                }

                CreateParserParams jitterBufferCompletionParams = originalParams;
                jitterBufferCompletionParams.Flags |= MessagesParserFlag.DisableMultithreading;
                jitterBufferCompletionParams.Range = null;
                jitterBufferCompletionParams.StartPosition = originalParams.Direction == MessagesParserDirection.Forward ? originalParams.Range.Value.End : originalParams.Range.Value.Begin;
                await using (var completionParser = underlyingParserFactory(jitterBufferCompletionParams).GetAsyncEnumerator())
                {
                    for (int i = 0; i < jitterBufferSize; ++i)
                    {
                        if (!await completionParser.MoveNextAsync())
                            break;
                        if (!await yieldAsync.YieldAsync(completionParser.Current))
                            break;
                    }
                }
            });
        }

        struct LoadNextMessageResult
        {
            public IMessage LoadedMessage;
            public int DequeuedMessages;
        };

        async ValueTask<LoadNextMessageResult> LoadNextMessage()
        {
            LoadNextMessageResult ret = new LoadNextMessageResult();
            if (eofReached)
                return ret;
            if (!await enumerator.MoveNext())
            {
                eofReached = true;
            }
            else
            {
                var tmp = enumerator.Current;
                ret.LoadedMessage = tmp.Message;
                jitterBuffer.Enqueue(new Entry() { data = tmp, index = currentIndex++ });
                positionsBuffer.Push(new MessagesPositions(tmp.Message));
                if (jitterBuffer.Count > jitterBufferSize)
                {
                    jitterBuffer.Dequeue();
                    positionsBuffer.Pop();
                    ret.DequeuedMessages = 1;
                }
            }
            return ret;
        }

        struct Entry
        {
            public PostprocessedMessage data;
            public long index;
        };

        struct MessagesPositions
        {
            public readonly long Position;
            public readonly long EndPosition;
            public MessagesPositions(IMessage msg)
            {
                this.Position = msg.Position;
                this.EndPosition = msg.EndPosition;
            }
        };

        readonly CreateParserParams originalParams;
        readonly VCSKicksCollection.PriorityQueue<Entry> jitterBuffer;
        readonly Generic.CircularBuffer<MessagesPositions> positionsBuffer;
        readonly int jitterBufferSize;
        IEnumeratorAsync<PostprocessedMessage> enumerator;
        long currentIndex;
        bool eofReached;
        bool disposed;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using LogJoint.MessagesContainers;

namespace LogJoint
{
    public static class PositionedMessagesUtils
    {

        public static async Task<long> NormalizeMessagePosition(IPositionedMessagesReader reader,
            long position)
        {
            if (position == reader.BeginPosition)
                return position;
            IMessage m = await ReadNearestMessage(reader, position,
                ReadMessagesFlag.HintMessageTimeIsNotNeeded | ReadMessagesFlag.HintMessageContentIsNotNeeed);
            if (m != null)
                return m.Position;
            return reader.EndPosition;
        }

        public static async Task<long?> FindNextMessagePosition(IPositionedMessagesReader reader,
            long originalMessagePos)
        {
            // Validate the input.
            if (originalMessagePos < reader.BeginPosition)
                return null;
            if (originalMessagePos >= reader.EndPosition)
                return null;
            int msgIndex = 0;
            await foreach (PostprocessedMessage msg in reader.Read(new ReadMessagesParams(originalMessagePos,
                null, ReadMessagesFlag.HintMessageContentIsNotNeeed | ReadMessagesFlag.HintMessageTimeIsNotNeeded,
                ReadMessagesDirection.Forward, null)))
            {
                if (msgIndex == 1)
                    return msg.Message.Position;
                ++msgIndex;
            }
            return null;
        }

        public static async Task<long?> FindPrevMessagePosition(IPositionedMessagesReader reader,
            long originalMessagePos)
        {
            long nextMessagePos = reader.EndPosition;
            await foreach (PostprocessedMessage msgAtOriginalPos in reader.Read(new ReadMessagesParams(originalMessagePos, null,
                ReadMessagesFlag.HintMessageContentIsNotNeeed | ReadMessagesFlag.HintMessageContentIsNotNeeed,
                ReadMessagesDirection.Forward)))
            {
                nextMessagePos = msgAtOriginalPos.Message.Position;
                break;
            }
            await foreach (PostprocessedMessage msg in reader.Read(new ReadMessagesParams(nextMessagePos, null,
                ReadMessagesFlag.HintMessageContentIsNotNeeed | ReadMessagesFlag.HintMessageContentIsNotNeeed,
                ReadMessagesDirection.Backward)))
            {
                return msg.Message.Position;
            }
            return null;
        }

        public static async Task<MessageTimestamp?> ReadNearestMessageTimestamp(IPositionedMessagesReader reader, long position)
        {
            IMessage m = await ReadNearestMessage(reader, position, ReadMessagesFlag.HintMessageContentIsNotNeeed);
            if (m != null)
                return m.Time;
            return null;
        }

        /// <summary>
        /// Finds the first and the last available messages in the reader.
        /// </summary>
        /// <param name="reader">Messages reader to read from</param>
        /// <param name="cachedFirstMessage">When the caller passes non-null value
        /// the function doesn't search for the first message in the reader and return the
        /// value precalculated by the client instead. That can be user for optimization:
        /// if the client is sure that the first message didn't change then it can
        /// pass the value calculated before. If <paramref name="firstMessage"/> is <value>null</value>
        /// the function will search for the first message in the reader.</param>
        /// <param name="firstMessage">When the function returns <paramref name="firstMessage"/> receives 
        /// the message with the smallest available position.</param>
        /// <param name="lastMessage">When the function returns 
        /// <paramref name="lastMessage"/> receives the message with the largest available position.</param>
        public static async Task<(IMessage firstMessage, IMessage lastMessage)> GetBoundaryMessages(
            IPositionedMessagesReader reader,
            IMessage cachedFirstMessage)
        {
            IMessage firstMessage, lastMessage;
            if (cachedFirstMessage == null)
            {
                firstMessage = await ReadNearestMessage(reader, reader.BeginPosition);
            }
            else
            {
                firstMessage = cachedFirstMessage;
            }

            lastMessage = firstMessage;

            await foreach (var tmp in reader.Read(new ReadMessagesParams(reader.EndPosition,
                null, ReadMessagesFlag.Default, ReadMessagesDirection.Backward)))
            {
                lastMessage = tmp.Message;
                break;
            }
            return (firstMessage, lastMessage);
        }

        public static async Task<long> LocateDateBound(IPositionedMessagesReader reader, DateTime date, ValueBound bound)
        {
            return await LocateDateBound(reader, date, bound, CancellationToken.None);
        }

        public static async Task<long> LocateDateBound(IPositionedMessagesReader reader, DateTime date, ValueBound bound, CancellationToken cancellation)
        {
            var d = new MessageTimestamp(date);

            long begin = reader.BeginPosition;
            long end = reader.EndPosition;

            long pos = begin;
            long count = end - begin;

            for (; 0 < count;)
            {
                long count2 = count / 2;

                MessageTimestamp d2 = (await ReadNearestMessageTimestamp(reader, pos + count2)).GetValueOrDefault(MessageTimestamp.MaxValue);
                bool moveRight = false;
                switch (bound)
                {
                    case ValueBound.Lower:
                    case ValueBound.UpperReversed:
                        moveRight = d2 < d;
                        break;
                    case ValueBound.Upper:
                    case ValueBound.LowerReversed:
                        moveRight = d2 <= d;
                        break;
                }
                if (moveRight)
                {
                    pos += count2 + 1;
                    count -= count2 + 1;
                }
                else
                {
                    count = count2;
                }

                cancellation.ThrowIfCancellationRequested();
            }

            if (bound == ValueBound.LowerReversed || bound == ValueBound.UpperReversed)
            {
                long? tmp = await FindPrevMessagePosition(reader, pos);
                if (tmp == null)
                    return begin - 1;
                pos = tmp.Value;
            }

            return pos;
        }

        static public Task<IMessage> ReadNearestMessage(IPositionedMessagesReader reader, long position)
        {
            return ReadNearestMessage(reader, position, ReadMessagesFlag.Default);
        }

        static public async Task<IMessage> ReadNearestMessage(IPositionedMessagesReader reader, long position, ReadMessagesFlag flags)
        {
            await foreach (var msg in reader.Read(new ReadMessagesParams(position, null, flags, ReadMessagesDirection.Forward)))
            {
                return msg.Message;
            }
            return null;
        }
    }
}

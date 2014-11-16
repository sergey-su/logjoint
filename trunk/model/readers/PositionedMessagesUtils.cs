using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LogJoint
{
	public static class PositionedMessagesUtils
	{

		public static long NormalizeMessagePosition(IPositionedMessagesReader reader,
			long position)
		{
			if (position == reader.BeginPosition)
				return position;
			IMessage m = ReadNearestMessage(reader, position,
				MessagesParserFlag.HintMessageTimeIsNotNeeded | MessagesParserFlag.HintMessageContentIsNotNeeed);
			if (m != null)
				return m.Position;
			return reader.EndPosition;
		}

		public static long? FindNextMessagePosition(IPositionedMessagesReader reader,
			long originalMessagePos)
		{
			// Validate the input.
			if (originalMessagePos < reader.BeginPosition)
				return null;
			if (originalMessagePos >= reader.EndPosition)
				return null;
			using (IPositionedMessagesParser parser = reader.CreateParser(new CreateParserParams(originalMessagePos,
				null, MessagesParserFlag.HintMessageContentIsNotNeeed | MessagesParserFlag.HintMessageTimeIsNotNeeded, 
				MessagesParserDirection.Forward, null)))
			{
				if (parser.ReadNext() == null)
					return null;
				IMessage p = parser.ReadNext();
				if (p == null)
					return null;
				return p.Position;
			}
		}

		public static long? FindPrevMessagePosition(IPositionedMessagesReader reader,
			long originalMessagePos)
		{
			long nextMessagePos;
			using (IPositionedMessagesParser p = reader.CreateParser(new CreateParserParams(originalMessagePos, null,
				MessagesParserFlag.HintMessageContentIsNotNeeed | MessagesParserFlag.HintMessageContentIsNotNeeed,
				MessagesParserDirection.Forward)))
			{
				var msgAtOriginalPos = p.ReadNext();
				if (msgAtOriginalPos != null)
					nextMessagePos = msgAtOriginalPos.Position;
				else
					nextMessagePos = reader.EndPosition;
			}
			using (IPositionedMessagesParser p = reader.CreateParser(new CreateParserParams(nextMessagePos, null,
				MessagesParserFlag.HintMessageContentIsNotNeeed | MessagesParserFlag.HintMessageContentIsNotNeeed,
				MessagesParserDirection.Backward)))
			{
				IMessage msg = p.ReadNext();
				if (msg != null)
					return msg.Position;
				return null;
			}
		}

		public static MessageTimestamp ReadNearestMessageTimestamp(IPositionedMessagesReader reader, long position)
		{
			IMessage m = ReadNearestMessage(reader, position, MessagesParserFlag.HintMessageContentIsNotNeeed);
			if (m != null)
				return m.Time;
			return MessageTimestamp.MinValue;
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
		public static void GetBoundaryMessages(
			IPositionedMessagesReader reader,
			IMessage cachedFirstMessage,
			out IMessage firstMessage, out IMessage lastMessage)
		{
			if (cachedFirstMessage == null)
			{
				firstMessage = ReadNearestMessage(reader, reader.BeginPosition);
			}
			else
			{
				firstMessage = cachedFirstMessage;
			}

			lastMessage = firstMessage;

			using (IPositionedMessagesParser parser = reader.CreateParser(new CreateParserParams(reader.EndPosition, 
				null, MessagesParserFlag.Default, MessagesParserDirection.Backward)))
			{
				IMessage tmp = parser.ReadNext();
				if (tmp != null)
					lastMessage = tmp;
			}
		}

		public enum ValueBound
		{
			/// <summary>
			/// Finds the FIRST position that yields a messsages with the date GREATER than or EQUIVALENT to the date in question
			/// </summary>
			Lower,
			/// <summary>
			/// Finds the FIRST position that yields a messsages with the date GREATER than the date in question
			/// </summary>
			Upper,
			/// <summary>
			/// Finds the LAST position that yields a message with the date LESS than or EQUIVALENT to the date in question
			/// </summary>
			LowerReversed,
			/// <summary>
			/// Finds the LAST position that yields a message with the date LESS than to the date in question
			/// </summary>
			UpperReversed
		};

		public static long LocateDateBound(IPositionedMessagesReader reader, DateTime date, ValueBound bound)
		{
			var d = new MessageTimestamp(date);

			long begin = reader.BeginPosition;
			long end = reader.EndPosition;
			
			long pos = begin;
			long count = end - begin;

			for (; 0 < count; )
			{
				long count2 = count / 2;

				MessageTimestamp d2 = ReadNearestMessageTimestamp(reader, pos + count2);
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
			}

			if (bound == ValueBound.LowerReversed || bound == ValueBound.UpperReversed)
			{
				long? tmp = FindPrevMessagePosition(reader, pos);
				if (tmp == null)
					return begin - 1;
				pos = tmp.Value;
			}

			return pos;
		}

		static public IMessage ReadNearestMessage(IPositionedMessagesReader reader, long position)
		{
			return ReadNearestMessage(reader, position, MessagesParserFlag.Default);
		}

		static public IMessage ReadNearestMessage(IPositionedMessagesReader reader, long position, MessagesParserFlag flags)
		{
			using (IPositionedMessagesParser parser = reader.CreateParser(new CreateParserParams(position, null, flags, MessagesParserDirection.Forward)))
			{
				IMessage ret = parser.ReadNext();
				return ret;
			}
		}
	}
}

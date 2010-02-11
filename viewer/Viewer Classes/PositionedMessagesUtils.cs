using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LogJoint
{
	public static class PositionedMessagesUtils
	{

		public static long NormalizeMessagePosition(IPositionedMessagesProvider provider,
			long position)
		{
			MessageBase m = ReadNearestMessage(provider, position);
			if (m != null)
				return m.Position;
			return provider.EndPosition;
		}

		public static long? FindNextMessagePosition(IPositionedMessagesProvider provider,
			long originalMessagePos)
		{
			// Validate the input.
			if (originalMessagePos < provider.BeginPosition)
				return null;
			if (originalMessagePos >= provider.EndPosition)
				return null;
			using (IPositionedMessagesParser parser = provider.CreateParser(originalMessagePos, null, false))
			{
				if (parser.ReadNext() == null)
					return null;
				MessageBase p = parser.ReadNext();
				if (p == null)
					return null;
				return p.Position;
			}
		}


		public static long? FindPrevMessagePosition(IPositionedMessagesProvider provider,
			long originalMessagePos)
		{
			// Distance to jump back at an iterattion.
			long positionDelta = 1024;

			// Normalize the input: originalMessagePos will be a valid provider's position
			originalMessagePos = NormalizeMessagePosition(provider, originalMessagePos);

			// We are not going to step back farther than this minPositionLimit.
			long minPositionLimit = Math.Max(originalMessagePos - provider.MaximumMessageSize, provider.BeginPosition);

			for (long pos = originalMessagePos; ; )
			{
				if (pos < minPositionLimit)
				{
					return null;
				}

				pos -= positionDelta;

				long startPos = Utils.PutInRange(provider.BeginPosition,
					provider.EndPosition, pos);

				bool startingFromStreamBeginnig = startPos == provider.BeginPosition;

				// Reading the messages and trying to figure out which is this "prev msg"
				using (IPositionedMessagesParser parser = provider.CreateParser(startPos, null, false))
				{
					int messagesCount = 0;
					long? prevPosition = null;
					for (; ; )
					{
						MessageBase tmp = parser.ReadNext();

						// No next message. We may have reached the end of the stream.
						if (tmp == null)
						{
							if (originalMessagePos == provider.EndPosition // If we are searching for the message preceeding EndPosition (last message)
							 && prevPosition != null) // and have found at least one message
							{
								// The this message is what we are searching for
								return prevPosition.Value;
							}

							// Break to outer loop.
							break;
						}

						// We went through the position of the original message.
						// No chance to find the original message. Break.
						if (tmp.Position > originalMessagePos)
						{
							break;
						}

						if (prevPosition.HasValue // If we have read enought messages
						 && tmp.Position == originalMessagePos) // and the message being read is the original one
						{
							// Then we found what we want
							return prevPosition.Value;
						}

						// Storing the position of current message to analyze it on the next iteration
						prevPosition = tmp.Position;
						messagesCount++;
					}
				}
			}
		}

		public static DateTime ReadNearestDate(IPositionedMessagesProvider provider, long position)
		{
			MessageBase m = ReadNearestMessage(provider, position);
			if (m != null)
				return m.Time;
			return DateTime.MinValue;
		}

		/// <summary>
		/// Finds the first and the last available messages in the provider.
		/// </summary>
		/// <param name="provider">Messages provider to read from</param>
		/// <param name="cachedFirstMessage">When the caller passes non-null value
		/// the function doesn't search for the first message in the provider and return the
		/// value precalculated by the client instead. That can be user for optimization:
		/// if the client is sure that the first message didn't change then it can
		/// pass the value calculated before. If <paramref name="firstMessage"/> is <value>null</value>
		/// the function will search for the first message in the provider.</param>
		/// <param name="firstMessage">When the function returns <paramref name="firstMessage"/> receives 
		/// the message with the smallest available position.</param>
		/// <param name="lastMessage">Whan the function returns 
		/// <paramref name="lastMessage"/> receives the message with the largest available position.</param>
		public static void GetBoundaryMessages(
			IPositionedMessagesProvider provider,
			MessageBase cachedFirstMessage,
			out MessageBase firstMessage, out MessageBase lastMessage)
		{
			if (cachedFirstMessage == null)
			{
				firstMessage = ReadNearestMessage(provider, provider.BeginPosition);
			}
			else
			{
				firstMessage = cachedFirstMessage;
			}

			lastMessage = firstMessage;

			long posStep = Math.Min(1024, provider.EndPosition);

			for (long pos = provider.EndPosition - posStep; pos >= 0; pos -= posStep)
			{
				using (IPositionedMessagesParser parser = provider.CreateParser(pos, null, false))
				{
					MessageBase tempLast = null;
					for (;;)
					{
						MessageBase tmp = parser.ReadNext();
						if (tmp == null)
							break;
						tempLast = tmp;
					}
					if (tempLast != null)
					{
						lastMessage = tempLast;
						break;
					}
					if ((provider.EndPosition - pos) >= provider.MaximumMessageSize)
					{
						break;
					}
				}
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

		public static long LocateDateBound(IPositionedMessagesProvider provider, DateTime d, ValueBound bound)
		{
			long begin = provider.BeginPosition;
			long end = provider.EndPosition;
			
			long pos = begin;
			long count = end - begin;

			for (; 0 < count; )
			{
				long count2 = count / 2;

				DateTime d2 = ReadNearestDate(provider, pos + count2);
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
				long? tmp = FindPrevMessagePosition(provider, pos);
				if (tmp == null)
					return begin - 1;
				pos = tmp.Value;
			}

			return pos;
		}

		// todo implement this correctly: 
		// This implementation takes into account 'read-message-from-the-middle' problem. 
		// See IPositionedMessagesProvider remarks for details.

		// The algorithm moves back from the originalMessagePos and tries to read
		// at least three messages:
		// |---guard msg---| |---prev msg---| |----original msg---|
		//                   |                |
		//                 pos to             |
		//                 return       originalMessagePos

		// "original msg" starts from originalMessagePos
		// the begin of "prev msg" is what we are looking for.
		// "guard msg" is needed because of 'read-message-from-the-middle' problem.

		// There might be no "quard msg" required if "prev msg" starts at the beginning of the stream.
		static public MessageBase ReadNearestMessage(IPositionedMessagesProvider provider, long position)
		{
			using (IPositionedMessagesParser parser = provider.CreateParser(position, null, false))
			{
				MessageBase ret = parser.ReadNext();
				return ret;
			}
		}
	}
}

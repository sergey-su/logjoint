using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public static class PositionedMessagesUtils
	{

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

		public static long? FindPrevMessagePosition(IPositionedMessagesProvider provider,
			long originalMessagePos)
		{
			// Validate the input.
			if (originalMessagePos < provider.BeginPosition)
				return null;
			if (originalMessagePos >= provider.EndPosition)
				return null;

			// Distance to jump back at an iterattion.
			long positionDelta = 1024;
			// We are not going to go farther than this minPositionLimit.
			long minPositionLimit = Math.Max(originalMessagePos - positionDelta * 3, provider.BeginPosition);

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

				// Reading the messages and trying to figure out which is this "prev msg".
				using (IPositionedMessagesParser parser = provider.CreateParser(startPos, null, false))
				{
					int messagesCount = 0;
					long? prevPosition = null;
					for (; ; )
					{
						MessageBase tmp = parser.ReadNext();

						// No next message. We may have reached the end of the stream.
						// Break to step back in the outer loop.
						if (tmp == null)
						{
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
		/*
		/// <summary>
		/// Finds the position of the message that follows the message starting at <paramref name="originalMessagePos"/>.
		/// It's callers responsiblity to guarantee that <paramref name="originalMessagePos"/> points exactly 
		/// to the beginning of a message.
		/// </summary>
		/// <param name="originalMessagePos">Position of an existing message. The position must
		/// be inside the position's range of <paramref name="provider"/></param>
		/// <param name="provider">The object that represents messages stream</param>
		/// <returns>The position of next messsage or null if the appropriate message is not found</returns>
		public static long? FindNextMessagePosition(IPositionedMessagesProvider provider,
			long originalMessagePos)
		{
			// Validate the input.
			if (originalMessagePos < provider.BeginPosition)
				return null;
			if (originalMessagePos >= provider.EndPosition)
				return null;

			// Go to the beginning of the original message
			provider.Position = originalMessagePos;

			// Create the reader and read two messages one after the other.
			// It is done to take into account 'read-message-from-the-middle' problem. 
			// See IPositionedMessagesProvider remarks for details.
			using (IPositionedMessagesParser parser = provider.CreateParser(null, false))
			{
				// The first message must exist
				long? firstMsg = parser.GetPositionOfNextMessage();
				if (firstMsg == null)
					return null;

				// And must be exctly at the originalMessagePos
				if (firstMsg.Value != originalMessagePos)
					return null;

				// Read the first message. If it not exist, return null.
				if (parser.ReadNext().Message == null)
					return null;

				// Here we are. Return the position of the second message.
				return parser.GetPositionOfNextMessage();
			}
		}

		/// <summary>
		/// Rerurns the porision of the message that preceed the message starting at <paramref name="originalMessagePos"/>.
		/// It's callers responsiblity to guarantee that <paramref name="originalMessagePos"/> points exactly 
		/// to the beginning of a message.
		/// </summary>
		/// <param name="originalMessagePos">Position of an existing message. The position must
		/// be inside the position's range of <paramref name="provider"/></param>
		/// <param name="provider">The object that represents messages stream</param>
		/// <returns>The position of preceeding messsage or null if the appropriate message is not found</returns>
		public static long? FindPrevMessagePosition(IPositionedMessagesProvider provider, long originalMessagePos)
		{
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


			// Distance to jump back at an iterattion.
			long positionDelta = 1024;
			// We are not going to go farther than this minPositionLimit.
			long minPositionLimit = Math.Max(originalMessagePos - positionDelta * 3, provider.BeginPosition);

			for (long pos = originalMessagePos; ; )
			{
				if (pos < minPositionLimit)
				{
					return null;
				}

				pos -= positionDelta;

				provider.Position = Utils.PutInRange(provider.BeginPosition,
					provider.EndPosition, pos);

				bool startingFromStreamBeginnig = provider.Position == provider.BeginPosition;

				// Reading the messages and trying to figure out which is this "prev msg".
				using (IPositionedMessagesParser parser = provider.CreateParser(null, false))
				{
					// Index of the message being read
					int messageIndex = 0;

					for (long? prevPosition = null; ; )
					{
						long? tmp = parser.GetPositionOfNextMessage();

						// No next message. We may have reached the end of the stream.
						// Break to step back in the outer loop.
						if (tmp == null)
						{
							break;
						}

						// We went through the position of the original message.
						// No chance to find the original message. Break.
						if (tmp.Value > originalMessagePos)
						{
							break;
						}


						if (messageIndex >= (startingFromStreamBeginnig ? 1 : 2) // If we have read enought messages
						 && tmp.Value == originalMessagePos) // and the message being read is the original one
						{
							// Then we found what we want
							return prevPosition.Value;
						}

						// Storing the position of current message to analyze it on the next iteration
						prevPosition = tmp;

						// Advance the reader
						if (parser.ReadNext().Message == null)
						{
							break;
						}

						++messageIndex;
					}

					// We started from position pos. If we have read 3 or more messages 
					// then it doesn't make sense to try to decrease "pos" anymore.
					if (messageIndex >= 2)
					{
						return null;
					}
				}
			}
		}
		*/
		/*
		/// <summary>
		/// Returns the (position, message) struct for the nearest message that is located at 
		/// position <paramref name="pos"/> of farther in the messages stream. 
		/// Returns (provider.EndPosition, null) if there is not messages 
		/// at or after <paramref name="pos"/>.
		/// </summary>
		public static PositionedMessage SeekAndReadElement(IPositionedMessagesProvider provider, long pos)
		{
			// Distance to jump back at an iterattion.
			long positionDelta = 1024;
			// We are not going to go farther than this minPositionLimit.
			long minPositionLimit = Math.Max(pos - positionDelta * 3, provider.BeginPosition);

			for (long pos2 = pos; ; )
			{
				if (pos2 < minPositionLimit)
				{
					break;
				}

				pos2 -= positionDelta;

				provider.Position = Utils.PutInRange(provider.BeginPosition,
					provider.EndPosition, pos2);

				bool startingFromStreamBeginnig = provider.Position == provider.BeginPosition;

				using (IPositionedMessagesParser parser = provider.CreateParser(null, false))
				{
					for (int messageIndex = 0; ; ++messageIndex)
					{
						PositionedMessage msg = parser.ReadNext();

						if (msg.Message == null)
						{
							break;
						}

						if (messageIndex >= (startingFromStreamBeginnig ? 0 : 1) // If we have read at least 1 message
						 && msg.Position >= pos) // and the message is located after "pos"
						{
							// Then this is what we need
							return msg;
						}
					}
				}
			}
		 

			return new PositionedMessage(provider.EndPosition, null);
		}
* */
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

			long posStep = 1024;
			int iteration = 0;

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
					if (iteration++ > 3)
					{
						break;
					}
				}
			}
		}

		public static DateRange GetAvailableDateRange(IPositionedMessagesProvider provider, DateTime? knownBeginDate)
		{
			DateTime begin;
			if (!knownBeginDate.HasValue)
			{
				begin = ReadNearestDate(provider, provider.BeginPosition);
			}
			else
			{
				begin = knownBeginDate.Value;
			}
			if (begin == DateTime.MinValue)
				return new DateRange();

			long posStep = 6;
			long pos = provider.EndPosition - posStep;
			if (pos < 0)
				return DateRange.MakeFromBoundaryValues(begin, begin);

			for (; ; )
			{
				DateTime d = ReadNearestDate(provider, pos);
				if (d != DateTime.MinValue)
					return DateRange.MakeFromBoundaryValues(begin, d);
				if (pos == 0)
					return DateRange.MakeFromBoundaryValues(begin, begin);
				pos -= posStep;
				if (pos < 0)
					pos = 0;
			}
		}

		public enum ValueBound
		{
			Lower,
			Upper,
			LowerReversed,
			UpperReversed
		};

		public static KeyValuePair<long, DateTime> LocateDateBound(IPositionedMessagesProvider provider, DateTime d, ValueBound bound)
		{
			long begin = provider.BeginPosition;
			long end = provider.EndPosition;
			long count = end - begin;
			DateTime ret = d;

			for (; 0 < count; )
			{
				long count2 = count / 2;

				DateTime d2 = ReadNearestDate(provider, begin + count2);
				bool setReturn = false;
				bool moveRight = false;
				switch (bound)
				{
					case ValueBound.Lower:
						setReturn = d2 < d;
						moveRight = setReturn;
						break;
					case ValueBound.Upper:
						setReturn = d2 <= d;
						moveRight = setReturn;
						break;
					case ValueBound.LowerReversed:
						setReturn = d2 > d;
						moveRight = !setReturn;
						break;
					case ValueBound.UpperReversed:
						setReturn = d2 >= d;
						moveRight = !setReturn;
						break;
				}
				if (moveRight)
				{
					begin += count2 + 1;
					count -= count2 + 1;	
				}
				else
				{
					count = count2;
				}
				if (setReturn)
				{
					ret = d2;
				}
			}

			return new KeyValuePair<long,DateTime>(begin, ret);
		}

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

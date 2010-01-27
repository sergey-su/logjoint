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
			// Normalize the input: originalMessagePos will be a valid provider's position
			originalMessagePos = NormalizeMessagePosition(provider, originalMessagePos);

			// Distance to jump back at an iterattion.
			long positionDelta = 1024;
			// We are not going to step back farther than this minPositionLimit.
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

		class TestProvider : IPositionedMessagesProvider
		{
			public TestProvider(long[] positions)
			{
				this.positions = positions;
			}

			public static readonly DateTime DateOrigin = new DateTime(2009, 1, 1);
			public static DateTime PositionToDate(long position)
			{
				return DateOrigin.AddSeconds(position);
			}

			public long BeginPosition
			{
				get 
				{
					CheckDisposed();
					return positions.Length > 0 ? positions[0] : 0;
				}
			}

			public long EndPosition
			{
				get 
				{
					CheckDisposed();
					return positions.Length > 0 ? (positions[positions.Length - 1] + 1) : 0;
				}
			}

			public bool UpdateAvailableBounds(bool incrementalMode)
			{
				CheckDisposed();
				return true;
			}

			public long ActiveRangeRadius
			{
				get 
				{
					CheckDisposed();
					return 0;
				}
			}

			public long PositionRangeToBytes(FileRange.Range range)
			{
				CheckDisposed();
				return range.Length;
			}

			class Parser : IPositionedMessagesParser
			{
				public Parser(TestProvider provider, long startPosition, FileRange.Range? range)
				{
					this.provider = provider;
					this.range = range;

					for (positionIndex = 0; positionIndex < provider.positions.Length; ++positionIndex)
					{
						if (provider.positions[positionIndex] >= startPosition)
							break;
					}
				}

				public MessageBase ReadNext()
				{
					CheckDisposed();
					provider.CheckDisposed();

					if (positionIndex >= provider.positions.Length)
						return null;

					long currPos = provider.positions[positionIndex];
					if (range.HasValue && currPos > range.Value.End)
						return null;

					++positionIndex;

					return new Content(currPos, null, PositionToDate(currPos), currPos.ToString(), Content.SeverityFlag.Info);
				}

				public void Dispose()
				{
					isDisposed = true;
				}

				void CheckDisposed()
				{
					if (isDisposed)
						throw new ObjectDisposedException(this.ToString());
				}

				readonly TestProvider provider;
				readonly FileRange.Range? range;
				long positionIndex;
				bool isDisposed;
			};

			public IPositionedMessagesParser CreateParser(long startPosition, FileRange.Range? range, bool isMainStreamReader)
			{
				CheckDisposed();
				return new Parser(this, startPosition, range);
			}

			public void Dispose()
			{
				isDisposed = true;
			}

			void CheckDisposed()
			{
				if (isDisposed)
					throw new ObjectDisposedException(this.ToString());
			}

			readonly long[] positions;
			bool isDisposed;
		};

		static TestProvider CreateTestProvider1()
		{
			TestProvider provider = new TestProvider(
				new long[] { 0, 4, 5, 6, 10, 15, 20, 30, 40, 50 });
			return provider;
		}

		static public void ReadNearestDate_Test1()
		{
			TestProvider provider = CreateTestProvider1();
			
			// Exact hit to a position
			Debug.Assert(ReadNearestDate(provider, 5) == TestProvider.PositionToDate(5));

			// Needing to move forward to find the nearest position
			Debug.Assert(ReadNearestDate(provider, 7) == TestProvider.PositionToDate(10));

			// Over-the-end position
			Debug.Assert(ReadNearestDate(provider, 55) == DateTime.MinValue);

			//Begore the begin position
			Debug.Assert(ReadNearestDate(provider, -5) == TestProvider.PositionToDate(0));
		}

		static public void LocateDateBound_LowerBound_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			// Exact hit to a position
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(5), ValueBound.Lower) == 5);

			// Test that LowerBound returns the first (smallest) position that yields the messages with date <= date in question
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(7), ValueBound.Lower) == 7);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(8), ValueBound.Lower) == 7);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(9), ValueBound.Lower) == 7);

			// Before begin position
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(-5), ValueBound.Lower) == 0);

			// After end position
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(55), ValueBound.Lower) == 51);
		}

		static public void LocateDateBound_UpperBound_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(15), ValueBound.Upper) == 16);

			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(16), ValueBound.Upper) == 16);

			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(0), ValueBound.Upper) == 1);

			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(4), ValueBound.Upper) == 5);

			// Before begin position
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(-5), ValueBound.Upper) == 0);

			// After end position
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(55), ValueBound.Upper) == 51);
		}

		static public void LocateDateBound_LowerRevBound_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(30), ValueBound.LowerReversed) == 30);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(8), ValueBound.LowerReversed) == 6);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(0), ValueBound.LowerReversed) == 0);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(-1), ValueBound.LowerReversed) == -1);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(-5), ValueBound.LowerReversed) == -1);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(50), ValueBound.LowerReversed) == 50);
			Debug.Assert(LocateDateBound(provider, TestProvider.PositionToDate(55), ValueBound.LowerReversed) == 50);
		}

		static public void NormalizeMessagePosition_Test1()
		{	
			TestProvider provider = CreateTestProvider1();

			Debug.Assert(NormalizeMessagePosition(provider, 0) == 0);
			Debug.Assert(NormalizeMessagePosition(provider, 4) == 4);
			Debug.Assert(NormalizeMessagePosition(provider, 2) == 4);
			Debug.Assert(NormalizeMessagePosition(provider, 3) == 4);
			Debug.Assert(NormalizeMessagePosition(provider, -1) == 0);
			Debug.Assert(NormalizeMessagePosition(provider, -5) == 0);
			Debug.Assert(NormalizeMessagePosition(provider, 16) == 20);
			Debug.Assert(NormalizeMessagePosition(provider, 50) == 50);
			Debug.Assert(NormalizeMessagePosition(provider, 51) == 51);
			Debug.Assert(NormalizeMessagePosition(provider, 52) == 51);
			Debug.Assert(NormalizeMessagePosition(provider, 888) == 51);
		}

		static public void Tests()
		{
			NormalizeMessagePosition_Test1();
			ReadNearestDate_Test1();
			LocateDateBound_LowerBound_Test1();
			LocateDateBound_UpperBound_Test1();
			LocateDateBound_LowerRevBound_Test1();
		}
	}
}

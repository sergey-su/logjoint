using System;
using System.Threading.Tasks;
using LogJoint.Settings;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class PositionedMessagesUtilsTests
	{
		public class TestReader : IPositionedMessagesReader
		{
			public TestReader(long[] positions)
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

			public long SizeInBytes => 0xffff;

			public Task<UpdateBoundsStatus> UpdateAvailableBounds(bool incrementalMode)
			{
				CheckDisposed();
				return Task.FromResult(UpdateBoundsStatus.NewMessagesAvailable);
			}

			public long CalcMaxActiveRangeSize(IGlobalSettingsAccessor settings)
			{
				CheckDisposed();
				return 0;
			}

			public long MaximumMessageSize => 0;

			public ITimeOffsets TimeOffsets
			{
				get { return LogJoint.TimeOffsets.Empty; }
				set { }
			}

			public long PositionRangeToBytes(LogJoint.FileRange.Range range)
			{
				CheckDisposed();
				return range.Length;
			}

			ValueTask<int> IPositionedMessagesReader.GetContentsEtag()
			{
				CheckDisposed();
				return new ValueTask<int>(0);
			}

			class Parser : IPositionedMessagesParser
			{
				public Parser(TestReader reader, long startPosition, LogJoint.FileRange.Range? range, MessagesParserDirection direction)
				{
					this.reader = reader;
					this.range = range;
					this.direction = direction;

					if (direction == MessagesParserDirection.Forward)
					{
						for (positionIndex = 0; positionIndex < reader.positions.Length; ++positionIndex)
						{
							if (reader.positions[positionIndex] >= startPosition)
								break;
						}
					}
					else 
					{
						for (positionIndex = reader.positions.Length - 1; positionIndex >= 0; --positionIndex)
						{
							if (reader.positions[positionIndex] < startPosition)
								break;
						}
					}
				}

				public async ValueTask<IMessage> ReadNext()
				{
					CheckDisposed();
					reader.CheckDisposed();

					long currPos;
					if (direction == MessagesParserDirection.Forward)
					{
						if (positionIndex >= reader.positions.Length)
							return null;

						currPos = reader.positions[positionIndex];
						if (range.HasValue && currPos >= range.Value.End)
							return null;

						++positionIndex;
					}
					else
					{
						if (positionIndex < 0)
							return null;

						currPos = reader.positions[positionIndex];
						if (range.HasValue && currPos < range.Value.Begin)
							return null;

						--positionIndex;
					}

					return new Message(currPos, currPos + 1, null, new MessageTimestamp(PositionToDate(currPos)), new StringSlice(currPos.ToString()), SeverityFlag.Info);
				}

				public async ValueTask<PostprocessedMessage> ReadNextAndPostprocess()
				{
					return new PostprocessedMessage(await ReadNext(), null);
				}

				public Task Dispose()
				{
					isDisposed = true;
					return Task.CompletedTask;
				}

				void CheckDisposed()
				{
					if (isDisposed)
						throw new ObjectDisposedException(this.ToString());
				}

				readonly TestReader reader;
				readonly LogJoint.FileRange.Range? range;
				long positionIndex;
				MessagesParserDirection direction;
				bool isDisposed;
			};

			public async Task<IPositionedMessagesParser> CreateParser(CreateParserParams p)
			{
				CheckDisposed();
				return new Parser(this, p.StartPosition, p.Range, p.Direction);
			}

			public ISearchingParser CreateSearchingParser(CreateSearchingParserParams p)
			{
				return null;
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

		static TestReader CreateTestReader1()
		{
			TestReader reader = new TestReader(
				new long[] { 0, 4, 5, 6, 10, 15, 20, 30, 40, 50 });
			return reader;
		}

		[Test]
		public async Task ReadNearestDate_Test1()
		{
			TestReader reader = CreateTestReader1();

			// Exact hit to a position
			Assert.AreEqual(TestReader.PositionToDate(5), (await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 5)).Value.ToLocalDateTime());

			// Needing to move forward to find the nearest position
			Assert.AreEqual(TestReader.PositionToDate(10), (await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 7)).Value.ToLocalDateTime());

			// Over-the-end position
			Assert.AreEqual(new MessageTimestamp?(), await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 55));

			//Begore the begin position
			Assert.AreEqual(TestReader.PositionToDate(0), (await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, -5)).Value.ToLocalDateTime());
		}

		[Test]
		public async Task LocateDateBound_LowerBound_Test1()
		{
			TestReader reader = CreateTestReader1();

			// Exact hit to a position
			Assert.AreEqual(5, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(5), ValueBound.Lower));

			// Test that LowerBound returns the first (smallest) position that yields the messages with date <= date in question
			Assert.AreEqual(7, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(7), ValueBound.Lower));
			Assert.AreEqual(7, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(8), ValueBound.Lower));
			Assert.AreEqual(7, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(9), ValueBound.Lower));

			// Before begin position
			Assert.AreEqual(0, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), ValueBound.Lower));

			// After end position
			Assert.AreEqual(51, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), ValueBound.Lower));
		}

		[Test]
		public async Task LocateDateBound_UpperBound_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.AreEqual(16, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(15), ValueBound.Upper));

			Assert.AreEqual(16, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(16), ValueBound.Upper));

			Assert.AreEqual(1, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(0), ValueBound.Upper));

			Assert.AreEqual(5, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(4), ValueBound.Upper));

			// Before begin position
			Assert.AreEqual(0, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), ValueBound.Upper));

			// After end position
			Assert.AreEqual(51, await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), ValueBound.Upper));
		}

		[Test]
		public async Task LocateDateBound_LowerRevBound_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.IsTrue(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(30), ValueBound.LowerReversed) == 30);
			Assert.IsTrue(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(8), ValueBound.LowerReversed) == 6);
			Assert.IsTrue(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(0), ValueBound.LowerReversed) == 0);
			Assert.IsTrue(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-1), ValueBound.LowerReversed) == -1);
			Assert.IsTrue(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), ValueBound.LowerReversed) == -1);
			Assert.IsTrue(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(50), ValueBound.LowerReversed) == 50);
			Assert.IsTrue(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), ValueBound.LowerReversed) == 50);
		}

		[Test]
		public async Task NormalizeMessagePosition_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 0) == 0);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 4) == 4);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 2) == 4);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 3) == 4);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, -1) == 0);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, -5) == 0);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 16) == 20);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 50) == 50);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 51) == 51);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 52) == 51);
			Assert.IsTrue(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 888) == 51);
		}

		[Test]
		public async Task FindPrevMessagePosition_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.IsTrue(await PositionedMessagesUtils.FindPrevMessagePosition(reader, 1) == 0);
		}
	}
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using LogJoint;

namespace LogJointTests
{
	[TestClass]
	public class PositionedMessagesUtilsTests
	{
		class TestReader : IPositionedMessagesReader
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

			public long SizeInBytes
			{
				get { return 0xffff; }
			}

			public UpdateBoundsStatus UpdateAvailableBounds(bool incrementalMode)
			{
				CheckDisposed();
				return UpdateBoundsStatus.NewMessagesAvailable;
			}

			public long ActiveRangeRadius
			{
				get
				{
					CheckDisposed();
					return 0;
				}
			}

			public long MaximumMessageSize
			{
				get
				{
					return 0;
				}
			}

			public long PositionRangeToBytes(LogJoint.FileRange.Range range)
			{
				CheckDisposed();
				return range.Length;
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

				public MessageBase ReadNext()
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

					return new Content(currPos, null, PositionToDate(currPos), new StringSlice(currPos.ToString()), Content.SeverityFlag.Info);
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

				readonly TestReader reader;
				readonly LogJoint.FileRange.Range? range;
				long positionIndex;
				MessagesParserDirection direction;
				bool isDisposed;
			};

			public IPositionedMessagesParser CreateParser(CreateParserParams p)
			{
				CheckDisposed();
				return new Parser(this, p.StartPosition, p.Range, p.Direction);
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

		[TestMethod]
		public void ReadNearestDate_Test1()
		{
			TestReader reader = CreateTestReader1();

			// Exact hit to a position
			Assert.AreEqual(TestReader.PositionToDate(5), PositionedMessagesUtils.ReadNearestDate(reader, 5));

			// Needing to move forward to find the nearest position
			Assert.AreEqual(TestReader.PositionToDate(10), PositionedMessagesUtils.ReadNearestDate(reader, 7));

			// Over-the-end position
			Assert.AreEqual(DateTime.MinValue, PositionedMessagesUtils.ReadNearestDate(reader, 55));

			//Begore the begin position
			Assert.AreEqual(TestReader.PositionToDate(0), PositionedMessagesUtils.ReadNearestDate(reader, -5));
		}

		[TestMethod]
		public void LocateDateBound_LowerBound_Test1()
		{
			TestReader reader = CreateTestReader1();

			// Exact hit to a position
			Assert.AreEqual<long>(5, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(5), PositionedMessagesUtils.ValueBound.Lower));

			// Test that LowerBound returns the first (smallest) position that yields the messages with date <= date in question
			Assert.AreEqual<long>(7, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(7), PositionedMessagesUtils.ValueBound.Lower));
			Assert.AreEqual<long>(7, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(8), PositionedMessagesUtils.ValueBound.Lower));
			Assert.AreEqual<long>(7, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(9), PositionedMessagesUtils.ValueBound.Lower));

			// Before begin position
			Assert.AreEqual<long>(0, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), PositionedMessagesUtils.ValueBound.Lower));

			// After end position
			Assert.AreEqual<long>(51, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), PositionedMessagesUtils.ValueBound.Lower));
		}

		[TestMethod]
		public void LocateDateBound_UpperBound_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.AreEqual<long>(16, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(15), PositionedMessagesUtils.ValueBound.Upper));

			Assert.AreEqual<long>(16, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(16), PositionedMessagesUtils.ValueBound.Upper));

			Assert.AreEqual<long>(1, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(0), PositionedMessagesUtils.ValueBound.Upper));

			Assert.AreEqual<long>(5, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(4), PositionedMessagesUtils.ValueBound.Upper));

			// Before begin position
			Assert.AreEqual<long>(0, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), PositionedMessagesUtils.ValueBound.Upper));

			// After end position
			Assert.AreEqual<long>(51, PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), PositionedMessagesUtils.ValueBound.Upper));
		}

		[TestMethod]
		public void LocateDateBound_LowerRevBound_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(30), PositionedMessagesUtils.ValueBound.LowerReversed) == 30);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(8), PositionedMessagesUtils.ValueBound.LowerReversed) == 6);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(0), PositionedMessagesUtils.ValueBound.LowerReversed) == 0);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-1), PositionedMessagesUtils.ValueBound.LowerReversed) == -1);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), PositionedMessagesUtils.ValueBound.LowerReversed) == -1);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(50), PositionedMessagesUtils.ValueBound.LowerReversed) == 50);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), PositionedMessagesUtils.ValueBound.LowerReversed) == 50);
		}

		[TestMethod]
		public void NormalizeMessagePosition_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 0) == 0);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 4) == 4);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 2) == 4);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 3) == 4);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, -1) == 0);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, -5) == 0);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 16) == 20);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 50) == 50);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 51) == 51);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 52) == 51);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(reader, 888) == 51);
		}

		[TestMethod]
		public void FindPrevMessagePosition_Test1()
		{
			TestReader reader = CreateTestReader1();

			Assert.IsTrue(PositionedMessagesUtils.FindPrevMessagePosition(reader, 1) == 0);
		}
	}
}

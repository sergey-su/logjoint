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
				public Parser(TestProvider provider, long startPosition, LogJoint.FileRange.Range? range)
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
				readonly LogJoint.FileRange.Range? range;
				long positionIndex;
				bool isDisposed;
			};

			public IPositionedMessagesParser CreateParser(long startPosition, LogJoint.FileRange.Range? range, bool isMainStreamReader)
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

		[TestMethod]
		public void ReadNearestDate_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			// Exact hit to a position
			Assert.AreEqual(TestProvider.PositionToDate(5), PositionedMessagesUtils.ReadNearestDate(provider, 5));

			// Needing to move forward to find the nearest position
			Assert.AreEqual(TestProvider.PositionToDate(10), PositionedMessagesUtils.ReadNearestDate(provider, 7));

			// Over-the-end position
			Assert.AreEqual(DateTime.MinValue, PositionedMessagesUtils.ReadNearestDate(provider, 55));

			//Begore the begin position
			Assert.AreEqual(TestProvider.PositionToDate(0), PositionedMessagesUtils.ReadNearestDate(provider, -5));
		}

		[TestMethod]
		public void LocateDateBound_LowerBound_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			// Exact hit to a position
			Assert.AreEqual<long>(5, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(5), PositionedMessagesUtils.ValueBound.Lower));

			// Test that LowerBound returns the first (smallest) position that yields the messages with date <= date in question
			Assert.AreEqual<long>(7, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(7), PositionedMessagesUtils.ValueBound.Lower));
			Assert.AreEqual<long>(7, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(8), PositionedMessagesUtils.ValueBound.Lower));
			Assert.AreEqual<long>(7, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(9), PositionedMessagesUtils.ValueBound.Lower));

			// Before begin position
			Assert.AreEqual<long>(0, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(-5), PositionedMessagesUtils.ValueBound.Lower));

			// After end position
			Assert.AreEqual<long>(51, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(55), PositionedMessagesUtils.ValueBound.Lower));
		}

		[TestMethod]
		public void LocateDateBound_UpperBound_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			Assert.AreEqual<long>(16, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(15), PositionedMessagesUtils.ValueBound.Upper));

			Assert.AreEqual<long>(16, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(16), PositionedMessagesUtils.ValueBound.Upper));

			Assert.AreEqual<long>(1, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(0), PositionedMessagesUtils.ValueBound.Upper));

			Assert.AreEqual<long>(5, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(4), PositionedMessagesUtils.ValueBound.Upper));

			// Before begin position
			Assert.AreEqual<long>(0, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(-5), PositionedMessagesUtils.ValueBound.Upper));

			// After end position
			Assert.AreEqual<long>(51, PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(55), PositionedMessagesUtils.ValueBound.Upper));
		}

		[TestMethod]
		public void LocateDateBound_LowerRevBound_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(30), PositionedMessagesUtils.ValueBound.LowerReversed) == 30);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(8), PositionedMessagesUtils.ValueBound.LowerReversed) == 6);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(0), PositionedMessagesUtils.ValueBound.LowerReversed) == 0);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(-1), PositionedMessagesUtils.ValueBound.LowerReversed) == -1);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(-5), PositionedMessagesUtils.ValueBound.LowerReversed) == -1);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(50), PositionedMessagesUtils.ValueBound.LowerReversed) == 50);
			Assert.IsTrue(PositionedMessagesUtils.LocateDateBound(provider, TestProvider.PositionToDate(55), PositionedMessagesUtils.ValueBound.LowerReversed) == 50);
		}

		[TestMethod]
		public void NormalizeMessagePosition_Test1()
		{
			TestProvider provider = CreateTestProvider1();

			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 0) == 0);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 4) == 4);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 2) == 4);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 3) == 4);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, -1) == 0);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, -5) == 0);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 16) == 20);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 50) == 50);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 51) == 51);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 52) == 51);
			Assert.IsTrue(PositionedMessagesUtils.NormalizeMessagePosition(provider, 888) == 51);
		}
	}
}

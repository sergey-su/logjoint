using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogJoint.Settings;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class PositionedMessagesUtilsTests
    {
        public sealed class TestReader : IPositionedMessagesReader
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

            public async IAsyncEnumerable<PostprocessedMessage> Read(ReadMessagesParams p)
            {
                CheckDisposed();

                var range = p.Range;
                long positionIndex = 0;
                long startPosition = p.StartPosition;

                if (p.Direction == ReadMessagesDirection.Forward)
                {
                    for (positionIndex = 0; positionIndex < positions.Length; ++positionIndex)
                    {
                        if (positions[positionIndex] >= startPosition)
                            break;
                    }
                }
                else
                {
                    for (positionIndex = positions.Length - 1; positionIndex >= 0; --positionIndex)
                    {
                        if (positions[positionIndex] < startPosition)
                            break;
                    }
                }

                var result = new List<PostprocessedMessage>();
                for (; ; )
                {
                    CheckDisposed();

                    long currPos;
                    if (p.Direction == ReadMessagesDirection.Forward)
                    {
                        if (positionIndex >= positions.Length)
                            break;

                        currPos = positions[positionIndex];
                        if (range.HasValue && currPos >= range.Value.End)
                            break;

                        ++positionIndex;
                    }
                    else
                    {
                        if (positionIndex < 0)
                            break;

                        currPos = positions[positionIndex];
                        if (range.HasValue && currPos < range.Value.Begin)
                            break;

                        --positionIndex;
                    }

                    IMessage m = new Message(currPos, currPos + 1, null,
                        new MessageTimestamp(PositionToDate(currPos)), new StringSlice(currPos.ToString()), SeverityFlag.Info);
                    yield return new PostprocessedMessage(m, null);
                }
            }

            public IAsyncEnumerable<SearchResultMessage> Search(SearchMessagesParams p)
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
            Assert.That(TestReader.PositionToDate(5), Is.EqualTo((await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 5)).Value.ToLocalDateTime()));

            // Needing to move forward to find the nearest position
            Assert.That(TestReader.PositionToDate(10), Is.EqualTo((await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 7)).Value.ToLocalDateTime()));

            // Over-the-end position
            Assert.That(new MessageTimestamp?(), Is.EqualTo(await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 55)));

            //Begore the begin position
            Assert.That(TestReader.PositionToDate(0), Is.EqualTo((await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, -5)).Value.ToLocalDateTime()));
        }

        [Test]
        public async Task LocateDateBound_LowerBound_Test1()
        {
            TestReader reader = CreateTestReader1();

            // Exact hit to a position
            Assert.That(5, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(5), ValueBound.Lower)));

            // Test that LowerBound returns the first (smallest) position that yields the messages with date <= date in question
            Assert.That(7, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(7), ValueBound.Lower)));
            Assert.That(7, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(8), ValueBound.Lower)));
            Assert.That(7, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(9), ValueBound.Lower)));

            // Before begin position
            Assert.That(0, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), ValueBound.Lower)));

            // After end position
            Assert.That(51, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), ValueBound.Lower)));
        }

        [Test]
        public async Task LocateDateBound_UpperBound_Test1()
        {
            TestReader reader = CreateTestReader1();

            Assert.That(16, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(15), ValueBound.Upper)));

            Assert.That(16, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(16), ValueBound.Upper)));

            Assert.That(1, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(0), ValueBound.Upper)));

            Assert.That(5, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(4), ValueBound.Upper)));

            // Before begin position
            Assert.That(0, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), ValueBound.Upper)));

            // After end position
            Assert.That(51, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), ValueBound.Upper)));
        }

        [Test]
        public async Task LocateDateBound_LowerRevBound_Test1()
        {
            TestReader reader = CreateTestReader1();

            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(30), ValueBound.LowerReversed), Is.EqualTo(30));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(8), ValueBound.LowerReversed), Is.EqualTo(6));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(0), ValueBound.LowerReversed), Is.EqualTo(0));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-1), ValueBound.LowerReversed), Is.EqualTo(-1));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(-5), ValueBound.LowerReversed), Is.EqualTo(-1));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(50), ValueBound.LowerReversed), Is.EqualTo(50));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, TestReader.PositionToDate(55), ValueBound.LowerReversed), Is.EqualTo(50));
        }

        [Test]
        public async Task NormalizeMessagePosition_Test1()
        {
            TestReader reader = CreateTestReader1();

            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 0), Is.EqualTo(0));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 4), Is.EqualTo(4));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 2), Is.EqualTo(4));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 3), Is.EqualTo(4));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, -1), Is.EqualTo(0));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, -5), Is.EqualTo(0));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 16), Is.EqualTo(20));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 50), Is.EqualTo(50));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 51), Is.EqualTo(51));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 52), Is.EqualTo(51));
            Assert.That(await PositionedMessagesUtils.NormalizeMessagePosition(reader, 888), Is.EqualTo(51));
        }

        [Test]
        public async Task FindPrevMessagePosition_Test1()
        {
            TestReader reader = CreateTestReader1();

            Assert.That(await PositionedMessagesUtils.FindPrevMessagePosition(reader, 1), Is.EqualTo(0));
        }
    }
}

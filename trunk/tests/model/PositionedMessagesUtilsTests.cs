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
        static FakeMessagesReader CreateFakeMessagesReader1()
        {
            FakeMessagesReader reader = new([0, 4, 5, 6, 10, 15, 20, 30, 40, 50]);
            return reader;
        }

        [Test]
        public async Task ReadNearestDate_Test1()
        {
            FakeMessagesReader reader = CreateFakeMessagesReader1();

            // Exact hit to a position
            Assert.That(FakeMessagesReader.PositionToDate(5), Is.EqualTo((await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 5)).Value.ToLocalDateTime()));

            // Needing to move forward to find the nearest position
            Assert.That(FakeMessagesReader.PositionToDate(10), Is.EqualTo((await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 7)).Value.ToLocalDateTime()));

            // Over-the-end position
            Assert.That(new MessageTimestamp?(), Is.EqualTo(await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, 55)));

            //Begore the begin position
            Assert.That(FakeMessagesReader.PositionToDate(0), Is.EqualTo((await PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, -5)).Value.ToLocalDateTime()));
        }

        [Test]
        public async Task LocateDateBound_LowerBound_Test1()
        {
            FakeMessagesReader reader = CreateFakeMessagesReader1();

            // Exact hit to a position
            Assert.That(5, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(5), ValueBound.Lower)));

            // Test that LowerBound returns the first (smallest) position that yields the messages with date <= date in question
            Assert.That(7, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(7), ValueBound.Lower)));
            Assert.That(7, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(8), ValueBound.Lower)));
            Assert.That(7, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(9), ValueBound.Lower)));

            // Before begin position
            Assert.That(0, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(-5), ValueBound.Lower)));

            // After end position
            Assert.That(51, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(55), ValueBound.Lower)));
        }

        [Test]
        public async Task LocateDateBound_UpperBound_Test1()
        {
            FakeMessagesReader reader = CreateFakeMessagesReader1();

            Assert.That(16, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(15), ValueBound.Upper)));

            Assert.That(16, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(16), ValueBound.Upper)));

            Assert.That(1, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(0), ValueBound.Upper)));

            Assert.That(5, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(4), ValueBound.Upper)));

            // Before begin position
            Assert.That(0, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(-5), ValueBound.Upper)));

            // After end position
            Assert.That(51, Is.EqualTo(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(55), ValueBound.Upper)));
        }

        [Test]
        public async Task LocateDateBound_LowerRevBound_Test1()
        {
            FakeMessagesReader reader = CreateFakeMessagesReader1();

            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(30), ValueBound.LowerReversed), Is.EqualTo(30));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(8), ValueBound.LowerReversed), Is.EqualTo(6));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(0), ValueBound.LowerReversed), Is.EqualTo(0));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(-1), ValueBound.LowerReversed), Is.EqualTo(-1));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(-5), ValueBound.LowerReversed), Is.EqualTo(-1));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(50), ValueBound.LowerReversed), Is.EqualTo(50));
            Assert.That(await PositionedMessagesUtils.LocateDateBound(reader, FakeMessagesReader.PositionToDate(55), ValueBound.LowerReversed), Is.EqualTo(50));
        }

        [Test]
        public async Task NormalizeMessagePosition_Test1()
        {
            FakeMessagesReader reader = CreateFakeMessagesReader1();

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
            FakeMessagesReader reader = CreateFakeMessagesReader1();

            Assert.That(await PositionedMessagesUtils.FindPrevMessagePosition(reader, 1), Is.EqualTo(0));
        }
    }
}

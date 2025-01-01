using System;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class MessageTimestampTest
    {
        [Test]
        public void TimeZoneTest()
        {
            var m1 = new MessageTimestamp(new DateTime(123213213, DateTimeKind.Local));
            Assert.That(MessageTimestampTimezone.Unknown, Is.EqualTo(m1.TimeZone));

            var m2 = new MessageTimestamp(new DateTime(123213213, DateTimeKind.Unspecified));
            Assert.That(MessageTimestampTimezone.Unknown, Is.EqualTo(m2.TimeZone));

            var m3 = new MessageTimestamp(new DateTime(123213213, DateTimeKind.Utc));
            Assert.That(MessageTimestampTimezone.UTC, Is.EqualTo(m3.TimeZone));
        }

        [Test]
        public void MinimumIsLessThanAnyDateTime()
        {
            Func<long, long> localTicksToUtcTicks = ticks => new DateTime(ticks, DateTimeKind.Local).ToUniversalTime().Ticks;

            var t1 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Local));
            Assert.That(MessageTimestamp.Compare(t1, MessageTimestamp.MinValue), Is.GreaterThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MinValue, t1), Is.LessThan(0));

            var t2 = new MessageTimestamp(new DateTime(localTicksToUtcTicks(3423), DateTimeKind.Utc));
            Assert.That(MessageTimestamp.Compare(t2, MessageTimestamp.MinValue), Is.GreaterThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MinValue, t2), Is.LessThan(0));

            var t3 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Unspecified));
            Assert.That(MessageTimestamp.Compare(t3, MessageTimestamp.MinValue), Is.GreaterThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MinValue, t3), Is.LessThan(0));

            var t4 = new MessageTimestamp(new DateTime(1, DateTimeKind.Local));
            Assert.That(MessageTimestamp.Compare(t4, MessageTimestamp.MinValue), Is.GreaterThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MinValue, t4), Is.LessThan(0));

            var t5 = new MessageTimestamp(new DateTime(localTicksToUtcTicks(1), DateTimeKind.Utc));
            Assert.That(MessageTimestamp.Compare(t5, MessageTimestamp.MinValue), Is.GreaterThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MinValue, t5), Is.LessThan(0));
        }


        [Test]
        public void MaximumIsGreaterThanAnyDateTime()
        {
            var t1 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Local));
            Assert.That(MessageTimestamp.Compare(t1, MessageTimestamp.MaxValue), Is.LessThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t1), Is.GreaterThan(0));

            var t2 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Utc));
            Assert.That(MessageTimestamp.Compare(t2, MessageTimestamp.MaxValue), Is.LessThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t2), Is.GreaterThan(0));

            var t3 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Unspecified));
            Assert.That(MessageTimestamp.Compare(t3, MessageTimestamp.MaxValue), Is.LessThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t3), Is.GreaterThan(0));

            var t4 = new MessageTimestamp(new DateTime(1, DateTimeKind.Local));
            Assert.That(MessageTimestamp.Compare(t4, MessageTimestamp.MaxValue), Is.LessThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t4), Is.GreaterThan(0));

            var t5 = new MessageTimestamp(new DateTime(1, DateTimeKind.Utc));
            Assert.That(MessageTimestamp.Compare(t5, MessageTimestamp.MaxValue), Is.LessThan(0));
            Assert.That(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t5), Is.GreaterThan(0));
        }

        [Test]
        public void LoselessFormatTest()
        {
            var d1 = new MessageTimestamp(new DateTime(2010, 10, 22, 3, 3, 4, DateTimeKind.Local));
            var d2 = new MessageTimestamp(new DateTime(2010, 10, 22, 3, 3, 4, DateTimeKind.Utc));
            var d3 = new MessageTimestamp(new DateTime(2010, 10, 22, 3, 3, 4, DateTimeKind.Unspecified));

            var d1_str = d1.StoreToLoselessFormat();
            var d2_str = d2.StoreToLoselessFormat();
            var d3_str = d3.StoreToLoselessFormat();
            var d1_restored = MessageTimestamp.ParseFromLoselessFormat(d1_str);
            var d2_restored = MessageTimestamp.ParseFromLoselessFormat(d2_str);
            var d3_restored = MessageTimestamp.ParseFromLoselessFormat(d3_str);
            Assert.That(0, Is.EqualTo(MessageTimestamp.Compare(d1, d1_restored)));
            Assert.That(0, Is.EqualTo(MessageTimestamp.Compare(d2, d2_restored)));
            Assert.That(0, Is.EqualTo(MessageTimestamp.Compare(d2, d2_restored)));
        }
    }
}

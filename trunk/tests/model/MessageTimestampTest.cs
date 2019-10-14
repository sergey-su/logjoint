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
			Assert.AreEqual(MessageTimestampTimezone.Unknown, m1.TimeZone);

			var m2 = new MessageTimestamp(new DateTime(123213213, DateTimeKind.Unspecified));
			Assert.AreEqual(MessageTimestampTimezone.Unknown, m2.TimeZone);

			var m3 = new MessageTimestamp(new DateTime(123213213, DateTimeKind.Utc));
			Assert.AreEqual(MessageTimestampTimezone.UTC, m3.TimeZone);
		}

		[Test]
		public void MinimumIsLessThanAnyDateTime()
		{
			Func<long, long> localTicksToUtcTicks = ticks => new DateTime(ticks, DateTimeKind.Local).ToUniversalTime().Ticks;

			var t1 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Local));
			Assert.IsTrue(MessageTimestamp.Compare(t1, MessageTimestamp.MinValue) > 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MinValue, t1) < 0);

			var t2 = new MessageTimestamp(new DateTime(localTicksToUtcTicks(3423), DateTimeKind.Utc));
			Assert.IsTrue(MessageTimestamp.Compare(t2, MessageTimestamp.MinValue) > 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MinValue, t2) < 0);

			var t3 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Unspecified));
			Assert.IsTrue(MessageTimestamp.Compare(t3, MessageTimestamp.MinValue) > 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MinValue, t3) < 0);

			var t4 = new MessageTimestamp(new DateTime(1, DateTimeKind.Local));
			Assert.IsTrue(MessageTimestamp.Compare(t4, MessageTimestamp.MinValue) > 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MinValue, t4) < 0);

			var t5 = new MessageTimestamp(new DateTime(localTicksToUtcTicks(1), DateTimeKind.Utc));
			Assert.IsTrue(MessageTimestamp.Compare(t5, MessageTimestamp.MinValue) > 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MinValue, t5) < 0);
		}


		[Test]
		public void MaximumIsGreaterThanAnyDateTime()
		{
			var t1 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Local));
			Assert.IsTrue(MessageTimestamp.Compare(t1, MessageTimestamp.MaxValue) < 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t1) > 0);

			var t2 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Utc));
			Assert.IsTrue(MessageTimestamp.Compare(t2, MessageTimestamp.MaxValue) < 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t2) > 0);

			var t3 = new MessageTimestamp(new DateTime(3423, DateTimeKind.Unspecified));
			Assert.IsTrue(MessageTimestamp.Compare(t3, MessageTimestamp.MaxValue) < 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t3) > 0);

			var t4 = new MessageTimestamp(new DateTime(1, DateTimeKind.Local));
			Assert.IsTrue(MessageTimestamp.Compare(t4, MessageTimestamp.MaxValue) < 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t4) > 0);

			var t5 = new MessageTimestamp(new DateTime(1, DateTimeKind.Utc));
			Assert.IsTrue(MessageTimestamp.Compare(t5, MessageTimestamp.MaxValue) < 0);
			Assert.IsTrue(MessageTimestamp.Compare(MessageTimestamp.MaxValue, t5) > 0);
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
			Assert.AreEqual(0, MessageTimestamp.Compare(d1, d1_restored));
			Assert.AreEqual(0, MessageTimestamp.Compare(d2, d2_restored));
			Assert.AreEqual(0, MessageTimestamp.Compare(d2, d2_restored));
		}
	}
}

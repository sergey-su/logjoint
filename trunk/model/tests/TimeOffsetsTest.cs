using System;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class TimeOffsetsTest
	{
		static readonly DateTime testDT1 = new DateTime(2016, 3, 4);
		static readonly DateTime testDT2 = new DateTime(2018, 1, 1, 1, 2, 3);
		static readonly DateTime testDT3 = new DateTime(2019, 1, 12, 11, 23, 9);
		static readonly DateTime testDT4 = new DateTime(2020, 1, 11, 1, 12, 3);

		ITimeOffsetsBuilder init;

		[SetUp]
		public void InitTest()
		{
			init = new TimeOffsets.Builder();
		}

		void TestDate(DateTime expectedOutputDate, DateTime inputDate)
		{
			var timeOffsets = init.ToTimeOffsets();
			var actualOutputDate = timeOffsets.Get(inputDate);
			Assert.AreEqual(expectedOutputDate, actualOutputDate);

			var inverseTimeOffsets = timeOffsets.Inverse();
			var actualInvserseOutputDate = inverseTimeOffsets.Get(expectedOutputDate);
			Assert.AreEqual(inputDate, actualInvserseOutputDate);
		}

		void TestEquality()
		{
			var timeOffsets = init.ToTimeOffsets();
			Assert.IsTrue(timeOffsets.Equals(timeOffsets));
			Assert.AreEqual(timeOffsets.GetHashCode(), timeOffsets.GetHashCode());

			var timeOffsets2 = timeOffsets.Inverse().Inverse();
			Assert.IsTrue(timeOffsets2.Equals(timeOffsets));
			Assert.IsTrue(timeOffsets.Equals(timeOffsets2));
			Assert.AreEqual(timeOffsets2.GetHashCode(), timeOffsets.GetHashCode());
		}

		void TestStringification()
		{
			var timeOffsets = init.ToTimeOffsets();
			ITimeOffsets parsed;
			var str = timeOffsets.ToString();
			Assert.IsTrue(TimeOffsets.TryParse(str, out parsed));
			Assert.IsTrue(parsed.Equals(timeOffsets));
			Assert.IsTrue(timeOffsets.Equals(parsed));
			Assert.AreEqual(timeOffsets.GetHashCode(), parsed.GetHashCode());
		}

		[Test]
		public void NoOffsetsTest()
		{
			TestDate(testDT1, testDT1);
			TestDate(testDT2, testDT2);
			TestEquality();
			TestStringification();
		}

		[Test]
		public void OnlyBaseOffsetTest()
		{
			var baseOffset = TimeSpan.FromSeconds(2.3);
			init.SetBaseOffset(baseOffset);
			TestDate(testDT1 + baseOffset, testDT1);
			TestDate(testDT2 + baseOffset, testDT2);
			TestEquality();
			TestStringification();

		}

		[Test]
		public void OneOffsetTest()
		{
			var baseOffset = TimeSpan.FromSeconds(6.3);
			init.SetBaseOffset(baseOffset);
			var offset1 = TimeSpan.FromSeconds(0.01);
			init.AddOffset(testDT1, offset1);

			TestDate(testDT1 - TimeSpan.FromHours(1) + baseOffset, testDT1 - TimeSpan.FromHours(1));
			TestDate(testDT1 + baseOffset + offset1, testDT1);
			TestDate(testDT1 + TimeSpan.FromSeconds(1) + baseOffset + offset1, testDT1 + TimeSpan.FromSeconds(1));

			TestEquality();
			TestStringification();
		}

		[Test]
		public void OneOffsetWithoutBaseOffsetTest()
		{
			var offset1 = TimeSpan.FromSeconds(0.01);
			init.AddOffset(testDT1, offset1);

			TestDate(testDT1 - TimeSpan.FromHours(1), testDT1 - TimeSpan.FromHours(1));
			TestDate(testDT1 + offset1, testDT1);
			TestDate(testDT1 + TimeSpan.FromSeconds(1) + offset1, testDT1 + TimeSpan.FromSeconds(1));

			TestEquality();
			TestStringification();
		}

		[Test]
		public void TwoOffsetsTest()
		{
			var baseOffset = TimeSpan.FromSeconds(9.3);
			init.SetBaseOffset(baseOffset);
			var offset1 = TimeSpan.FromSeconds(0.01);
			init.AddOffset(testDT1, offset1);
			var offset2 = TimeSpan.FromSeconds(0.08);
			init.AddOffset(testDT2, offset2);

			TestDate(testDT1 - TimeSpan.FromHours(2.3) + baseOffset, testDT1 - TimeSpan.FromHours(2.3));
			TestDate(testDT1 + baseOffset + offset1, testDT1);
			TestDate(testDT1 + TimeSpan.FromHours(3.4) + baseOffset + offset1, testDT1 + TimeSpan.FromHours(3.4));
			TestDate(testDT2 + baseOffset + offset1 + offset2, testDT2);
			TestDate(testDT2 + TimeSpan.FromDays(1.2) + baseOffset + offset1 + offset2, testDT2 + TimeSpan.FromDays(1.2));

			TestEquality();
			TestStringification();
		}

		[Test]
		public void FourOffsetsTest()
		{
			var baseOffset = TimeSpan.FromSeconds(9.3);
			init.SetBaseOffset(baseOffset);
			var o1 = TimeSpan.FromSeconds(0.01);
			init.AddOffset(testDT1, o1);
			var o2 = TimeSpan.FromSeconds(0.08);
			init.AddOffset(testDT2, o2);
			var o3 = TimeSpan.FromSeconds(0.001);
			init.AddOffset(testDT3, o3);
			var o4 = TimeSpan.FromSeconds(0.7);
			init.AddOffset(testDT4, o4);

			TestDate(testDT1 - TimeSpan.FromSeconds(1) + baseOffset, testDT1 - TimeSpan.FromSeconds(1));
			TestDate(testDT1 + baseOffset + o1, testDT1);
			TestDate(testDT1 + TimeSpan.FromMilliseconds(1) + baseOffset + o1, testDT1 + TimeSpan.FromMilliseconds(1));

			TestDate(testDT2 - TimeSpan.FromMinutes(1.1) + baseOffset + o1, testDT2 - TimeSpan.FromMinutes(1.1));
			TestDate(testDT2 + baseOffset + o1 + o2, testDT2);
			TestDate(testDT2 + TimeSpan.FromDays(34.2) + baseOffset + o1 + o2, testDT2 + TimeSpan.FromDays(34.2));

			TestDate(testDT3 - TimeSpan.FromMinutes(1.1) + baseOffset + o1 + o2, testDT3 - TimeSpan.FromMinutes(1.1));
			TestDate(testDT3 + baseOffset + o1 + o2 + o3, testDT3);
			TestDate(testDT3 + TimeSpan.FromDays(34.2) + baseOffset + o1 + o2 + o3, testDT3 + TimeSpan.FromDays(34.2));

			TestDate(testDT4 - TimeSpan.FromDays(0.1) + baseOffset + o1 + o2 + o3, testDT4 - TimeSpan.FromDays(0.1));
			TestDate(testDT4 + baseOffset + o1 + o2 + o3 + o4, testDT4);
			TestDate(testDT4 + TimeSpan.FromMilliseconds(0.1) + baseOffset + o1 + o2 + o3 + o4, testDT4 + TimeSpan.FromMilliseconds(0.1));

			TestEquality();
			TestStringification();
		}

		[Test]
		public void GreaterThanMaxValueResultTest()
		{
			init.SetBaseOffset(TimeSpan.FromSeconds(2));
			var offsets = init.ToTimeOffsets();
			Assert.AreEqual(DateTime.MaxValue, offsets.Get(DateTime.MaxValue - TimeSpan.FromSeconds(1)));
		}

		[Test]
		public void SmallerThanMinValueResultTest()
		{
			init.SetBaseOffset(TimeSpan.FromSeconds(-2));
			var offsets = init.ToTimeOffsets();
			Assert.AreEqual(DateTime.MinValue, offsets.Get(DateTime.MinValue + TimeSpan.FromSeconds(1)));
		}

		[Test]
		public void DuplicatedToOffsetsCallTest()
		{
			var offsets1 = init.ToTimeOffsets();
			var offsets2 = init.ToTimeOffsets();
			Assert.AreSame(offsets1, offsets2);
		}

		[Test]
		public void EmptyTimeOffsetsIsEmpty()
		{
			Assert.IsTrue(TimeOffsets.Empty.IsEmpty);
		}

		[Test]
		public void NotEmptyAdditionalOffsetMakesObjectIsNotEmpty()
		{
			init.AddOffset(testDT1, TimeSpan.FromMilliseconds(1));
			Assert.IsFalse(init.ToTimeOffsets().IsEmpty);
		}

		[Test]
		public void EmptyAdditionalOffsetKeepsObjectEmpty()
		{
			init.SetBaseOffset(TimeSpan.Zero);
			init.AddOffset(testDT1, TimeSpan.Zero);
			Assert.IsTrue(init.ToTimeOffsets().IsEmpty);
		}


		[Test]
		public void DefaultBaseOffsetTest()
		{
			Assert.AreEqual(TimeSpan.Zero, init.ToTimeOffsets().BaseOffset);
			Assert.AreEqual(TimeSpan.Zero, init.ToTimeOffsets().Inverse().BaseOffset);
		}

		[Test]
		public void NonDefaultBaseOffsetTest()
		{
			init.SetBaseOffset(TimeSpan.FromMinutes(1.7));
			Assert.AreEqual(TimeSpan.FromMinutes(1.7), init.ToTimeOffsets().BaseOffset);
			Assert.AreEqual(-TimeSpan.FromMinutes(1.7), init.ToTimeOffsets().Inverse().BaseOffset);
		}

		[Test]
		public void SettingBaseOffsetTwiceIsNotAllowed()
		{
			init.SetBaseOffset(TimeSpan.FromMinutes(1));
			Assert.Throws<InvalidOperationException>(() =>
				init.SetBaseOffset(TimeSpan.FromMinutes(1)));
		}

		[Test]
		public void EmptyObjectEqualityTest()
		{
			init.SetBaseOffset(TimeSpan.Zero);
			TestEquality();
			TestStringification();
		}

		[Test]
		public void EmptyObjectToStringTest()
		{
			Assert.AreEqual("00:00:00", init.ToTimeOffsets().ToString());
		}

		[Test]
		public void TimeSpanCanBeParsedAsTimeOffsetsObject()
		{
			ITimeOffsets parsed;
			Assert.IsTrue(TimeOffsets.TryParse("00:00:00", out parsed));
			Assert.AreEqual(TimeSpan.Zero, parsed.BaseOffset);

			Assert.IsTrue(TimeOffsets.TryParse("00:00:01", out parsed));
			Assert.AreEqual(TimeSpan.FromSeconds(1), parsed.BaseOffset);

			Assert.IsTrue(TimeOffsets.TryParse("00:00:00.001", out parsed));
			Assert.AreEqual(TimeSpan.FromMilliseconds(1), parsed.BaseOffset);
		}

		[Test]
		public void NullOrEmptyStringCanBeParsedAsTimeOffsetsObject()
		{
			ITimeOffsets parsed;
			Assert.IsFalse(TimeOffsets.TryParse(null, out parsed));
			Assert.IsFalse(TimeOffsets.TryParse("", out parsed));
		}
	}
}

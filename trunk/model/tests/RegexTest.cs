using System;
using LogJoint;
using LogJoint.RegularExpressions;
using System.Text;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class RegexTest
	{
		void TestAllImplementations(Action<IRegexFactory> tester)
		{
			tester(FCLRegexFactory.Instance);
			// tester(LJRegexFactory.Instance); todo: test LJ impl
		}

		void BasicReTest(IRegexFactory factory)
		{
			var re = factory.Create(@"^\s+(?<test>\d{2})", ReOptions.None);
			IMatch m = null;
			string buf = "   34   56";
			
			Assert.IsTrue(re.Match(buf, 0, 10, ref m));
			Assert.AreEqual(0, m.Index);
			Assert.AreEqual(5, m.Length);
			Assert.AreEqual(2, m.Groups.Length);
			Assert.AreEqual(3, m.Groups[1].Index);
			Assert.AreEqual(2, m.Groups[1].Length);

			Assert.IsTrue(re.Match(buf, 5, 5, ref m));
			Assert.AreEqual(8, m.Groups[1].Index);
			Assert.AreEqual(2, m.Groups[1].Length);
			
			Assert.IsFalse(re.Match(buf, 9, 1, ref m));
		}

		[Test]
		public void BasicReTest()
		{
			TestAllImplementations(BasicReTest);
		}

		enum StringSliceSetup
		{
			SliceOccupiesWholeBuffer,
			SliceIsAtTheBeginningOfBuffer,
			SliceIsAtTheEndOfBuffer,
			SliceIsInTheMiddleOfBuffer
		};

		static string GenerateGarbage(string template)
		{
			var builder = new StringBuilder();
			for (int i = 0; i < 5; ++i)
				builder.AppendFormat("daks " + template + " jksad 1212 ksajd" + template + "asa");
			return builder.ToString();
		}

		static StringSlice CreateTestSlice(string sliceValue, StringSliceSetup setup)
		{
			var garbage = GenerateGarbage(sliceValue);
			switch (setup)
			{
				case StringSliceSetup.SliceOccupiesWholeBuffer:
					return new StringSlice(sliceValue);
				case StringSliceSetup.SliceIsAtTheBeginningOfBuffer:
					return new StringSlice(sliceValue + garbage, 0, sliceValue.Length);
				case StringSliceSetup.SliceIsAtTheEndOfBuffer:
					return new StringSlice(garbage + sliceValue, garbage.Length, sliceValue.Length);
				case StringSliceSetup.SliceIsInTheMiddleOfBuffer:
					return new StringSlice(garbage + sliceValue + garbage, garbage.Length, sliceValue.Length);
				default:
					Assert.Fail();
					return new StringSlice();
			}
		}

		void TestOnAllSliceVariations(string sliceValue, Action<StringSlice> tester)
		{
			foreach (var config in new StringSliceSetup[] {
				StringSliceSetup.SliceOccupiesWholeBuffer,
				StringSliceSetup.SliceIsAtTheBeginningOfBuffer,
				StringSliceSetup.SliceIsAtTheEndOfBuffer,
				StringSliceSetup.SliceIsInTheMiddleOfBuffer
			})
			{
				var slice = CreateTestSlice(sliceValue, config);
				tester(slice);
			}
		}

		void ForwardStringSliceMatchingTest(IRegexFactory factory)
		{
			var re = factory.Create(@"f\wo", ReOptions.None);
			IMatch m = null;

			var testString = "bar foo boo";
			TestOnAllSliceVariations(testString, slice =>
			{
				Action testMatch = () => { Assert.AreEqual(4, m.Index); Assert.AreEqual(3, m.Length); };

				Assert.IsTrue(re.Match(slice, 0, ref m));
				testMatch();

				Assert.IsTrue(re.Match(slice, 1, ref m));
				testMatch();

				Assert.IsTrue(re.Match(slice, 4, ref m));
				testMatch();

				Assert.IsFalse(re.Match(slice, 5, ref m));
				Assert.IsFalse(re.Match(slice, testString.Length, ref m));
			});

			Assert.IsFalse(re.Match(new StringSlice("foo 222", 1, 5), 0, ref m));
			Assert.IsFalse(re.Match(new StringSlice("111 foo 333", 0, 6), 0, ref m));
		}

		[Test]
		public void ForwardStringSliceMatchingTest()
		{
			TestAllImplementations(ForwardStringSliceMatchingTest);
		}

		void ReverseStringSliceMatchingTest(IRegexFactory factory)
		{
			var re = factory.Create(@"f\wo", ReOptions.RightToLeft);
			IMatch m = null;

			var testString = "bar foo boo";
			TestOnAllSliceVariations(testString, slice =>
			{
				Action testMatch = () => { Assert.AreEqual(4, m.Index); Assert.AreEqual(3, m.Length); };

				Assert.IsFalse(re.Match(slice, 0, ref m));
				Assert.IsFalse(re.Match(slice, 1, ref m));
				Assert.IsFalse(re.Match(slice, 4, ref m));
				Assert.IsFalse(re.Match(slice, 6, ref m));

				Assert.IsTrue(re.Match(slice, 7, ref m));
				testMatch();

				Assert.IsTrue(re.Match(slice, testString.Length, ref m));
				testMatch();
			});

			Assert.IsFalse(re.Match(new StringSlice("foo 222", 1, 5), 5, ref m));
			Assert.IsFalse(re.Match(new StringSlice("111 foo 333", 0, 6), 6, ref m));
		}

		[Test]
		public void ReverseStringSliceMatchingTest()
		{
			TestAllImplementations(ReverseStringSliceMatchingTest);
		}

		void CaseMatchingTest(IRegexFactory factory)
		{
			var re = factory.Create(@"B(o+)m", ReOptions.None);
			IMatch m = null;

			Assert.IsTrue(re.Match("Boooom", 0, ref m));
			Assert.IsFalse(re.Match("boooom", 0, ref m));

			re = factory.Create(@"B(o+)m", ReOptions.IgnoreCase);
			m = null;
			Assert.IsTrue(re.Match("Boooom", 0, ref m));
			Assert.IsTrue(re.Match("boooom", 0, ref m));
		}

		[Test]
		public void CaseMatchingTest()
		{
			TestAllImplementations(CaseMatchingTest);
		}
	}
}

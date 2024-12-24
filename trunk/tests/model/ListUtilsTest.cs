﻿using System;
using System.Text;
using System.Collections.Generic;
using LogJoint;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class ListUtilsTest
	{

		[Test]
		public void LowerBoundTest()
		{
			List<int> list = new List<int>(new int[] { 0, 2, 4, 5, 6 });

			Assert.That(0, Is.EqualTo(ListUtils.LowerBound(list, -1)));
			Assert.That(0, Is.EqualTo(ListUtils.LowerBound(list, 0)));
			Assert.That(2, Is.EqualTo(ListUtils.LowerBound(list, 3)));
			Assert.That(2, Is.EqualTo(ListUtils.LowerBound(list, 4)));
			Assert.That(4, Is.EqualTo(ListUtils.LowerBound(list, 6)));
			Assert.That(5, Is.EqualTo(ListUtils.LowerBound(list, 7)));
			Assert.That(2, Is.EqualTo(ListUtils.LowerBound(list, 0, 3, 4, Comparer<int>.Default)));
			Assert.That(3, Is.EqualTo(ListUtils.LowerBound(list, 0, 3, 5, Comparer<int>.Default)));
			Assert.That(3, Is.EqualTo(ListUtils.LowerBound(list, 0, 3, 7, Comparer<int>.Default)));
			Assert.That(1, Is.EqualTo(ListUtils.LowerBound(list, 1, 3, 0, Comparer<int>.Default)));
		}

		[Test]
		public void UpperBoundTest()
		{
			List<int> list = new List<int>(new int[] { 0, 2, 4, 5, 6 });

			Assert.That(0, Is.EqualTo(ListUtils.UpperBound(list, -1)));
			Assert.That(1, Is.EqualTo(ListUtils.UpperBound(list, 0)));
			Assert.That(2, Is.EqualTo(ListUtils.UpperBound(list, 3)));
			Assert.That(3, Is.EqualTo(ListUtils.UpperBound(list, 4)));
			Assert.That(5, Is.EqualTo(ListUtils.UpperBound(list, 6)));
			Assert.That(5, Is.EqualTo(ListUtils.UpperBound(list, 7)));
			Assert.That(3, Is.EqualTo(ListUtils.UpperBound(list, 0, 3, 4, Comparer<int>.Default)));
			Assert.That(3, Is.EqualTo(ListUtils.UpperBound(list, 0, 3, 5, Comparer<int>.Default)));
			Assert.That(0, Is.EqualTo(ListUtils.UpperBound(list, 0, 3, -1, Comparer<int>.Default)));
			Assert.That(1, Is.EqualTo(ListUtils.UpperBound(list, 1, 3, 0, Comparer<int>.Default)));
			Assert.That(2, Is.EqualTo(ListUtils.UpperBound(list, 1, 3, 2, Comparer<int>.Default)));
		}


		[Test]
		public void BinarySearchTest()
		{
			List<int> list = new List<int>(new int[] { 0, 2, 4, 5, 6 });

			Assert.That(1, Is.EqualTo(ListUtils.BinarySearch(list, 0, list.Count, 
				delegate(int x) { return x < 2; })));
			Assert.That(2, Is.EqualTo(ListUtils.BinarySearch(list, 0, list.Count,
				delegate(int x) { return x <= 2; })));
			Assert.That(3, Is.EqualTo(ListUtils.BinarySearch(list, 3, 3,
				delegate(int x) { return x <= 2; })));
		}

		static void TestBound(int value, ValueBound bound, int expectedIdx)
		{
			List<int> lst = new List<int>(new int[] { 0, 2, 2, 2, 3, 5, 7, 8, 8, 10 });
			int actual = ListUtils.GetBound(lst, value, bound, Comparer<int>.Default);
			Assert.That(expectedIdx, Is.EqualTo(actual));
		}

		[Test]
		public void TestLowerBound()
		{
			TestBound(2, ValueBound.Lower, 1);
			TestBound(1, ValueBound.Lower, 1);
			TestBound(-2, ValueBound.Lower, 0);
			TestBound(20, ValueBound.Lower, 10);
		}

		[Test]
		public void TestUpperBound()
		{
			TestBound(2, ValueBound.Upper, 4);
			TestBound(1, ValueBound.Upper, 1);
			TestBound(-2, ValueBound.Upper, 0);
			TestBound(20, ValueBound.Upper, 10);
		}

		[Test]
		public void TestLowerRevBound()
		{
			TestBound(2, ValueBound.LowerReversed, 3);
			TestBound(1, ValueBound.LowerReversed, 0);
			TestBound(-2, ValueBound.LowerReversed, -1);
			TestBound(20, ValueBound.LowerReversed, 9);
		}

		[Test]
		public void TestUpperRevBound()
		{
			TestBound(2, ValueBound.UpperReversed, 0);
			TestBound(1, ValueBound.UpperReversed, 0);
			TestBound(-2, ValueBound.UpperReversed, -1);
			TestBound(20, ValueBound.UpperReversed, 9);
		}
	}


}

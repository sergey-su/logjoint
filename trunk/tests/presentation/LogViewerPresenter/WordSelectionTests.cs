using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using LogJoint.UI.Presenters.LogViewer;

namespace LogJoint.UI.Presenters.Tests.WordSelectionTests
{
	[TestFixture]
	public class WordSelectionTests
	{
		IWordSelection wordSelection = new WordSelection(RegularExpressions.FCLRegexFactory.Instance);

		[Test]
		public void FindsRegularWord()
		{
			Assert.That(Tuple.Create(0, 4), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("test foo bar"), 0)));
			Assert.That(Tuple.Create(0, 4), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("test foo bar"), 1)));
			Assert.That(Tuple.Create(0, 4), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("test foo bar"), 4)));
			Assert.That(Tuple.Create(5, 8), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("test foo bar"), 5)));
			Assert.That(Tuple.Create(9, 12), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("test foo bar"), 12)));
		}

		[Test]
		public void DoesNotFindWordsAmongNonWordChars()
		{
			Assert.That(wordSelection.FindWordBoundaries(new StringSlice("-=-~ test"), 2), Is.Null);
		}

		[Test]
		public void FindsGuid()
		{
			Assert.That(Tuple.Create(3, 39), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= f427e8d3-8779-4a0f-9cfd-735a10a00a0b test"), 4)));
			Assert.That(Tuple.Create(4, 40), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= {f427e8d3-8779-4a0f-9cfd-735a10a00a0b} test"), 4)));
		}

		[Test]
		public void FindsIPv4()
		{
			Assert.That(Tuple.Create(3, 18), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 192.168.255.255 test"), 4)));
			Assert.That(Tuple.Create(3, 19), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 10.11.12.13:8000 test"), 4)));
			Assert.That(Tuple.Create(3, 15), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 127.0.0.1:25 test"), 4)));

			Assert.That(Tuple.Create(3, 6), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 127.0.999.1 test"), 4)));
			Assert.That(Tuple.Create(3, 6), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 127.0.999999999999999999999999999.1 test"), 4)));
			Assert.That(Tuple.Create(3, 6), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 127.0.0.1:99999 test"), 4)));
			Assert.That(Tuple.Create(3, 6), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 127.0.0.1:999999999999999999999999999 test"), 4)));
		}

		[Test]
		public void FindsIPv6()
		{
			Assert.That(Tuple.Create(3, 42), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 2001:0db8:85a3:0000:0000:8a2e:0370:7334 test"), 4)));
			Assert.That(Tuple.Create(3, 49), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= [2001:0db8:85a3:0000:0000:8a2e:0370:7334]:8080 test"), 4)));
			Assert.That(Tuple.Create(3, 16), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= 2001:db8::1:0 test"), 4)));
			Assert.That(Tuple.Create(3, 6), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= ::1 test"), 6)));
			Assert.That(Tuple.Create(3, 13), Is.EqualTo(wordSelection.FindWordBoundaries(new StringSlice("-= [::1]:8080 test"), 6)));
		}
	}
}

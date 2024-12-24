using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LogJoint;
using System.Globalization;
using System.Xml;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class SpikeUnitTest
	{
		[Test]
		public void SpikeUnitTest1()
		{
			var d1 = new DateTime(2010, 10, 22, 3, 3, 4, DateTimeKind.Local);
			var d2 = new DateTime(2010, 10, 22, 3, 3, 4, DateTimeKind.Utc);
			var d3 = new DateTime(2010, 10, 22, 3, 3, 4, DateTimeKind.Unspecified);

			Action<DateTime, DateTime> assertDatesAreEqual = (x, y) =>
				Assert.That(
					x == y && !(x != y) &&
					!(x < y) && !(x > y) && !(y < x) && !(y > x) &&
					x.CompareTo(y) == 0,
					Is.True
				);

			// DateTimeKind is ignored on comparision
			assertDatesAreEqual(d1, d2);
			assertDatesAreEqual(d1, d3);
			assertDatesAreEqual(d2, d3);

			Func<string, string, DateTime> toDateTime = (str, fmt) =>
				DateTime.ParseExact(str, fmt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

			// log that contains tz info will be parsed to Utc DateTime
			var parsedUtc = toDateTime("2010-10-22T06:03:04.0000000+04:00", "yyyy-MM-ddTHH:mm:ss.fffffffzzzzzz");
			Assert.That(parsedUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
			assertDatesAreEqual(parsedUtc, new DateTime(2010, 10, 22, 2, 3, 4));

			// log that does not contain tz info will be parsed to Unspecified DateTime
			var pasredUnspec = toDateTime("2010-10-22T02:03:04.0000000", "yyyy-MM-ddTHH:mm:ss.fffffff");
			Assert.That(pasredUnspec.Kind, Is.EqualTo(DateTimeKind.Unspecified));
			assertDatesAreEqual(pasredUnspec, new DateTime(2010, 10, 22, 2, 3, 4));

			// log that is parsed with format specifier K will be parsed as Utc when possible
			var parsedUtc2 = toDateTime("2010-10-22T06:03:04.0000000+05:00", "yyyy-MM-ddTHH:mm:ss.fffffffK");
			Assert.That(parsedUtc2.Kind, Is.EqualTo(DateTimeKind.Utc));
			assertDatesAreEqual(parsedUtc2, new DateTime(2010, 10, 22, 1, 3, 4));
			var parsedUtc3 = toDateTime("2010-10-22T03:03:04.0000000Z", "yyyy-MM-ddTHH:mm:ss.fffffffK");
			Assert.That(parsedUtc2.Kind, Is.EqualTo(DateTimeKind.Utc));
			var parsedUnspec2 = toDateTime("2010-10-22T03:03:04.0000000", "yyyy-MM-ddTHH:mm:ss.fffffffK");
			Assert.That(parsedUnspec2.Kind, Is.EqualTo(DateTimeKind.Unspecified));


			// Kind-preserving string conversions
			Func<DateTime, string> toStringLoseless = d =>
				XmlConvert.ToString(d, XmlDateTimeSerializationMode.RoundtripKind);
			Func<string, DateTime> fromStringLoseless = s =>
				XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);

			var d1_str = toStringLoseless(d1);
			var d2_str = toStringLoseless(d2);
			var d3_str = toStringLoseless(d3);
			var d1_restored = fromStringLoseless(d1_str);
			var d2_restored = fromStringLoseless(d2_str);
			var d3_restored = fromStringLoseless(d3_str);
			Assert.That(d1.Kind, Is.EqualTo(d1_restored.Kind));
			assertDatesAreEqual(d1, d1_restored);
			Assert.That(d2.Kind, Is.EqualTo(d2_restored.Kind));
			assertDatesAreEqual(d2, d2_restored);
			Assert.That(d3.Kind, Is.EqualTo(d3_restored.Kind));
			assertDatesAreEqual(d3, d3_restored);
		}


	}
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public class GuessDateLocale
	{
		public DateTime ParseDateGuessingLocale(string value, string format, DateTimeStyles parseStyle = DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite)
		{
			DateTime ret;

			if (lastSuccessfulDateTimeFormat != null)
			{
				if (DateTime.TryParseExact(value, format, lastSuccessfulDateTimeFormat, parseStyle, out ret))
				{
					return ret;
				}
			}

			CultureInfo[] cinfos = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
			foreach (CultureInfo ci in cinfos)
			{
				if (DateTime.TryParseExact(value, format, ci.DateTimeFormat,
					parseStyle, out ret))
				{
					lastSuccessfulDateTimeFormat = ci.DateTimeFormat;
					return ret;
				}
			}

			throw new FormatException(string.Format("Cannot parse '{0}' as datetime with format '{1}'", value, format));
		}

		DateTimeFormatInfo lastSuccessfulDateTimeFormat;
	};

	public class TimeZonesParsing
	{
		public static TimeSpan GetTimeZoneOffset(string timeZoneAbbreviation)
		{
			TimeZoneInfo tzi;
			if (!zonesMap.TryGetValue(timeZoneAbbreviation, out tzi))
				throw new TimeZoneNotFoundException();
			return tzi.Offset;
		}

		struct TimeZoneInfo
		{
			public readonly string Abbreviation;
			public readonly string Name;
			public readonly TimeSpan Offset;

			public TimeZoneInfo(string abb, string name, TimeSpan offset)
			{
				Abbreviation = abb;
				Name = name;
				Offset = offset;
			}
		};

		static TimeZonesParsing()
		{
			Add(new TimeZoneInfo("ACDT","Australian Central Daylight Time",TimeSpan.Parse("10:30")));
			Add(new TimeZoneInfo("ACST","Australian Central Standard Time",TimeSpan.Parse("09:30")));
			Add(new TimeZoneInfo("ACT","ASEAN Common Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("ADT","Atlantic Daylight Time",TimeSpan.Parse("-03:00")));
			Add(new TimeZoneInfo("AEDT","Australian Eastern Daylight Time",TimeSpan.Parse("11:00")));
			Add(new TimeZoneInfo("AEST","Australian Eastern Standard Time",TimeSpan.Parse("10:00")));
			Add(new TimeZoneInfo("AFT","Afghanistan Time",TimeSpan.Parse("04:30")));
			Add(new TimeZoneInfo("AKDT","Alaska Daylight Time",TimeSpan.Parse("-08:00")));
			Add(new TimeZoneInfo("AKST","Alaska Standard Time",TimeSpan.Parse("-09:00")));
			//Add(new TimeZoneInfo("AMST","Armenia Summer Time",TimeSpan.Parse("05:00")));
			Add(new TimeZoneInfo("AMT","Armenia Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("ART","Argentina Time",TimeSpan.Parse("-03:00")));
			//Add(new TimeZoneInfo("AST","Arab Standard Time (Kuwait, Riyadh));",TimeSpan.Parse("03:00")));
			//Add(new TimeZoneInfo("AST","Arabian Standard Time (Abu Dhabi, Muscat));",TimeSpan.Parse("04:00")));
			//Add(new TimeZoneInfo("AST","Arabic Standard Time (Baghdad));",TimeSpan.Parse("03:00")));
			Add(new TimeZoneInfo("AST","Atlantic Standard Time",TimeSpan.Parse("-04:00")));
			Add(new TimeZoneInfo("AWDT","Australian Western Daylight Time",TimeSpan.Parse("09:00")));
			Add(new TimeZoneInfo("AWST","Australian Western Standard Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("AZOST","Azores Standard Time",TimeSpan.Parse("-01:00")));
			Add(new TimeZoneInfo("AZT","Azerbaijan Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("BDT","Brunei Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("BIOT","British Indian Ocean Time",TimeSpan.Parse("06:00")));
			Add(new TimeZoneInfo("BIT","Baker Island Time",TimeSpan.Parse("-12:00")));
			Add(new TimeZoneInfo("BOT","Bolivia Time",TimeSpan.Parse("-04:00")));
			Add(new TimeZoneInfo("BRT","Brasilia Time",TimeSpan.Parse("-03:00")));
			//Add(new TimeZoneInfo("BST","Bangladesh Standard Time",TimeSpan.Parse("06:00")));
			Add(new TimeZoneInfo("BST","British Summer Time (British Standard Time from Feb 1968 to Oct 1971));",TimeSpan.Parse("01:00")));
			Add(new TimeZoneInfo("BTT","Bhutan Time",TimeSpan.Parse("06:00")));
			Add(new TimeZoneInfo("CAT","Central Africa Time",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("CCT","Cocos Islands Time",TimeSpan.Parse("06:30")));
			Add(new TimeZoneInfo("CDT","Central Daylight Time (North America));",TimeSpan.Parse("-05:00")));
			Add(new TimeZoneInfo("CEDT","Central European Daylight Time",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("CEST","Central European Summer Time (Cf. HAEC));",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("CET","Central European Time",TimeSpan.Parse("01:00")));
			Add(new TimeZoneInfo("CHADT","Chatham Daylight Time",TimeSpan.Parse("13:45")));
			Add(new TimeZoneInfo("CHAST","Chatham Standard Time",TimeSpan.Parse("12:45")));
			Add(new TimeZoneInfo("CIST","Clipperton Island Standard Time",TimeSpan.Parse("-08:00")));
			Add(new TimeZoneInfo("CKT","Cook Island Time",TimeSpan.Parse("-10:00")));
			Add(new TimeZoneInfo("CLST","Chile Summer Time",TimeSpan.Parse("-03:00")));
			Add(new TimeZoneInfo("CLT","Chile Standard Time",TimeSpan.Parse("-04:00")));
			Add(new TimeZoneInfo("COST","Colombia Summer Time",TimeSpan.Parse("-04:00")));
			Add(new TimeZoneInfo("COT","Colombia Time",TimeSpan.Parse("-05:00")));
			Add(new TimeZoneInfo("CST","Central Standard Time (North America))",TimeSpan.Parse("-06:00")));
			//Add(new TimeZoneInfo("CST","China Standard Time",TimeSpan.Parse("08:00")));
			//Add(new TimeZoneInfo("CST","Central Standard Time (Australia))",TimeSpan.Parse("09:30")));
			Add(new TimeZoneInfo("CT","China Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("CVT","Cape Verde Time",TimeSpan.Parse("-01:00")));
			Add(new TimeZoneInfo("CXT","Christmas Island Time",TimeSpan.Parse("07:00")));
			Add(new TimeZoneInfo("CHST","Chamorro Standard Time",TimeSpan.Parse("10:00")));
			Add(new TimeZoneInfo("DFT","AIX specific equivalent of Central European Time",TimeSpan.Parse("01:00")));
			Add(new TimeZoneInfo("EAST","Easter Island Standard Time",TimeSpan.Parse("-06:00")));
			Add(new TimeZoneInfo("EAT","East Africa Time",TimeSpan.Parse("03:00")));
			Add(new TimeZoneInfo("ECT","Eastern Caribbean Time (does not recognise DST))",TimeSpan.Parse("-04:00")));
			//Add(new TimeZoneInfo("ECT","Ecuador Time",TimeSpan.Parse("-05:00")));
			Add(new TimeZoneInfo("EDT","Eastern Daylight Time (North America));",TimeSpan.Parse("-04:00")));
			Add(new TimeZoneInfo("EEDT","Eastern European Daylight Time",TimeSpan.Parse("03:00")));
			Add(new TimeZoneInfo("EEST","Eastern European Summer Time",TimeSpan.Parse("03:00")));
			Add(new TimeZoneInfo("EET","Eastern European Time",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("EST","Eastern Standard Time (North America));",TimeSpan.Parse("-05:00")));
			Add(new TimeZoneInfo("FJT","Fiji Time",TimeSpan.Parse("12:00")));
			Add(new TimeZoneInfo("FKST","Falkland Islands Summer Time",TimeSpan.Parse("-03:00")));
			Add(new TimeZoneInfo("FKT","Falkland Islands Time",TimeSpan.Parse("-04:00")));
			Add(new TimeZoneInfo("GALT","Galapagos Time",TimeSpan.Parse("-06:00")));
			Add(new TimeZoneInfo("GET","Georgia Standard Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("GFT","French Guiana Time",TimeSpan.Parse("-03:00")));
			Add(new TimeZoneInfo("GILT","Gilbert Island Time",TimeSpan.Parse("12:00")));
			Add(new TimeZoneInfo("GIT","Gambier Island Time",TimeSpan.Parse("-09:00")));
			Add(new TimeZoneInfo("GMT","Greenwich Mean Time",new TimeSpan()));
			Add(new TimeZoneInfo("GST","South Georgia and the South Sandwich Islands",TimeSpan.Parse("-02:00")));
			//Add(new TimeZoneInfo("GST","Gulf Standard Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("GYT","Guyana Time",TimeSpan.Parse("-04:00")));
			Add(new TimeZoneInfo("HADT","Hawaii-Aleutian Daylight Time",TimeSpan.Parse("-09:00")));
			Add(new TimeZoneInfo("HAEC","Heure Avancee d'Europe Centrale francised name for CEST",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("HAST","Hawaii-Aleutian Standard Time",TimeSpan.Parse("-10:00")));
			Add(new TimeZoneInfo("HKT","Hong Kong Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("HMT","Heard and McDonald Islands Time",TimeSpan.Parse("05:00")));
			Add(new TimeZoneInfo("HST","Hawaii Standard Time",TimeSpan.Parse("-10:00")));
			Add(new TimeZoneInfo("ICT","Indochina Time",TimeSpan.Parse("07:00")));
			Add(new TimeZoneInfo("IDT","Israeli Daylight Time",TimeSpan.Parse("03:00")));
			Add(new TimeZoneInfo("IRKT","Irkutsk Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("IRST","Iran Standard Time",TimeSpan.Parse("03:30")));
			Add(new TimeZoneInfo("IST","Indian Standard Time",TimeSpan.Parse("05:30")));
			//Add(new TimeZoneInfo("IST","Irish Summer Time",TimeSpan.Parse("01:00")));
			//Add(new TimeZoneInfo("IST","Israel Standard Time",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("JST","Japan Standard Time",TimeSpan.Parse("09:00")));
			Add(new TimeZoneInfo("KRAT","Krasnoyarsk Time",TimeSpan.Parse("07:00")));
			Add(new TimeZoneInfo("KST","Korea Standard Time",TimeSpan.Parse("09:00")));
			Add(new TimeZoneInfo("LHST","Lord Howe Standard Time",TimeSpan.Parse("10:30")));
			Add(new TimeZoneInfo("LINT","Line Islands Time",TimeSpan.Parse("14:00")));
			Add(new TimeZoneInfo("MAGT","Magadan Time",TimeSpan.Parse("11:00")));
			Add(new TimeZoneInfo("MDT","Mountain Daylight Time (North America));",TimeSpan.Parse("-06:00")));
			Add(new TimeZoneInfo("MET","Middle European Time Same zone as CET",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("MEST","Middle European Saving Time Same zone as CEST",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("MIT","Marquesas Islands Time",TimeSpan.Parse("-09:30")));
			Add(new TimeZoneInfo("MSD","Moscow Summer Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("MSK","Moscow Standard Time",TimeSpan.Parse("03:00")));
			//Add(new TimeZoneInfo("MST","Malaysian Standard Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("MST","Mountain Standard Time (North America));",TimeSpan.Parse("-07:00")));
			//Add(new TimeZoneInfo("MST","Myanmar Standard Time",TimeSpan.Parse("06:30")));
			Add(new TimeZoneInfo("MUT","Mauritius Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("MYT","Malaysia Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("NDT","Newfoundland Daylight Time",TimeSpan.Parse("-02:30")));
			Add(new TimeZoneInfo("NFT","Norfolk Time[1]",TimeSpan.Parse("11:30")));
			Add(new TimeZoneInfo("NPT","Nepal Time",TimeSpan.Parse("05:45")));
			Add(new TimeZoneInfo("NST","Newfoundland Standard Time",TimeSpan.Parse("-03:30")));
			Add(new TimeZoneInfo("NT","Newfoundland Time",TimeSpan.Parse("-03:30")));
			Add(new TimeZoneInfo("NZDT","New Zealand Daylight Time",TimeSpan.Parse("13:00")));
			Add(new TimeZoneInfo("NZST","New Zealand Standard Time",TimeSpan.Parse("12:00")));
			Add(new TimeZoneInfo("OMST","Omsk Time",TimeSpan.Parse("06:00")));
			Add(new TimeZoneInfo("PDT","Pacific Daylight Time (North America));",TimeSpan.Parse("-07:00")));
			Add(new TimeZoneInfo("PETT","Kamchatka Time",TimeSpan.Parse("12:00")));
			Add(new TimeZoneInfo("PHOT","Phoenix Island Time",TimeSpan.Parse("13:00")));
			Add(new TimeZoneInfo("PKT","Pakistan Standard Time",TimeSpan.Parse("05:00")));
			Add(new TimeZoneInfo("PST","Pacific Standard Time (North America))",TimeSpan.Parse("-08:00")));
			//Add(new TimeZoneInfo("PST","Philippine Standard Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("RET","Reunion Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("SAMT","Samara Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("SAST","South African Standard Time",TimeSpan.Parse("02:00")));
			Add(new TimeZoneInfo("SBT","Solomon Islands Time",TimeSpan.Parse("11:00")));
			Add(new TimeZoneInfo("SCT","Seychelles Time",TimeSpan.Parse("04:00")));
			Add(new TimeZoneInfo("SGT","Singapore Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("SLT","Sri Lanka Time",TimeSpan.Parse("05:30")));
			//Add(new TimeZoneInfo("SST","Samoa Standard Time",TimeSpan.Parse("-11:00")));
			Add(new TimeZoneInfo("SST","Singapore Standard Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("TAHT","Tahiti Time",TimeSpan.Parse("-10:00")));
			Add(new TimeZoneInfo("THA","Thailand Standard Time",TimeSpan.Parse("07:00")));
			Add(new TimeZoneInfo("UTC","Coordinated Universal Time",new TimeSpan()));
			Add(new TimeZoneInfo("UYST","Uruguay Summer Time",TimeSpan.Parse("-02:00")));
			Add(new TimeZoneInfo("UYT","Uruguay Standard Time",TimeSpan.Parse("-03:00")));
			Add(new TimeZoneInfo("VET","Venezuelan Standard Time",TimeSpan.Parse("-04:30")));
			Add(new TimeZoneInfo("VLAT","Vladivostok Time",TimeSpan.Parse("10:00")));
			Add(new TimeZoneInfo("WAT","West Africa Time",TimeSpan.Parse("01:00")));
			Add(new TimeZoneInfo("WEDT","Western European Daylight Time",TimeSpan.Parse("01:00")));
			Add(new TimeZoneInfo("WEST","Western European Summer Time",TimeSpan.Parse("01:00")));
			Add(new TimeZoneInfo("WET","Western European Time",new TimeSpan()));
			Add(new TimeZoneInfo("WST","Western Standard Time",TimeSpan.Parse("08:00")));
			Add(new TimeZoneInfo("YAKT","Yakutsk Time",TimeSpan.Parse("09:00")));
			Add(new TimeZoneInfo("YEKT","Yekaterinburg Time",TimeSpan.Parse("05:00")));
		}

		static void Add(TimeZoneInfo tzi)
		{
			zonesMap[tzi.Abbreviation] = tzi;
		}

		static readonly Dictionary<string, TimeZoneInfo> zonesMap = new Dictionary<string,TimeZoneInfo>();
	};

	public static class DateTimeFormatParsing
	{
		[Flags]
		public enum DateTimeFormatFlag
		{
			None = 0,
			ContainsYear = 1,
			ContainsMonth = 2,
			ContainsDay = 4,
			ContainsHour = 8,
			ContainsMinutes = 16,
			ContainsSeconds = 32,
			ContainsSecondFraction = 64,
			ContainsTimeZone = 128,
			/// <summary>
			/// Date time format string contains format specifiers that render differently in different cultures.
			/// </summary>
			IsCultureDependent = 1024
		};

		public struct ParsedDateTimeFormat
		{
			public DateTimeFormatFlag Flags;
			public string Regex;
		};

		public static ParsedDateTimeFormat ParseDateTimeFormat(string formatString, CultureInfo culture)
		{
			if (formatString == null)
				throw new ArgumentNullException("formatString");
			if (culture == null)
				throw new ArgumentNullException("culture");

			ParsedDateTimeFormat? parsedStdFormat = ParseStandardDateTimeFormat(formatString, culture);
			if (parsedStdFormat.HasValue)
				return parsedStdFormat.Value;

			return ParseCustomDateTimeFormat(formatString, culture);
		}

		private static StringBuilder AppandMatcherForOneOf(StringBuilder regexBuilder, IEnumerable<string> options)
		{
			return regexBuilder.AppendFormat(@"  ({0})", options.Select(n => "(" + Regex.Escape(n) + ")").Aggregate((ret, n) => ret + "|" + n));
		}

		public static ParsedDateTimeFormat ParseCustomDateTimeFormat(string formatString, CultureInfo culture)
		{
			if (formatString == null)
				throw new ArgumentNullException("formatString");

			DateTimeFormatFlag flags = DateTimeFormatFlag.None;
			StringBuilder re = new StringBuilder();
			re.AppendLine();
			foreach (var t in TokenizeCustomDatePattern(formatString))
			{
				if (!t.IsSpecifier)
					re.AppendFormat("  {0} # fixed string '{1}'{2}", Regex.Escape(t.Value), t.Value, Environment.NewLine);
				else
				switch (t.Value)
				{
					case "d":
						re.AppendLine(@"  \d{1,2} # day of the month");
						flags |= DateTimeFormatFlag.ContainsDay;
						break;
					case "dd":
						re.AppendLine(@"  \d{2} # day of the month");
						flags |= DateTimeFormatFlag.ContainsDay;
						break;
					case "ddd":
						flags |= DateTimeFormatFlag.ContainsDay;
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							AppandMatcherForOneOf(re, culture.DateTimeFormat.AbbreviatedDayNames);
						else
							re.Append(@"  \w+");
						re.AppendLine(" # abbreviated name of the day of the week");
						break;
					case "dddd":
						flags |= DateTimeFormatFlag.ContainsDay;
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							AppandMatcherForOneOf(re, culture.DateTimeFormat.DayNames);
						else
							re.Append(@"  \w+");
						re.AppendLine(" #  full name of the day of the week");
						break;
					case "f":
					case "ff":
					case "fff":
					case "ffff":
					case "fffff":
					case "ffffff":
					case "fffffff":
						re.AppendFormat(@"  \d{0}{1}{2} # the most significant digits of the seconds fraction{3}", "{", t.Value.Length, "}", Environment.NewLine);
						flags |= DateTimeFormatFlag.ContainsSecondFraction;
						break;
					case "F":
					case "FF":
					case "FFF":
					case "FFFF":
					case "FFFFF":
					case "FFFFFF":
					case "FFFFFFF":
						re.AppendFormat(@"  (\d{0}{1}{2})? # the most significant digits of the seconds fraction (no trailing zeros){3}", "{", t.Value.Length, "}", Environment.NewLine);
						flags |= DateTimeFormatFlag.ContainsSecondFraction;
						break; 
					case "g":
					case "gg":
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							AppandMatcherForOneOf(re, culture.Calendar.Eras.Select(era => culture.DateTimeFormat.GetAbbreviatedEraName(era)));
						else
							re.Append(@"  .+");
						re.AppendLine(@" # the era");
						break;
					case "h":
					case "H":
						re.AppendLine(@"  \d{1,2} # hours");
						flags |= DateTimeFormatFlag.ContainsHour;
						break;
					case "hh":
					case "HH":
						re.AppendLine(@"  \d{2} # hours");
						flags |= DateTimeFormatFlag.ContainsHour;
						break;
					case "m":
						re.AppendLine(@"  \d{1,2} # minutes");
						flags |= DateTimeFormatFlag.ContainsMinutes;
						break;
					case "mm":
						re.AppendLine(@"  \d{2} # minutes");
						flags |= DateTimeFormatFlag.ContainsMinutes;
						break;
					case "M":
						re.AppendLine(@"  \d{1,2} # month");
						flags |= DateTimeFormatFlag.ContainsMonth;
						break;
					case "MM":
						re.AppendLine(@"  \d{2} # month");
						flags |= DateTimeFormatFlag.ContainsMonth;
						break;
					case "MMM":
						flags |= DateTimeFormatFlag.ContainsMonth;
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							AppandMatcherForOneOf(re, culture.DateTimeFormat.AbbreviatedMonthGenitiveNames.Union(culture.DateTimeFormat.AbbreviatedMonthNames).Where(s => s != ""));
						else
							re.Append(@"  \w+");
						re.AppendLine(@" # abbreviated name of the month");
						break;
					case "MMMM":
						flags |= DateTimeFormatFlag.ContainsMonth;
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							AppandMatcherForOneOf(re, culture.DateTimeFormat.MonthGenitiveNames.Union(culture.DateTimeFormat.MonthNames).Where(s => s != ""));
						else
							re.Append(@"  \w+");
						re.AppendLine(@" # full name of the month");
						break;
					case "s":
						re.AppendLine(@"  \d{1,2} # seconds");
						flags |= DateTimeFormatFlag.ContainsSeconds;
						break;
					case "ss":
						re.AppendLine(@"  \d{2} # seconds");
						flags |= DateTimeFormatFlag.ContainsSeconds;
						break;
					case "t":
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							re.AppendFormat(@"  ({0}|{1})", culture.DateTimeFormat.AMDesignator[0], culture.DateTimeFormat.PMDesignator[0]);
						else
							re.Append(@"  \w");
						re.AppendLine(@" # the first character of the A.M./P.M. designator");
						break;
					case "tt":
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							AppandMatcherForOneOf(re, new string[] {culture.DateTimeFormat.AMDesignator, culture.DateTimeFormat.PMDesignator});
						else
							re.Append(@"  \w+");
						re.AppendLine(@" # A.M./P.M. designator");
						break;
					case "y":
						re.AppendLine(@"  \d{1,2} # year");
						flags |= DateTimeFormatFlag.ContainsYear;
						break;
					case "yy":
						re.AppendLine(@"  \d{2} # year");
						flags |= DateTimeFormatFlag.ContainsYear;
						break;
					case "yyyy":
						re.AppendLine(@"  \d{4} # year");
						flags |= DateTimeFormatFlag.ContainsYear;
						break;
					case "z":
						re.AppendLine(@"  [\+\-]\d{1,2} # time zone offset");
						flags |= DateTimeFormatFlag.ContainsTimeZone;
						break;
					case "zz":
						re.AppendLine(@"  [\+\-]\d{2} # time zone offset");
						flags |= DateTimeFormatFlag.ContainsTimeZone;
						break;
					case "zzz":
						re.AppendLine(@"  [\+\-]\d{2}\:\d{2} # time zone offset");
						flags |= DateTimeFormatFlag.ContainsTimeZone;
						break;
					case "K":
						re.AppendLine(@"  ([\+\-]\d{2}\:\d{2})? # time zone information");
						flags |= DateTimeFormatFlag.ContainsTimeZone;
						break;
					case ":":
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							re.AppendFormat(@"  ({0})", Regex.Escape(culture.DateTimeFormat.TimeSeparator));
						else
							re.Append(@"  .");
						re.AppendLine(@" # time separator");
						break;
					case "/":
						flags |= DateTimeFormatFlag.IsCultureDependent;
						if (culture != null)
							re.AppendFormat(@"  ({0})", Regex.Escape(culture.DateTimeFormat.DateSeparator));
						else
							re.Append(@"  .");
						re.AppendLine(@" # date separator");
						break;
				}
			}
			return new ParsedDateTimeFormat() { Regex = re.ToString(), Flags = flags };
		}

		public static bool TryParseStandardDateTimeFormat(string formatString, CultureInfo culture, out ParsedDateTimeFormat fmt)
		{
			if (culture == null)
				throw new ArgumentNullException("culture");
			if (formatString == null)
				throw new ArgumentNullException("formatString");

			var tmp = ParseStandardDateTimeFormat(formatString, culture);
			if (tmp != null)
			{
				fmt = tmp.Value;
				return true;
			}
			else
			{
				fmt = new ParsedDateTimeFormat();
				return false;
			}
		}

		static ParsedDateTimeFormat? ParseStandardDateTimeFormat(string formatString, CultureInfo culture)
		{
			var ret = ParseStandardDateTimeFormatHelper(formatString, culture);
			if (ret == null)
				return ret;
			var val = ret.Value;
			val.Flags |= DateTimeFormatFlag.IsCultureDependent;
			return val;
		}

		static ParsedDateTimeFormat? ParseStandardDateTimeFormatHelper(string formatString, CultureInfo culture)
		{
			if (formatString.Length != 1)
				return null;

			switch (formatString[0])
			{
				case 'd':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.ShortDatePattern, culture);
				case 'D':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.LongDatePattern, culture);
				case 'f':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern, culture);
				case 'F':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.FullDateTimePattern, culture);
				case 'g':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern, culture);
				case 'G':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern, culture);
				case 'M':
				case 'm':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.MonthDayPattern, culture);
				case 'O':
				case 'o':
					return ParseCustomDateTimeFormat(@"yyyy\-MM\-dd\THH\:mm\:ss\.fffffffK", culture);
				case 'R':
				case 'r':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.RFC1123Pattern, CultureInfo.InvariantCulture);
				case 's':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.SortableDateTimePattern, CultureInfo.InvariantCulture);
				case 't':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.ShortTimePattern, culture);
				case 'T':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.LongTimePattern, culture);
				case 'u':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.UniversalSortableDateTimePattern, CultureInfo.InvariantCulture);
				case 'U':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.FullDateTimePattern, culture);
				case 'Y':
				case 'y':
					return ParseCustomDateTimeFormat(culture.DateTimeFormat.YearMonthPattern, culture);
				default:
					return null;
			}
		}

		static readonly Regex customDateTimeFormatRe = new Regex(@"
		(
			(dddd+)|ddd|dd|d| # day of the month
			fffffff|ffffff|fffff|ffff|fff|ff|f| # the N most significant digits of the seconds fraction
			FFFFFFF|FFFFFF|FFFFF|FFFF|FFF|FF|F| # the N most significant digits of the seconds fraction, no trailing zeros
			(gg+)|g| # the era
			(hh+)|h| # 1-12 hour
			(HH+)|h| # 0-24 hour
			(mm+)|m| # minutes
			MMMM|MMM|MM|M| # month
			(ss+)|s| # seconds
			(tt+)|t| # A.M./P.M. designator
			yyyy|yy|y| # year
			(zzz+)|zz|z| # time zone offset 
			K| # time zone information 
			\:| # time separator
			\/| # date separator
			\\(.)| # escape character
			(\') # start of escaped sequence
		)
		", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		struct CustomDatePatternToken
		{
			public bool IsSpecifier;
			public string Value;
		};

		static IEnumerable<CustomDatePatternToken> TokenizeCustomDatePattern(string pattern)
		{
			Func<string, CustomDatePatternToken> specifier = s => new CustomDatePatternToken() { IsSpecifier = true, Value = s };
			Func<string, CustomDatePatternToken> literal = s => new CustomDatePatternToken() { IsSpecifier = false, Value = s };

			bool withinEscapedString = false;
			int idx = 0;
			for (; ; )
			{
				Match m = customDateTimeFormatRe.Match(pattern, idx);
				if (m.Success)
				{
					if (m.Index > idx) // Yield the text before the specifier found (if any)
					{
						yield return literal(pattern.Substring(idx, m.Index - idx));
					}

					if (m.Groups[10].Value != "")
						yield return literal(m.Groups[10].Value);
					else if (m.Groups[11].Value != "")
						withinEscapedString = !withinEscapedString;
					else if (withinEscapedString)
						yield return literal(m.Groups[1].Value);
					else if (m.Groups[2].Value != "")
						yield return specifier("dddd");
					else if (m.Groups[3].Value != "")
						yield return specifier("gg");
					else if (m.Groups[4].Value != "")
						yield return specifier("hh");
					else if (m.Groups[5].Value != "")
						yield return specifier("HH");
					else if (m.Groups[6].Value != "")
						yield return specifier("mm");
					else if (m.Groups[7].Value != "")
						yield return specifier("ss");
					else if (m.Groups[8].Value != "")
						yield return specifier("tt");
					else if (m.Groups[9].Value != "")
						yield return specifier("zzz");
					else
						yield return specifier(m.Groups[1].Value);

					idx = m.Index + m.Length;
				}
				else
				{
					if (idx < pattern.Length) // Yield the rest of the pattern if any
					{
						yield return literal(pattern.Substring(idx));
					}

					// Stop parsing
					break;
				}
			}
		}
	};
}

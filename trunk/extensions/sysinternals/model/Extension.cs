using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace LogJoint.SysinternalsFormats
{
	public class Extension
	{
		class ParseParams
		{
			public CultureInfo Culture;
			public string Format;
			public bool MillisecondsPresent;
		};
		ParseParams lastSuccessfulParams;

		public DateTime PARSE_XML_TIME_OF_DAY(string value)
		{
			return ParseTimeInternal(value, false, 7);
		}

		public DateTime PARSE_FILEMON_TIME(string value)
		{
			return ParseTimeInternal(value, true, 3);
		}

		public DateTime ParseTimeInternal(string value, bool tryWithoutMSecs, int msecPrecision)
		{
			DateTime ret;
			DateTimeStyles parseExactStyle = DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite;

			if (lastSuccessfulParams != null)
			{
				if (DateTime.TryParseExact(value, lastSuccessfulParams.Format, lastSuccessfulParams.Culture, parseExactStyle, out ret))
				{
					return ret;
				}
			}

			CultureInfo[] cinfos = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
			foreach (CultureInfo ci in cinfos)
			{
				string longTimePattern = ci.DateTimeFormat.LongTimePattern;

				if (tryWithoutMSecs)
				{
					if (DateTime.TryParseExact(value, longTimePattern, ci.DateTimeFormat,
						parseExactStyle, out ret))
					{
						ParseParams pp = new ParseParams();
						pp.Culture = ci;
						pp.Format = longTimePattern;
						pp.MillisecondsPresent = false;
						lastSuccessfulParams = pp;
						return ret;
					}
				}

				string longTimePatternWithMillisecs = Regex.Replace(longTimePattern, "(:ss|:s)", "$1" + ci.NumberFormat.NumberDecimalSeparator + new string('f', msecPrecision));
				if (DateTime.TryParseExact(value, longTimePatternWithMillisecs, ci.DateTimeFormat,
					parseExactStyle, out ret))
				{
					ParseParams pp = new ParseParams();
					pp.Culture = ci;
					pp.Format = longTimePatternWithMillisecs;
					pp.MillisecondsPresent = true;
					lastSuccessfulParams = pp;
					return ret;
				}
			}

			throw new FormatException(string.Format("Cannot parse '{0}' as time", value));
		}
	}
}

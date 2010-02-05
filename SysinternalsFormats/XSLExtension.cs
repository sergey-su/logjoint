using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace LogJoint.SysinternalsFormats
{
	public class XSLExtension
	{
		CultureInfo lastSuccessfulCulture = null;
		string lastSuccessfulFormat = null;

		public DateTime PARSE_TIME_OF_DAY(string value)
		{
			DateTime ret;
			DateTimeStyles parseExactStyle = DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite;

			if (lastSuccessfulFormat != null)
			{
				if (DateTime.TryParseExact(value, lastSuccessfulFormat, lastSuccessfulCulture, parseExactStyle, out ret))
				{
					return ret;
				}
			}

			CultureInfo[] cinfos = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
			foreach (CultureInfo ci in cinfos)
			{
				string longTimePattern = ci.DateTimeFormat.LongTimePattern;
				string longTimePatternWithMillisecs = Regex.Replace(longTimePattern, "(:ss|:s)", "$1" + ci.NumberFormat.NumberDecimalSeparator + "fffffff");
				if (DateTime.TryParseExact(value, longTimePatternWithMillisecs, ci.DateTimeFormat,
					parseExactStyle, out ret))
				{
					lastSuccessfulCulture = ci;
					lastSuccessfulFormat = longTimePatternWithMillisecs;
					return ret;
				}
			}

			throw new FormatException(string.Format("Cannot parse {0} as time", value));
		}
	}
}

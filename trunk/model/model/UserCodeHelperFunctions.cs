using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace LogJoint
{
	public abstract class UserCodeHelperFunctions
	{
		public string TRIM(string str)
		{
			return StringUtils.TrimInsignificantSpace(str);
		}

		public int HEX_TO_INT(string str)
		{
			return int.Parse(str, NumberStyles.HexNumber);
		}

		public DateTime TICKS_TO_DATETIME(long ticks)
		{
			return new DateTime(ticks,  DateTimeKind.Unspecified);
		}

		public int TO_INT(string str)
		{
			return ParseInt(str, 0, str.Length);
		}

		protected static int ParseInt(string str, int idx, int len)
		{
			int ret = 0;
			int maxIdx = idx + len;
			for (int i = idx; i < maxIdx; ++i)
			{
				int digit = ((int)str[i]) - (int)0x30;
				ret = ret * 10 + digit;
			}
			return ret;
		}

		public DateTime TO_DATETIME(string value, string format)
		{
			return TO_DATETIME_Impl(value, format, CultureInfo.InvariantCulture);
		}

		public DateTime TO_DATETIME(string value, string format, string culture)
		{
			return TO_DATETIME_Impl(value, format, CultureInfo.GetCultureInfo(culture));
		}

		public string TO_NATIVE_DATETIME_STR(DateTime dateTime)
		{
			return new MessageTimestamp(dateTime).StoreToLoselessFormat();
		}

		static private DateTime TO_DATETIME_Impl(string value, string format, CultureInfo culture)
		{
			try
			{
				return DateTime.ParseExact(value, format, culture.DateTimeFormat, DateTimeStyles.AdjustToUniversal);
			}
			catch (FormatException e)
			{
				throw new FormatException(string.Format("Failed to parse DateTime value '{2}' of format '{1}'. {0}", e.Message, format, value));
			}
		}

		public int PARSE_YEAR(string year)
		{
			int y = TO_INT(year);
			return PARSE_YEAR_impl(y);
		}

		protected int PARSE_YEAR_impl(int y)
		{
			if (y < 100)
			{
				if (y < 60)
					return 2000 + y;
				return 1900 + y;
			}
			return y;
		}

		public string DEFAULT_DATETIME_FORMAT()
		{
			//return "yyyy-MM-ddTHH:mm:stringSlice.fff";
			//2009-08-07 13:17:55
			return "yyyy-MM-dd HH:mm:ss";
		}

		public DateTime EPOCH_TIME(long epochTime)
		{
			DateTime ret = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return ret.ToLocalTime().AddMilliseconds(epochTime);
		}

		public string NEW_LINE()
		{
			return Environment.NewLine;
		}

		public DateTime DATETIME_FROM_TIMEOFDAY(DateTime timeOfDay)
		{
			DateTime tmp = SOURCE_TIME();
			return new DateTime(tmp.Year, tmp.Month, tmp.Day, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond, tmp.Kind);
		}

		public DateTime DATETIME_FROM_DATE_AND_TIMEOFDAY(DateTime date, DateTime timeOfDay)
		{
			return date.Date + timeOfDay.TimeOfDay;
		}

		public string TSV_UNESCAPE(string str)
		{
			return "";
		}

		protected abstract DateTime SOURCE_TIME();
	};

	public abstract class StringSliceAwareUserCodeHelperFunctions : UserCodeHelperFunctions
	{
		public StringSlice CONCAT(StringSlice s1, StringSlice s2)
		{
			return StringSlice.Concat(s1, s2);
		}

		public StringSlice CONCAT(StringSlice s1, string s2)
		{
			return StringSlice.Concat(s1, new StringSlice(s2));
		}

		public StringSlice CONCAT(string s1, StringSlice s2)
		{
			return StringSlice.Concat(new StringSlice(s1), s2);
		}

		public DateTime TICKS_TO_DATETIME(StringSlice ticksStr)
		{
			return new DateTime(long.Parse(ticksStr.Value), DateTimeKind.Unspecified);
		}

		public bool EMPTY(StringSlice str)
		{
			return str.IsEmpty;
		}

		public int TO_INT(StringSlice str, int idx, int len)
		{
			StringSlice tmp = str.SubString(idx, len);
			return ParseInt(tmp.Buffer, tmp.StartIndex, tmp.Length);
		}

		public int TO_INT(StringSlice str)
		{
			return ParseInt(str.Buffer, str.StartIndex, str.Length);
		}

		public StringSlice TRIM(StringSlice str)
		{
			return str.Trim();
		}

		public DateTime TO_DATETIME(StringSlice value, string format)
		{
			return TO_DATETIME(value.Value, format);
		}

		public DateTime TO_DATETIME(StringSlice value, string format, string culture)
		{
			return TO_DATETIME(value.Value, format, culture);
		}

		public int PARSE_YEAR(StringSlice year)
		{
			int y = TO_INT(year);
			return PARSE_YEAR_impl(y);
		}

		public StringSlice FORMAT(string fmt, params object[] args)
		{
			return new StringSlice(string.Format(fmt, args));
		}
	};
}

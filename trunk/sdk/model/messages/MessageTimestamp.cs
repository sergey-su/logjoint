using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Globalization;

namespace LogJoint
{
	public enum MessageTimestampTimezone
	{
		Unknown,
		UTC
	};

	/// <summary>
	/// Repressents the timestamp of a log message.
	/// </summary>
	public struct MessageTimestamp
	{
		[DebuggerStepThrough]
		public MessageTimestamp(DateTime value)
		{
			data = value;
		}

		public MessageTimestampTimezone TimeZone
		{
			get
			{
				switch (data.Kind)
				{
					case DateTimeKind.Local:
					case DateTimeKind.Unspecified:
						return MessageTimestampTimezone.Unknown;
					case DateTimeKind.Utc:
						return MessageTimestampTimezone.UTC;
					default:
						return MessageTimestampTimezone.Unknown;
				}
			}
		}

		public static MessageTimestamp MinValue = new MessageTimestamp(DateTime.MinValue);
		public static MessageTimestamp MaxValue = new MessageTimestamp(DateTime.MaxValue);

		/// <summary>
		/// Converts timestamp to string representation that allows restoring exactly the same timestamp object later.
		/// </summary>
		public string StoreToLoselessFormat()
		{
			DateTime dateTimeToStore = data;
			if (dateTimeToStore.Kind == DateTimeKind.Local)
				dateTimeToStore = new DateTime(data.Ticks, DateTimeKind.Unspecified);
			return dateTimeToStore.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
		}
		
		/// <summary>
		/// Restores timestamp object from a string created by StoreToLoselessFormat().
		/// </summary>
		public static MessageTimestamp ParseFromLoselessFormat(string str)
		{
			return new MessageTimestamp(XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.RoundtripKind));
		}

		/// <summary>
		/// Converts message timestamp to local DateTime that can be displayed to the user.
		/// If message timestamp has UTC timezone it is converted to local timezone.
		/// Timestamps with Unknown timezone are not changed by ToLocalDateTime(). 
		/// Timestamp with Unknwon timezone is meant to be parsed from local log and therefore 
		/// it does not need any adjustments to be displayed to the user in his/her local timezone.
		/// </summary>
		public DateTime ToLocalDateTime()
		{
			switch (data.Kind)
			{
				case DateTimeKind.Utc: 
					return data.ToLocalTime();
				case DateTimeKind.Unspecified: 
					return new DateTime(data.Ticks, DateTimeKind.Local);
				default:
					return data;
			}
		}

		public DateTime ToUniversalTime()
		{
			switch (data.Kind)
			{
				case DateTimeKind.Utc:
					return data;
				case DateTimeKind.Unspecified:
					return new DateTime(data.Ticks, DateTimeKind.Local).ToUniversalTime();
				default:
					return data.ToUniversalTime();
			}
		}

		public DateTime ToUnspecifiedTime()
		{
			return new DateTime(data.Ticks, DateTimeKind.Unspecified);
		}

		/// <summary>
		/// Converts message timestamp to local timezone 
		/// </summary>
		/// <param name="showMilliseconds"></param>
		public string ToUserFrendlyString(bool showMilliseconds, bool showDate = true)
		{
			string fmt;
			if (showDate)
				fmt = showMilliseconds ? "yyyy-MM-dd HH:mm:ss.fff" : "yyyy-MM-dd HH:mm:ss";
			else
				fmt = showMilliseconds ? "HH:mm:ss.fff" : "HH:mm:ss";
			return ToLocalDateTime().ToString(fmt);
		}

		public override string ToString()
		{
			return StoreToLoselessFormat();
		}

		public string ToUserFrendlyString()
		{
			return ToUserFrendlyString(data.Millisecond != 0);
		}

		public MessageTimestamp Advance(TimeSpan ts)
		{
			return new MessageTimestamp(data + ts);
		}

		public MessageTimestamp Adjust(ITimeOffsets offsets)
		{
			return new MessageTimestamp(offsets.Get(data));
		}

		public static int Compare(MessageTimestamp t1, MessageTimestamp t2)
		{
			DateTime d1 = t1.data;
			DateTime d2 = t2.data;
			DateTimeKind k1 = d1.Kind;
			DateTimeKind k2 = d2.Kind;
			if (NormalizeKindForComparision(k1) == NormalizeKindForComparision(k2))
				return DateTime.Compare(d1, d2);
			if (k1 == DateTimeKind.Utc)
				d1 = t1.ToLocalDateTime();
			else
				d2 = t2.ToLocalDateTime();
			return DateTime.Compare(d1, d2);
		}

		public int GetStableHashCode()
		{
			if (data.Kind == DateTimeKind.Unspecified)
				return Hashing.GetStableHashCode(new DateTime(data.Ticks, DateTimeKind.Local));
			return Hashing.GetStableHashCode(data);
		}

		

		public static bool operator <(MessageTimestamp t1, MessageTimestamp t2)
		{
			return Compare(t1, t2) < 0;
		}
		public static bool operator >(MessageTimestamp t1, MessageTimestamp t2)
		{
			return Compare(t1, t2) > 0;
		}
		public static bool operator <=(MessageTimestamp t1, MessageTimestamp t2)
		{
			return Compare(t1, t2) <= 0;
		}
		public static bool operator >=(MessageTimestamp t1, MessageTimestamp t2)
		{
			return Compare(t1, t2) >= 0;
		}
		public static TimeSpan operator -(MessageTimestamp t1, MessageTimestamp t2)
		{
			return t1.ToUnspecifiedTime() - t2.ToUnspecifiedTime();
		}

		static DateTimeKind NormalizeKindForComparision(DateTimeKind k)
		{
			if (k == DateTimeKind.Unspecified)
				return DateTimeKind.Local;
			else
				return k;
		}

		internal static bool EqualStrict(MessageTimestamp t1, MessageTimestamp t2)
		{
			DateTime d1 = t1.data;
			DateTime d2 = t2.data;
			if (d1.Kind != d2.Kind)
				return false;
			return d1 == d2;
		}

		private DateTime data;
	};
}

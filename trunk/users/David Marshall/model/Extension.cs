using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LogJoint;
using System.IO;

namespace LogJoint.dmarshal
{
	public class Extension : LogJoint.IMessagesReaderExtension
	{
		MediaBasedPositionedMessagesReader reader;

		public void Attach(IPositionedMessagesReader reader)
		{
			this.reader = (MediaBasedPositionedMessagesReader)reader;
		}

		public void OnAvailableBoundsUpdated(bool incrementalMode, UpdateBoundsStatus updateBoundsStatus)
		{
			DetectStartDate();
		}

		public void Dispose()
		{
		}

		public DateTime DATETIME_FROM_TIME(StringSlice timeStr)
		{
			DateTime time = DateTime.ParseExact(timeStr.Value, "HH:mm:ss",
				System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
			return new DateTime(startDate.Year, startDate.Month, startDate.Day,
				time.Hour, time.Minute, time.Second) + timeZoneOffset;
		}

		void DetectStartDate()
		{
			startDate = DateTime.Now;
			timeZoneOffset = new TimeSpan();

			byte[] buf = new byte[256];
			reader.VolatileStream.Position = 0;
			int bytesRead = reader.VolatileStream.Read(buf, 0, buf.Length);
			string str = reader.StreamEncoding.GetString(buf, 0, bytesRead);
			var m = dateRe1.Match(str);
			if (m.Success)
			{
				DateTime date;
				TimeSpan offset;
				try
				{
					date = guessDateLocale.ParseDateGuessingLocale(m.Groups[1].Value, "ddd MMM d hh:mm:ss");
				}
				catch (FormatException)
				{
					return;
				}
				try
				{
					offset = TimeZonesParsing.GetTimeZoneOffset(m.Groups[2].Value);
				}
				catch (TimeZoneNotFoundException)
				{
					return;
				}
				int year = int.Parse(m.Groups[3].Value);

				this.startDate = date;
				this.timeZoneOffset = offset;
			}
		}

		static readonly Regex dateRe1 = new Regex(@"\*\*\*(.+?)\ (\w+)\ (\d{4})\ Sample\ interval", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
		GuessDateLocale guessDateLocale = new GuessDateLocale();
		DateTime startDate;
		TimeSpan timeZoneOffset;
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LogJoint
{
	public class DateRangeArgumentException: ArgumentException
	{
		public DateRangeArgumentException():
			base("End position must be geater or equal to begin position", "end")
		{
		}
	};

	public struct DateRange
	{
		public readonly DateTime Begin;
		public readonly DateTime End;
		[DebuggerStepThrough]
		public DateRange(DateTime begin, DateTime end)
		{
			if (end < begin)
				throw new DateRangeArgumentException();
			Begin = begin;
			End = end;
		}
		[DebuggerStepThrough]
		public DateRange(DateTime begin)
		{
			Begin = begin;
			End = begin.AddTicks(1);
		}
		public DateTime Minimum
		{
			get { return Begin; }
		}
		public DateTime Maximum
		{
			get { return End.AddTicks(-1); }
		}
		public static DateRange MakeFromBoundaryValues(DateTime first, DateTime last)
		{
			return new DateRange(first, last.AddTicks(1));
		}
		public static DateRange MakeEmpty()
		{
			return new DateRange();
		}
		public static DateRange Union(DateRange r1, DateRange r2)
		{
			if (r1.IsEmpty)
				return r2;
			if (r2.IsEmpty)
				return r1;
			DateTime b = r1.Begin;
			if (r2.Begin < b)
				b = r2.Begin;
			DateTime e = r1.End;
			if (r2.End > e)
				e = r2.End;
			return new DateRange(b, e);
		}
		public static DateRange Intersect(DateRange r1, DateRange r2)
		{
			if (r1.IsEmpty)
				return r1;
			if (r2.IsEmpty)
				return r2;
			DateTime b = r1.Begin;
			if (r2.Begin > b)
				b = r2.Begin;
			DateTime e = r1.End;
			if (r2.End < e)
				e = r2.End;
			if (e < b)
				e = b;
			return new DateRange(b, e);
		}
		public DateRange Offset(TimeSpan offset)
		{
			return new DateRange(Begin + offset, End + offset);
		}
		public bool IsEmpty
		{
			get { return Begin >= End; }
		}
		public DateTime PutInRange(DateTime d)
		{
			if (d < Begin)
				return Begin;
			if (d > End)
				return End;
			return d;
		}
		public bool IsInRange(DateTime d)
		{
			return Contains(d);
		}
		public bool Contains(DateTime d)
		{
			return d >= Begin && d < End;
		}
		public TimeSpan Length
		{
			get { return End - Begin; }
		}
		public override string ToString()
		{
			return string.Format("DateRange: ({0:o}) - ({1:o})", Begin, End);
		}
		public bool Equals(DateRange r)
		{
			return Begin == r.Begin && End == r.End;
		}
	};
}

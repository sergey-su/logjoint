using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint
{
	public struct TimeGap
	{
		public DateRange Range { get { return range; } }
		public TimeSpan CumulativeLengthExclusive { get { return cumulativeLenEx; } }
		public TimeSpan CumulativeLengthInclusive { get { return cumulativeLenInc; } }
		public DateTime Mid { get { return mid; } }
		public TimeSpan Length { get { return len; } }
		public TimeGap(DateRange r, TimeSpan cumulativeLen)
		{
			this.range = r;
			this.len = r.Length;
			this.mid = r.Begin + TimeSpan.FromMilliseconds(len.TotalMilliseconds / 2);
			this.cumulativeLenEx = cumulativeLen;
			this.cumulativeLenInc = cumulativeLen + len;
		}
		public override string ToString()
		{
			return string.Format("TimeGap ({0}) - ({1})", range.Begin, range.End);
		}
		DateRange range;
		TimeSpan len;
		DateTime mid;
		TimeSpan cumulativeLenEx;
		TimeSpan cumulativeLenInc;
	};

	public interface ITimeGaps : IEnumerable<TimeGap>
	{
		int Count { get; }
		TimeSpan Length { get; }
		int BinarySearch(int begin, int end, Predicate<TimeGap> lessThanValueBeingSearched);
		TimeGap this[int idx] { get; }
	};

	/// <summary>
	/// GapsDetector implements the logic of finding the gaps (periods of time where there are no messages)
	/// on the timeline.
	/// </summary>
	/// <remarks>
	/// This class starts to work when a client calls Update(DateRange) method. The value passed
	/// to Update() is a dare range there the client wants to find the gaps. The dates range
	/// is divided to a fixed number of pieces. The length of the piece is used as a threshold.
	/// The periods of time with no messages and with the lenght greated than the threshold are
	/// considered as time gaps.
	/// </remarks>
	public interface ITimeGapsDetector
	{
		event EventHandler OnTimeGapsChanged;
		void Update(DateRange r);
		bool IsWorking { get; }
		ITimeGaps Gaps { get; }
		Task Dispose();
	};

	public interface ITimeGapsSource
	{
		bool IsDisposed { get; }
		Task<DateBoundPositionResponseData> GetDateBoundPosition(
			DateTime d,
			ListUtils.ValueBound bound,
			CancellationToken cancellation
		);
	};
}

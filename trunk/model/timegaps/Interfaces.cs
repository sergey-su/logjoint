using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint
{
	public struct TimeGap
	{
		public DateRange Range { get { return range; } }

		public TimeGap(DateRange r)
		{
			this.range = r;
		}

		public override string ToString()
		{
			return string.Format("TimeGap {0}", range);
		}
		readonly DateRange range;
	};

	public interface ITimeGaps : IEnumerable<TimeGap>
	{
		int Count { get; }
		TimeGap this[int idx] { get; }
		int BinarySearch(int begin, int end, Predicate<TimeGap> lessThanValueBeingSearched);
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

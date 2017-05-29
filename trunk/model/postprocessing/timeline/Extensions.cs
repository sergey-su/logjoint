using System;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.Timeline
{
	public static class TimelineEntitiesExtensions
	{
		public static TimeSpan GetTimelineBegin(this IActivity a)
		{
			return a.Begin + a.BeginOwner.TimelineOffset;
		}

		public static TimeSpan GetTimelineEnd(this IActivity a)
		{
			return a.End + a.EndOwner.TimelineOffset;
		}

		public static TimeSpan GetDuration(this IActivity a)
		{
			return a.End - a.Begin;
		}

		public static TimeSpan GetTimelineTime(this IEvent e)
		{
			return e.Time + e.Owner.TimelineOffset;
		}

		public static TimeSpan GetTimelineTime(this ActivityMilestone ms)
		{
			return ms.Time + ms.Owner.TimelineOffset;
		}

		public static TimeSpan GetTimelineTime(this IBookmark bmk, ITimelineVisualizerModel model)
		{
			return bmk.Time.ToUnspecifiedTime() - model.Origin;
		}
	};
}

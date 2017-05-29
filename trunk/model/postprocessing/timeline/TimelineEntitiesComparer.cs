using LogJoint.Analytics;
using System;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.Timeline
{
	class TimelineEntitiesComparer : IEntitiesComparer, IComparer<IActivity>, IComparer<IEvent>
	{
		public readonly static IEntitiesComparer Instance = new TimelineEntitiesComparer();

		int IComparer<IActivity>.Compare(IActivity x, IActivity y)
		{
			int ret = TimeSpan.Compare(x.GetTimelineBegin(), y.GetTimelineBegin());
			if (ret != 0)
				return ret;
			ret = CompareSources(x.BeginOwner.LogSource, y.BeginOwner.LogSource);
			if (ret != 0)
				return ret;
			return CompareTriggers(x.BeginTrigger, y.BeginTrigger);
		}

		int IComparer<IEvent>.Compare(IEvent x, IEvent y)
		{
			int ret = TimeSpan.Compare(x.GetTimelineTime(), y.GetTimelineTime());
			if (ret != 0)
				return ret;
			ret = CompareSources(x.Owner.LogSource, y.Owner.LogSource);
			if (ret != 0)
				return ret;
			return CompareTriggers(x.Trigger, y.Trigger);
		}

		static int CompareSources(ILogSource src1, ILogSource src2)
		{
			if (src1 != src2)
				return string.Compare(src1.GetSafeConnectionId(), src2.GetSafeConnectionId());
			return 0;
		}

		static int CompareTriggers(object trigger1, object trigger2)
		{
			var pos1 = trigger1 as ITriggerStreamPosition;
			var pos2 = trigger2 as ITriggerStreamPosition;
			if (pos1 != null && pos2 != null)
				return Math.Sign(pos1.StreamPosition - pos2.StreamPosition);
			return 0;
		}
	};
}

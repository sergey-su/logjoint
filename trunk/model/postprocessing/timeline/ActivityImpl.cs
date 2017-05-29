using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.Postprocessing.Timeline
{
	class ActivityImpl: IActivity
	{
		readonly TimeSpan begin;
		readonly ITimelinePostprocessorOutput beginOwner;
		readonly TimeSpan end;
		readonly ITimelinePostprocessorOutput endOwner;
		readonly string displayName;
		readonly string activityMatchingId;
		readonly ActivityType type;
		readonly object beginTrigger;
		readonly object endTrigger;
		readonly IReadOnlyList<ActivityMilestone> milestones;
		readonly HashSet<string> tags;

		public ActivityImpl(
			ITimelinePostprocessorOutput beginOwner,
			ITimelinePostprocessorOutput endOwner,
			TimeSpan begin, TimeSpan end,
			string displayName, string activityMatchingId,
			ActivityType type, 
			object beginTrigger, object endTrigger, 
			IEnumerable<ActivityMilestone> detachedMilestones,
			IEnumerable<string> tags)
		{
			this.beginOwner = beginOwner;
			this.endOwner = endOwner;
			this.begin = begin;
			this.end = end;
			this.displayName = displayName;
			this.activityMatchingId = activityMatchingId;
			this.type = type;
			this.beginTrigger = beginTrigger;
			this.endTrigger = endTrigger;
			this.milestones = detachedMilestones.Select(
				ms => new ActivityMilestone(this, ms.Owner, ms.Time, ms.DisplayName, ms.Trigger)).ToList().AsReadOnly();
			this.tags = new HashSet<string>(tags);
		}

		TimeSpan IActivity.Begin { get { return begin; } }

		ITimelinePostprocessorOutput IActivity.BeginOwner { get { return beginOwner; } }

		TimeSpan IActivity.End { get { return end; } }

		ITimelinePostprocessorOutput IActivity.EndOwner { get { return endOwner; } }

		string IActivity.DisplayName { get { return displayName; } }

		string IActivity.ActivityMatchingId { get { return activityMatchingId; } }

		ActivityType IActivity.Type { get { return type; } }

		object IActivity.BeginTrigger { get { return beginTrigger; } }

		object IActivity.EndTrigger { get { return endTrigger; } }


		IReadOnlyList<ActivityMilestone> IActivity.Milestones { get { return milestones; } }
		
		ISet<string> IActivity.Tags { get { return tags; } }
	}
}

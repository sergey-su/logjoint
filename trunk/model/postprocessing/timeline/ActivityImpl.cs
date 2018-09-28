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
		readonly IReadOnlyList<ActivityPhase> phases;
		readonly HashSet<string> tags;
		readonly bool isError;

		public ActivityImpl(
			ITimelinePostprocessorOutput beginOwner,
			ITimelinePostprocessorOutput endOwner,
			TimeSpan begin, TimeSpan end,
			string displayName, string activityMatchingId,
			ActivityType type, 
			object beginTrigger, object endTrigger, 
			IEnumerable<ActivityMilestone> detachedMilestones,
			IEnumerable<ActivityPhase> detachedPhases,
			IEnumerable<string> tags,
			bool isError)
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
			this.phases = detachedPhases.Select(
				ph => new ActivityPhase(this, ph.Owner, ph.Begin, ph.End, ph.Type, ph.DisplayName)).ToList().AsReadOnly();
			this.tags = new HashSet<string>(tags);
			this.isError = isError;
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

		IReadOnlyList<ActivityPhase> IActivity.Phases { get { return phases; } }
		
		ISet<string> IActivity.Tags { get { return tags; } }

		bool IActivity.IsError { get { return isError; } }
	}
}

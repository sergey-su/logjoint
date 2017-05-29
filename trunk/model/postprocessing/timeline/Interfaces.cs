using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Timeline
{
	public interface ITimelinePostprocessorOutput
	{
		ILogSource LogSource { get; }
		IList<Event> TimelineEvents { get; }
		TimeSpan TimelineOffset { get; }
		void SetTimelineOffset(TimeSpan value);
		string SequenceDiagramName { get; }
		void SetSequenceDiagramName(string value);
		ILogPartToken RotatedLogPartToken { get; }
	};

	public interface ITimelineVisualizerModel
	{
		ICollection<ITimelinePostprocessorOutput> Outputs { get; }
		DateTime Origin { get; }
		IList<IActivity> Activities { get; }
		IList<IEvent> Events { get; }
		Tuple<TimeSpan, TimeSpan> AvailableRange { get; }
		Tuple<IActivity, IActivity> GetPairedActivities(IActivity a);
		IEntitiesComparer Comparer { get; }

		event EventHandler EverythingChanged;
		event EventHandler SequenceDiagramNamesChanged;
	};

	public interface IEntitiesComparer : IComparer<IActivity>, IComparer<IEvent>
	{
	};

	public enum ActivityType
	{
		Unknown,
		Lifespan,
		Procedure,
		OutgoingNetworking,
		IncomingNetworking
	};

	public interface IActivity
	{
		ActivityType Type { get; }
		TimeSpan Begin { get; }
		ITimelinePostprocessorOutput BeginOwner { get; }
		TimeSpan End { get; }
		ITimelinePostprocessorOutput EndOwner { get; }
		string DisplayName { get; }
		string ActivityMatchingId { get; }
		object BeginTrigger { get; }
		object EndTrigger { get; }
		IReadOnlyList<ActivityMilestone> Milestones { get; }
		ISet<string> Tags { get; }
	};

	public struct ActivityMilestone
	{
		public readonly IActivity Activity;
		public readonly ITimelinePostprocessorOutput Owner;
		public readonly TimeSpan Time;
		public readonly string DisplayName;
		public readonly object Trigger;

		public ActivityMilestone(IActivity a, ITimelinePostprocessorOutput owner, TimeSpan t, string displayName, object trigger)
		{
			Activity = a;
			Owner = owner;
			Time = t;
			DisplayName = displayName;
			Trigger = trigger;
		}
	};

	public enum EventType
	{
		Invalid,
		UserAction,
		APICall
	};

	public interface IEvent
	{
		ITimelinePostprocessorOutput Owner { get; }
		TimeSpan Time { get; }
		string DisplayName { get; }
		EventType Type { get; }
		object Trigger { get; }
	};
}

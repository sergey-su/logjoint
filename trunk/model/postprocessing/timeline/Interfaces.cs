using System;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.Timeline
{
    public interface ITimelinePostprocessorOutput : IPostprocessorOutputETag
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
        IReadOnlyCollection<ITimelinePostprocessorOutput> Outputs { get; }
        DateTime Origin { get; }
        IReadOnlyList<IActivity> Activities { get; }
        IReadOnlyList<IEvent> Events { get; }
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
        IReadOnlyList<ActivityMilestoneInfo> Milestones { get; }
        IReadOnlyList<ActivityPhaseInfo> Phases { get; }
        ISet<string> Tags { get; }
        bool IsError { get; }
        /// <summary>
        /// true if the activity is missing corresponding <see cref="ActivityEventType.End"/> event and was ended
        /// forcefully by <see cref="EndOfTimelineEvent"/>.
        /// </summary>
        bool IsEndedForcefully { get; }
    };

    public struct ActivityMilestoneInfo
    {
        public readonly IActivity Activity;
        public readonly ITimelinePostprocessorOutput Owner;
        public readonly TimeSpan Time;
        public readonly string DisplayName;
        public readonly object Trigger;

        public ActivityMilestoneInfo(IActivity a, ITimelinePostprocessorOutput owner, TimeSpan t, string displayName, object trigger)
        {
            Activity = a;
            Owner = owner;
            Time = t;
            DisplayName = displayName;
            Trigger = trigger;
        }
    };

    public struct ActivityPhaseInfo
    {
        public readonly IActivity Activity;
        public readonly ITimelinePostprocessorOutput Owner;
        public readonly TimeSpan Begin;
        public readonly TimeSpan End;
        /// <summary>
        /// Concrete phase types are defined by activity producers.
        /// Phases of same type will be painted same color.
        /// </summary>
        public readonly int Type;
        public readonly string DisplayName;

        public ActivityPhaseInfo(IActivity a, ITimelinePostprocessorOutput owner,
            TimeSpan b, TimeSpan e, int type, string displayName)
        {
            Activity = a;
            Owner = owner;
            Begin = b;
            End = e;
            Type = type;
            DisplayName = displayName;
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

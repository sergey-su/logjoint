using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.Postprocessing.Timeline
{
    public class TimelineBuilder : IEventsVisitor
    {
        readonly IEntitiesComparer entitiesComparer;
        ITimelinePostprocessorOutput currentPostprocessorOutput;
        ITimeOffsets currentTimeOffsets;
        bool currentIsLastEventsSet;
        List<IActivity> activities = new List<IActivity>();
        List<IEvent> events = new List<IEvent>();
        Dictionary<string, StartedActivity> startedActivities = new Dictionary<string, StartedActivity>();
        DateTime origin;
        bool originSet;
        EventInfo? endOfTimelineEventInfo;
        string timelineDisplayName;

        public TimelineBuilder(IEntitiesComparer entitiesComparer)
        {
            this.entitiesComparer = entitiesComparer;
        }

        public void AddEvents(ITimelinePostprocessorOutput postprocessorOutput, IEnumerable<Event> events, bool isLastEventsSet)
        {
            this.currentPostprocessorOutput = postprocessorOutput;
            this.currentIsLastEventsSet = isLastEventsSet;
            this.currentTimeOffsets = postprocessorOutput.LogSource.TimeOffsets;
            foreach (var e in events)
                e.Visit(this);
            this.currentPostprocessorOutput = null;
            this.currentTimeOffsets = null;
            this.currentIsLastEventsSet = false;
        }

        public struct TimelineData
        {
            public List<IActivity> Activities;
            public List<IEvent> Events;
            public DateTime? Origin;
            public string TimelineDisplayName;
        };

        public TimelineData FinalizeAndGetTimelineData()
        {
            if (endOfTimelineEventInfo != null)
                HandleEndOfTimelineEvent();
            activities.Sort(entitiesComparer.Compare);
            events.Sort(entitiesComparer.Compare);

            return new TimelineData()
            {
                Activities = activities,
                Events = events,
                Origin = originSet ? origin : new DateTime?(),
                TimelineDisplayName = timelineDisplayName
            };
        }

        void IEventsVisitor.Visit(ProcedureEvent evt)
        {
            HandleActivityEvent(evt, ActivityType.Procedure);
        }

        void IEventsVisitor.Visit(ObjectLifetimeEvent evt)
        {
            HandleActivityEvent(evt, ActivityType.Lifespan);
        }

        void IEventsVisitor.Visit(NetworkMessageEvent evt)
        {
            HandleActivityEvent(evt, ToNetworkActivityType(evt.Direction), evt.ActivityId);
        }

        static ActivityType ToNetworkActivityType(NetworkMessageDirection direction)
        {
            switch (direction)
            {
                case NetworkMessageDirection.Incoming:
                    return ActivityType.IncomingNetworking;
                case NetworkMessageDirection.Outgoing:
                    return ActivityType.OutgoingNetworking;
                default:
                    return ActivityType.Procedure;
            }
        }

        void IEventsVisitor.Visit(UserActionEvent evt)
        {
            AddEvent(evt, EventType.UserAction);
        }

        void IEventsVisitor.Visit(APICallEvent evt)
        {
            AddEvent(evt, EventType.APICall);
        }

        void IEventsVisitor.Visit(EndOfTimelineEvent evt)
        {
            var eventInfo = new EventInfo(evt, currentTimeOffsets);
            SetOrigin(eventInfo);
            if (currentIsLastEventsSet)
                endOfTimelineEventInfo = eventInfo;
        }

        void HandleEndOfTimelineEvent()
        {
            foreach (var startedActivity in startedActivities)
            {
                if (startedActivity.Value.mayLackEnd)
                    continue;
                YieldActivity(startedActivity.Value, endOfTimelineEventInfo.Value, startedActivity.Value.beginOwner, isEndedForcefully: true);
            }
            startedActivities.Clear();

            var evt = endOfTimelineEventInfo.Value.evt as EndOfTimelineEvent;
            if (evt != null && !string.IsNullOrEmpty(evt.DisplayName))
            {
                timelineDisplayName = evt.DisplayName;
            }
        }

        void SetOrigin(EventInfo e)
        {
            if (!originSet)
            {
                origin = e.timestamp;
                originSet = true;
            }
        }

        void AddEvent(Event sourceEvt, EventType type)
        {
            var eventInfo = new EventInfo(sourceEvt, currentTimeOffsets);
            SetOrigin(eventInfo);
            events.Add(new EventImpl(currentPostprocessorOutput, eventInfo.timestamp - origin, sourceEvt.DisplayName, type, sourceEvt.Trigger));
        }

        struct EventInfo
        {
            public Event evt;
            public DateTime timestamp;
            public EventInfo(Event e, ITimeOffsets timeOffsets)
            {
                evt = e;
                var m = (TextLogEventTrigger)e.Trigger;
                timestamp = m.Timestamp.Adjust(timeOffsets).ToLocalDateTime();
            }
        };

        class StartedActivity
        {
            internal EventInfo begin;
            internal ITimelinePostprocessorOutput beginOwner;
            internal string activityMatchingId;
            internal List<ActivityMilestoneInfo> milestones;
            internal List<ActivityPhaseInfo> phases;
            internal ActivityType type;
            internal bool mayLackEnd;
            internal ActivityStatus status;
        };

        void HandleActivityEvent(ActivityEventBase evt, ActivityType type, string activityMatchingId = null)
        {
            var eventInfo = new EventInfo(evt, currentTimeOffsets);
            SetOrigin(eventInfo);
            if (evt.Type == ActivityEventType.Begin || evt.Type == ActivityEventType.PotentialBegin)
            {
                var activity = new StartedActivity()
                {
                    begin = eventInfo,
                    beginOwner = currentPostprocessorOutput,
                    type = type,
                    activityMatchingId = activityMatchingId,
                    milestones = new List<ActivityMilestoneInfo>(),
                    phases = new List<ActivityPhaseInfo>(),
                    mayLackEnd = evt.Type == ActivityEventType.PotentialBegin
                };
                startedActivities[evt.ActivityId] = activity;
                UpdateStatus(activity, evt.Status);
                AddPhases(activity, evt);
            }
            else if (evt.Type == ActivityEventType.End)
            {
                StartedActivity startedActivity;
                if (startedActivities.TryGetValue(evt.ActivityId, out startedActivity))
                {
                    AddPhases(startedActivity, evt);
                    UpdateStatus(startedActivity, evt.Status);
                    YieldActivity(startedActivity, eventInfo, currentPostprocessorOutput, isEndedForcefully: false);
                    startedActivities.Remove(evt.ActivityId);
                }
            }
            else if (evt.Type == ActivityEventType.Milestone)
            {
                StartedActivity startedActivity;
                if (startedActivities.TryGetValue(evt.ActivityId, out startedActivity))
                {
                    startedActivity.milestones.Add(new ActivityMilestoneInfo(
                        null,
                        currentPostprocessorOutput,
                        eventInfo.timestamp - origin,
                        evt.DisplayName,
                        evt.Trigger
                    ));
                    AddPhases(startedActivity, evt);
                    UpdateStatus(startedActivity, evt.Status);
                }
            }
        }

        void UpdateStatus(StartedActivity activity, ActivityStatus eventStatus)
        {
            if (activity.status == ActivityStatus.Unspecified)
            {
                activity.status = eventStatus;
            }
            else if (eventStatus == ActivityStatus.Error)
            {
                activity.status = eventStatus;
            }
        }

        void AddPhases(StartedActivity startedActivity, ActivityEventBase evt)
        {
            if (evt.Phases != null && evt.Phases.Count > 0 && startedActivity.phases.Count == 0)
            {
                startedActivity.phases.AddRange(evt.Phases.Select(ph => new ActivityPhaseInfo(
                    null,
                    currentPostprocessorOutput,
                    startedActivity.begin.timestamp - origin + ph.Begin,
                    startedActivity.begin.timestamp - origin + ph.End,
                    ph.Type,
                    ph.DisplayName
                )));
            }
        }

        void YieldActivity(StartedActivity startedActivity, EventInfo endEvtInfo, ITimelinePostprocessorOutput endOwner, bool isEndedForcefully)
        {
            var begin = startedActivity.begin;
            activities.Add(new ActivityImpl(
                startedActivity.beginOwner,
                endOwner,
                begin.timestamp - origin,
                endEvtInfo.timestamp - origin,
                (startedActivity.type == ActivityType.Lifespan && !isEndedForcefully && endEvtInfo.evt.DisplayName.Length > begin.evt.DisplayName.Length) ?
                    endEvtInfo.evt.DisplayName : // for lifespans get End's display name because it may be more specific than Begin's one
                    begin.evt.DisplayName,
                startedActivity.activityMatchingId,
                startedActivity.type,
                begin.evt.Trigger,
                endEvtInfo.evt.Trigger,
                startedActivity.milestones,
                startedActivity.phases,
                startedActivity.begin.evt.Tags.Concat(endEvtInfo.evt.Tags),
                isError: startedActivity.status == ActivityStatus.Error,
                isEndedForcefully: isEndedForcefully
            ));
        }
    };
}

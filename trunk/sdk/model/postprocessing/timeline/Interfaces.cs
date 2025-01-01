﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Timeline
{
    public class PostprocessorOutputBuilder
    {
        public PostprocessorOutputBuilder SetLogPartToken(Task<ILogPartToken> value) { rotatedLogPartToken = value; return this; }
        public PostprocessorOutputBuilder SetEvents(IEnumerableAsync<Event[]> value) { events = value; return this; }
        public PostprocessorOutputBuilder SetTriggersConverter(Func<object, TextLogEventTrigger> value) { triggersConverter = value; return this; }
        public Task Build(LogSourcePostprocessorInput postprocessorParams) { return build(postprocessorParams, this); }

        internal IEnumerableAsync<Event[]> events;
        internal Task<ILogPartToken> rotatedLogPartToken;
        internal Func<object, TextLogEventTrigger> triggersConverter;
        internal Func<LogSourcePostprocessorInput, PostprocessorOutputBuilder, Task> build;
    };

    public interface IModel
    {
        PostprocessorOutputBuilder CreatePostprocessorOutputBuilder();
        [Obsolete]
        Task SavePostprocessorOutput(
            IEnumerableAsync<Event[]> events,
            Task<ILogPartToken> rotatedLogPartToken,
            Func<object, TextLogEventTrigger> triggersConverter,
            LogSourcePostprocessorInput postprocessorInput
        );
        IEndOfTimelineEventSource<Message> CreateEndOfTimelineEventSource<Message>(
            Func<Message, object> triggetSelector = null);
        IInspectedObjectsLifetimeEventsSource CreateInspectedObjectsLifetimeEventsSource(
            Predicate<StateInspector.Event> inspectedObjectsFilter = null);
        IMessagingEventsSource CreateMessagingEventsSource();
    };

    public interface IEndOfTimelineEventSource<Message>
    {
        IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<Message[]> input);
    }

    public interface IInspectedObjectsLifetimeEventsSource
    {
        IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<StateInspector.Event[]> input);
    }

    public interface IMessagingEventsSource
    {
        IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<Messaging.Event[]> input);
    }

    public abstract class Event : ITagged, IVisitable<IEventsVisitor>
    {
        public object Trigger;
        public readonly string DisplayName;
        public readonly int TemplateId;
        public HashSet<string> Tags { get { return tags; } set { tags = value; } }

        protected Event(object trigger, string displayName, int templateId)
        {
            this.Trigger = trigger;
            this.DisplayName = displayName;
            this.tags = new HashSet<string>();
            this.TemplateId = templateId;
        }

        public abstract void Visit(IEventsVisitor visitor);

        public override string ToString()
        {
            var stringifier = new EventsStringifier();
            Visit(stringifier);
            return stringifier.Output.ToString();
        }

        HashSet<string> tags;
    };

    public enum ActivityEventType
    {
        Begin,
        End,
        Milestone,
        /// <summary>
        /// This event will generate new activity only if corresponding <see cref="End"/> event is emitted.
        /// </summary>
        PotentialBegin
    };

    public enum ActivityStatus
    {
        Unspecified,
        Success,
        Error
    };

    public abstract class ActivityEventBase : Event
    {
        public readonly string ActivityId;
        public readonly ActivityEventType Type;
        public ActivityStatus Status { get; set; }
        public List<ActivityPhase> Phases { get { return phases; } set { phases = value; } }

        protected ActivityEventBase(object trigger, string displayName, string activityId, ActivityEventType type,
                                 int templateId = 0, ActivityStatus status = ActivityStatus.Unspecified)
            : base(trigger, displayName, templateId)
        {
            this.ActivityId = activityId;
            this.Type = type;
            this.Status = status;
        }

        private List<ActivityPhase> phases;
    };

    public struct ActivityPhase
    {
        public readonly TimeSpan Begin;
        public readonly TimeSpan End;
        public readonly int Type;
        public readonly string DisplayName;

        public ActivityPhase(TimeSpan b, TimeSpan e, int type, string displayName)
        {
            Begin = b;
            End = e;
            Type = type;
            DisplayName = displayName;
        }
    };

    public class ProcedureEvent : ActivityEventBase
    {
        public ProcedureEvent(object trigger, string displayName, string procedureId, ActivityEventType type, int templateId = 0, ActivityStatus status = ActivityStatus.Unspecified) :
            base(trigger, displayName, procedureId, type, templateId, status)
        { }

        public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
    };

    public enum NetworkMessageDirection
    {
        Unknown,
        Outgoing,
        Incoming
    };

    public class NetworkMessageEvent : ActivityEventBase
    {
        public readonly NetworkMessageDirection Direction;

        public NetworkMessageEvent(object trigger, string displayName, string messageId, ActivityEventType type, NetworkMessageDirection direction,
                                   int templateId = 0, ActivityStatus status = ActivityStatus.Unspecified) :
            base(trigger, displayName, messageId, type, templateId, status)
        { Direction = direction; }

        public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
    };

    public class ObjectLifetimeEvent : ActivityEventBase
    {
        public ObjectLifetimeEvent(object trigger, string displayName, string objectId, ActivityEventType type, int templateId = 0) :
            base(trigger, displayName, objectId, type, templateId)
        { }

        public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
    };

    public class UserActionEvent : Event
    {
        public UserActionEvent(object trigger, string displayName, int templateId = 0) : base(trigger, displayName, templateId) { }

        public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
    };

    public class APICallEvent : Event
    {
        public APICallEvent(object trigger, string displayName, int templateId = 0) : base(trigger, displayName, templateId) { }

        public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
    };

    public class EndOfTimelineEvent : Event
    {
        public EndOfTimelineEvent(object trigger, string displayName) : base(trigger, displayName, 0) { }

        public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
    };

    public interface IEventsVisitor
    {
        void Visit(ProcedureEvent evt);
        void Visit(ObjectLifetimeEvent evt);
        void Visit(UserActionEvent evt);
        void Visit(NetworkMessageEvent evt);
        void Visit(APICallEvent evt);
        void Visit(EndOfTimelineEvent evt);
    };
}

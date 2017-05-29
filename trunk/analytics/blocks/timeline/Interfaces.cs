using System;
using System.Collections.Generic;

namespace LogJoint.Analytics.Timeline
{
	public abstract class Event: ITagged
	{
		public object Trigger;
		public readonly string DisplayName;
		public readonly int TemplateId;
		public HashSet<string> Tags { get { return tags; } set { tags = value; } }

		public Event(object trigger, string displayName, int templateId)
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
		PotentialBegin
	};

	public abstract class ActivityEventBase : Event
	{
		public readonly string ActivityId;
		public readonly ActivityEventType Type;

		public ActivityEventBase(object trigger, string displayName, string activityId, ActivityEventType type, int templateId = 0)
			: base(trigger, displayName, templateId)
		{
			this.ActivityId = activityId;
			this.Type = type;
		}
	};

	public class ProcedureEvent : ActivityEventBase
	{
		public ProcedureEvent(object trigger, string displayName, string procedureId, ActivityEventType type, int templateId = 0) :
			base(trigger, displayName, procedureId, type, templateId) { }

		public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
	};

	public enum NetworkMessageDirection
	{
		Unknown,
		Outgoing,
		Incoming
	};

	public class NetworkMessageEvent: ActivityEventBase
	{
		public readonly NetworkMessageDirection Direction;

		public NetworkMessageEvent(object trigger, string displayName, string messageId, ActivityEventType type, NetworkMessageDirection direction, int templateId = 0) :
			base(trigger, displayName, messageId, type, templateId) { Direction = direction; }

		public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
	};

	public class ObjectLifetimeEvent : ActivityEventBase
	{
		public ObjectLifetimeEvent(object trigger, string displayName, string objectId, ActivityEventType type, int templateId = 0) :
			base(trigger, displayName, objectId, type, templateId) { }

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
		public EndOfTimelineEvent(object trigger, string displayName) : base(trigger, displayName, 0) {}

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

using System;
using System.Linq;
using System.Collections.Generic;
using SI = LogJoint.Postprocessing.StateInspector;

namespace LogJoint.Postprocessing.Timeline
{
	public class InspectedObjectsLifetimeEventsSource : StateInspector.IEventsVisitor, IInspectedObjectsLifetimeEventsSource
	{
		public InspectedObjectsLifetimeEventsSource(Predicate<SI.Event> inspectedObjectsFilter = null)
		{
			this.inspectedObjectsFilter = inspectedObjectsFilter ?? defaultFilter;
		}

		public InspectedObjectsLifetimeEventsSource(Predicate<string> inspectedObjectsFilter)
		{
			this.inspectedObjectsFilter = inspectedObjectsFilter != null ? (evt => inspectedObjectsFilter(evt.ObjectId)) : defaultFilter;
		}

		public IEnumerableAsync<Timeline.Event[]> GetEvents(IEnumerableAsync<StateInspector.Event[]> input)
		{
			return input.Select<StateInspector.Event, Timeline.Event>((evt, buffer) =>
			{
				resultEvents = buffer;
				evt.Visit(this);
			}, YieldFinalEvents);
		}

		void StateInspector.IEventsVisitor.Visit(StateInspector.ObjectCreation objectCreation)
		{
			if (inspectedObjectsFilter(objectCreation))
			{
				InspectedObject inspectedObject = EnsureInspectedObjectExists(objectCreation, forceOverwrite: true);
				inspectedObject.CreationTrigger = objectCreation.Trigger;
				if (!string.IsNullOrEmpty(objectCreation.DisplayName))
					inspectedObject.DisplayName = objectCreation.DisplayName;
			}
		}

		void StateInspector.IEventsVisitor.Visit(StateInspector.ObjectDeletion objectDeletion)
		{
			if (inspectedObjectsFilter(objectDeletion))
			{
				InspectedObject inspectedObject = EnsureInspectedObjectExists(objectDeletion);
				inspectedObjects.Remove(objectDeletion.ObjectId);
				inspectedObject.YieldPendingEvents(resultEvents);
				resultEvents.Enqueue(inspectedObject.AddTags(new Timeline.ObjectLifetimeEvent(
					objectDeletion.Trigger,
					inspectedObject.MakeActivityDisplayName(),
					inspectedObject.Id,
					ActivityEventType.End)));
			}
		}

		void StateInspector.IEventsVisitor.Visit(StateInspector.PropertyChange propertyChange)
		{
			if (inspectedObjectsFilter(propertyChange))
			{
				InspectedObject inspectedObject = EnsureInspectedObjectExists(propertyChange);

				if (propertyChange.ObjectType.PrimaryPropertyName != null
				 && propertyChange.ObjectType.PrimaryPropertyName == propertyChange.PropertyName)
				{
					inspectedObject.Milestones.Add(new Timeline.ObjectLifetimeEvent(
						propertyChange.Trigger,
						string.Format("{0}.{1}->{2}", propertyChange.ObjectId, propertyChange.PropertyName, propertyChange.Value),
						propertyChange.ObjectId,
						ActivityEventType.Milestone
					));
				}

				if (propertyChange.Tags != null)
					inspectedObject.Tags.UnionWith(propertyChange.Tags);

				if (propertyChange.ObjectType.CommentPropertyName != null
				 && inspectedObject.DisplayId == null
				 && propertyChange.ObjectType.CommentPropertyName == propertyChange.PropertyName)
				{
					if (propertyChange.ValueType == SI.ValueType.UserHash)
						inspectedObject.DisplayId = string.Format("<uh>{0}</uh>", propertyChange.Value);
					else
						inspectedObject.DisplayId = propertyChange.Value;
					inspectedObject.YieldPendingEvents(resultEvents);
				}
			}
		}

		void StateInspector.IEventsVisitor.Visit(StateInspector.ParentChildRelationChange parentChildRelationChange) {}

		InspectedObject EnsureInspectedObjectExists(StateInspector.Event evt, bool forceOverwrite = false)
		{
			InspectedObject obj;
			if (forceOverwrite || !inspectedObjects.TryGetValue(evt.ObjectId, out obj))
			{
				inspectedObjects[evt.ObjectId] = obj = new InspectedObject()
				{
					Id = evt.ObjectId,
					Tags = evt.Tags != null ? new HashSet<string>(evt.Tags) : new HashSet<string>()
				};
			}
			return obj;
		}

		void YieldFinalEvents(Queue<Event> buffer)
		{
			foreach (var inspectedObject in inspectedObjects.Values)
				inspectedObject.YieldPendingEvents(buffer);
			inspectedObjects.Clear();
		}

		class InspectedObject
		{
			public string Id;
			public string DisplayId;
			public string DisplayName;
			public HashSet<string> Tags;
			public object CreationTrigger;
			public List<Timeline.Event> Milestones = new List<Event>();

			public string MakeActivityDisplayName()
			{
				var ret = DisplayName ?? Id;
				if (!string.IsNullOrEmpty(DisplayId))
					ret += " (" + DisplayId + ")";
				return ret;
			}

			public void YieldPendingEvents(Queue<Event> buffer)
			{
				if (CreationTrigger != null)
				{
					buffer.Enqueue(AddTags(new Timeline.ObjectLifetimeEvent(CreationTrigger, MakeActivityDisplayName(), Id, ActivityEventType.Begin)));
					CreationTrigger = null;
				}
				if (Milestones.Count > 0)
				{
					foreach (var m in Milestones)
						buffer.Enqueue(AddTags(m));
					Milestones.Clear();
				}
			}

			public Event AddTags(Event evt)
			{
				evt.Tags.UnionWith(this.Tags);
				return evt;
			}
		};

		readonly Predicate<SI.Event> inspectedObjectsFilter;
		readonly Dictionary<string, InspectedObject> inspectedObjects = new Dictionary<string, InspectedObject>();
		Queue<Timeline.Event> resultEvents;
		static readonly Predicate<SI.Event> defaultFilter = _ => true;
	}
}

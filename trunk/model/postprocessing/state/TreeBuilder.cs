using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.StateInspector
{
	public class TreeBuilder : IEventsVisitor
	{
		public TreeBuilder(IStateInspectorOutputsGroup owner, IUserNamesProvider shortNames)
		{
			this.owner = owner;
			this.shortNames = shortNames;
		}

		public void AddEventsFrom(IStateInspectorOutputsGroup stateInspectorOutput)
		{
			foreach (var evt in stateInspectorOutput.Events)
			{
				currentEvent = evt;
				evt.OriginalEvent.Visit(this);
			}
		}

		public List<IInspectedObject> Build()
		{
			return BuildTree();
		}

		List<IInspectedObject> BuildTree()
		{
			finalizedObjects.AddRange(objects.Values);
			return finalizedObjects.Where(i => i.Parent == null).ToList();
		}

		IInspectedObject GetObject(string id)
		{
			IInspectedObject item;
			if (objects.TryGetValue(id, out item))
				return item;
			item = new InspectedObject(owner, id, shortNames);
			objects.Add(id, item);
			return item;
		}

		void FinalizeExistingObject(string id)
		{
			IInspectedObject existingItem;
			if (objects.TryGetValue(id, out existingItem) && existingItem.CreationEvent != null)
			{
				finalizedObjects.Add(existingItem);
				objects.Remove(id);
			}
		}

		bool ObjectExists(string id)
		{
			return objects.ContainsKey(id);
		}

		void IEventsVisitor.Visit(ParentChildRelationChange parentChildEvt)
		{
			var obj = GetObject(parentChildEvt.ObjectId);
			var parent = GetObject(parentChildEvt.NewParentObjectId);
			if (obj.Parent != null)
				if (parentChildEvt.IsWeak)
					return;
				else
					obj.Parent.RemoveChild(obj);
			obj.SetParent(parent);
			if (obj.Parent != null)
				parent.AddChild(obj);
		}

		void IEventsVisitor.Visit(PropertyChange propertyChange)
		{
			GetObject(propertyChange.ObjectId).AddStateChangeEvent(currentEvent);
		}

		void IEventsVisitor.Visit(ObjectDeletion objectDeletion)
		{
			var obj = GetObject(objectDeletion.ObjectId);
			obj.SetDeletionEvent(currentEvent);
			obj.AddStateChangeEvent(currentEvent);
		}

		void IEventsVisitor.Visit(ObjectCreation objectCreation)
		{
			bool recordEventToObjectHistory;
			if (objectCreation.IsWeak)
			{
				recordEventToObjectHistory = !ObjectExists(objectCreation.ObjectId);
			}
			else
			{
				// ensure any existing object with given id is finalized
				FinalizeExistingObject(objectCreation.ObjectId);
				recordEventToObjectHistory = true;
			}
			var obj = GetObject(objectCreation.ObjectId);
			if (recordEventToObjectHistory)
			{
				obj.SetCreationEvent(currentEvent);
				obj.AddStateChangeEvent(currentEvent);
			}
		}

		readonly IStateInspectorOutputsGroup owner;
		readonly Dictionary<string, IInspectedObject> objects = new Dictionary<string, IInspectedObject>(); // object id -> object
		readonly List<IInspectedObject> finalizedObjects = new List<IInspectedObject>(); // objects that wont accept new children or prop changes
		readonly IUserNamesProvider shortNames;
		StateInspectorEvent currentEvent;
	};
}

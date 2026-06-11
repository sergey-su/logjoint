using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint.Postprocessing.StateInspector
{
    public class TreeBuilder
    {
        public TreeBuilder(IStateInspectorOutputsGroup owner)
        {
            this.owner = owner;
        }

        public void AddEventsFrom(IStateInspectorOutputsGroup stateInspectorOutput)
        {
            foreach (var evt in stateInspectorOutput.Events)
            {
                evt.OriginalEvent.Visit(new Visitor(this, evt));
            }
        }

        public ImmutableArray<IInspectedObject> Build()
        {
            return BuildTree();
        }

        ImmutableArray<IInspectedObject> BuildTree()
        {
            finalizedObjects.AddRange(objects.Values);
            return finalizedObjects.Where(i => i.Parent == null).ToImmutableArray();
        }

        IInspectedObject GetObject(string id)
        {
            IInspectedObject? item;
            if (objects.TryGetValue(id, out item))
                return item;
            item = new InspectedObject(owner, id);
            objects.Add(id, item);
            return item;
        }

        void FinalizeExistingObject(string id)
        {
            IInspectedObject? existingItem;
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

        class Visitor : IEventsVisitor
        {
            public Visitor(TreeBuilder treeBuilder, StateInspectorEvent currentEvent)
            {
                this.treeBuilder = treeBuilder;
                this.currentEvent = currentEvent;
            }

            void IEventsVisitor.Visit(ParentChildRelationChange parentChildEvt)
            {
                var obj = treeBuilder.GetObject(parentChildEvt.ObjectId);
                var parent = treeBuilder.GetObject(parentChildEvt.NewParentObjectId);
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
                treeBuilder.GetObject(propertyChange.ObjectId).AddStateChangeEvent(currentEvent);
            }

            void IEventsVisitor.Visit(ObjectDeletion objectDeletion)
            {
                void DeleteObject(IInspectedObject obj)
                {
                    if (obj.SetDeletionEvent(currentEvent))
                    {
                        obj.AddStateChangeEvent(currentEvent);
                    }
                    foreach (var c in obj.Children)
                    {
                        DeleteObject(c);
                    }
                }
                DeleteObject(treeBuilder.GetObject(objectDeletion.ObjectId));
            }

            void IEventsVisitor.Visit(ObjectCreation objectCreation)
            {
                bool recordEventToObjectHistory;
                if (objectCreation.IsWeak)
                {
                    recordEventToObjectHistory = !treeBuilder.ObjectExists(objectCreation.ObjectId);
                }
                else
                {
                    // ensure any existing object with given id is finalized
                    treeBuilder.FinalizeExistingObject(objectCreation.ObjectId);
                    recordEventToObjectHistory = true;
                }
                var obj = treeBuilder.GetObject(objectCreation.ObjectId);
                if (recordEventToObjectHistory)
                {
                    obj.SetCreationEvent(currentEvent);
                    obj.AddStateChangeEvent(currentEvent);
                }
            }

            readonly TreeBuilder treeBuilder;
            readonly StateInspectorEvent currentEvent;
        };

        readonly IStateInspectorOutputsGroup owner;
        readonly Dictionary<string, IInspectedObject> objects = new(); // object id -> object
        readonly List<IInspectedObject> finalizedObjects = new(); // objects that wont accept new children or prop changes
    };
}

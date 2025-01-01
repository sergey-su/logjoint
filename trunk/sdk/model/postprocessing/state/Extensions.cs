using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.StateInspector
{
    public static class StateInspectorExtensions
    {
        public static IEnumerableAsync<Event[]> EnsureParented(
            this IEnumerableAsync<Event[]> input,
            Func<ObjectCreation, Queue<Event>, string> getParent
        )
        {
            var unparentedObjects = new Dictionary<string, ObjectCreation>();
            return input.Select<Event, Event>((e, buffer) =>
            {
                buffer.Enqueue(e);
                if (e is ObjectCreation c)
                    unparentedObjects[e.ObjectId] = c;
                else if (e is ParentChildRelationChange)
                    unparentedObjects.Remove(e.ObjectId);
            }, buffer =>
            {
                foreach (var obj in unparentedObjects)
                {
                    var p = getParent(obj.Value, buffer);
                    if (p != null)
                        buffer.Enqueue(new ParentChildRelationChange(obj.Value.Trigger,
                            obj.Value.ObjectId, obj.Value.ObjectType, p));
                }
            });
        }

        public static IEnumerableAsync<Event[]> TrackTemplates(this ICodepathTracker codepathTracker, IEnumerableAsync<Event[]> events)
        {
            return events.Select(batch =>
            {
                if (codepathTracker != null)
                    foreach (var e in batch)
                        codepathTracker.RegisterUsage(e.TemplateId);
                return batch;
            });
        }
    };
}

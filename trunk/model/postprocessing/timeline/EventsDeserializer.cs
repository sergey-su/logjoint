using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using SC = LogJoint.Postprocessing.Timeline.SerializationCommon;

namespace LogJoint.Postprocessing.Timeline
{
    public class EventsDeserializer
    {
        public EventsDeserializer(Func<XElement, object>? triggerDeserializer = null)
        {
            this.triggerDeserializer = triggerDeserializer;
        }

        public bool TryDeserialize(XElement elt, [NotNullWhen(true)] out Event? ret)
        {
            ret = null;
            string? id;
            ActivityEventType? type;
            switch (elt.Name.LocalName)
            {
                case SC.Elt_Procedure:
                    if ((id = Attr(elt, SC.Attr_ActivityId)) == null)
                        return false;
                    if ((type = ActivityEventType(elt, SC.Attr_Type)) == null)
                        return false;
                    ret = new ProcedureEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName), id, type.Value,
                            status: Status(elt, SC.Attr_Status));
                    break;
                case SC.Elt_Lifetime:
                    if ((id = Attr(elt, SC.Attr_ActivityId)) == null)
                        return false;
                    if ((type = ActivityEventType(elt, SC.Attr_Type)) == null)
                        return false;
                    ret = new ObjectLifetimeEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName), id, type.Value);
                    break;
                case SC.Elt_NetworkMessage:
                    if ((id = Attr(elt, SC.Attr_ActivityId)) == null)
                        return false;
                    if ((type = ActivityEventType(elt, SC.Attr_Type)) == null)
                        return false;
                    ret = new NetworkMessageEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName), id,
                            type.Value, NetworkMessageDirection(elt, SC.Attr_Direction),
                            status: Status(elt, SC.Attr_Status));
                    break;
                case SC.Elt_UserAction:
                    ret = new UserActionEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName));
                    break;
                case SC.Elt_APICall:
                    ret = new APICallEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName));
                    break;
                case SC.Elt_EOF:
                    ret = new EndOfTimelineEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName));
                    break;
            }
            if (ret != null)
            {
                ret.Tags = tagsPool.Intern(
                    new HashSet<string>((Attr(elt, SC.Attr_Tags) ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
                ReadPhases(elt, ret);
            }
            return ret != null;
        }

        static void ReadPhases(XElement elt, Event ret)
        {
            if (elt.HasElements)
            {
                var act = ret as ActivityEventBase;
                if (act != null)
                {
                    var phases = elt.Elements(SC.Elt_Phase).Select(ph => new ActivityPhase(
                        TimeSpan.FromTicks(long.Parse(Attr(ph, SC.Attr_Begin))),
                        TimeSpan.FromTicks(long.Parse(Attr(ph, SC.Attr_End))),
                        int.Parse(Attr(ph, SC.Attr_Type)),
                        Attr(ph, SC.Attr_DisplayName)
                    )).ToList();
                    if (phases.Count > 0)
                    {
                        act.Phases = phases;
                    }
                }
            }
        }

        object? MakeTrigger(XElement e)
        {
            if (triggerDeserializer != null)
                return triggerDeserializer(e);
            return null;
        }

        static string? Attr(XElement e, string name)
        {
            var attr = e.Attribute(name);
            return attr == null ? null : attr.Value;
        }

        static ActivityEventType? ActivityEventType(XElement e, string name)
        {
            int? code = e.IntValue(name);
            if (code == null)
                return null;
            return (ActivityEventType)code;
        }

        static NetworkMessageDirection NetworkMessageDirection(XElement e, string name)
        {
            return (NetworkMessageDirection)int.Parse(Attr(e, name) ?? "0");
        }

        static ActivityStatus Status(XElement e, string name)
        {
            return (ActivityStatus)int.Parse(Attr(e, name) ?? "0");
        }

        readonly Func<XElement, object>? triggerDeserializer;
        readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
    }
}

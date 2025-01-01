using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using SC = LogJoint.Postprocessing.Timeline.SerializationCommon;

namespace LogJoint.Postprocessing.Timeline
{
    public class EventsDeserializer
    {
        public EventsDeserializer(Func<XElement, object> triggerDeserializer = null)
        {
            this.triggerDeserializer = triggerDeserializer;
        }

        public bool TryDeserialize(XElement elt, out Event ret)
        {
            ret = null;
            switch (elt.Name.LocalName)
            {
                case SC.Elt_Procedure:
                    ret = new ProcedureEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName), Attr(elt, SC.Attr_ActivityId), ActivityEventType(elt, SC.Attr_Type),
                            status: Status(elt, SC.Attr_Status));
                    break;
                case SC.Elt_Lifetime:
                    ret = new ObjectLifetimeEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName), Attr(elt, SC.Attr_ActivityId), ActivityEventType(elt, SC.Attr_Type));
                    break;
                case SC.Elt_NetworkMessage:
                    ret = new NetworkMessageEvent(
                        MakeTrigger(elt), Attr(elt, SC.Attr_DisplayName), Attr(elt, SC.Attr_ActivityId),
                            ActivityEventType(elt, SC.Attr_Type), NetworkMessageDirection(elt, SC.Attr_Direction),
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

        object MakeTrigger(XElement e)
        {
            if (triggerDeserializer != null)
                return triggerDeserializer(e);
            return null;
        }

        static string Attr(XElement e, string name)
        {
            var attr = e.Attribute(name);
            return attr == null ? null : attr.Value;
        }

        static ActivityEventType ActivityEventType(XElement e, string name)
        {
            return (ActivityEventType)int.Parse(Attr(e, name));
        }

        static NetworkMessageDirection NetworkMessageDirection(XElement e, string name)
        {
            return (NetworkMessageDirection)int.Parse(Attr(e, name) ?? "0");
        }

        static ActivityStatus Status(XElement e, string name)
        {
            return (ActivityStatus)int.Parse(Attr(e, name) ?? "0");
        }

        readonly Func<XElement, object> triggerDeserializer;
        readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
    }
}

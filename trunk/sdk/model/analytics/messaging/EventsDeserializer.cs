using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using SC = LogJoint.Analytics.Messaging.SerializationCommon;

namespace LogJoint.Analytics.Messaging
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
				case SC.Elt_HttpMessage:
					ret = new HttpMessage(
						MakeTrigger(elt), 
						Attr(elt, SC.Attr_DisplayName), 
						MessageDirection(elt, SC.Attr_MessageDirection), 
						MessageType(elt, SC.Attr_MessageType), 
						Attr(elt, SC.Attr_MessageId),
						Attr(elt, SC.Attr_Remote),
						Attr(elt, SC.Attr_Method),
						"",
						Enumerable.Empty<KeyValuePair<string, string>>(),
						StatusCode(elt, SC.Attr_StatusCode),
						targetIdHint: Attr(elt, SC.Attr_TargetId),
						statusComment: Attr(elt, SC.Attr_StatusComment)
					);
					break;
				case SC.Elt_NetworkMessage:
					ret = new NetworkMessageEvent(
						MakeTrigger(elt),
						Attr(elt, SC.Attr_DisplayName), 
						MessageDirection(elt, SC.Attr_MessageDirection),
						MessageType(elt, SC.Attr_MessageType),
						typesPool.Intern(Attr(elt, SC.Attr_EventType)),
						Attr(elt, SC.Attr_MessageId),
						Attr(elt, SC.Attr_TargetId),
						remoteNodeIdsPool.Intern(Attr(elt, SC.Attr_Remote))
					);
					break;
				case SC.Elt_ResponselessNetworkMessage:
					ret = new ResponselessNetworkMessageEvent(
						MakeTrigger(elt),
						Attr(elt, SC.Attr_DisplayName),
						MessageDirection(elt, SC.Attr_MessageDirection),
						MessageType(elt, SC.Attr_MessageType), 
						messageId: Attr(elt, SC.Attr_MessageId),
						targetIdHint: Attr(elt, SC.Attr_TargetId)
					);
					break;
				case SC.Elt_Cancellation:
					ret = new RequestCancellationEvent(
						MakeTrigger(elt),
						Attr(elt, SC.Attr_DisplayName),
						Attr(elt, SC.Attr_MessageId)
					);
					break;
				case SC.Elt_Function:
					ret = new FunctionInvocationEvent(
						MakeTrigger(elt),
						Attr(elt, SC.Attr_DisplayName),
						MessageDirection(elt, SC.Attr_MessageDirection),
						MessageType(elt, SC.Attr_MessageType),
						Attr(elt, SC.Attr_MessageId)
					);
					break;
				case SC.Elt_Meta:
					ret = new MetadataEvent(
						MakeTrigger(elt),
						Attr(elt, SC.Attr_MetaKey),
						Attr(elt, SC.Attr_MetaValue)
					);
					break;
			}
			if (ret != null)
			{
				ret.Tags = tagsPool.Intern(
					new HashSet<string>((Attr(elt, SC.Attr_Tags) ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
			}
			return ret != null;
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

		static MessageDirection MessageDirection(XElement e, string name)
		{
			return (MessageDirection)int.Parse(Attr(e, name));
		}

		static MessageType MessageType(XElement e, string name)
		{
			return (MessageType)int.Parse(Attr(e, name));
		}

		static int? StatusCode(XElement e, string name)
		{
			var a = Attr(e, name);
			int r;
			if (a != null && int.TryParse(a, out r))
				return r;
			return null;
		}

		readonly Func<XElement, object> triggerDeserializer;
		readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
		readonly StringInternPool typesPool = new StringInternPool();
		readonly StringInternPool remoteNodeIdsPool = new StringInternPool();
	}
}

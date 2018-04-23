using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LogJoint.Chromium.ChromeDriver
{
	public interface ITimelineEvents
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input);
	};

	public class TimelineEvents : ITimelineEvents
	{
		public TimelineEvents(
			IPrefixMatcher matcher
		)
		{
			devToolsNetworkEventPrefix = matcher.RegisterPrefix(DevTools.Events.Network.Generic.Prefix);
		}


		IEnumerableAsync<Event[]> ITimelineEvents.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags));
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			var msg = msgPfx.Message;
			if (msgPfx.Prefixes.Contains(devToolsNetworkEventPrefix))
			{
				var m = DevTools.Events.LogMessage.Parse(msg.Text);
				if (m != null)
				{
					var payload = m.ParsePayload<DevTools.Events.Network.Generic>();
					if (payload?.requestId != null)
					{
						string displayName = payload.requestId;
						var type = 
							m.EventType == "requestWillBeSent" ? ActivityEventType.Begin :
							m.EventType == "loadingFinished" ? ActivityEventType.End :
							ActivityEventType.Milestone;
						if (type == ActivityEventType.Milestone)
						{
							displayName = m.EventType;
						}
						else if (type == ActivityEventType.Begin)
						{
							if (payload.request?.url != null)
							{
								string methodPart = payload.request.method;
								methodPart = string.IsNullOrEmpty(methodPart) ? "" : (methodPart + " ");
								string urlPart = payload.request.url;
								Uri uri;
								if (Uri.TryCreate(payload.request?.url, UriKind.Absolute, out uri)
								&& !(uri.PathAndQuery == "" || uri.PathAndQuery == "/"))
								{
									urlPart = uri.PathAndQuery;
								}
								displayName = string.Format("{0}{1}", methodPart, urlPart);
							}
						}
						buffer.Enqueue(new NetworkMessageEvent(msg, displayName, payload.requestId, type, NetworkMessageDirection.Outgoing));
					}
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		readonly int devToolsNetworkEventPrefix;
		static readonly HashSet<string> tags = new HashSet<string>() { "devtools" };
	}
}

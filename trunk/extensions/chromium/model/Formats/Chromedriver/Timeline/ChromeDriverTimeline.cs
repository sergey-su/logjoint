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
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents);
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
						buffer.Enqueue(new NetworkMessageEvent(msg, displayName, payload.requestId, type, NetworkMessageDirection.Outgoing)
							.SetTags(GetRequestTags(payload.frameId)));
					}
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		HashSet<string> GetRequestTags(string frameId)
		{
			if (string.IsNullOrEmpty(frameId))
				return defaultTags;
			HashSet<string> ret;
			if (!tagsCache.TryGetValue(frameId, out ret))
				tagsCache.Add(frameId, ret = new HashSet<string>(new[] { string.Format("frame-{0}", tagsCache.Count + 1) }));
			return ret;
		}

		readonly int devToolsNetworkEventPrefix;
		static readonly Dictionary<string, HashSet<string>> tagsCache = new Dictionary<string, HashSet<string>>();
		static readonly HashSet<string> defaultTags = new HashSet<string>();
	}
}

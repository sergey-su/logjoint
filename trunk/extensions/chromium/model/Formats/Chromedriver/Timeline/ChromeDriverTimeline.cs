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
						string host = null;
						string pidTag = DevTools.Events.Network.Generic.ParseRequestPid(payload.requestId);
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
									host = uri.Host;
								}
								displayName = string.Format("{0}{1}", methodPart, urlPart);
							}
						}
						buffer.Enqueue(new NetworkMessageEvent(msg, displayName, payload.requestId, type, NetworkMessageDirection.Outgoing)
							.SetTags(GetRequestTags(payload.frameId, host, pidTag)));
					}
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		HashSet<string> GetRequestTags(string frameId, string host, string pid)
		{
			string key = string.Format("frame: {0}, tag: {1}", frameId, host);
			HashSet<string> ret;
			if (!tagsCache.TryGetValue(key, out ret))
			{
				ret = new HashSet<string>();
				if (frameId != null)
				{
					string alias;
					if (!frameAliases.TryGetValue(frameId, out alias))
						frameAliases.Add(frameId, alias = string.Format(string.Format("frame-{0}", frameAliases.Count + 1)));
					ret.Add(alias);
				}
				if (!string.IsNullOrEmpty(host))
					ret.Add(host);
				if (!string.IsNullOrEmpty(pid))
					ret.Add(string.Format("process-{0}", pid));
				tagsCache.Add(key, ret);
			}
			return ret;
		}

		readonly int devToolsNetworkEventPrefix;
		static readonly Dictionary<string, HashSet<string>> tagsCache = new Dictionary<string, HashSet<string>>();
		static readonly Dictionary<string, string> frameAliases = new Dictionary<string, string>();
	}
}

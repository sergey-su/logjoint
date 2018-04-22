using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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
			devToolsNetworkEventPrefix = matcher.RegisterPrefix("DEVTOOLS EVENT Network.");
		}


		IEnumerableAsync<Event[]> ITimelineEvents.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags));
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			var msg = msgPfx.Message;
			Match m;
			if (msgPfx.Prefixes.Contains(devToolsNetworkEventPrefix))
			{
				if ((m = devToolsNetworkEventRegex.Match(msg.Text)).Success)
				{
					var evt = m.Groups["evt"].Value;
					DevToolsNetworkEvent payload;
					try
					{
						payload = JsonConvert.DeserializeObject<DevToolsNetworkEvent>(m.Groups["payload"].Value);
					}
					catch (Exception)
					{
						return;
					}
					if (payload.requestId != null)
					{
						string displayName = payload.requestId;
						var type = 
							evt == "requestWillBeSent" ? ActivityEventType.Begin :
                          	evt == "loadingFinished" ? ActivityEventType.End :
							ActivityEventType.Milestone;
						if (type == ActivityEventType.Milestone)
						{
							displayName = evt;
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
		readonly Regex devToolsNetworkEventRegex = new Regex(@"^DEVTOOLS EVENT Network\.(?<evt>\w+) (?<payload>.*)$",
			RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static readonly HashSet<string> tags = new HashSet<string>() { "devtools" };
	}

	class DevToolsNetworkEvent
	{
		public string requestId;
		public Request request;

		public class Request
		{
			public string method;
			public string url;
		};
	};
}

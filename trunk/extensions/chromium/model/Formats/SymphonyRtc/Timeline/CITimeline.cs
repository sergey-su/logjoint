using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using CD = LogJoint.Chromium.ChromeDriver;

namespace LogJoint.Symphony.Rtc
{
	public interface ICITimelineEvents
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<CDL.Message[]> input);
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<CD.MessagePrefixesPair[]> input);
	};

	public class CITimelineEvents : ICITimelineEvents
	{
		public CITimelineEvents(IPrefixMatcher matcher)
		{
			devToolsConsoleEventPrefix = matcher.RegisterPrefix(CD.DevTools.Events.Runtime.LogAPICalled.Prefix);
		}

		IEnumerableAsync<Event[]> ICITimelineEvents.GetEvents(IEnumerableAsync<CDL.Message[]> input)
		{
			return input.Select<CDL.Message, Event>(GetEvents, GetFinalEvents);
		}

		IEnumerableAsync<Event[]> ICITimelineEvents.GetEvents(IEnumerableAsync<CD.MessagePrefixesPair[]> input)
		{
			return input.Select<CD.MessagePrefixesPair, Event>(GetEvents, GetFinalEvents);
		}

		void GetEvents(CDL.Message msg, Queue<Event> buffer)
		{
			if (string.Compare(msg.File, "CONSOLE", StringComparison.OrdinalIgnoreCase) == 0)
			{
				var m = chromeDebugRe.Match(msg.Text);
				if (m.Success)
					GetCIEvents(m.Groups[1].Value, msg, buffer);
			}
		}

		void GetEvents(CD.MessagePrefixesPair msg, Queue<Event> buffer)
		{
			if (msg.Prefixes.Contains(devToolsConsoleEventPrefix))
			{
				var parsed = CD.DevTools.Events.LogMessage.Parse(msg.Message.Text);
				if (parsed != null)
				{
					var payload = parsed.ParsePayload<CD.DevTools.Events.Runtime.LogAPICalled>();
					if (payload != null && payload.args.Length == 1 && payload.args[0].type == "string")
					{
						GetCIEvents((string)payload.args[0].value, msg.Message, buffer);
					}
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		void GetCIEvents(string consoleMessage, object trigger, Queue<Event> buffer)
		{
			Match m = ciTestRegex.Match(consoleMessage);
			if (m.Success)
			{
				buffer.Enqueue(new ProcedureEvent(
					trigger, m.Groups["test"].Value, m.Groups["test"].Value,
					m.Groups["op"].Value == "start" ? ActivityEventType.Begin : ActivityEventType.End).SetTags(tags));
			}
		}

		readonly int devToolsConsoleEventPrefix;
		readonly Regex chromeDebugRe = new Regex(@"^\""(?<text>.+)\""[^\""]*$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		readonly Regex ciTestRegex = new Regex(@"^TEST (?<op>start|done): (?<test>.+)$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		static readonly HashSet<string> tags = new HashSet<string>(new[] { "CI" });
	}
}

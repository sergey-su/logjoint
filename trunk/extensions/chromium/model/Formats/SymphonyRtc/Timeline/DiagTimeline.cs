using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.Rtc.Diag
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
		}


		IEnumerableAsync<Event[]> ITimelineEvents.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags));
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			if (logableIdUtils.TryParseLogableId(msgPfx.Message.Logger.Value, out string type, out string id))
			{
				switch (type)
				{
					case "diag":
						GetDiagEvents(msgPfx, buffer, id);
						break;
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		void GetDiagEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			var msg = msgPfx.Message;
			if (msg.Text == "started")
			{
				buffer.Enqueue(new ProcedureEvent(msg, diagProcedureId, diagProcedureId, ActivityEventType.Begin));
			}
			else if (msg.Text == "finished")
			{
				buffer.Enqueue(new ProcedureEvent(msg, diagProcedureId, diagProcedureId, ActivityEventType.End));
			}
			else if ((m = stepRegex.Match(msg.Text)).Success)
			{
				var step = m.Groups["step"].Value;
				var status = m.Groups["status"].Value;
				buffer.Enqueue(new ProcedureEvent(msg, $"diag.{step}", step,
					status == "Pending" ? ActivityEventType.Begin : ActivityEventType.End,
					status: status == "Failed" ? ActivityStatus.Error : ActivityStatus.Unspecified));
			}
		}

		readonly LogableIdUtils logableIdUtils = new LogableIdUtils();
		readonly static string diagProcedureId = "RTC diagnostics";
		static readonly RegexOptions reopts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
		readonly Regex stepRegex = new Regex(@"^step \""(?<step>[^\""]+)\"" -> (?<status>.+)$", reopts);
		static readonly HashSet<string> tags = new HashSet<string>(new[] { "diagnostics" });
	}
}

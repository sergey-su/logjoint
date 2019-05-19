using LogJoint.Postprocessing.Timeline;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;

namespace LogJoint.Postprocessing.InternalTrace
{
	public class ProfilingTimelineEventsSource
	{
		public ProfilingTimelineEventsSource()
		{
		}

		public IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<Message[]> input)
		{
			return input.Select<Message, Event>(GetEvents);
		}

		void GetEvents(Message msg, Queue<Event> buffer)
		{
			Match m;
			if (msg.Text.StartsWith("perfop"))
			{
				if ((m = perfopRegex.Match(msg.Text)).Success)
				{
					string displayName = string.Format("{0} {1}", msg.Source, m.Groups[2].Value);
					ActivityEventType t;
					switch (m.Groups[3].Value)
					{
					case "started": t = ActivityEventType.Begin; break;
					case "stopped": t = ActivityEventType.End; break;
					case "milestone": t = ActivityEventType.Milestone; displayName = m.Groups[4].Value; break;
					default: return;
					}
					buffer.Enqueue(new ProcedureEvent(msg, displayName, m.Groups[1].Value, t));
				}
			}
			else if (msg.Text.StartsWith("user action: "))
			{
				if ((m = userActionRegex.Match(msg.Text)).Success)
				{
					var action = new StringBuilder();
					if (msg.Source.Length > 0)
						action.AppendFormat("{0}: ", msg.Source);
					action.Append(m.Groups[1].Value);
					buffer.Enqueue(new UserActionEvent(msg, action.ToString()));
				}
			}
		}

		readonly Regex perfopRegex = new Regex(@"^perfop \#(?<id>\w+) \'(?<op>[^\']+)\' (?<type>started|stopped|milestone)( \'(?<data>[^\']*)\')?$", 
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex userActionRegex = new Regex(@"^user action\: (?<action>.+)$", 
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

	}
}

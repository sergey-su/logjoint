using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Analytics.Timeline
{
	public class EventsStringifier: IEventsVisitor
	{
		public readonly StringBuilder Output = new StringBuilder();

		void IEventsVisitor.Visit(ProcedureEvent evt)
		{
			Output.AppendFormat("Procedure.{0}: activity={1}. comment={2}", evt.Type, evt.ActivityId, evt.DisplayName);
		}

		void IEventsVisitor.Visit(ObjectLifetimeEvent evt)
		{
			Output.AppendFormat("Lifetime.{0}: object={1}. comment={2}. ", evt.Type, evt.ActivityId, evt.DisplayName);
		}

		void IEventsVisitor.Visit(UserActionEvent evt)
		{
			Output.AppendFormat("UserAction: {0}", evt.DisplayName);
		}

		void IEventsVisitor.Visit(NetworkMessageEvent evt)
		{
			Output.AppendFormat("NetworkRequest: {0}", evt.DisplayName);
		}

		void IEventsVisitor.Visit(APICallEvent evt)
		{
			Output.AppendFormat("APICall: {0}", evt.DisplayName);
		}

		void IEventsVisitor.Visit(EndOfTimelineEvent evt)
		{
			Output.AppendFormat("EndOfTimeline");
		}
	}
}

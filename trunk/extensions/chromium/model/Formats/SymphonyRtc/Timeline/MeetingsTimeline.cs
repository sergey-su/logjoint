using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LogJoint.Symphony.Rtc
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
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents);
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			string id, type;
			if (logableIdUtils.TryParseLogableId(msgPfx.Message.Logger.Value, out type, out id))
			{
				if (type == "meeting")
				{
					
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		readonly LogableIdUtils logableIdUtils = new LogableIdUtils();
	}
}

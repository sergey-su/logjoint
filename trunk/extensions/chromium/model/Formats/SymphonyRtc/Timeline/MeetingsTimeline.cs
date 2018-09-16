﻿using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

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
				switch (type)
				{
					case "ui.overlay":
						GetFlowInitiatorEvents(msgPfx, buffer, id);
						break;
					case "ui.localMedia":
						GetLocalMediaEvents(msgPfx, buffer, id);
						break;
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		void GetLocalMediaEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			var msg = msgPfx.Message;
			if ((m = localMediaButtonRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new UserActionEvent(msg, m.Groups["btn"].Value));
			}
		}

		void GetFlowInitiatorEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			var msg = msgPfx.Message;
			if (msg.Text == "leave flow")
			{
				buffer.Enqueue(new UserActionEvent(msg, "leave"));
			}
		}

		readonly LogableIdUtils logableIdUtils = new LogableIdUtils();
		static readonly RegexOptions reopts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
		readonly Regex localMediaButtonRegex = new Regex(@"^(?<btn>audio|video|screen) button pressed$", reopts);
	}
}

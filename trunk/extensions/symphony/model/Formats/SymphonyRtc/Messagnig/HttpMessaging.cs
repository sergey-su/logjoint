using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Messaging;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.Rtc
{
	public interface IMessagingEvents
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<Message[]> input);
	};

	public class MessagingEvents : IMessagingEvents
	{
		IEnumerableAsync<Event[]> IMessagingEvents.GetEvents(IEnumerableAsync<Message[]> input)
		{
			return input.Select<Message, Event>(GetEvents);
		}

		void GetEvents(Message msg, Queue<Event> buffer)
		{
			impl.GetEvents(msg.Text, msg, buffer);

			Match m;
			if ((m = abortRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new NetworkMessageEvent(
					msg, "aborted", MessageDirection.Incoming, MessageType.Response, "", m.Groups["id"].Value, null, null));
			}
			if ((m = failRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new NetworkMessageEvent(
					msg, "failed", MessageDirection.Incoming, MessageType.Response, "", m.Groups["id"].Value, null, null, EventStatus.Error));
			}
		}

		private readonly Messaging.IMessagingEvents impl = new Messaging.MessagingEvents();

		private readonly Regex abortRegex = new Regex(@"Request \[(?<id>\w+)\] aborted", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		private readonly Regex failRegex = new Regex(@"Request \[(?<id>\w+)\] failed", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
	}
}

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
			if ((m = abortOrFailRegex.Match(msg.Text)).Success)
			{
				// todo: Request cancellation event for cancelled
				buffer.Enqueue(new NetworkMessageEvent( // todo: http with code 0
					msg, "aborted", MessageDirection.Incoming, MessageType.Response, "", m.Groups["id"].Value, null, null));
			}
		}

		private readonly Messaging.IMessagingEvents impl = new Messaging.MessagingEvents();

		private readonly Regex abortOrFailRegex = new Regex(@"Request \[(?<id>\w+)\] (aborted|failed)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
	}
}

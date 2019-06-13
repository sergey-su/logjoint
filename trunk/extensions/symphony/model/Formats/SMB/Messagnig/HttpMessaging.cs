using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Messaging;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.SMB
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
		}

		private readonly Messaging.IMessagingEvents impl = new Messaging.MessagingEvents();
	}
}

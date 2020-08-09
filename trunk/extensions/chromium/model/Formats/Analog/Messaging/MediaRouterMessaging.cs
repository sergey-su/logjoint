using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Messaging;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Google.Analog.MediaRouter
{
	public interface IMessagingEvents
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<Message[]> input);
	};

	public class MessagingEvents : IMessagingEvents
	{
		IEnumerableAsync<Event[]> IMessagingEvents.GetEvents(IEnumerableAsync<Message[]> input)
		{
			return input.Select<Message, Event>(GetEvents, GetFinalEvents);
		}

		void GetEvents(Message msg, Queue<Event> buffer)
		{
			Match match;
			if ((match = requestRegex.Match(msg.Text)).Success)
			{
				if (!requests.ContainsKey(msg.ThreadId.Value))
				{
					var start = new NetworkMessageEvent(msg, msg.File, MessageDirection.Incoming, MessageType.Request, "rpc", Guid.NewGuid().ToString("N"), null, null);
					buffer.Enqueue(start);
					requests.Add(msg.ThreadId.Value, new PendingRequest
					{
						start = start
					});
				}
			}
			else if ((match = responseRegex.Match(msg.Text)).Success)
			{
				if (requests.TryGetValue(msg.ThreadId.Value, out var pendingRequest))
				{
					var end = new NetworkMessageEvent(msg, pendingRequest.start.DisplayName, MessageDirection.Incoming, MessageType.Response,
						pendingRequest.start.EventType, pendingRequest.start.MessageId, null, null);
					buffer.Enqueue(end);
					requests.Remove(msg.ThreadId.Value);
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		class PendingRequest
		{
			public NetworkMessageEvent start;
		};

		readonly Dictionary</*thread id*/string, PendingRequest> requests = new Dictionary<string, PendingRequest>();
		readonly Regex requestRegex = new Regex(Helpers.IdsPrefixRegex + @" REQ\: (?<body>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex responseRegex= new Regex(Helpers.IdsPrefixRegex + @" Response\: (?<body>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
	}
}

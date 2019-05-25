using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Messaging;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.SpringServiceLog
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
			var match = regex.Match(msg.Text);
			if (match.Success)
			{
				MessageDirection dir = match.Groups["dir"].Value == "Incoming" ? MessageDirection.Incoming : MessageDirection.Outgoing;
				MessageType type = match.Groups["type"].Value == "request" ? MessageType.Request : MessageType.Response;
				string requestId = match.Groups["id"].Value;
				string requestName = match.Groups["name"].Value;
				buffer.Enqueue(new NetworkMessageEvent(
					msg, requestName, dir, type, "", requestId, null, DetectTargetIdHint(requestName, requestId)));
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		string DetectTargetIdHint(string name, string id)
		{
			if (name.StartsWith("client/", StringComparison.OrdinalIgnoreCase)
			 || name.StartsWith("mbr/", StringComparison.OrdinalIgnoreCase)
			 || name.StartsWith("sip/", StringComparison.OrdinalIgnoreCase))
			{
				var split = id.Split('/');
				if (split.Length > 1)
					return split[0]; // session id
			}
			return null;
		}

		readonly Regex regex = new Regex(@"^(?<dir>Incoming|Outgoing) (?<type>request|response) \[(?<id>[^\]]+)\] (?<name>\S+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
	}
}

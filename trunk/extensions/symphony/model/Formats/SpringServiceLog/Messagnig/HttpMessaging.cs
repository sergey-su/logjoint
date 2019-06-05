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
				string rest = match.Groups["rest"].Value;
				string remoteSideId = DetectRemoteId(requestName, rest);
				if (type == MessageType.Request)
				{
					requests[requestId] = new PendingRequest()
					{
						RemoteSideId = remoteSideId
					};
				}
				else if (requests.TryGetValue(requestId, out var pendingRequest))
				{
					if (remoteSideId == null) 
						remoteSideId = pendingRequest.RemoteSideId;
					requests.Remove(requestId);
				}
				string displayName = MakeDisplayName(requestName, rest);
				buffer.Enqueue(new NetworkMessageEvent(
					msg, displayName, dir, type, "", requestId, null, remoteSideId)
						.SetTags(GetTags(requestName)));
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		string DetectRemoteId(string requestName, string rest)
		{
			if (requestName.StartsWith("mixer"))
				return "mixer";
			var m = restMatch.Match(rest);
			if (m.Success)
				return m.Groups["sessionid"].Value;
			return null;
		}

		string MakeDisplayName(string requestName, string rest)
		{
			if (IsPoll(requestName))
			{
				var m = pollRestMatch.Match(rest);
				if (m.Success)
				{
					return requestName + "->" + string.Join(",", m.Groups[1].Value.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
				}
				else if (IsPollTimeout(rest))
				{
					return requestName + "->timeout";
				}
			}
			return requestName;
		}

		static bool IsPoll(string requestName)
		{
			return requestName.Contains("poll");
		}

		static bool IsPollTimeout(string rest)
		{
			return rest.Contains("timeout");
		}

		HashSet<string> GetTags(string requestName)
		{
			var hashSet = new HashSet<string> ();
			var m = requestNameRegex.Match(requestName);
			if (m.Success)
			{
				hashSet.Add(m.Groups["ns"].Value);
			}
			return tagsPool.Intern(hashSet);
		}

		readonly Regex regex = new Regex(@"^(?<dir>Incoming|Outgoing) (?<type>request|response) \[(?<id>[^\]]+)\] (?<name>\S+)(?<rest>.*)$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex restMatch = new Regex(@"session id (?<sessionid>[\w\-_]+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		readonly Regex pollRestMatch = new Regex(@"polled\: (?<value>[\w\-_\,]+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		readonly Regex requestNameRegex = new Regex(@"^(?<ns>\w+)\/.+", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		class PendingRequest
		{
			public string RemoteSideId;
		};

		readonly Dictionary<string, PendingRequest> requests = new Dictionary<string, PendingRequest>();
		readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
	}
}

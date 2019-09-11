using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Messaging;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.Messaging
{
	public interface IMessagingEvents
	{
		void GetEvents(string message, object trigger, Queue<Event> buffer);
	};

	public class MessagingEvents : IMessagingEvents
	{
		void IMessagingEvents.GetEvents(string msg, object trigger, Queue<Event> buffer)
		{
			var match = regex.Match(msg);
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
				int? status = GetStatus(rest);
				buffer.Enqueue(new HttpMessage(
					trigger, displayName, dir, type, requestId, remoteSideId, null, null, null, status)
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

		int? GetStatus(string rest)
		{
			var m = statusRestMatch.Match(rest);
			if (m.Success)
				return int.Parse(m.Groups[1].Value);
			return null;
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
		readonly Regex statusRestMatch = new Regex(@"status (?<value>\d+)$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		class PendingRequest
		{
			public string RemoteSideId;
		};

		readonly Dictionary<string, PendingRequest> requests = new Dictionary<string, PendingRequest>();
		readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
	}
}

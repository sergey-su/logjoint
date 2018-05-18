using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.Rtc
{
	public interface IMeetingsStateInspector
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input);
	};

	public class MeetingsStateInspector : IMeetingsStateInspector
	{
		public MeetingsStateInspector(
			IPrefixMatcher matcher
		)
		{
		}


		IEnumerableAsync<Event[]> IMeetingsStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags));
		}

		public static ObjectTypeInfo MeetingTypeInfo { get { return meetingTypeInfo; } }
		public static ObjectTypeInfo MeetingSessionTypeInfo { get { return meetingSessionTypeInfo; } }

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			string id, type;
			if (logableIdUtils.TryParseLogableId(msgPfx.Message.Logger.Value, out type, out id))
			{
				switch (type)
				{
					case "meeting":
						GetMeetingEvents(msgPfx, buffer, id);
						break;
					case "session":
						GetSessionEvents(msgPfx, buffer, id);
						break;
					case "protocol":
						GetProtocolEvents(msgPfx, buffer, id);
						break;
				}
			}
		}

		void GetMeetingEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			var msg = msgPfx.Message;
			Match m;
			if ((m = meetingCtrRegex.Match(msg.Text)).Success)
			{
				EnsureRootReported(msg, buffer);
				buffer.Enqueue(new ObjectCreation(msg, loggableId, meetingTypeInfo));
				buffer.Enqueue(new ParentChildRelationChange(msg, loggableId, meetingTypeInfo, rootObjectId));
				buffer.Enqueue(new PropertyChange(msg, loggableId, meetingTypeInfo, "stream", m.Groups["stmId"].Value));
			}
			else if (msg.Text == "disposed")
			{
				buffer.Enqueue(new ObjectDeletion(msg, loggableId, meetingTypeInfo));
			}
			else if ((m = meetingStateRegex.Match(msg.Text)).Success)
			{
				var state = m.Groups["value"].Value;
				buffer.Enqueue(new PropertyChange(msg, loggableId, meetingTypeInfo, "state", state));
			}
			else if ((m = meetingRecoveryStartRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msg, loggableId, meetingTypeInfo, "recovery", "started"));
			}
			else if ((m = meetingRecoveryAttemptRegex.Match(msg.Text)).Success 
			      || (m = meetingRecoveryAttemptEndRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msg, loggableId, meetingTypeInfo, "recovery", m.Groups["value"].Value));
			}
			else if ((m = meetingSessionRegex.Match(msg.Text)).Success)
			{
				var sid = m.Groups["value"].Value;
				sessionToMeeting[sid] = loggableId;
				buffer.Enqueue(new ParentChildRelationChange(msg, sid, meetingSessionTypeInfo, loggableId));
			}
		}

		void GetSessionEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			var msg = msgPfx.Message;
			Match m;
			if ((m = sessionCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, loggableId, meetingSessionTypeInfo));
			}
			else if ((m = sessionJoinedRegex.Match(msg.Text)).Success)
			{
				string meetingLoggableId;
				if (sessionToMeeting.TryGetValue(loggableId, out meetingLoggableId))
				{
					buffer.Enqueue(new PropertyChange(msg, meetingLoggableId, 
						meetingTypeInfo, "meeting ID", m.Groups["meetingId"].Value));
					buffer.Enqueue(new PropertyChange(msg, meetingLoggableId, 
						meetingTypeInfo, "meeting initiator",
						string.Format("{0}{1}", m.Groups["initiator"], m.Groups["isInitator"].Value == "true" ? " (local user)" : "")));
				}
				buffer.Enqueue(new PropertyChange(msg, loggableId, 
					meetingSessionTypeInfo, "status", "joined"));
			}
			else if ((m = sessionIceStatusRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msg, loggableId, meetingSessionTypeInfo, "ice status", m.Groups["value"].Value));
			}
			else if (msg.Text == "disposed")
			{
				buffer.Enqueue(new ObjectDeletion(msg, loggableId, meetingSessionTypeInfo));
			}
			else if ((m = sessionStartedProtocolSession.Match(msg.Text)).Success)
			{
				var protocolSessionId = m.Groups["value"].Value;
				ProtocolSessionData sessionData;
				if (protocolSessionData.TryGetValue(protocolSessionId, out sessionData))
				{
					sessionData.meetingSessionId = loggableId;
					sessionData.pendingMessages.ForEach(pmsg => GetProtocolEvents(pmsg, buffer, protocolSessionId));
				}
			}
		}

		void GetProtocolEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			ProtocolSessionData sessionData;
			if (!protocolSessionData.TryGetValue(loggableId, out sessionData))
				protocolSessionData[loggableId] = sessionData = new ProtocolSessionData();
			if (sessionData.meetingSessionId == null)
			{
				sessionData.pendingMessages.Add(msgPfx);
				return;
			}

			var msg = msgPfx.Message;
			Match m;
			if ((m = protocolSessionIdAllocated.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msg, sessionData.meetingSessionId, meetingSessionTypeInfo, "session id", m.Groups["value"].Value));
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		void EnsureRootReported(Message trigger, Queue<Event> buffer)
		{
			if (rootReported)
				return;
			rootReported = true;

			buffer.Enqueue(new ObjectCreation(trigger, rootObjectId, rootTypeInfo));
		}

		class ProtocolSessionData
		{
			public string meetingSessionId;
			public List<MessagePrefixesPair> pendingMessages = new List<MessagePrefixesPair>();
		};

		bool rootReported;
		Dictionary<string, string> sessionToMeeting = new Dictionary<string, string>();
		Dictionary<string, ProtocolSessionData> protocolSessionData = new Dictionary<string, ProtocolSessionData>();

		readonly static ObjectTypeInfo rootTypeInfo = new ObjectTypeInfo("sym.rtc", isTimeless: true);
		readonly string rootObjectId = "Symphony RTC";
		readonly static ObjectTypeInfo meetingTypeInfo = new ObjectTypeInfo("sym.meeting", primaryPropertyName: "state");
		readonly static ObjectTypeInfo meetingSessionTypeInfo = new ObjectTypeInfo("sym.meeting.session");

		static readonly RegexOptions reopts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
		readonly LogableIdUtils logableIdUtils = new LogableIdUtils();

		readonly Regex meetingCtrRegex = new Regex(@"^created for stream (?<stmId>[^\,]+)", reopts);
		readonly Regex meetingStateRegex = new Regex(@"^state changed to (?<value>\w+)", reopts);
		readonly Regex meetingRecoveryStartRegex = new Regex(@"^starting recovery", reopts);
		readonly Regex meetingRecoveryAttemptRegex = new Regex(@"^recovery (?<value>attempt #\d+): attempting to start new session", reopts);
		readonly Regex meetingRecoveryAttemptEndRegex = new Regex(@"^recovery (?<value>attempt #\d+ (succeeded|failed))", reopts);
		readonly Regex meetingSessionRegex = new Regex(@"^created session (?<value>[\w\-]+)", reopts);

		readonly Regex sessionCtrRegex = new Regex(@"^created$", reopts);
		readonly Regex sessionJoinedRegex = new Regex(@"^""Joined"" message received. Meeting id: (?<meetingId>[^\.]+). Initiator id: (?<initiator>\w+). Local is initiator: (?<isInitator>true|false)", reopts);
		readonly Regex sessionIceStatusRegex = new Regex(@"^ICE connection status -> (?<value>\w+)", reopts);
		readonly Regex sessionStartedProtocolSession = new Regex(@"^started protocol session (?<value>\S+)", reopts);

		readonly Regex protocolSessionIdAllocated = new Regex(@"^session id allocated: (?<value>\S+)", reopts);

		readonly HashSet<string> tags = new HashSet<string>() { "meetings" };
	}
}

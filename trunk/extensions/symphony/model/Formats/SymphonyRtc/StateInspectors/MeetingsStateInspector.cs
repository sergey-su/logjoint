using LogJoint.Postprocessing;
using LogJoint.Postprocessing.StateInspector;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.Rtc
{
	public interface IMeetingsStateInspector
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input);
		string EnsureRootObjectCreated(Message trigger, Queue<Event> buffer);
	};

	public class MeetingsStateInspector : IMeetingsStateInspector
	{
		public MeetingsStateInspector(
			IPrefixMatcher matcher
		)
		{
		}

		IEnumerableAsync<Event[]> IMeetingsStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input)
		{
			return input
				.Select<MessagePrefixesPair<Message>, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags))
				.EnsureParented((creationEvt, buffer) => 
					creationEvt.ObjectType == rootTypeInfo ? null :
						EnsureRootReported((Message)creationEvt.Trigger, buffer));
		}

		public static ObjectTypeInfo MeetingTypeInfo { get { return meetingTypeInfo; } }
		public static ObjectTypeInfo MeetingSessionTypeInfo { get { return meetingSessionTypeInfo; } }
		public static ObjectTypeInfo MeetingRemoteParticipantTypeInfo { get { return meetingRemotePartTypeInfo; } }
		public static ObjectTypeInfo ProbeSessionTypeInfo { get { return psessionTypeInfo; } }
		public static ObjectTypeInfo InvitationTypeInfo { get { return invitationTypeInfo; } }

		public static bool ShouldBePresentedCollapsed(Event stateInspectorSelectedNode)
		{
			var objectType = stateInspectorSelectedNode?.ObjectType?.TypeName;
			return defaultCollapsedNodesTypes.Contains(objectType);
		}

		public static (string id, DateTime referenceTime, string env) GetMeetingRelatedId(
			Event stateInspectorObjectCreationEvent, IEnumerable<PropertyChange> stateInspectorObjectChanges,
			Event rootStateInspectorCreationEvent, IEnumerable<PropertyChange> rootStateInspectorObjectChanges
		)
		{
			var objectType = stateInspectorObjectCreationEvent?.ObjectType?.TypeName;
			string FindProp(string name, IEnumerable<PropertyChange> changes) => changes.Where(c => c.PropertyName == name).Select(c => c.Value).FirstOrDefault();
			var id =
				objectType == MeetingTypeInfo.TypeName ? FindProp("meeting ID", stateInspectorObjectChanges) :
				objectType == InvitationTypeInfo.TypeName ? FindProp("meeting id", stateInspectorObjectChanges) :
				objectType == MeetingSessionTypeInfo.TypeName ? FindProp("session id", stateInspectorObjectChanges) :
				null;
			var env = FindProp("environment", rootStateInspectorObjectChanges);
			var referenceTime = stateInspectorObjectChanges.Select(c => c.Trigger).OfType<ITriggerTime>().Select(t => t.Timestamp).FirstOrDefault();
			return (id, referenceTime, env);
		}

		void GetEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer)
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
					case "remotePart":
						GetRemotePartEvents(msgPfx, buffer, id);
						break;
					case "participants":
						GetParticipantsEvents(msgPfx, buffer, id);
						break;
					case "invitation":
						GetInvitationsEvents(msgPfx, buffer, id);
						break;
					case "psession":
						GetProbeSessionEvents(msgPfx, buffer, id);
						break;
					case "users":
						GetUsersEvents(msgPfx, buffer, id);
						break;
				}
			}
		}

		void GetMeetingEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
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

		void GetSessionEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
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
			else if ((m = sessionPropChangeRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(
					msg, loggableId, meetingSessionTypeInfo,
					m.Groups["prop"].Value, m.Groups["value"].Value));
			}
			else if (msg.Text == "disposed")
			{
				buffer.Enqueue(new ObjectDeletion(msg, loggableId, meetingSessionTypeInfo));
				foreach (var i in sessionUserIdToRemotePartId.Where(i => i.Key.StartsWith(loggableId, StringComparison.InvariantCulture)).ToArray())
				{
					buffer.Enqueue(new ObjectDeletion(msgPfx.Message, i.Value, meetingRemotePartTypeInfo));
					sessionUserIdToRemotePartId.Remove(i.Key);
				}
			}
			else if ((m = sessionStartedProtocolSession.Match(msg.Text)).Success)
			{
				var protocolSessionId = m.Groups["value"].Value;
				ProtocolSessionData sessionData;
				if (protocolSessionData.TryGetValue(protocolSessionId, out sessionData))
				{
					sessionData.meetingSessionId = loggableId;
					sessionData.pendingMessages.ForEach(pmsg => GetProtocolEvents(pmsg, buffer, protocolSessionId));
					sessionData.pendingMessages.Clear();
				}
			}
			else if ((m = sessionLocalMediaRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ParentChildRelationChange(
					msg, m.Groups["value"].Value, MediaStateInspector.LocalMediaTypeInfo, loggableId));
			}
			else if ((m = sessionParticipantsRegex.Match(msg.Text)).Success)
			{
				participantsLoggableIdToSessionLoggableId[m.Groups["value"].Value] = loggableId;
			}
		}

		void GetProtocolEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
		{
			var msg = msgPfx.Message;
			Match m;
			if ((m = protocolCSUrlRegex.Match(msg.Text)).Success)
			{
				var env = m.Groups["value"].Value;
				if (env != reportedEnv)
				{
					EnsureRootReported(msg, buffer);
					reportedEnv = env;
					buffer.Enqueue(new PropertyChange(msg, rootObjectId, rootTypeInfo, "environment", env));
				}
			}

			if (!protocolSessionData.TryGetValue(loggableId, out var sessionData))
			{
				protocolSessionData[loggableId] = sessionData = new ProtocolSessionData();
			}
			if (sessionData.meetingSessionId == null)
			{
				sessionData.pendingMessages.Add(msgPfx);
				return;
			}

			if ((m = protocolSessionIdAllocated.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msg, sessionData.meetingSessionId, meetingSessionTypeInfo, "session id", m.Groups["value"].Value));
			}
		}

		void GetRemotePartEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
		{
			var msg = msgPfx.Message;
			Match m;
			if ((m = remotePartCreationRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, loggableId, meetingRemotePartTypeInfo));
				buffer.Enqueue(new PropertyChange(msg, loggableId, meetingRemotePartTypeInfo, "user name", m.Groups["userName"].Value));
				buffer.Enqueue(new PropertyChange(msg, loggableId, meetingRemotePartTypeInfo, "user id", m.Groups["userId"].Value));
				string sessionLoggableId;
				if (participantsLoggableIdToSessionLoggableId.TryGetValue(m.Groups["partsId"].Value, out sessionLoggableId))
				{
					sessionUserIdToRemotePartId[MakeSessionUserId(sessionLoggableId, m.Groups["userId"].Value)] = loggableId;
					buffer.Enqueue(new ParentChildRelationChange(msg, loggableId, meetingRemotePartTypeInfo, sessionLoggableId));
				}
			}
		}

		void GetParticipantsEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			if ((m = participantsUserLeftRegex.Match(msgPfx.Message.Text)).Success)
			{
				string sessionLoggableId;
				string remotePartId;
				if (participantsLoggableIdToSessionLoggableId.TryGetValue(loggableId, out sessionLoggableId)
				 && sessionUserIdToRemotePartId.TryGetValue(MakeSessionUserId(sessionLoggableId, m.Groups["userId"].Value), out remotePartId))
				{
					buffer.Enqueue(new ObjectDeletion(msgPfx.Message, remotePartId, meetingRemotePartTypeInfo));
					sessionUserIdToRemotePartId.Remove(MakeSessionUserId(sessionLoggableId, m.Groups["userId"].Value));
				}
			}
			else if ((m = localParticipantRegex.Match(msgPfx.Message.Text)).Success)
			{
				ReportLocalUserProps(m.Groups["id"].Value, m.Groups["name"].Value, msgPfx.Message, buffer);
			}
		}

		void ReportLocalUserProps(string id, string name, Message trigger, Queue<Event> buffer)
		{
			if (id != null && id != reportedLocalUserId)
			{
				EnsureRootReported(trigger, buffer);
				buffer.Enqueue(new PropertyChange(trigger, rootObjectId, rootTypeInfo, "local user id", id));
				reportedLocalUserId = id;
			}
			if (name != null && name != reportedLocalUserName)
			{
				EnsureRootReported(trigger, buffer);
				buffer.Enqueue(new PropertyChange(trigger, rootObjectId, rootTypeInfo, "local user name", name));
				reportedLocalUserName = name;
			}
		}

		void GetInvitationsEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			if ((m = invitationCtrRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msgPfx.Message, loggableId, invitationTypeInfo));
				buffer.Enqueue(new ParentChildRelationChange(msgPfx.Message, loggableId, invitationTypeInfo, EnsureInvitationsReport(msgPfx.Message, buffer)));
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, invitationTypeInfo, "meeting id", m.Groups["meetingId"].Value));
			}
			else if ((m = invitationPropsRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, invitationTypeInfo, "state", "aggressive"));
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, invitationTypeInfo, "stream id", m.Groups["streamId"].Value));
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, invitationTypeInfo, "initiator", m.Groups["initiator"].Value));
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, invitationTypeInfo, "SS only", m.Groups["ss"].Value));
			}
			else if ((m = invitationPassiveRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, invitationTypeInfo, "state", "passive"));
			}
			else if ((m = invitationTotalRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, invitationTypeInfo, "participants count", m.Groups["value"].Value));
			}
			else if ((m = invitationDtrRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new ObjectDeletion(msgPfx.Message, loggableId, meetingRemotePartTypeInfo));
			}
		}

		void GetProbeSessionEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			if ((m = psessionCtrRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msgPfx.Message, loggableId, psessionTypeInfo));
				EnsureRootReported(msgPfx.Message, buffer);
				buffer.Enqueue(new ParentChildRelationChange(msgPfx.Message, loggableId, psessionTypeInfo, rootObjectId));
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, psessionTypeInfo, "region", m.Groups["region"].Value));
			}
			else if ((m = psessionDtrRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new ObjectDeletion(msgPfx.Message, loggableId, psessionTypeInfo));
			}
			else if ((m = psessionJoinedRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, psessionTypeInfo, "session id", m.Groups["value"].Value));
			}
			else if ((m = psessionIceRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msgPfx.Message, loggableId, psessionTypeInfo, "ice status", m.Groups["value"].Value));
			}
		}

		void GetUsersEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			if ((m = localUserRegex.Match(msgPfx.Message.Text)).Success)
			{
				ReportLocalUserProps(m.Groups["id"].Value, m.Groups["name"].Value, msgPfx.Message, buffer);
			}
		}

		string EnsureInvitationsReport(Message trigger, Queue<Event> buffer)
		{
			string objectId = "Invitations";
			if (!invitationsReported)
			{
				invitationsReported = true;
				EnsureRootReported(trigger, buffer);
				buffer.Enqueue(new ObjectCreation(trigger, objectId, invitationsTypeInfo));
				buffer.Enqueue(new ParentChildRelationChange(trigger, objectId, invitationsTypeInfo, rootObjectId));
			}
			return objectId;
		}

		static string MakeSessionUserId(string sessionLoggableId, string userId)
		{
			return sessionLoggableId + "|" + userId;
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		string IMeetingsStateInspector.EnsureRootObjectCreated(Message trigger, Queue<Event> buffer)
		{
			return EnsureRootReported(trigger, buffer);
		}

		string EnsureRootReported(Message trigger, Queue<Event> buffer)
		{
			if (rootReported)
				return rootObjectId;
			rootReported = true;

			buffer.Enqueue(new ObjectCreation(trigger, rootObjectId, rootTypeInfo));
			return rootObjectId;
		}

		class ProtocolSessionData
		{
			public string meetingSessionId;
			public List<MessagePrefixesPair<Message>> pendingMessages = new List<MessagePrefixesPair<Message>>();
		};

		bool rootReported;
		Dictionary<string, string> sessionToMeeting = new Dictionary<string, string>();
		Dictionary<string, ProtocolSessionData> protocolSessionData = new Dictionary<string, ProtocolSessionData>();
		Dictionary<string, string> participantsLoggableIdToSessionLoggableId = new Dictionary<string, string>();
		Dictionary<string, string> sessionUserIdToRemotePartId = new Dictionary<string, string>();
		bool invitationsReported;
		string reportedLocalUserId, reportedLocalUserName, reportedEnv;

		readonly static ObjectTypeInfo rootTypeInfo = new ObjectTypeInfo("sym.rtc", isTimeless: false);
		readonly static string rootObjectId = "Symphony RTC";
		readonly static ObjectTypeInfo meetingTypeInfo = new ObjectTypeInfo("sym.meeting", primaryPropertyName: "state");
		readonly static ObjectTypeInfo meetingSessionTypeInfo = new ObjectTypeInfo("sym.meeting.session", primaryPropertyName: "status");
		readonly static ObjectTypeInfo meetingRemotePartTypeInfo = new ObjectTypeInfo("sym.meeting.remotePart", displayIdPropertyName: "user name", primaryPropertyName: "audio state");
		readonly static ObjectTypeInfo invitationTypeInfo = new ObjectTypeInfo("sym.invitation", primaryPropertyName: "state");
		readonly static ObjectTypeInfo invitationsTypeInfo = new ObjectTypeInfo("sym.invitations", isTimeless: true);
		readonly static ObjectTypeInfo psessionTypeInfo = new ObjectTypeInfo("sym.psession", primaryPropertyName: "ice status");

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
		readonly Regex sessionPropChangeRegex = new Regex(@"^(?<prop>ICE connection status|signaling state|ICE gathering state) -> (?<value>\S+)", reopts);
		readonly Regex sessionStartedProtocolSession = new Regex(@"^started protocol session (?<value>\S+)", reopts);
		readonly Regex sessionLocalMediaRegex = new Regex(@"^created local media: (?<value>\S+)$", reopts);
		readonly Regex sessionParticipantsRegex = new Regex(@"^created participants: (?<value>\S+)$", reopts);

		readonly Regex remotePartCreationRegex = new Regex(@"^created in scope of (?<partsId>\S+) for user (?<userId>\S+) \((?<userName>[^\)]*)\)", reopts);

		readonly Regex participantsUserLeftRegex = new Regex(@"^User left: (?<userId>\S+)", reopts);

		readonly Regex protocolSessionIdAllocated = new Regex(@"^session id allocated: (?<value>\S+)", reopts);
		readonly Regex protocolCSUrlRegex = new Regex(@"^conference service url: https://(?<value>[^\.]+)\.", reopts);

		readonly Regex invitationPropsRegex = new Regex(@"^created for stream '(?<streamId>\S+)', initiator (?<initiator>\S+), initiated as SS (?<ss>\S+)", reopts);
		readonly Regex invitationCtrRegex = new Regex(@"^created for meeting (?<meetingId>\S+)", reopts);
		readonly Regex invitationPassiveRegex = new Regex(@"^passive by (?<reason>\S+)", reopts);
		readonly Regex invitationTotalRegex = new Regex(@"total: (?<value>[\-\d]+)$", reopts);
		readonly Regex invitationDtrRegex = new Regex(@"^dispose", reopts);

		readonly Regex psessionCtrRegex = new Regex(@"^created with protocol session (?<session>\S+) (?<region>\S+)", reopts);
		readonly Regex psessionJoinedRegex = new Regex(@"^Joined with CS conference id\: (?<value>\S+)", reopts);
		readonly Regex psessionIceRegex = new Regex(@"^ICE connection status \-\> (?<value>\S+)", reopts);
		readonly Regex psessionDtrRegex = new Regex(@"^disposed", reopts);

		readonly Regex localUserRegex = new Regex(@"^current user:.*""id"":""(?<id>\w+)"".*""prettyName"":""(?<name>[^\"")]+)""", reopts);
		readonly Regex localParticipantRegex = new Regex(@"^local participant created (?<id>\w+) '(?<name>[^\)]+)'$", reopts);

		readonly HashSet<string> tags = new HashSet<string>() { "meetings" };

		static readonly HashSet<string> defaultCollapsedNodesTypes = new HashSet<string>(new [] 
		{
			meetingSessionTypeInfo, psessionTypeInfo, 
		}.Select(i => i.TypeName));
	}
}

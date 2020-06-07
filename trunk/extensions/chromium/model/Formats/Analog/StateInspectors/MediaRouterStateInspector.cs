using LogJoint.Postprocessing;
using LogJoint.Postprocessing.StateInspector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.Google.Analog.MediaRouter
{
	public interface IStateInspector
	{

		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input);
	};

	public class StateInspector : IStateInspector
	{
		public StateInspector(IPrefixMatcher matcher)
		{
		}

		IEnumerableAsync<Event[]> IStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input)
		{
			return input.Select<MessagePrefixesPair<Message>, Event>(GetEvents, GetFinalEvents);
		}


		void GetEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> eventsBuffer)
		{
			var message = msgPfx.Message;
			Match match;
			if (TryMatch(message, "add_media_session_handler.cc", addSessionRequestRe, out match))
			{
				EnsureSessionReported(message, match, eventsBuffer);
			}
			if (TryMatch(message, "session.cc", conferenceIdRe, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var conferenceId = match.Groups["value"].Value;
				eventsBuffer.Enqueue(new PropertyChange(message, sessionState.stateInspectorId, sessionTypeInfo, "conference id", conferenceId));
			}
			if (TryMatch(message, "versioned_session_view.cc", peerAddedRegex, out match)
			 || TryMatch(message, "router_media_path_controller.cc", extensionPeerAddedRegex, out match)
			 || TryMatch(message, "router_media_path_controller.cc", sessionPeerAddedRegex, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var peerId = match.Groups["peerId"].Value;
				if (!sessionState.peers.TryGetValue(peerId, out var peerState))
				{
					sessionState.peers.Add(peerId, peerState = new PeerState(sessionState, peerId));
					eventsBuffer.Enqueue(new ObjectCreation(message, peerState.stateInspectorId, peerTypeInfo, displayName: peerId));
					eventsBuffer.Enqueue(new ParentChildRelationChange(message, peerState.stateInspectorId, peerTypeInfo, sessionState.peersStateInspectorId));
					if (match.Groups["type"].Value == "extension")
					{
						var target = match.Groups["target"].Value;
						var targetMatch = Regex.Match(target, @"^(\w+)\:");
						eventsBuffer.Enqueue(new PropertyChange(message, peerState.stateInspectorId, peerTypeInfo, "type",
							targetMatch.Success ? targetMatch.Groups[1].Value : "extension"));
						eventsBuffer.Enqueue(new PropertyChange(message, peerState.stateInspectorId, peerTypeInfo, "target", target));
					}
				}
			}
			if (TryMatch(message, "router_media_path_controller.cc", peedRemovedRegex1, out match)
			 || TryMatch(message, "router_media_path_controller.cc", peedRemovedRegex2, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var peerId = match.Groups["peerId"].Value;
				if (sessionState.peers.TryGetValue(peerId, out var peerInfo))
				{
					eventsBuffer.Enqueue(new ObjectDeletion(message, peerInfo.stateInspectorId, peerTypeInfo));
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		string EnsureRootReported(Message trigger, Queue<Event> eventsBuffer)
		{
			if (!rootReported)
			{
				eventsBuffer.Enqueue(new ObjectCreation(trigger, rootObjectId, rootTypeInfo));
				rootReported = true;
			}
			return rootObjectId;
		}

		// returns session id
		SessionState EnsureSessionReported(Message trigger, Match loglineMatch, Queue<Event> eventsBuffer)
		{
			string sessionId = loglineMatch.Groups["sessionId"].Value;
			if (!sessions.TryGetValue(sessionId, out var sessionState))
			{
				sessionState = new SessionState(sessionId);
				sessions.Add(sessionId, sessionState);
				string rootId = EnsureRootReported(trigger, eventsBuffer);
				eventsBuffer.Enqueue(new ObjectCreation(trigger, sessionState.stateInspectorId, sessionTypeInfo));
				eventsBuffer.Enqueue(new ParentChildRelationChange(trigger, sessionState.stateInspectorId, sessionTypeInfo, rootId));

				eventsBuffer.Enqueue(new ObjectCreation(trigger, sessionState.peersStateInspectorId, peersTypeInfo, displayName: "Peers"));
				eventsBuffer.Enqueue(new ParentChildRelationChange(trigger, sessionState.peersStateInspectorId, peersTypeInfo, sessionState.stateInspectorId));
			}
			if (string.IsNullOrEmpty(sessionState.participantLogid))
			{
				string participantLogid = loglineMatch.Groups["plid"].Value;
				if (!string.IsNullOrEmpty(participantLogid))
				{
					sessionState.participantLogid = participantLogid;
					eventsBuffer.Enqueue(new PropertyChange(trigger, sessionState.stateInspectorId, sessionTypeInfo, "participant log id", participantLogid));
				}
			}
			return sessionState;
		}

		bool TryMatch(Message message, string file, Regex regex, out Match match)
		{
			match = null;
			if (message.File == file)
			{
				var m = regex.Match(message.Text);
				if (m.Success)
				{
					match = m;
					return true;
				}
			}
			return false;
		}

		class PeerState
		{
			public readonly string id;
			public readonly string stateInspectorId;

			public PeerState(SessionState sessionState, string id)
			{
				this.id = id;
				this.stateInspectorId = $"{sessionState.stateInspectorId}.peer:{id}";
			}
		};

		class SessionState
		{
			public readonly string id;
			public readonly string stateInspectorId;
			public readonly string peersStateInspectorId;
			public string participantLogid;
			public readonly Dictionary<string, PeerState> peers = new Dictionary<string, PeerState>();

			public SessionState(string id)
			{
				this.id = id;
				this.stateInspectorId = id;
				this.peersStateInspectorId = $"{id}.peers";
			}
		};

		#region Mutable state

		bool rootReported = false;
		readonly Dictionary<string, SessionState> sessions = new Dictionary<string, SessionState>();

		#endregion

		#region Constants

		const string rootObjectId = "MediaRouter objects";

		readonly static ObjectTypeInfo rootTypeInfo = new ObjectTypeInfo("media_router.root", isTimeless: true);
		readonly static ObjectTypeInfo sessionTypeInfo = new ObjectTypeInfo("media_router.session");
		readonly static ObjectTypeInfo peersTypeInfo = new ObjectTypeInfo("media_router.peers", isTimeless: true);
		readonly static ObjectTypeInfo peerTypeInfo = new ObjectTypeInfo("media_router.peer", displayIdPropertyName: "type");

		const RegexOptions regexOptions = RegexOptions.ExplicitCapture | RegexOptions.Compiled;

		const string sessionLogidRe = Helpers.SessionLogidPrefixRegex;
		readonly Regex addSessionRequestRe = new Regex(sessionLogidRe + @" \w+ REQ: media_session", regexOptions);
		readonly Regex conferenceIdRe = new Regex(sessionLogidRe + " Setting conference id to \"(?<value>[^\"]*)\"", regexOptions);
		readonly Regex peerAddedRegex = new Regex(sessionLogidRe + @" (?<type>Peer) (?<peerId>\S+) added in version", regexOptions);
		readonly Regex sessionPeerAddedRegex = new Regex(sessionLogidRe + @" CreateSessionPeer to session_id (?<peerId>[^,]+)", regexOptions);
		readonly Regex extensionPeerAddedRegex = new Regex(sessionLogidRe + @" Adding (?<type>extension) peer (?<peerId>\S+) with target (?<target>\S+)", regexOptions);
		readonly Regex peedRemovedRegex1 = new Regex(sessionLogidRe + @" Erasing peer (?<peerId>\S+)", regexOptions);
		readonly Regex peedRemovedRegex2 = new Regex(sessionLogidRe + @" Removing obsolete extension peer (?<peerId>\S+)", regexOptions);


		#endregion
	}
}

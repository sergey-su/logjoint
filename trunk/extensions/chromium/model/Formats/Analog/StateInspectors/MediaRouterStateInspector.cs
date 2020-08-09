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
			if (TryMatch(message, "session.cc", removeSessionRequestRe, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				eventsBuffer.Enqueue(new ObjectDeletion(message, sessionState.stateInspectorId, sessionTypeInfo));
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
					eventsBuffer.Enqueue(new PropertyChange(message, peerState.stateInspectorId, peerTypeInfo,
						"fingerprint", match.Groups["peerIdFingerprint"].Value));
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
			if (TryMatch(message, "router_media_path_controller.cc", peerRemovedRegex1, out match)
			 || TryMatch(message, "router_media_path_controller.cc", peerRemovedRegex2, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var peerId = match.Groups["peerId"].Value;
				if (sessionState.peers.TryGetValue(peerId, out var peerInfo))
				{
					eventsBuffer.Enqueue(new ObjectDeletion(message, peerInfo.stateInspectorId, peerTypeInfo));
				}
			}
			if (TryMatch(message, "router_media_path_controller.cc", peerParticipantIdRegex, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var peerId = match.Groups["peerId"].Value;
				if (sessionState.peers.TryGetValue(peerId, out var peerInfo))
				{
					eventsBuffer.Enqueue(new PropertyChange(message, peerInfo.stateInspectorId, peerTypeInfo,
						"participant id", match.Groups["value"].Value));
				}
			}
			if (TryMatch(message, "router_media_path_controller.cc", firstOfferRegex, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var peerId = match.Groups["peerId"].Value;
				if (sessionState.peers.TryGetValue(peerId, out var peerState))
				{
					ApplyChangesToOfferAndReportChanges(peerState, () =>
					{
						peerState.currentOffer = TextProtoParser.Parse("{" + match.Groups["offer"].Value + "}");
					}, message, eventsBuffer);
				}
				// TODO: else complain somehow
			}
			if (TryMatch(message, "router_media_path_controller.cc", offerChangedRegex, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var peerId = match.Groups["peerId"].Value;
				if (sessionState.peers.TryGetValue(peerId, out var peerState))
				{
					ApplyChangesToOfferAndReportChanges(peerState, () =>
					{
						foreach (Match diff in offerChangeRegex.Matches(match.Groups["differences"].Value))
						{
							string selectNonEmpty(string s1, string s2) => s1.Length > 0 ? s1 : s2;
							ApplyModification(peerState.currentOffer, diff.Groups["action"].Value, diff.Groups["path"].Value,
								selectNonEmpty(diff.Groups["value2"].Value, diff.Groups["value1"].Value));
						}
					}, message, eventsBuffer);
				}
			}
			if (TryMatch(message, "vas_descriptor.cc", virtualSsrcMappedRegex, out match))
			{
				var vssrcState = EnsureVirtualSsrcReported(message, match, eventsBuffer);
				var ssrc = uint.Parse(match.Groups["ssrc"].Value);
				eventsBuffer.Enqueue(new PropertyChange(message, vssrcState.stateInspectorId, virtualSsrcTypeInfo,
					"mapped SSRC", ssrc.ToString(), valueType: Postprocessing.StateInspector.ValueType.Reference));
				if (vssrcState.mappedSsrc != ssrc && ssrcToAudioStreamId.ContainsKey(vssrcState.mappedSsrc))
				{
					eventsBuffer.Enqueue(new PropertyChange(message, vssrcState.mappedSsrc.ToString(), audioStreamReceiverTypeInfo, "Virtual Audio SSRC", ""));
				}
				vssrcState.mappedSsrc = ssrc;
				if (ssrcToAudioStreamId.ContainsKey(ssrc))
				{
					eventsBuffer.Enqueue(new PropertyChange(message, ssrc.ToString(), audioStreamReceiverTypeInfo, "Virtual Audio SSRC", vssrcState.stateInspectorId,
						valueType: Postprocessing.StateInspector.ValueType.Reference));
				}
			}
			if (TryMatch(message, "audio_media_stream_receiver.cc", audioStreamAddedRegex, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var streamId = match.Groups["streamId"].Value;
				var ssrc = uint.Parse(match.Groups["ssrc"].Value);
				eventsBuffer.Enqueue(new ObjectCreation(message, ssrc.ToString(), audioStreamReceiverTypeInfo));
				eventsBuffer.Enqueue(new ParentChildRelationChange(message, ssrc.ToString(), audioStreamReceiverTypeInfo, streamId));
				ssrcToAudioStreamId[ssrc] = streamId;
			}
			if (TryMatch(message, "relay_receive_endpoint.cc", ingressStreamCreationRegex, out match))
			{
				var sessionState = EnsureSessionReported(message, match, eventsBuffer);
				var streamName = match.Groups["stream_name"].Value;
				PeerState streamPeer = null;
				string mediaType = null;
				foreach (var peer in sessions.SelectMany(s => s.Value.peers.Values))
				{
					foreach (var ms in Query(peer.currentOffer, "media_stream", strict: false) ?? new JArray())
					{
						if (Query(ms, "media_stream[0].stream_id[0].value", strict: true).ToString() == streamName)
						{
							streamPeer = peer;
							mediaType = Query(ms, "media_stream[0].media_type", strict: false)?.ToString();
						}
					}
				}
				eventsBuffer.Enqueue(new ObjectCreation(message, streamName, ingressRelayStreamTypeInfo));
				eventsBuffer.Enqueue(new ParentChildRelationChange(message, streamName, ingressRelayStreamTypeInfo,
					streamPeer?.stateInspectorId ?? sessionState.stateInspectorId));
				if (mediaType != null)
					eventsBuffer.Enqueue(new PropertyChange(message, streamName, ingressRelayStreamTypeInfo, "media_type", mediaType));
			}
			if (TryMatch(message, "audio_media_stream_receiver.cc", changeActiveStreamRegex, out match))
			{
				eventsBuffer.Enqueue(new PropertyChange(message, match.Groups["streamId"].Value, ingressRelayStreamTypeInfo, 
					"active audio stream", match.Groups["ssrc"].Value, valueType: Postprocessing.StateInspector.ValueType.Reference));
			}
			if (TryMatch(message, "down_stream_controller.cc", muteStateRegex, out match))
			{
				eventsBuffer.Enqueue(new PropertyChange(message, match.Groups["streamId"].Value, ingressRelayStreamTypeInfo,
					"mute state", match.Groups["value"].Value));
			}
			if (TryMatch(message, "down_stream_controller.cc", deletingEgressStreamRegex, out match))
			{
				eventsBuffer.Enqueue(new ObjectDeletion(message, match.Groups["stream_name"].Value, ingressRelayStreamTypeInfo));
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

		JToken Query(JToken token, string path, bool strict)
		{
			// todo: extract common helper
			int? getIndex(string indexStr) => indexStr.Length == 0 ? new int?() : int.Parse(indexStr);
			foreach (Match pathPart in textProtoPathPartRegex.Matches(path))
			{
				var prop = pathPart.Groups["prop"].Value;
				JToken nextToken = null;
				if (token is JObject obj)
				{
					if (obj.TryGetValue(prop, out var propValue))
					{
						var index = getIndex(pathPart.Groups["index"].Value);
						if (index.HasValue)
						{
							if (propValue is JArray array)
							{
								if (index.Value >= 0 && index.Value < array.Count)
								{
									nextToken = array[index.Value];
								}
								else if (strict)
								{
									throw new IndexOutOfRangeException(
										$"Array index {index.Value} is out of range 0-{array.Count-1} in property {prop} in path: {path}");
								}
							}
							else if (strict)
							{
								throw new InvalidOperationException(
									$"Property {prop} is not an array in path {path}");
							}
						}
						else
						{
							nextToken = propValue;
						}
					}
					else if (strict)
					{
						throw new InvalidOperationException(
							$"Property {prop} is found missing while following path {path}");
					}
				}
				else if (strict)
				{
					throw new InvalidOperationException(
						$"Can not access property {prop} of not an object {token?.GetType() ?? null} when following path {path}");
				}
				token = nextToken;
			}
			return token;
		}

		void ApplyModification(JToken token, string action, string path, string value)
		{
			int? getIndex(string indexStr) => indexStr.Length == 0 ? new int?() : int.Parse(indexStr);
			JObject assertObject(JToken t, string context) => t as JObject ?? 
				throw new InvalidCastException($"Expected object got {t?.GetType() ?? null}. Context: {context}");
			JArray assertArray(JToken t, string context) => t as JArray ??
				throw new InvalidCastException($"Expected array got {t?.GetType() ?? null}. Context: {context}");

			foreach (Match pathPart in textProtoPathPartRegex.Matches(path))
			{
				var prop = pathPart.Groups["prop"].Value;
				var index = getIndex(pathPart.Groups["index"].Value);
				if (assertObject(token, $"accessing {prop} in path {path}").TryGetValue(prop, out var propValue))
				{
					token = propValue;
				}
				else if (action == "added")
				{
					var newToken = index.HasValue ? (JToken)new JArray() : new JObject();
					assertObject(token, $"adding {prop} in path {path}").Add(prop, newToken);
					token = newToken;
				}
				else
				{
					return;
				}
				if (index.HasValue)
				{
					var array = assertArray(token, $"accessing {prop}[{index}] in path {path}");
					if (index.Value < array.Count)
					{
						token = array[index.Value];
					}
					else if (action == "added" && index.Value == array.Count)
					{
						array.Add(token = new JObject());
					}
					else
					{
						return;
					}
				}
				else if (token is JArray array)
				{
					if (array.Count != 1)
						throw new InvalidOperationException(
							$"can not access property {prop} because the value is an array with size {array.Count}. Full path: {path}");
					token = array[0];
				}
			}
			if (action == "deleted")
			{
				token.Remove();
			}
			else
			{
				token.Replace(TextProtoParser.Parse(value));
			}
		}

		VirtualAudioSsrcState EnsureVirtualSsrcReported(Message trigger, Match loglineMatch, Queue<Event> eventsBuffer)
		{
			var virtualSsrcId = loglineMatch.Groups["fourthId"].Value;
			if (!virtualAudioSsrcs.TryGetValue(virtualSsrcId, out var state))
			{
				state = new VirtualAudioSsrcState(virtualSsrcId);
				virtualAudioSsrcs[virtualSsrcId] = state;
				eventsBuffer.Enqueue(new ObjectCreation(trigger, state.stateInspectorId, virtualSsrcTypeInfo,
					displayName: $"Virtual Audio SSRC {state.vssrc}"));
				var sessionState = EnsureSessionReported(trigger, loglineMatch, eventsBuffer);
				eventsBuffer.Enqueue(new ParentChildRelationChange(trigger, state.stateInspectorId,
					virtualSsrcTypeInfo, sessionState.id));
			}
			return state;
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

		void ApplyChangesToOfferAndReportChanges(PeerState peerState, Action action,
			Message trigger, Queue<Event> eventsBuffer)
		{
			List<Action> monitors = new List<Action>();

			void monitorPath(string path, string stateInspectorPropertyName)
			{
				var value1 = Query(peerState.currentOffer, path, strict: false)?.ToString();
				monitors.Add(() =>
				{
					var value2 = Query(peerState.currentOffer, path, strict: false)?.ToString();
					if (value1 != value2)
					{
						eventsBuffer.Enqueue(new PropertyChange(
							trigger, peerState.stateInspectorId, peerTypeInfo, stateInspectorPropertyName, value2));
					}
				});
			};

			monitorPath("version", "offer version");
			monitorPath("router_bns", "router BNS");
			monitorPath("router_rpc_address", "router RPC address");
			monitorPath("router_udp_address", "router UDP address");
			monitorPath("rtc_client[0].device", "device");
			monitorPath("rtc_client[0].application", "application");
			monitorPath("rtc_client[0].platform", "type");
			action();
			monitors.ForEach(a => a());
		}

		class PeerState
		{
			public readonly string id;
			public readonly string stateInspectorId;
			public JToken currentOffer;

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

		class VirtualAudioSsrcState
		{
			readonly Regex virtualSsrcIdRegex = new Regex(@"^virtual_audio_ssrc_(?<vssrc>\d+)$", regexOptions);

			public readonly string id;
			public readonly string stateInspectorId;
			public readonly uint vssrc;
			public uint mappedSsrc;

			public VirtualAudioSsrcState(string id)
			{
				var virtualSsrcIdMatch = virtualSsrcIdRegex.Match(id);
				vssrc = uint.Parse(virtualSsrcIdMatch.Groups["vssrc"].Value);
				this.id = id;
				this.stateInspectorId = id;
			}
		};

		#region Mutable state

		bool rootReported = false;
		readonly Dictionary<string, SessionState> sessions = new Dictionary<string, SessionState>();
		readonly Dictionary<string, VirtualAudioSsrcState> virtualAudioSsrcs = new Dictionary<string, VirtualAudioSsrcState>();
		readonly Dictionary<uint, string> ssrcToAudioStreamId = new Dictionary<uint, string>();

		#endregion

		#region Constants

		const string rootObjectId = "MediaRouter objects";

		readonly static ObjectTypeInfo rootTypeInfo = new ObjectTypeInfo("media_router.root", isTimeless: true);

		const RegexOptions regexOptions = RegexOptions.ExplicitCapture | RegexOptions.Compiled;

		readonly Regex textProtoPathPartRegex = new Regex(@"(?<prop>\w+)(\[(?<index>\d+)\])?(\.|$)", regexOptions | RegexOptions.Multiline);

		const string idsPrefixRegex = Helpers.IdsPrefixRegex;
		const string peerIdRegex = Helpers.PeerIdRegex;

		readonly static ObjectTypeInfo sessionTypeInfo = new ObjectTypeInfo("media_router.session");
		readonly Regex addSessionRequestRe = new Regex(idsPrefixRegex + @" \w+ REQ: media_session", regexOptions);
		readonly Regex removeSessionRequestRe = new Regex(idsPrefixRegex + @" Ending session with endcause (?<endcause>\w+)", regexOptions);
		readonly Regex conferenceIdRe = new Regex(idsPrefixRegex + " Setting conference id to \"(?<value>[^\"]*)\"", regexOptions);

		readonly static ObjectTypeInfo peersTypeInfo = new ObjectTypeInfo("media_router.peers", isTimeless: true);
		readonly static ObjectTypeInfo peerTypeInfo = new ObjectTypeInfo("media_router.peer", displayIdPropertyName: "type");
		readonly Regex peerAddedRegex = new Regex(idsPrefixRegex + @" (?<type>Peer) " + peerIdRegex + @" added in version", regexOptions);
		readonly Regex sessionPeerAddedRegex = new Regex(idsPrefixRegex + @" CreateSessionPeer to session_id " + peerIdRegex, regexOptions);
		readonly Regex extensionPeerAddedRegex = new Regex(idsPrefixRegex + @" Adding (?<type>extension) peer " + peerIdRegex + @" with target (?<target>\S+)", regexOptions);
		readonly Regex peerRemovedRegex1 = new Regex(idsPrefixRegex + @" Erasing peer " + peerIdRegex, regexOptions);
		readonly Regex peerRemovedRegex2 = new Regex(idsPrefixRegex + @" Removing obsolete extension peer " + peerIdRegex, regexOptions);
		readonly Regex peerParticipantIdRegex = new Regex(idsPrefixRegex + @" Setting participant id of peer "+ peerIdRegex + " to (?<value>.+)$", regexOptions);

		readonly static ObjectTypeInfo virtualSsrcTypeInfo = new ObjectTypeInfo("media_router.vssrc", primaryPropertyName: "mapped SSRC");
		readonly Regex virtualSsrcMappedRegex = new Regex(idsPrefixRegex + @" Newly mapped CSRC (?<ssrc>\d+) has been relayed", regexOptions);

		readonly Regex firstOfferRegex = new Regex(idsPrefixRegex + @" First offer from peer " + peerIdRegex + " is: (?<offer>.+)$", regexOptions);
		readonly Regex offerChangedRegex = new Regex(idsPrefixRegex + @" Offer from peer " + peerIdRegex + " changed. Differences: (?<differences>.+)$", regexOptions | RegexOptions.Singleline);
		readonly Regex offerChangeRegex = new Regex(@"^(?<action>added|deleted|modified): (?<path>[^\:]+): (?<value1>.+?)( -> (?<value2>.+))?$", regexOptions | RegexOptions.Multiline);

		readonly static ObjectTypeInfo ingressRelayStreamTypeInfo = new ObjectTypeInfo("media_router.ingress_relay_stream", displayIdPropertyName: "media_type");
		readonly Regex deletingEgressStreamRegex = new Regex(idsPrefixRegex + @" Deleting egress stream: (?<stream_name>\S+)", regexOptions);
		readonly Regex ingressStreamCreationRegex = new Regex(idsPrefixRegex + @" CreateIngressStream: \{ stream_name: (?<stream_name>[\w_\/]+)", regexOptions);
		readonly Regex changeActiveStreamRegex = new Regex(idsPrefixRegex + @" (?<streamId>[^\:]+): Changing active audio stream to { ssrc: (?<ssrc>\d+)", regexOptions);
		readonly Regex muteStateRegex = new Regex(idsPrefixRegex + @" Setting mute state for stream (?<streamId>\S+) to (?<value>\w+)", regexOptions);

		readonly static ObjectTypeInfo audioStreamReceiverTypeInfo = new ObjectTypeInfo("media_router.audio_stream_receiver", displayIdPropertyName: "SSRC");
		readonly Regex audioStreamAddedRegex = new Regex(idsPrefixRegex + @" (?<streamId>[^\:]+): Creating audio stream \{ ssrc: (?<ssrc>\d+)", regexOptions);

		readonly Regex requestRe = new Regex(idsPrefixRegex + @" REQ\: (?<payload>.+)$", regexOptions);

		#endregion
	}
}

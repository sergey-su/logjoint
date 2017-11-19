using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
	public interface IWebRtcStateInspector
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input);
	};

	public class WebRtcStateInspector : IWebRtcStateInspector
	{
		public WebRtcStateInspector(
			IPrefixMatcher matcher
		)
		{
		}

		IEnumerableAsync<Event[]> IWebRtcStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents);
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			var msg = msgPfx.Message;
			EnsureWebRtcRootReported(msg, buffer);
			if (msg.RootObjectType == Message.RootObjectTypes.Connection)
			{
				var objectIdMatch = objectIdRegex.Match(msg.ObjectId);
				if (objectIdMatch.Success)
				{
					ObjectType objectType;
					if (objectTypes.TryGetValue(objectIdMatch.Groups["type"].Value.ToLowerInvariant(), out objectType))
					{
						HandlePeerConnectionObjectMessage(objectType, msg, buffer);
					}
				}
				else if (msg.ObjectId == "log")
				{
					HandlePeerConnectionLogMessage(msg, buffer);
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
			foreach (var peerConnection in reportedPeerConnection)
			{
				foreach (var obj in peerConnection.Value.ReportedObjects)
				{
					buffer.Enqueue(new ObjectDeletion(obj.Value.LastTrigger, obj.Key, obj.Value.Type.TypeInfo));
				}
			}
		}

		void EnsureWebRtcRootReported(Message trigger, Queue<Event> buffer)
		{
			if (rootReported)
				return;
			rootReported = true;

			buffer.Enqueue(new ObjectCreation(trigger, rootObjectId, rootTypeInfo));

			buffer.Enqueue(new ObjectCreation(trigger, peerConnectionsRootObjectId, peerConnectionRootTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, peerConnectionsRootObjectId, peerConnectionRootTypeInfo, rootObjectId));
		}

		PeerConnectionState EnsurePeerConnectionReported(string peerConnectionId, Message trigger, Queue<Event> buffer)
		{
			PeerConnectionState state;
			if (reportedPeerConnection.TryGetValue(peerConnectionId, out state))
				return state;
			state = new PeerConnectionState()
			{
				ObjectId = peerConnectionId,
				ConnsRootObjectId = peerConnectionId + ".Connections",
				CandidatesRootObjectId = peerConnectionId + ".Candidates",
				SSRCsRootObjectId = peerConnectionId +  ".Streams"

			};
			reportedPeerConnection.Add(peerConnectionId, state);
			buffer.Enqueue(new ObjectCreation(trigger, peerConnectionId, peerConnectionTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, peerConnectionId, peerConnectionTypeInfo, peerConnectionsRootObjectId));

			Action<ObjectTypeInfo, string, string> reportCategoryRoot = (typeInfo, objId, displayName) =>
			{
				buffer.Enqueue(new ObjectCreation(trigger, objId, typeInfo, displayName: displayName));
				buffer.Enqueue(new ParentChildRelationChange(trigger, objId, typeInfo, state.ObjectId));
			};

			reportCategoryRoot(connectionsRootTypeInfo, state.ConnsRootObjectId, "Connections");
			reportCategoryRoot(candidatesRootTypeInfo, state.CandidatesRootObjectId, "Candidates");
			reportCategoryRoot(ssrcsRootTypeInfo, state.SSRCsRootObjectId, "SSRC");

			return state;
		}

		void HandlePeerConnectionObjectMessage(
			ObjectType objectType, 
			Message message, 
			Queue<Event> buffer)
		{
			var peerConnectionState = EnsurePeerConnectionReported(
				message.RootObjectId, message, buffer);
			ObjectState obj;
			if (!peerConnectionState.ReportedObjects.TryGetValue(message.ObjectId, out obj))
			{
				obj = new ObjectState()
				{
					Type = objectType
				};
				buffer.Enqueue(new ObjectCreation(message, message.ObjectId, objectType.TypeInfo));
				buffer.Enqueue(new ParentChildRelationChange(message, message.ObjectId, 
					objectType.TypeInfo, objectType.GetParentObjectId(peerConnectionState)));
				peerConnectionState.ReportedObjects.Add(message.ObjectId, obj);
			}
			Analytics.StateInspector.ValueType? propValueType = null;
			if (objectType.ScalarProperties.Contains(message.PropName))
				propValueType = Analytics.StateInspector.ValueType.Scalar;
			else if (objectType.ReferenceProperties.Contains(message.PropName))
				propValueType = Analytics.StateInspector.ValueType.Reference;
			if (propValueType != null)
			{
				string oldValue;
				if (!obj.Properties.TryGetValue(message.PropName, out oldValue) || oldValue != message.PropValue)
				{
					buffer.Enqueue(new PropertyChange(
						message, message.ObjectId, objectType.TypeInfo,
						message.PropName, message.PropValue, propValueType.Value
					));
					obj.Properties[message.PropName] = message.PropValue;
				}
				if (obj.LastTrigger == null || message.Timestamp >= obj.LastTrigger.Timestamp)
				{
					obj.LastTrigger = message;
				}
			}
		}

		void HandlePeerConnectionLogMessage(Message message, Queue<Event> buffer)
		{
			var peerConnectionState = EnsurePeerConnectionReported(
				message.RootObjectId, message, buffer);
			buffer.Enqueue(new PropertyChange(
				message, peerConnectionState.ObjectId, peerConnectionTypeInfo,
				"API call", message.PropName
			));
		}
		
		#region Mutable state

		bool rootReported = false;
		readonly Dictionary<string, PeerConnectionState> reportedPeerConnection = new Dictionary<string, PeerConnectionState>();

		class PeerConnectionState
		{
			public string ObjectId;
			public string ConnsRootObjectId;
			public string CandidatesRootObjectId;
			public string SSRCsRootObjectId;
			public Dictionary<string, ObjectState> ReportedObjects = new Dictionary<string, ObjectState>();
		};

		class ObjectState
		{
			public ObjectType Type;
			public Dictionary<string, string> Properties = new Dictionary<string, string>();
			public Message LastTrigger;
		};

		#endregion

		#region Constants

		const string rootObjectId = "WebRTC";
		const string peerConnectionsRootObjectId = "PeerConnections";

		readonly static ObjectTypeInfo rootTypeInfo = new ObjectTypeInfo("webrtc", isTimeless: true);
		readonly static ObjectTypeInfo peerConnectionRootTypeInfo = new ObjectTypeInfo("webrtc.peerconns", isTimeless: true);
		readonly static ObjectTypeInfo connectionsRootTypeInfo = new ObjectTypeInfo("webrtc.conns", isTimeless: true);
		readonly static ObjectTypeInfo candidatesRootTypeInfo = new ObjectTypeInfo("webrtc.ports", isTimeless: true);
		readonly static ObjectTypeInfo ssrcsRootTypeInfo = new ObjectTypeInfo("webrtc.ssrcs", isTimeless: true);

		readonly static ObjectTypeInfo peerConnectionTypeInfo = new ObjectTypeInfo("webrtc.peerconn");
		readonly static ObjectTypeInfo connectionTypeInfo = new ObjectTypeInfo("webrtc.conn");
		readonly static ObjectTypeInfo candidateTypeInfo = new ObjectTypeInfo("webrtc.candidate", isTimeless: true);
		readonly static ObjectTypeInfo ssrcTypeInfo = new ObjectTypeInfo("webrtc.ssrc");
		readonly static ObjectTypeInfo propertyNodeTypeInfo = new ObjectTypeInfo("webrtc.obj_prop", isTimeless: true);

		readonly Regex objectIdRegex = new Regex(@"^(?<type>\w+?)[-_].+$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		static readonly Dictionary<string, ObjectType> objectTypes = new []
		{
			new ObjectType(
				"Conn", 
				connectionTypeInfo, 
				obj => obj.ConnsRootObjectId,
				"googActiveConnection", "googReadable", "r:googChannelId", "googLocalAddress", 
				"r:localCandidateId", "googLocalCandidateType", "googRemoteAddress", "r:remoteCandidateId", 
				"googRemoteCandidateType", "googTransportType", "googTransportType"
			),
			new ObjectType(
				"Cand",
				candidateTypeInfo,
				obj => obj.CandidatesRootObjectId,
				"ipAddress", "portNumber", "portNumber", "transport", "candidateType"
			),
			new ObjectType(
				"ssrc",
				ssrcTypeInfo,
				obj => obj.SSRCsRootObjectId,
				"ssrc", "r:transportId", "googCodecName", "googTrackId", "googTypingNoiseState", 
				"codecImplementationName", "mediaType", "googTypingNoiseState"
			)
		}.ToDictionary(i => i.TypePrefix);

		class ObjectType
		{
			public readonly string TypePrefix;
			public readonly ObjectTypeInfo TypeInfo;
			public readonly Func<PeerConnectionState, string> GetParentObjectId;
			public readonly HashSet<string> ScalarProperties = new HashSet<string>();
			public readonly HashSet<string> ReferenceProperties = new HashSet<string>();
			public ObjectType(string prefix, ObjectTypeInfo typeInfo, 
				Func<PeerConnectionState, string> getParentId, params string [] scalarProperties)
			{
				TypePrefix = prefix.ToLowerInvariant();
				TypeInfo = typeInfo;
				GetParentObjectId = getParentId;
				foreach (var prop in scalarProperties)
					if (prop.StartsWith("r:"))
						ReferenceProperties.Add(prop.Substring(2));
					else
						ScalarProperties.Add(prop);	
			}
		};

		#endregion
	}
}

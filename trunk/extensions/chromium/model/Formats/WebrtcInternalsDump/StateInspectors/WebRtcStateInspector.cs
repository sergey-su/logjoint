using LogJoint.Postprocessing;
using LogJoint.Postprocessing.StateInspector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
    public interface IWebRtcStateInspector
    {
        IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input);
        ObjectTypeInfo CandidateTypeInfo { get; }
        ObjectTypeInfo PeerConnectionTypeInfo { get; }
    };

    public class WebRtcStateInspector : IWebRtcStateInspector
    {
        public WebRtcStateInspector(
            IPrefixMatcher matcher
        )
        {
        }

        public static bool ShouldBePresentedCollapsed(Event creationEvent)
        {
            var objectType = creationEvent?.ObjectType?.TypeName;
            return defaultCollapsedNodesTypes.Contains(objectType);
        }

        public static bool HasTimeSeries(Event creationEvent)
        {
            var objectType = creationEvent?.ObjectType?.TypeName;
            return objectType == ssrcTypeInfo.TypeName || objectType == connectionTypeInfo.TypeName;
        }

        IEnumerableAsync<Event[]> IWebRtcStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input)
        {
            return input.Select<MessagePrefixesPair<Message>, Event>(GetEvents, GetFinalEvents);
        }

        ObjectTypeInfo IWebRtcStateInspector.CandidateTypeInfo
        {
            get { return candidateTypeInfo; }
        }

        ObjectTypeInfo IWebRtcStateInspector.PeerConnectionTypeInfo
        {
            get { return peerConnectionTypeInfo; }
        }


        void GetEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer)
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
                StreamsRootObjectId = peerConnectionId + ".Streams",
                ChannelsRootObjectId = peerConnectionId + ".Channels",
                TracksRootObjectId = peerConnectionId + ".Tracks",
                CertsRootObjectId = peerConnectionId + ".Certs",
                DataChannelsRootObjectId = peerConnectionId + ".DataChannels",
                SourcesRootObjectId = peerConnectionId + ".Sources",
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
            reportCategoryRoot(ssrcsRootTypeInfo, state.StreamsRootObjectId, "Streams");
            reportCategoryRoot(candidatesRootTypeInfo, state.CandidatesRootObjectId, "Candidates");
            reportCategoryRoot(channelsRootTypeInfo, state.ChannelsRootObjectId, "Channels");
            reportCategoryRoot(tracksRootTypeInfo, state.TracksRootObjectId, "Tracks");
            reportCategoryRoot(certsRootTypeInfo, state.CertsRootObjectId, "Certificates");
            reportCategoryRoot(dataChannelsRootTypeInfo, state.DataChannelsRootObjectId, "Data channels");
            reportCategoryRoot(mediaSourcesRootTypeInfo, state.SourcesRootObjectId, "Media sources");

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
            Postprocessing.StateInspector.ValueType? propValueType = null;
            if (objectType.ScalarProperties.Contains(message.PropName))
                propValueType = Postprocessing.StateInspector.ValueType.Scalar;
            else if (objectType.ReferenceProperties.Contains(message.PropName))
                propValueType = Postprocessing.StateInspector.ValueType.Reference;
            if (propValueType != null)
            {
                string propValue = message.PropValue;
                Func<string, string> converter;
                if (objectType.Converters != null && objectType.Converters.TryGetValue(message.PropName, out converter))
                    propValue = converter(propValue);

                string oldValue;
                if (!obj.Properties.TryGetValue(message.PropName, out oldValue) || oldValue != propValue)
                {
                    buffer.Enqueue(new PropertyChange(
                        message, message.ObjectId, objectType.TypeInfo,
                        message.PropName, propValue, propValueType.Value
                    ));
                    obj.Properties[message.PropName] = propValue;
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
                "last API call", message.PropName
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
            public string StreamsRootObjectId;
            public string ChannelsRootObjectId;
            public string TracksRootObjectId;
            public string CertsRootObjectId;
            public string DataChannelsRootObjectId;
            public string SourcesRootObjectId;
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
        readonly static ObjectTypeInfo tracksRootTypeInfo = new ObjectTypeInfo("webrtc.tracks", isTimeless: true);
        readonly static ObjectTypeInfo channelsRootTypeInfo = new ObjectTypeInfo("webrtc.channels", isTimeless: true);
        readonly static ObjectTypeInfo certsRootTypeInfo = new ObjectTypeInfo("webrtc.certs", isTimeless: true);
        readonly static ObjectTypeInfo dataChannelsRootTypeInfo = new ObjectTypeInfo("webrtc.dataChannels", isTimeless: true);
        readonly static ObjectTypeInfo mediaSourcesRootTypeInfo = new ObjectTypeInfo("webrtc.sources", isTimeless: true);

        readonly static ObjectTypeInfo peerConnectionTypeInfo = new ObjectTypeInfo("webrtc.peerconn");
        readonly static ObjectTypeInfo connectionTypeInfo = new ObjectTypeInfo("webrtc.conn", displayIdPropertyName: "googActiveConnection");
        readonly static ObjectTypeInfo candidateTypeInfo = new ObjectTypeInfo("webrtc.candidate", isTimeless: true);
        readonly static ObjectTypeInfo candidatePairTypeInfo = new ObjectTypeInfo("webrtc.candidatePait", primaryPropertyName: "state");
        readonly static ObjectTypeInfo ssrcTypeInfo = new ObjectTypeInfo("webrtc.ssrc", displayIdPropertyName: "mediaType");
        readonly static ObjectTypeInfo trackTypeInfo = new ObjectTypeInfo("webrtc.track", isTimeless: true);
        readonly static ObjectTypeInfo channelTypeInfo = new ObjectTypeInfo("webrtc.channel", isTimeless: true);
        readonly static ObjectTypeInfo certTypeInfo = new ObjectTypeInfo("webrtc.cert", isTimeless: true);
        readonly static ObjectTypeInfo dataChannelTypeInfo = new ObjectTypeInfo("webrtc.dataChannel", primaryPropertyName: "state");
        readonly static ObjectTypeInfo mediaSourceTypeInfo = new ObjectTypeInfo("webrtc.source");
        readonly static ObjectTypeInfo mediaStreamTypeInfo = new ObjectTypeInfo("webrtc.stream", isTimeless: true);

        readonly Regex objectIdRegex = new Regex(@"^(?<type>\w+?)[-_].+$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        static readonly HashSet<string> defaultCollapsedNodesTypes = new HashSet<string>(new[]
        {
            tracksRootTypeInfo, candidatesRootTypeInfo,
            channelsRootTypeInfo, certsRootTypeInfo
        }.Select(i => i.TypeName));

        static readonly string[] RtpSteamProps = new[]
        {
            "ssrc", "r:transportId", "codecId", "r:trackId", "kind",
            "isRemote", "mediaType"
        };

        static readonly string[] RemoteInboundRtpStreamProps = new[]
        {
            "ssrc", "r:transportId", "codecId", "kind", "localId"
        };

        static readonly Dictionary<string, ObjectType> objectTypes = new[]
        {
            new ObjectType( // old format's type
				"Conn",
                connectionTypeInfo,
                obj => obj.ConnsRootObjectId,
                new []
                {
                    "googActiveConnection", "googReadable", "r:googChannelId", "googLocalAddress",
                    "r:localCandidateId", "googLocalCandidateType", "googRemoteAddress", "r:remoteCandidateId",
                    "googRemoteCandidateType", "googTransportType", "googTransportType"
                },
                converters: new Dictionary<string, Func<string, string>>()
                {
                    { "googActiveConnection", val => string.Compare(val, "true", StringComparison.InvariantCultureIgnoreCase) == 0 ? "active" : "inactive" }
                }
            ),
            new ObjectType( // old format's type
				"Cand",
                candidateTypeInfo,
                obj => obj.CandidatesRootObjectId,
                new []
                {
                    "ipAddress", "portNumber", "portNumber", "transport", "candidateType"
                }
            ),
            new ObjectType(
                "RTCIceCandidate",
                candidateTypeInfo,
                obj => obj.CandidatesRootObjectId,
                new []
                {
                    "ip", "port", "r:transportId", "candidateType", "networkType", "isRemote"
                }
            ),
            new ObjectType(
                "RTCIceCandidatePair",
                candidatePairTypeInfo,
                obj => obj.CandidatesRootObjectId,
                new []
                {
                    "writable", "nominated", "state", "r:remoteCandidateId", "r:localCandidateId", "r:transportId", "priority"
                }
            ),
            new ObjectType( // old format's type
				"ssrc",
                ssrcTypeInfo,
                obj => obj.StreamsRootObjectId,
                new []
                {
                    "ssrc", "r:transportId", "googCodecName", "googTrackId", "googTypingNoiseState",
                    "codecImplementationName", "mediaType", "googTypingNoiseState"
                }
            ),
            new ObjectType(
                "RTCInboundRTPAudioStream",
                ssrcTypeInfo,
                obj => obj.StreamsRootObjectId,
                RtpSteamProps
            ),
            new ObjectType(
                "RTCOutboundRTPAudioStream",
                ssrcTypeInfo,
                obj => obj.StreamsRootObjectId,
                RtpSteamProps
            ),
            new ObjectType(
                "RTCInboundRTPVideoStream",
                ssrcTypeInfo,
                obj => obj.StreamsRootObjectId,
                RtpSteamProps
            ),
            new ObjectType(
                "RTCOutboundRTPVideoStream",
                ssrcTypeInfo,
                obj => obj.StreamsRootObjectId,
                RtpSteamProps
            ),
            new ObjectType(
                "RTCRemoteInboundRtpAudioStream",
                ssrcTypeInfo,
                obj => obj.StreamsRootObjectId,
                RemoteInboundRtpStreamProps
            ),
            new ObjectType(
                "RTCRemoteInboundRtpVideoStream",
                ssrcTypeInfo,
                obj => obj.StreamsRootObjectId,
                RemoteInboundRtpStreamProps
            ),
            new ObjectType( // old format's type
				"googTrack",
                trackTypeInfo,
                obj => obj.TracksRootObjectId,
                new []
                {
                    "googTrackId"
                }
            ),
            new ObjectType(
                "RTCMediaStreamTrack",
                trackTypeInfo,
                obj => obj.TracksRootObjectId,
                new []
                {
                    "kind", "detached", "ended", "remoteSource", "trackIdentifier"
                }
            ),
            new ObjectType( // old format's type
				"Channel",
                channelTypeInfo,
                obj => obj.ChannelsRootObjectId,
                new []
                {
                    "r:selectedCandidatePairId", "googComponent", "r:localCertificateId",
                    "dtlsCipher", "r:remoteCertificateId", "srtpCipher"
                }
            ),
            new ObjectType(
                "RTCTransport",
                channelTypeInfo,
                obj => obj.ChannelsRootObjectId,
                new []
                {
                    "r:selectedCandidatePairId", "dtlsState", "r:localCertificateId",
                    "r:remoteCertificateId", "transportId"
                }
            ),
            new ObjectType( // old format's type
				"googCertificate",
                certTypeInfo,
                obj => obj.CertsRootObjectId,
                new []
                {
                    "googDerBase64", "googFingerprint", "googFingerprintAlgorithm"
                }
            ),
            new ObjectType(
                "RTCCertificate",
                certTypeInfo,
                obj => obj.CertsRootObjectId,
                new []
                {
                    "base64Certificate", "fingerprintAlgorithm", "fingerprint", "fingerprintAlgorithm"
                }
            ),
            new ObjectType( // old format's type
				"datachannel",
                dataChannelTypeInfo,
                obj => obj.DataChannelsRootObjectId,
                new []
                {
                    "datachannelid", "protocol", "state", "label"
                }
            ),
            new ObjectType(
                "RTCDataChannel",
                dataChannelTypeInfo,
                obj => obj.DataChannelsRootObjectId,
                new []
                {
                    "datachannelid", "protocol", "state", "label"
                }
            ),
            new ObjectType(
                "RTCAudioSource",
                mediaSourceTypeInfo,
                obj => obj.SourcesRootObjectId,
                new []
                {
                    "trackIdentifier", "kind"
                }
            ),
            new ObjectType(
                "RTCVideoSource",
                mediaSourceTypeInfo,
                obj => obj.SourcesRootObjectId,
                new []
                {
                    "trackIdentifier", "kind", "width", "height"
                }
            ),
            new ObjectType(
                "RTCMediaStream",
                mediaStreamTypeInfo,
                obj => obj.StreamsRootObjectId,
                new []
                {
                    "streamIdentifier", "trackIds"
                }
            ),
        }.ToDictionary(i => i.TypePrefix);

        class ObjectType
        {
            public readonly string TypePrefix;
            public readonly ObjectTypeInfo TypeInfo;
            public readonly Func<PeerConnectionState, string> GetParentObjectId;
            public readonly HashSet<string> ScalarProperties = new HashSet<string>();
            public readonly HashSet<string> ReferenceProperties = new HashSet<string>();
            public readonly Dictionary<string, Func<string, string>> Converters;
            public ObjectType(
                string prefix, ObjectTypeInfo typeInfo,
                Func<PeerConnectionState, string> getParentId,
                string[] properties,
                Dictionary<string, Func<string, string>> converters = null
            )
            {
                TypePrefix = prefix.ToLowerInvariant();
                TypeInfo = typeInfo;
                GetParentObjectId = getParentId;
                Converters = converters;
                foreach (var prop in properties)
                    if (prop.StartsWith("r:", StringComparison.InvariantCultureIgnoreCase))
                        ReferenceProperties.Add(prop.Substring(2));
                    else
                        ScalarProperties.Add(prop);
            }
        };

        #endregion
    }
}

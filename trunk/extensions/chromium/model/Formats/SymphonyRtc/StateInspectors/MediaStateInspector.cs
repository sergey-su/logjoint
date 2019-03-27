using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.Rtc
{
	public interface IMediaStateInspector
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input);
	};

	public class MediaStateInspector : IMediaStateInspector
	{
		public MediaStateInspector(
			IPrefixMatcher matcher,
			IMeetingsStateInspector meetingsStateInspector
		)
		{
			this.meetingsStateInspector = meetingsStateInspector;
		}
			
		IEnumerableAsync<Event[]> IMediaStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input
				.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags))
				.EnsureParented((creationEvt, buffer) =>
					meetingsStateInspector.EnsureRootObjectCreated((Message)creationEvt.Trigger, buffer));
		}

		public static ObjectTypeInfo LocalMediaTypeInfo { get { return localMediaTypeInfo; } }
		public static ObjectTypeInfo LocalScreenTypeInfo { get { return localScreenObjectType; } }
		public static ObjectTypeInfo LocalAudioTypeInfo { get { return localAudioObjectType; } }
		public static ObjectTypeInfo LocalVideoTypeInfo { get { return localVideoObjectType; } }
		public static ObjectTypeInfo TestSessionTypeInfo { get { return testSessionObjectType; } }

		public static bool HasTimeSeries(Postprocessing.StateInspector.IInspectedObject obj)
		{
			var objectType = obj.CreationEvent?.OriginalEvent?.ObjectType?.TypeName;
			return objectType == webRtcStatsObjectObjectType.TypeName;
		}

		public static bool ShouldBePresentedCollapsed(Postprocessing.StateInspector.IInspectedObject obj)
		{
			var objectType = obj.CreationEvent?.OriginalEvent?.ObjectType?.TypeName;
			return defaultCollapsedNodesTypes.Contains(objectType);
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			string id, type;
			if (logableIdUtils.TryParseLogableId(msgPfx.Message.Logger.Value, out type, out id))
			{
				switch (type)
				{
					case "localMedia":
						GetLocalMediaEvents(msgPfx, buffer, id);
						break;
					case "localScreen":
						GetLocalScreenEvents(msgPfx, buffer, id);
						break;
					case "localAudio":
						GetLocalAudioVideoEvents(msgPfx, buffer, id, localAudioObjectType);
						break;
					case "localVideo":
						GetLocalAudioVideoEvents(msgPfx, buffer, id, localVideoObjectType);
						break;
					case "remotePart":
						TryLinkRemotePartToRemoteTracks(msgPfx, buffer, id);
						break;
					case "session":
						GetSessionEvents(msgPfx, buffer, id);
						break;
					case "remoteTrack":
						GetRemoteTrackEvents(msgPfx, buffer, id);
						break;
					case "remoteTracks":
						GetRemoteTracksEvents(msgPfx, buffer, id);
						break;
					case "remoteMedia":
						GetRemoteMediaEvents(msgPfx, buffer, id);
						break;
					case "stats":
						GetStatsEvents(msgPfx, buffer, id);
						break;
					case "tsession":
						GetTestSessionEvents(msgPfx, buffer, id);
						break;
				}
			}
		}

		void GetLocalMediaEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			var msg = msgPfx.Message;
			Match m;


			Action<string, string> attachSymTrack = (trackType, symTrackId) =>
			{
				if (!string.IsNullOrEmpty(symTrackId))
				{
					var trackObjectType =
						trackType == "audio" ? localAudioObjectType :
						trackType == "video" ? localVideoObjectType :
						localScreenObjectType;
					buffer.Enqueue(new ParentChildRelationChange(msg, symTrackId, trackObjectType, loggableId));
					buffer.Enqueue(new PropertyChange(
						msg, loggableId, localMediaTypeInfo, trackType + " track", symTrackId, Analytics.StateInspector.ValueType.Reference));
				}
				else
				{
					buffer.Enqueue(new PropertyChange(msg, loggableId, localMediaTypeInfo, trackType + " track", ""));
				}
			};

			if ((m = localMediaCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, loggableId, localMediaTypeInfo));
			}
			else if (msg.Text == "disposed")
			{
				buffer.Enqueue(new ObjectDeletion(msg, loggableId, localMediaTypeInfo));
			}
			else if ((m = localMediaScreenOnRegex.Match(msg.Text)).Success)
			{
				attachSymTrack("screen", m.Groups["value"].Value);
			}
			else if ((m = localMediaScreenOffRegex.Match(msg.Text)).Success)
			{
				attachSymTrack("screen", "");
			}
			else if ((m = localMediaAudioOnRegex.Match(msg.Text)).Success)
			{
				attachSymTrack("audio", m.Groups["value"].Value);
			}
			else if ((m = localMediaAudioOffRegex.Match(msg.Text)).Success)
			{
				attachSymTrack("audio", "");
			}
			else if ((m = localMediaVideoOnRegex.Match(msg.Text)).Success)
			{
				attachSymTrack("video", m.Groups["value"].Value);
			}
			else if ((m = localMediaVideoOffRegex.Match(msg.Text)).Success)
			{
				attachSymTrack("video", "");
			}
			else if ((m = localMediaInitialModalityRegex.Match(msg.Text)).Success)
			{
				var symTrackId = m.Groups["value"].Value;
				if (symTrackId != "undefined")
				{
					attachSymTrack(m.Groups["modality"].Value, symTrackId);
				}
			}
		}

		void GetLocalScreenEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			var msg = msgPfx.Message;
			Match m;
			if ((m = localScreenCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, loggableId, localScreenObjectType));
			}
			else if (msg.Text == "dispose")
			{
				buffer.Enqueue(new ObjectDeletion(msg, loggableId, localScreenObjectType));
			}
			else if ((m = localScreenPropRegex.Match(msg.Text)).Success)
			{
				var value = m.Groups["value"].Value;
				buffer.Enqueue(new PropertyChange(
					msg, loggableId, localScreenObjectType,
					m.Groups["streamId"].Length > 0 ? "WebRTC stream id" :
					m.Groups["label"].Length > 0 ? "label" :
					m.Groups["trackId"].Length > 0 ? "WebRTC track id" :
					"?",
					value));
				if (m.Groups["isLabel"].Length > 0 && m.Groups["prefix"].Length > 0)
				{
					buffer.Enqueue(new PropertyChange(
						msg, loggableId, localScreenObjectType, "type", m.Groups["prefix"].Value));
				}
				if (m.Groups["streamId"].Length > 0)
				{
					localWebRtcStreamIdToInfo[value] = new LocalWebRTCStreamInfo()
					{
						symTrackId = loggableId
					};
				}
			}
		}

		void GetLocalAudioVideoEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId, ObjectTypeInfo typeInfo)
		{
			var msg = msgPfx.Message;
			Match m;
			if ((m = localAudioVideoCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, loggableId, typeInfo));
				buffer.Enqueue(new PropertyChange(
					msg, loggableId, typeInfo, "device", m.Groups["device"].Value));
				if (typeInfo == localAudioObjectType || typeInfo == localVideoObjectType)
				{
					buffer.Enqueue(new PropertyChange(msg, loggableId, typeInfo, "state", "unmuted"));
				}
			}
			else if (msg.Text == "dispose")
			{
				buffer.Enqueue(new ObjectDeletion(msg, loggableId, typeInfo));
			}
			else if ((m = localAudioVideoPropRegex.Match(msg.Text)).Success)
			{
				var value = m.Groups["value"].Value;
				buffer.Enqueue(new PropertyChange(
					msg, loggableId, typeInfo,
					m.Groups["stream"].Success ? "WebRTC stream id" :
					m.Groups["tracks"].Success ? "WebRTC track id" :
					"?", value));
				if (m.Groups["stream"].Success)
				{
					localWebRtcStreamIdToInfo[value] = new LocalWebRTCStreamInfo()
					{
						symTrackId = loggableId
					};
				}
			}
			else if (msg.Text == "mute")
			{
				buffer.Enqueue(new PropertyChange(msg, loggableId, typeInfo, "state", "muted"));
			}
			else if ((typeInfo == localAudioObjectType) && msg.Text == "unmute")
			{
				buffer.Enqueue(new PropertyChange(msg, loggableId, typeInfo, "state", "unmuted"));
			}
			else if ((typeInfo == localVideoObjectType) && msg.Text == "unmuted")
			{
				buffer.Enqueue(new PropertyChange(msg, loggableId, typeInfo, "state", "unmuted"));
			}
		}

		void TryLinkRemotePartToRemoteTracks(MessagePrefixesPair msgPfx, Queue<Event> buffer, string remotePartLoggableId)
		{
			Match m;
			if ((m = remotePartCreationRegex.Match(msgPfx.Message.Text)).Success)
			{
				buffer.Enqueue(new ParentChildRelationChange(
					msgPfx.Message, m.Groups["remoteTracksId"].Value, remoteTracksObjectType, remotePartLoggableId));
				remoteTracksIdToParticipantsId[m.Groups["remoteTracksId"].Value] = m.Groups["participantsId"].Value;
				buffer.Enqueue(new PropertyChange(
					msgPfx.Message, m.Groups["remoteTracksId"].Value, remoteTracksObjectType, "particpant is lastN", m.Groups["lastN"].Value));
			}
		}

		void GetSessionEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string sessionLoggableId)
		{
			Match m;
			if ((m = sessionStatsCtrRegex.Match(msgPfx.Message.Text)).Success)
			{
				statsIdToSessionId[m.Groups["value"].Value] = sessionLoggableId;
			}
			else if ((m = sessionRemoteMediaCtrRegex.Match(msgPfx.Message.Text)).Success)
			{
				var remoteMediaId = m.Groups["value"].Value;
				buffer.Enqueue(new ObjectCreation(msgPfx.Message, remoteMediaId, remoteMediaObjectType));
				buffer.Enqueue(new ParentChildRelationChange(msgPfx.Message, remoteMediaId, remoteMediaObjectType, sessionLoggableId));
				remoteMediaIdToSessionId[remoteMediaId] = sessionLoggableId;
			}
			else if ((m = sessionParticipantsRegex.Match(msgPfx.Message.Text)).Success)
			{
				participantsIdToSessionId[m.Groups["value"].Value] = sessionLoggableId;
			}
		}

		void GetRemoteMediaEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			Func<string, string, string, bool, RemoteWebRTCStreamInfo> handleChange = (stmId, a, v, allowCreate) =>
			{
				string sessionId;
				if (!remoteMediaIdToSessionId.TryGetValue(loggableId, out sessionId))
				{
					return null;
				}
				Dictionary<string, RemoteWebRTCStreamInfo> sessionStreams;
				if (!remoteWebRtcStreamIdToInfo.TryGetValue(sessionId, out sessionStreams))
				{
					remoteWebRtcStreamIdToInfo[sessionId] = sessionStreams = new Dictionary<string, RemoteWebRTCStreamInfo>();
				}
				RemoteWebRTCStreamInfo stmInfo;
				if (!sessionStreams.TryGetValue(stmId, out stmInfo))
				{
					if (!allowCreate)
					{
						return null;
					}
					sessionStreams[stmId] = stmInfo = new RemoteWebRTCStreamInfo()
					{
						stateInspectorObjectId = RemoteWebRTCStreamInfo.MakeStateInspectorObjectId(stmId, sessionId),
						remoteMediaId = loggableId
					};
					buffer.Enqueue(new ObjectCreation(msgPfx.Message, stmInfo.stateInspectorObjectId, remoteWebRTCStreamObjectType));
					buffer.Enqueue(new ParentChildRelationChange(msgPfx.Message, stmInfo.stateInspectorObjectId, remoteWebRTCStreamObjectType, loggableId));
				}
				Action<string, string, Dictionary<string, RemoteWebRTCStreamInfo.TrackInfo>> handleList = (list, modalityName, dict) =>
				{
					HashSet<string> newTrackIds =
						remoteMediaStreamReceivedTrackRegex
							.Matches(list)
							.OfType<Match>()
							.Select(x => x.Groups["id"].Value)
							.ToHashSet();
					bool changed = false;
					foreach (var newTrackId in newTrackIds.Except(dict.Keys.ToArray()))
					{
						dict[newTrackId] = new RemoteWebRTCStreamInfo.TrackInfo()
						{
							added = msgPfx.Message
						};
						changed = true;
					}
					foreach (var knownTrack in dict.Where(t => t.Value.removed == null).ToArray())
					{
						if (!newTrackIds.Contains(knownTrack.Key))
						{
							dict[knownTrack.Key].removed = msgPfx.Message;
							changed = true;
						}
					}
					if (changed)
					{
						buffer.Enqueue(new PropertyChange(
							msgPfx.Message, stmInfo.stateInspectorObjectId, remoteWebRTCStreamObjectType,
							modalityName + " WebRTC track ids", list));
					}
				};
				handleList(a, "audio", stmInfo.audioWebRtcTracks);
				handleList(v, "video", stmInfo.videoWebRtcTracks);
				return stmInfo;
			};

			Match m;
			if ((m = remoteMediaStreamReceivedRegex.Match(msgPfx.Message.Text)).Success)
			{
				handleChange(m.Groups["streamId"].Value,
					m.Groups["audioTracks"].Value, m.Groups["videoTracks"].Value, true);
			}
			else if ((m = remoteMediaStreamRemovedRegex.Match(msgPfx.Message.Text)).Success)
			{
				var stmInfo = handleChange(m.Groups["streamId"].Value, "", "", false);
				if (stmInfo != null)
				{
					buffer.Enqueue(new ObjectDeletion(
						msgPfx.Message, stmInfo.stateInspectorObjectId, remoteWebRTCStreamObjectType));
					stmInfo.deleted = true;
				}
			}
			else if (msgPfx.Message.Text == "disposed")
			{
				buffer.Enqueue(new ObjectDeletion(
					msgPfx.Message, loggableId, remoteMediaObjectType));
				foreach (var sessionStreams in remoteWebRtcStreamIdToInfo.Values)
				foreach (var stmInfo in sessionStreams.Values.Where(
					stm => !stm.deleted && stm.remoteMediaId == loggableId))
				{
					buffer.Enqueue(new ObjectDeletion(
						msgPfx.Message, stmInfo.stateInspectorObjectId, remoteWebRTCStreamObjectType));
					stmInfo.deleted = true;
				}
			}
		}

		void GetRemoteTrackEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			var msg = msgPfx.Message;
			if ((m = remoteTrackCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, loggableId, remoteTrackObjectType));
				var webRtcStreamId = m.Groups["webRtcStreamId"].Value;
				var remoteTracksId = m.Groups["remoteTracksId"].Value;
				buffer.Enqueue(new PropertyChange(msg, loggableId, remoteTrackObjectType, "WebRTC stream id", webRtcStreamId));
				buffer.Enqueue(new PropertyChange(msg, loggableId, remoteTrackObjectType, "type", m.Groups["type"].Value));
				buffer.Enqueue(new ParentChildRelationChange(msg, loggableId, remoteTrackObjectType, remoteTracksId));
				if (remoteTracksIdToParticipantsId.TryGetValue(remoteTracksId, out var participantsId) 
				 && participantsIdToSessionId.TryGetValue(participantsId, out var sessionId))
				{
					buffer.Enqueue(new PropertyChange(
						msg, loggableId, remoteTrackObjectType,
						"stream", RemoteWebRTCStreamInfo.MakeStateInspectorObjectId(webRtcStreamId, sessionId),
						Analytics.StateInspector.ValueType.Reference));
				}
				var remoteTracksIdAndMediaType = $"{remoteTracksId}.{m.Groups["type"].Value}";
				if (remoteTracksIdAndMediaTypeToRemoteTrackId.TryGetValue(remoteTracksIdAndMediaType, out var oldRemoteTrackId))
				{
					buffer.Enqueue(new ObjectDeletion(msg, oldRemoteTrackId, remoteTrackObjectType));
				}
				remoteTracksIdAndMediaTypeToRemoteTrackId[remoteTracksIdAndMediaType] = loggableId;
			}
		}

		void GetRemoteTracksEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			var msg = msgPfx.Message;

			Action<bool> yieldAudioStatePropChange = (bool unmuted) =>
			{
				buffer.Enqueue(new PropertyChange(
					msg, loggableId, remoteTracksObjectType, "audio state", unmuted ? "unmuted" : "muted"));
			};

			if ((m = remoteTracksCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, loggableId, remoteTracksObjectType));
				yieldAudioStatePropChange(!(m.Groups["muted"].Success || m.Groups["audio"].Value == "undefined"));
				Action<string> yieldInitialStream = (type) =>
				{
					buffer.Enqueue(new PropertyChange(msg, loggableId, remoteTracksObjectType,
						type + " stream id", m.Groups[type].Value));
				};
				yieldInitialStream("audio");
				yieldInitialStream("video");
				yieldInitialStream("screen");
			}
			else if ((m = remoteTracksModalityStreamIdChangeRegex.Match(msg.Text)).Success)
			{
				var newStm = m.Groups["newWebRtcStreamId"].Value;
				var type = m.Groups["type"].Value;
				buffer.Enqueue(new PropertyChange(msg, loggableId, remoteTracksObjectType,
					type + " stream id", newStm));
			}
			else if ((m = remoteTracksMutedChangeRegex.Match(msg.Text)).Success)
			{
				yieldAudioStatePropChange(m.Groups["value"].Value == "false");
			}
			else if ((m = remoteTracksLastNChangeRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new PropertyChange(msg, loggableId, remoteTracksObjectType,
					"particpant is lastN", m.Groups["value"].Value));
			}
		}

		void GetStatsEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string loggableId)
		{
			Match m;
			var msg = msgPfx.Message;

			if ((m = statsConnQualityRegex.Match(msg.Text)).Success)
			{
				string sessionId;
				if (statsIdToSessionId.TryGetValue(loggableId, out sessionId))
				{
					buffer.Enqueue(new PropertyChange(
						msg, sessionId, MeetingsStateInspector.MeetingSessionTypeInfo, "connection quality", m.Groups["value"].Value));
				}
			}
			else if ((m = statsObjectPropRegex.Match(msg.Text)).Success
			      || (m = statsObjectGoneRegex.Match(msg.Text)).Success)
			{
				var id = m.Groups["id"].Value;
				var prop = m.Groups["prop"].Value;
				if (prop.Length > 0 && !webRtcStatsObjectAllAllowedProps.Contains(prop))
					return;
				string sessionId;
				if (!statsIdToSessionId.TryGetValue(loggableId, out sessionId))
					sessionId = "(no session)";
				Dictionary<string, WebRtcStatsObjectInfo> sessionStatsObjects;
				if (!webRtcStatsObjects.TryGetValue(sessionId, out sessionStatsObjects))
					webRtcStatsObjects[sessionId] = sessionStatsObjects = new Dictionary<string, WebRtcStatsObjectInfo>();
				WebRtcStatsObjectInfo objInfo;
				if (!sessionStatsObjects.TryGetValue(id, out objInfo))
				{
					sessionStatsObjects[id] = objInfo = new WebRtcStatsObjectInfo()
					{
						stateInspectorObjectId = WebRtcStatsObjectInfo.MakeStateInspectorObjectId(id, loggableId),
						statsId = loggableId,
						cluster = new HashSet<string> { id }
					};
				}
				objInfo.messages.Add(msg);
				if (prop == "type")
				{
					objInfo.type = m.Groups["value"].Value;
				}
			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
			GetFinalStatsObjectsEvents(buffer);

			foreach (var sessionStreams in remoteWebRtcStreamIdToInfo)
			foreach (var remoteStream in sessionStreams.Value)
			{
				string sessionId = sessionStreams.Key;
				Dictionary<string, WebRtcStatsObjectInfo> sessionStatsObjects;
				if (!webRtcStatsObjects.TryGetValue(sessionId, out sessionStatsObjects))
					continue;
				AttachWebRtcStatsObjectToSymTrack(buffer, sessionStatsObjects, remoteStream.Key, remoteStream.Value.stateInspectorObjectId);
				remoteStream.Value.audioWebRtcTracks.Keys.Union(
					remoteStream.Value.videoWebRtcTracks.Keys).ToList().ForEach(
						trackId => AttachWebRtcStatsObjectToSymTrack(
							buffer, sessionStatsObjects, trackId, remoteStream.Value.stateInspectorObjectId));
				Action<string, Dictionary<string, RemoteWebRTCStreamInfo.TrackInfo>> helper = (modalityName, dict) =>
				{
					var webRtcStatsObjectLinkPropName = "WebRTC " + modalityName + " track";
					foreach (var i in dict)
					{
						WebRtcStatsObjectInfo objInfo;
						if (sessionStatsObjects.TryGetValue(i.Key, out objInfo))
						{
							if (i.Value.added != null)
							{
								buffer.Enqueue(new PropertyChange(
									i.Value.added, remoteStream.Value.stateInspectorObjectId, remoteWebRTCStreamObjectType,
									webRtcStatsObjectLinkPropName, objInfo.stateInspectorObjectId,
									Analytics.StateInspector.ValueType.Reference));
							}
							if (i.Value.removed != null)
							{
								buffer.Enqueue(new PropertyChange(
									i.Value.removed, remoteStream.Value.stateInspectorObjectId, remoteWebRTCStreamObjectType,
									webRtcStatsObjectLinkPropName, ""));
							}
						}
					}
				};
				helper("audio", remoteStream.Value.audioWebRtcTracks);
				helper("video", remoteStream.Value.videoWebRtcTracks);
			}
			foreach (var localStream in localWebRtcStreamIdToInfo)
			{
				foreach (var sessionStatsObjects in webRtcStatsObjects.Values)
					AttachWebRtcStatsObjectToSymTrack(buffer, sessionStatsObjects, localStream.Key, localStream.Value.symTrackId);
			}

			AttachUnparentedWebRtcStatsObjects(buffer);
		}

		private void AttachWebRtcStatsObjectToSymTrack(Queue<Event> buffer, Dictionary<string, WebRtcStatsObjectInfo> sessionStatsObjects, string webRtcId, string symTrackId)
		{
			WebRtcStatsObjectInfo objInfo;
			if (symTrackId != null && sessionStatsObjects.TryGetValue(webRtcId, out objInfo) && !objInfo.parented)
				AttachClusterTo(buffer, sessionStatsObjects, symTrackId, objInfo.cluster);
		}

		private void AttachClusterTo(Queue<Event> buffer, Dictionary<string, WebRtcStatsObjectInfo> sessionStatsObjects, string newParent, HashSet<string> cluster)
		{
			foreach (var id in cluster)
			{
				var objInfo = sessionStatsObjects[id];
				if (!objInfo.parented)
				{
					objInfo.parented = true;
					buffer.Enqueue(new ParentChildRelationChange(
						objInfo.messages[0],
						objInfo.stateInspectorObjectId, webRtcStatsObjectObjectType, newParent));
				}
			}
		}

		void GetFinalStatsObjectsEvents(Queue<Event> buffer)
		{
			var metaProcessed = webRtcStatsObjectTypesMeta.Select(m => new
			{
				k = m.Key,
				v = m.Value
				     .Select(p => new WebRTCStatsObjectPropertyMeta(p))
				     .ToDictionary(p => p.name)
			}).ToDictionary(x => x.k, x => x.v);
			foreach (var sessionStatsObjects in webRtcStatsObjects.Values)
			foreach (var statsObjEntry in sessionStatsObjects.ToArray())
			{
				var statsObj = statsObjEntry.Value;
				if (statsObj.type == null || !metaProcessed.ContainsKey(statsObj.type))
					continue;
				var meta = metaProcessed[statsObj.type];
				string sessionId;
				if (!statsIdToSessionId.TryGetValue(statsObj.statsId, out sessionId))
					continue;
				buffer.Enqueue(new ObjectCreation(
					statsObj.messages[0], statsObj.stateInspectorObjectId, webRtcStatsObjectObjectType));
				foreach (var msg in statsObj.messages)
				{
					Match m;
					if ((m = statsObjectPropRegex.Match(msg.Text)).Success)
					{
						var prop = m.Groups["prop"].Value;
						var value = m.Groups["value"].Value;
						if (!meta.ContainsKey(prop))
							continue;
						if (meta[prop].needsEscaping)
							value = valuesEscapingRegex.Replace(value, "_");
						buffer.Enqueue(new PropertyChange(
							 msg, statsObj.stateInspectorObjectId, webRtcStatsObjectObjectType, prop,
							 meta[prop].isLink ?
									 WebRtcStatsObjectInfo.MakeStateInspectorObjectId(value, statsObj.statsId) :
									 value,
							meta[prop].isLink ?
								Analytics.StateInspector.ValueType.Reference :
								Analytics.StateInspector.ValueType.Scalar
						));
						if (meta[prop].isCluster)
						{
							foreach (var stateObjId2 in value
										 .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
							{
								WebRtcStatsObjectInfo statsObj2;
								if (sessionStatsObjects.TryGetValue(stateObjId2, out statsObj2))
									MergeClusters(statsObj, statsObj2, sessionStatsObjects);
								else
									sessionStatsObjects[stateObjId2] = statsObj;
							}
						}
					}
					else if ((m = statsObjectGoneRegex.Match(msg.Text)).Success)
					{
						buffer.Enqueue(new ObjectDeletion(
							msg, statsObj.stateInspectorObjectId, webRtcStatsObjectObjectType));
					}
				}
			}
		}

		void MergeClusters(WebRtcStatsObjectInfo obj1, WebRtcStatsObjectInfo obj2, Dictionary<string, WebRtcStatsObjectInfo> sessionStatsObjects)
		{
			if (obj1.cluster == obj2.cluster)
				return;
			var newCluster = new HashSet<string>();
			newCluster.UnionWith(obj1.cluster);
			newCluster.UnionWith(obj2.cluster);
			foreach (var id in newCluster)
				sessionStatsObjects[id].cluster = newCluster;
		}

		void AttachUnparentedWebRtcStatsObjects(Queue<Event> buffer)
		{
			foreach (var sessionStatsObjects in webRtcStatsObjects.Values)
			{
				string rootContainerId = null;
				int lastGroupNr = 0;
				foreach (var statsObj in sessionStatsObjects.Values)
				{
					if (!statsObj.parented 
					  && statsObj.type != null
					  && webRtcStatsObjectTypesMeta.ContainsKey(statsObj.type))
					{
						var trigger = statsObj.messages[0];
						if (rootContainerId == null)
						{
							string parentId;
							if (!statsIdToSessionId.TryGetValue(statsObj.statsId, out parentId))
								parentId = statsObj.statsId;
							rootContainerId = statsObj.statsId + " - WebRTC stats objects";
							buffer.Enqueue(new ObjectCreation(
								trigger, rootContainerId, statsObjectContainerObjectType));
							buffer.Enqueue(new ParentChildRelationChange(
								trigger, rootContainerId, statsObjectContainerObjectType, parentId));
						}
						if (statsObj.cluster.Count > 1)
						{
							var groupId = string.Format("group #{0}", ++lastGroupNr);
							buffer.Enqueue(new ObjectCreation(
								trigger, groupId, statsObjectsGroupObjectType));
							buffer.Enqueue(new ParentChildRelationChange(
								trigger, groupId, statsObjectsGroupObjectType, rootContainerId));
							AttachClusterTo(buffer, sessionStatsObjects, groupId, statsObj.cluster);
						}
						else
						{
							AttachClusterTo(buffer, sessionStatsObjects, rootContainerId, statsObj.cluster);
						}
					}
				}
			}
		}

		void GetTestSessionEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer, string testSessionLoggableId)
		{
			Match m;
			var msg = msgPfx.Message;

			if ((m = testSessionCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ObjectCreation(msg, testSessionLoggableId, testSessionObjectType));
			}
			else if ((m = testSessionRemoteMediaCtrRegex.Match(msg.Text)).Success)
			{
				buffer.Enqueue(new ParentChildRelationChange(msg, m.Groups["value"].Value, remoteMediaObjectType, testSessionLoggableId));
				remoteMediaIdToSessionId[m.Groups["value"].Value] = testSessionLoggableId;
			}
			else if ((m = testSessionStatsCtrRegex.Match(msg.Text)).Success)
			{
				statsIdToSessionId[m.Groups["value"].Value] = testSessionLoggableId;
			}
			else if (msg.Text == "disposed")
			{
				buffer.Enqueue(new ObjectDeletion(msg, testSessionLoggableId, testSessionObjectType));
			}
		}

		class RemoteWebRTCStreamInfo
		{
			public string stateInspectorObjectId;
			public string remoteMediaId;
			public bool deleted;

			public class TrackInfo
			{
				public Message added, removed;
			};
			public Dictionary<string, TrackInfo> audioWebRtcTracks = new Dictionary<string, TrackInfo>();
			public Dictionary<string, TrackInfo> videoWebRtcTracks = new Dictionary<string, TrackInfo>();

			public static string MakeStateInspectorObjectId(string webRtcStreamId, string sessionId)
			{
				var ret = "remote stream " + webRtcStreamId;
				if (webRtcStreamId.StartsWith("mixed", StringComparison.OrdinalIgnoreCase))
					ret += " in " + sessionId;
				return ret;
			}
		};

		class LocalWebRTCStreamInfo
		{
			public string symTrackId;
		};

		class WebRtcStatsObjectInfo
		{
			public string stateInspectorObjectId;
			public List<Message> messages = new List<Message>();
			public HashSet<string> cluster; // ids of logically connected objects
			public string type; // can be null if type is never logged
			public string statsId; // id of symphont stats object
			public bool parented; // true if already parented in SI tree

			public static string MakeStateInspectorObjectId(string webRtcId, string statsId)
			{
				return string.Format("{0}.{1}", statsId, webRtcId);
			}
		};

		readonly IMeetingsStateInspector meetingsStateInspector;

		readonly Dictionary<string, Dictionary<string, RemoteWebRTCStreamInfo>> remoteWebRtcStreamIdToInfo = new Dictionary<string, Dictionary<string, RemoteWebRTCStreamInfo>>();
		readonly Dictionary<string, LocalWebRTCStreamInfo> localWebRtcStreamIdToInfo = new Dictionary<string, LocalWebRTCStreamInfo>();
		readonly Dictionary<string, string> statsIdToSessionId = new Dictionary<string, string>();
		readonly Dictionary<string, string> remoteMediaIdToSessionId = new Dictionary<string, string>();
		readonly Dictionary<string, string> remoteTracksIdToParticipantsId = new Dictionary<string, string>();
		readonly Dictionary<string, string> participantsIdToSessionId = new Dictionary<string, string>();
		readonly Dictionary<string, string> remoteTracksIdAndMediaTypeToRemoteTrackId = new Dictionary<string, string>();
		readonly Dictionary<string, Dictionary<string, WebRtcStatsObjectInfo>> webRtcStatsObjects = new Dictionary<string, Dictionary<string, WebRtcStatsObjectInfo>>(); // session id -> stats object id -> object


		readonly static ObjectTypeInfo localMediaTypeInfo = new ObjectTypeInfo("sym.localMedia", primaryPropertyName: "state");
		readonly static ObjectTypeInfo localScreenObjectType = new ObjectTypeInfo("sym.localScreen", displayIdPropertyName: "type");
		readonly static ObjectTypeInfo localAudioObjectType = new ObjectTypeInfo("sym.localAudio", primaryPropertyName: "state");
		readonly static ObjectTypeInfo localVideoObjectType = new ObjectTypeInfo("sym.localVideo", primaryPropertyName: "state");
		readonly static ObjectTypeInfo remoteTracksObjectType = new ObjectTypeInfo("sym.remoteTracks", displayIdPropertyName: "audio state");
		readonly static ObjectTypeInfo remoteTrackObjectType = new ObjectTypeInfo("sym.remoteTrack", displayIdPropertyName: "type");
		readonly static ObjectTypeInfo remoteMediaObjectType = new ObjectTypeInfo("sym.remoteMedia");
		readonly static ObjectTypeInfo remoteWebRTCStreamObjectType = new ObjectTypeInfo("sym.remoteStream", displayIdPropertyName: "type");
		readonly static ObjectTypeInfo webRtcStatsObjectObjectType = new ObjectTypeInfo("sym.statsObject", primaryPropertyName: "state", displayIdPropertyName: "type");
		readonly static ObjectTypeInfo statsObjectContainerObjectType = new ObjectTypeInfo("sym.statsObjects", isTimeless: true);
		readonly static ObjectTypeInfo statsObjectsGroupObjectType = new ObjectTypeInfo("sym.statsObjectsGroup", isTimeless: true);
		readonly static ObjectTypeInfo testSessionObjectType = new ObjectTypeInfo("sym.testSession");

		readonly static Dictionary<string, string[]> webRtcStatsObjectTypesMeta = new Dictionary<string, string[]>
		{
			{ "track", new [] { "type", "kind", "ended", "remoteSource", "detached", "c:trackIdentifier" } },
			{ "inbound-rtp", new [] { "type", "ssrc", "isRemote", "mediaType", "cl:trackId", "l:codecId" } },
			{ "outbound-rtp", new [] { "type", "ssrc", "isRemote", "mediaType", "cl:trackId", "l:codecId" } },
			{ "stream", new [] { "type", "c:streamIdentifier", "cl:trackIds" } },
			{ "data-channel", new [] { "type", "label", "protocol", "datachannelid", "state" } },
			{ "codec", new [] { "type", "payloadType", "mimeType", "clockRate" } },
			{ "transport", new [] { "type", "dtlsState", "lc:selectedCandidatePairId", "lce:localCertificateId", "lce:remoteCertificateId" } },
			{ "local-candidate", new [] { "type", "lc:transportId", "isRemote", "networkType", "ip", "port", "protocol", "candidateType", "priority", "deleted" } },
			{ "remote-candidate", new [] { "type", "lc:transportId", "isRemote", "networkType", "ip", "port", "protocol", "candidateType", "priority", "deleted" } },
			{ "candidate-pair", new [] { "type", "writable", "nominated", "priority", "state", "lc:transportId", "lc:localCandidateId", "lc:remoteCandidateId" } },
			{ "certificate", new [] { "type", "fingerprint", "fingerprintAlgorithm", "base64Certificate" } }
		};
		readonly static HashSet<string> webRtcStatsObjectAllAllowedProps = webRtcStatsObjectTypesMeta
				.SelectMany(t => t.Value)
				.Select(p => new WebRTCStatsObjectPropertyMeta(p).name)
				.ToHashSet();

		struct WebRTCStatsObjectPropertyMeta
		{
			readonly static Regex statsObjectMetaPropRegex = new Regex(@"^(?<flags>\w+:)?(?<name>\w+)$", reopts);

			public readonly string name;
			public readonly bool isCluster;
			public readonly bool isLink;
			public readonly bool needsEscaping;

			public WebRTCStatsObjectPropertyMeta(string str)
			{
				var pm = statsObjectMetaPropRegex.Match(str);
				var flags = pm.Groups["flags"].Value;
				name = pm.Groups["name"].Value;
				isCluster = flags.Contains("c");
				isLink = flags.Contains("l");
				needsEscaping = flags.Contains("e");
			}
		};

		static readonly RegexOptions reopts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
		readonly LogableIdUtils logableIdUtils = new LogableIdUtils();

		readonly Regex localMediaCtrRegex = new Regex(@"^created", reopts);
		readonly Regex localMediaScreenOnRegex = new Regex(@"^Enabling screen share with (?<value>\S+)$", reopts);
		readonly Regex localMediaScreenOffRegex = new Regex(@"^Disabling screen share$", reopts);
		readonly Regex localMediaAudioOnRegex = new Regex(@"^Starting audio with device: (?<value>\S+)$", reopts);
		readonly Regex localMediaAudioOffRegex = new Regex(@"^Disabling audio$", reopts);
		readonly Regex localMediaVideoOnRegex = new Regex(@"^Enabling video with device: (?<value>\S+)$", reopts);
		readonly Regex localMediaVideoOffRegex = new Regex(@"^Disabling video$", reopts);
		readonly Regex localMediaInitialModalityRegex = new Regex(@"^initial local (?<modality>\w+): (?<value>\S+)$", reopts);

		readonly Regex localScreenCtrRegex = new Regex(@"^created", reopts);
		readonly Regex localScreenPropRegex = new Regex(@"^((?<streamId>source selected)|(?<trackId>track id)|(?<label>label)): (?<value>((?<prefix>\w+):)?\S+)$", reopts);

		readonly Regex localAudioVideoCtrRegex = new Regex(@"device: (?<device>\S+)", reopts);
		readonly Regex localAudioVideoPropRegex = new Regex(@"^((?<stream>created stream)|(?<tracks>created tracks)) (?<value>\S+)$", reopts);

		readonly Regex remotePartCreationRegex = new Regex(@"^created in scope of (?<participantsId>\S+) .+ media=(?<remoteTracksId>[\w\-]+)\. lastN=(?<lastN>\w+)", reopts);

		readonly Regex remoteTrackCtrRegex = new Regex(@"^created for stream (?<webRtcStreamId>\S+) in (?<remoteTracksId>\S+) type=(?<type>\w+)$", reopts);

		readonly Regex remoteTracksCtrRegex = new Regex(@"^created for user \S+ a='(?<audio>\S+)'(?<muted> .muted.)? v='(?<video>\S+)' s='(?<screen>\S+)'", reopts);
		readonly Regex remoteTracksModalityStreamIdChangeRegex = new Regex(@"^(?<type>\w+)StreamId changed (?<oldWebRtcStreamId>\S+) -> (?<newWebRtcStreamId>\S+)", reopts);
		readonly Regex remoteTracksMutedChangeRegex = new Regex(@"^audioMuted changed \w+ -> (?<value>\w+)", reopts);
		readonly Regex remoteTracksLastNChangeRegex = new Regex(@"^lastN changed \w+ -> (?<value>\w+)", reopts);

		readonly Regex remoteMediaStreamReceivedRegex = new Regex(@"^((remote stream received\.)|(tracks changed in remote stream)) id=(?<streamId>\S+) .*?a=(?<audioTracks>[\w\-\s\(\),]*?) v=(?<videoTracks>[\w\-\s\(\),]*?)$", reopts);
		readonly Regex remoteMediaStreamReceivedTrackRegex = new Regex(@"(?<id>[\S]+) \([^\)]*\)", reopts);
		readonly Regex remoteMediaStreamRemovedRegex = new Regex(@"^remote stream removed (?<streamId>\S+)$", reopts);

		readonly Regex sessionStatsCtrRegex = new Regex(@"^created stats: (?<value>\S+)$", reopts);
		readonly Regex sessionRemoteMediaCtrRegex = new Regex(@"^created remote media: (?<value>\S+)$", reopts);
		readonly Regex sessionParticipantsRegex = new Regex(@"^created participants: (?<value>\S+)$", reopts);

		readonly Regex statsConnQualityRegex = new Regex(@" ^ Local connection quality: (?<value>\S+)$", reopts);
		readonly Regex statsObjectPropRegex = new Regex(@"^(?<id>RTC[^\.]+)\.(?<prop>\w+)=(?<value>.*)$", reopts);
		readonly Regex statsObjectGoneRegex = new Regex(@"^(?<id>RTC[^\.]+) gone$", reopts);

		readonly Regex testSessionCtrRegex = new Regex(@"^created with protocol session (?<protocol>\S+) videoStream=(?<videoStream>\S+) dropTcp=(?<dropTcp>\S+) dropUdp=(?<dropUdp>\S+) counterpart=(?<remoteSessionId>\S+)$", reopts);
		readonly Regex testSessionRemoteMediaCtrRegex = new Regex(@"^created remote media: (?<value>\S+)", reopts);
		readonly Regex testSessionStatsCtrRegex = new Regex(@"^created stats: (?<value>\S+)", reopts);

		readonly Regex valuesEscapingRegex = new Regex(@"\.");

		readonly HashSet<string> tags = new HashSet<string>() { "meetings", "media" };

		static readonly HashSet<string> defaultCollapsedNodesTypes = new [] 
		{
			remoteMediaObjectType, localMediaTypeInfo, testSessionObjectType, statsObjectContainerObjectType
		}.Select(i => i.TypeName).ToHashSet();
	}
}

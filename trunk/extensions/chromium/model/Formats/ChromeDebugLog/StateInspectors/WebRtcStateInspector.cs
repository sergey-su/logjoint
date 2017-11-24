using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.Chromium.ChromeDebugLog
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
			sessionPrefix = matcher.RegisterPrefix("Session:");
			connPrefix = matcher.RegisterPrefix("Jingle:Conn[");
			prefixlessConnPrefix = matcher.RegisterPrefix("Conn[");
			portPrefix = matcher.RegisterPrefix("Jingle:Port[");
			audioRecvStreamCtrPrefix = matcher.RegisterPrefix("AudioReceiveStream: {");
			audioRecvStreamDtrPrefix = matcher.RegisterPrefix("~AudioReceiveStream: {");
			audioSendStreamCtrPrefix = matcher.RegisterPrefix("AudioSendStream: {");
			audioSendStreamDtrPrefix = matcher.RegisterPrefix("~AudioSendStream: {");
			videoRecvStreamCtrPrefix = matcher.RegisterPrefix("VideoReceiveStream: {");
			videoRecvStreamDtrPrefix = matcher.RegisterPrefix("~VideoReceiveStream: {");
			videoSendStreamCtrPrefix = matcher.RegisterPrefix("VideoSendStreamInternal: {");
			videoSendStreamDtrPrefix = matcher.RegisterPrefix("~VideoSendStreamInternal: {");

			connReGroupNames = GetReGroupNames(connCreatedRe);
			portReGroupNames = GetReGroupNames(portGeneric);
			candidateReGroupNames = GetReGroupNames(new Regex(string.Format(candidateRePattern, ""), RegexOptions.ExplicitCapture));
		}


		IEnumerableAsync<Event[]> IWebRtcStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags));
		}

		public static bool ShouldBePresentedCollapsed(Postprocessing.StateInspector.IInspectedObject obj)
		{
			if (obj.Id == portsRootObjectId && obj.Parent?.Id == rootObjectId)
				return true;
			var typeName = obj.CreationEvent?.OriginalEvent?.ObjectType?.TypeName;
			if (typeName == streamTypeInfo.TypeName && obj.Parent?.Id == streamsRootObjectId)
				return true;
			return false;
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			var msg = msgPfx.Message;
			Match m;
			if (msgPfx.Prefixes.Contains(sessionPrefix))
			{
				if ((m = sessionStateChangeRe.Match(msg.Text)).Success)
				{
					var objId = EnsureSessionReported(m.Groups["sid"].Value, msg, buffer);
					buffer.Enqueue(new PropertyChange(msg, objId, sessionTypeInfo, "state", m.Groups["new"].Value));
				}
				else if ((m = sessionDestroyedRe.Match(msg.Text)).Success)
				{
					var objId = EnsureSessionReported(m.Groups["sid"].Value, msg, buffer);
					buffer.Enqueue(new ObjectDeletion(msg, objId, sessionTypeInfo));
				}
			}
			else if (msgPfx.Prefixes.Contains(connPrefix))
			{
				if ((m = connStateRe.Match(msg.Text)).Success)
				{
					UpdateCommonConnectionProps(buffer, msg, m);
				}
				else if ((m = connCreatedRe.Match(msg.Text)).Success)
				{
					UpdateCommonConnectionProps(buffer, msg, m);
				}
				else if ((m = connDestroyed.Match(msg.Text)).Success)
				{
					var objId = UpdateCommonConnectionProps(buffer, msg, m);
					buffer.Enqueue(new ObjectDeletion(msg, objId, connectionTypeInfo));
				}
			}
			else if (msgPfx.Prefixes.Contains(prefixlessConnPrefix))
			{
				if ((m = connDumpRe.Match(msg.Text)).Success)
				{
					UpdateCommonConnectionProps(buffer, msg, m);
				}
			}
			else if (msgPfx.Prefixes.Contains(portPrefix))
			{
				if ((m = portGeneric.Match(msg.Text)).Success)
				{
					UpdateCommonPortProps(buffer, msg, m);
				}
			}
			else if (msgPfx.Prefixes.Contains(audioRecvStreamCtrPrefix) || msgPfx.Prefixes.Contains(audioRecvStreamDtrPrefix))
			{
				HandleStreamCtrDtrMessage(buffer, msg, audioReceiveStreamCtrDtrRe, "audio recv");
				// todo:
				// buffer.Enqueue(new PropertyChange(msg, objid, streamTypeInfo, "local SSRC", m.Groups["local_ssrc"].Value, Analytics.StateInspector.ValueType.Reference));
			}
			else if (msgPfx.Prefixes.Contains(audioSendStreamCtrPrefix) || msgPfx.Prefixes.Contains(audioSendStreamDtrPrefix))
			{
				HandleStreamCtrDtrMessage(buffer, msg, audioSendStreamCtrDtrRe, "audio send");
			}
			else if (msgPfx.Prefixes.Contains(videoRecvStreamCtrPrefix) || msgPfx.Prefixes.Contains(videoRecvStreamDtrPrefix))
			{
				HandleStreamCtrDtrMessage(buffer, msg, videoRecvStreamCtrDtrRe, "video recv");
			}
			else if (msgPfx.Prefixes.Contains(videoSendStreamCtrPrefix) || msgPfx.Prefixes.Contains(videoSendStreamDtrPrefix))
			{
				HandleStreamCtrDtrMessage(buffer, msg, videoSendStreamCtrDtrRe, "video send");
			}
		}

		private void HandleStreamCtrDtrMessage(Queue<Event> buffer, Message msg, Regex re, string type)
		{
			Match m;
			if ((m = re.Match(msg.Text)).Success)
			{
				var objid = m.Groups["id"].Value;
				if (m.Groups["dtr"].Length == 0)
				{
					buffer.Enqueue(new ObjectCreation(msg, objid, streamTypeInfo));
					buffer.Enqueue(new ParentChildRelationChange(msg, objid, streamTypeInfo, streamsRootObjectId));
					buffer.Enqueue(new PropertyChange(msg, objid, streamTypeInfo, "type", type));
					buffer.Enqueue(new PropertyChange(msg, objid, streamTypeInfo, "SSRC", objid));
					PrintJson(JsonLikeStringParser.Parse(m.Groups["json"].Value), buffer, msg, objid, streamTypeInfo, propName: null,
						converter: (node, propName) =>
						{
							if (propName == "extensions" && node is JArray)
							{
								return new JObject(
									from ext in node.OfType<JObject>()
									let id = ext.Property("id")
									let uri = ext.Property("uri")
									where id != null && uri != null
									select new JProperty(id.Value.ToString(), uri.Value)
								);
							}
							return node;
						}, 
						getValueType: propName =>
						{
							if (propName == "local_ssrc")
								return Analytics.StateInspector.ValueType.Reference;
							return Analytics.StateInspector.ValueType.Scalar;
						}
					);
				}
				else
				{
					buffer.Enqueue(new ObjectDeletion(msg, objid, streamTypeInfo));
				}
			}
		}

		static void PrintJson(JToken json, Queue<Event> buffer, Message trigger, string objId, ObjectTypeInfo objType, string propName, Func<JToken, string, JToken> converter, Func<string, Analytics.StateInspector.ValueType> getValueType)
		{
			if (converter != null)
				json = converter(json, propName);
			if (json is JObject || json is JArray)
			{
				if (json.Count() == 0)
					return; // skip empty objects and arrays
				if (propName != null)
				{
					buffer.Enqueue(new ObjectCreation(trigger, propName, propertyNodeTypeInfo));
					buffer.Enqueue(new ParentChildRelationChange(trigger, propName, propertyNodeTypeInfo, objId));
					objId = propName;
					objType = propertyNodeTypeInfo;
				}
				if (json is JObject)
					foreach (var prop in ((JObject)json).Properties())
						PrintJson(prop.Value, buffer, trigger, objId, objType, prop.Name, converter, getValueType);
				else if (json is JArray)
					for (int i = 0; i < json.Count(); ++i)
						PrintJson(json[i], buffer, trigger, objId, objType, string.Format("[{0}]", i), converter, getValueType);
			}
			else if (json is JValue)
			{
				buffer.Enqueue(new PropertyChange(trigger, objId, objType, propName ?? "property", json.ToString(), getValueType(propName)));
			}
		}

		static private IEnumerable<KeyValuePair<string, string>> GetChangedObjectProps(
			Dictionary<string, Dictionary<string, string>> valuesCache, 
			string objId, 
			Match m, IEnumerable<string> regexCaptures)
		{
			Dictionary<string, string> propsCache;
			if (!valuesCache.TryGetValue(objId, out propsCache))
			{
				valuesCache.Add(objId, propsCache = new Dictionary<string, string>());
			}
			foreach (var gName in regexCaptures)
			{
				string val = m.Groups[gName].Value;
				string oldVal;
				if (!propsCache.TryGetValue(gName, out oldVal) || oldVal != val)
				{
					propsCache[gName] = val;
					yield return new KeyValuePair<string, string>(gName, val);
				}
			}
		}

		private string UpdateCommonConnectionProps(Queue<Event> buffer, Message msg, Match m)
		{
			var objId = EnsureConnectionReported(m.Groups["id"].Value, msg, buffer);
			foreach (var prop in GetChangedObjectProps(connectionPropsCache, objId, m, connReGroupNames))
			{
				var name = prop.Key;
				var val = prop.Value;
				var valType = Analytics.StateInspector.ValueType.Scalar;
				switch (prop.Key)
				{
					case "connected": val = val == "C" ? "connected" : "not connected"; break;
					case "receiving": val = val == "R" ? "receiving" : "not receiving"; break;
					case "write_state":
						val = 
							val == "W" ? "WRITABLE" :
							val == "w" ? "WRITE_UNRELIABLE" :
							val == "-" ? "WRITE_INIT" :
							val == "x" ? "WRITE_TIMEOUT" :
							val;
						break;
					case "ice_state":
						val =
							val == "W" ? "WAITING" :
							val == "I" ? "INPROGRESS" :
							val == "S" ? "SUCCEEDED" :
							val == "F" ? "FAILED" :
							val;
						break;
					case "remote_id":
						valType = Analytics.StateInspector.ValueType.Reference;
						name = "remote candidate";
						break;
					case "local_id":
						valType = Analytics.StateInspector.ValueType.Reference;
						name = "local candidate";
						break;
					default:
						if (prop.Key.StartsWith("local_") || prop.Key.StartsWith("remote_"))
							continue; // skip all candidates props except id that is handled in separate case above
						break;
				}
				buffer.Enqueue(new PropertyChange(msg, objId, connectionTypeInfo, name, val, valueType: valType));
			}
			UpdateCommonCandidateProps(buffer, msg, m, "local", objId);
			UpdateCommonCandidateProps(buffer, msg, m, "remote", objId);
			return objId;
		}

		string UpdateCommonPortProps(Queue<Event> buffer, Message msg, Match m)
		{
			var objId = EnsurePortReported(m.Groups["id"].Value, msg, buffer);
			foreach (var prop in GetChangedObjectProps(portPropsCache, objId, m, portReGroupNames))
				buffer.Enqueue(new PropertyChange(msg, objId, connectionTypeInfo, prop.Key, prop.Value));
			return objId;
		}

		string UpdateCommonCandidateProps(Queue<Event> buffer, Message msg, Match m, string candidateSide, string connId)
		{
			var capturesPrefix = candidateSide + "_";
			var saveCandidatesCount = reportedCandidates.Count;
			var idCapture = capturesPrefix + "id";
			var objId = EnsureCandidateReported(m.Groups[idCapture].Value, msg, buffer);
			bool isNewlyCreated = reportedCandidates.Count > saveCandidatesCount;
			if (isNewlyCreated)
				buffer.Enqueue(new PropertyChange(msg, objId, candidateTypeInfo, "side", candidateSide));
			foreach (var prop in GetChangedObjectProps(candidatesPropsCache, objId, m, candidateReGroupNames.Select(g => capturesPrefix + g)))
				if (prop.Key != idCapture)
					buffer.Enqueue(new PropertyChange(msg, objId, candidateTypeInfo, prop.Key.Substring(capturesPrefix.Length), prop.Value));

			HashSet<string> reportedRelations;
			if (!reportedCandidateConnectionRelations.TryGetValue(objId, out reportedRelations))
				reportedCandidateConnectionRelations.Add(objId, reportedRelations = new HashSet<string>());
			if (reportedRelations.Add(connId))
				buffer.Enqueue(new PropertyChange(msg, objId, candidateTypeInfo, 
					string.Format("connection #{0}", reportedRelations.Count), connId, Analytics.StateInspector.ValueType.Reference));
			return objId;
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		void EnsureWebRtcRootReported(Message trigger, Queue<Event> buffer)
		{
			if (rootReported)
				return;
			rootReported = true;

			buffer.Enqueue(new ObjectCreation(trigger, rootObjectId, rootTypeInfo));

			Action<ObjectTypeInfo, string> reportCategoryRoot = (typeInfo, objId) =>
			{
				buffer.Enqueue(new ObjectCreation(trigger, objId, typeInfo));
				buffer.Enqueue(new ParentChildRelationChange(trigger, objId, typeInfo, rootObjectId));
			};

			reportCategoryRoot(sessionsRootTypeInfo, sessionsRootObjectId);
			reportCategoryRoot(connectionsRootTypeInfo, connsRootObjectId);
			reportCategoryRoot(candidatesRootTypeInfo, candidatesRootObjectId);
			reportCategoryRoot(streamsRootTypeInfo, streamsRootObjectId);
			reportCategoryRoot(portsRootTypeInfo, portsRootObjectId);
		}

		string EnsureObjectReported(string id, Message trigger, Queue<Event> buffer, HashSet<string> reportedObjects, ObjectTypeInfo typeInfo, string parentObjectId)
		{
			EnsureWebRtcRootReported(trigger, buffer);
			var objId = id;
			if (!reportedObjects.Add(id))
				return objId;
			buffer.Enqueue(new ObjectCreation(trigger, objId, typeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, objId, typeInfo, parentObjectId));
			return objId;
		}

		string EnsureSessionReported(string sid, Message trigger, Queue<Event> buffer)
		{
			return EnsureObjectReported(sid, trigger, buffer, reportedSessions, sessionTypeInfo, sessionsRootObjectId);
		}

		string EnsureConnectionReported(string id, Message trigger, Queue<Event> buffer)
		{
			return EnsureObjectReported(id, trigger, buffer, reportedConnections, connectionTypeInfo, connsRootObjectId);
		}

		string EnsurePortReported(string id, Message trigger, Queue<Event> buffer)
		{
			return EnsureObjectReported(id, trigger, buffer, reportedPorts, portTypeInfo, portsRootObjectId);
		}

		string EnsureCandidateReported(string id, Message trigger, Queue<Event> buffer)
		{
			return EnsureObjectReported(id, trigger, buffer, reportedCandidates, candidateTypeInfo, candidatesRootObjectId);
		}

		static string[] GetReGroupNames(Regex re)
		{
			return re.GetGroupNames().Where(n => n.Length > 0 && n != "0").Select(n => string.Intern(n)).ToArray();
		}

		#region Mutable state

		bool rootReported = false;
		HashSet<string> reportedSessions = new HashSet<string>();
		HashSet<string> reportedConnections = new HashSet<string>();
		HashSet<string> reportedPorts = new HashSet<string>();
		HashSet<string> reportedCandidates = new HashSet<string>();
		Dictionary<string, Dictionary<string, string>> connectionPropsCache = new Dictionary<string, Dictionary<string, string>>();
		Dictionary<string, Dictionary<string, string>> portPropsCache = new Dictionary<string, Dictionary<string, string>>();
		Dictionary<string, Dictionary<string, string>> candidatesPropsCache = new Dictionary<string, Dictionary<string, string>>();
		Dictionary<string, HashSet<string>> reportedCandidateConnectionRelations = new Dictionary<string, HashSet<string>>();

		#endregion

		#region Constants

		readonly int sessionPrefix, connPrefix, prefixlessConnPrefix, portPrefix, 
			audioRecvStreamCtrPrefix, audioRecvStreamDtrPrefix, audioSendStreamCtrPrefix, audioSendStreamDtrPrefix,
			videoRecvStreamCtrPrefix, videoRecvStreamDtrPrefix, videoSendStreamCtrPrefix, videoSendStreamDtrPrefix;

		const string rootObjectId = "WebRTC";
		const string sessionsRootObjectId = "Sessions";
		const string connsRootObjectId = "Connections";
		const string portsRootObjectId = "Ports";
		const string candidatesRootObjectId = "Candidates";
		const string streamsRootObjectId = "Streams";

		readonly static ObjectTypeInfo rootTypeInfo = new ObjectTypeInfo("webrtc", isTimeless: true);
		readonly static ObjectTypeInfo sessionsRootTypeInfo = new ObjectTypeInfo("webrtc.sessions", isTimeless: true);
		readonly static ObjectTypeInfo connectionsRootTypeInfo = new ObjectTypeInfo("webrtc.conns", isTimeless: true);
		readonly static ObjectTypeInfo portsRootTypeInfo = new ObjectTypeInfo("webrtc.ports", isTimeless: true);
		readonly static ObjectTypeInfo candidatesRootTypeInfo = new ObjectTypeInfo("webrtc.ports", isTimeless: true);
		readonly static ObjectTypeInfo streamsRootTypeInfo = new ObjectTypeInfo("webrtc.streams", isTimeless: true);

		readonly static ObjectTypeInfo sessionTypeInfo = new ObjectTypeInfo("webrtc.session", primaryPropertyName: "state");
		readonly static ObjectTypeInfo connectionTypeInfo = new ObjectTypeInfo("webrtc.conn", primaryPropertyName: "ice_state", displayIdPropertyName: "content_name");
		readonly static ObjectTypeInfo portTypeInfo = new ObjectTypeInfo("webrtc.port", displayIdPropertyName: "content_name", isTimeless: true);
		readonly static ObjectTypeInfo candidateTypeInfo = new ObjectTypeInfo("webrtc.candidate", displayIdPropertyName: "side", isTimeless: true);
		readonly static ObjectTypeInfo streamTypeInfo = new ObjectTypeInfo("webrtc.stream", displayIdPropertyName: "type");
		readonly static ObjectTypeInfo propertyNodeTypeInfo = new ObjectTypeInfo("webrtc.obj_prop", isTimeless: true);

		const string sessionRe = @"^Session:\s*(?<sid>\d+)";
		readonly Regex sessionStateChangeRe = new Regex(sessionRe + @" Old state:(?<old>\w+) New state:(?<new>\w+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex sessionDestroyedRe = new Regex(sessionRe + @" is destroyed", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		readonly static string ipRe = @"(\[[^\]]+?\]:\d+)|([\d+\.:x]+?)";

		readonly static string candidateRePattern = @"(?<{0}id>[^:]+):(?<{0}component>\d+):(?<{0}generation>\d+):(?<{0}type>\w+):(?<{0}proto>\w+):(?<{0}ip>" + ipRe + @")";
		readonly static string connRe = 
			@"Conn\["
			+ @"(?<id>\w+):"
			+ @"(?<content_name>\w+):"
			+ string.Format(candidateRePattern, "local_") 
			+ @"\-\>" + string.Format(candidateRePattern, "remote_")
			+ @"\|(?<connected>[\-C])" 
			+ @"(?<receiving>[\-R])"
			+ @"(?<write_state>[Ww\-x])" 
			+ @"(?<ice_state>[WISF])\|"
			+ @"(?<remote_nomination>\d+)\|" 
			+ @"(?<nomination>\d+)\|" 
			+ @"(?<prio>\d+)\|" 
			+ @"(?<rtt>\-|\d+)"
			+ @"\]";
		readonly Regex connCreatedRe = new Regex("^Jingle:" + connRe + @": Connection created", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex connStateRe = new Regex("^Jingle:" + connRe + @": UpdateState", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex connDumpRe = new Regex("^" + connRe + "$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex connDestroyed = new Regex("^Jingle:" + connRe + ": Connection destroyed", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly string[] connReGroupNames;

		readonly static string networkRe = @"Net\[(?<net>[^\]]*)\]";
		readonly static string portRe = @"^Jingle:Port\[(?<id>\w+):(?<content_name>\w*):(?<component>\d+):(?<generation>\d+):(?<type>\w*):" + networkRe + @"\]: ";
		readonly Regex portCreatedRe = new Regex(portRe + "Port created", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex portGeneric = new Regex(portRe, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly string[] portReGroupNames;

		readonly string[] candidateReGroupNames;

		readonly Regex audioReceiveStreamCtrDtrRe = new Regex(
			@"^(?<dtr>~)?AudioReceiveStream: (?<json>{rtp: {remote_ssrc: (?<id>\d+).+})$", 
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		readonly Regex audioSendStreamCtrDtrRe = new Regex(
			@"^(?<dtr>~)?AudioSendStream: (?<json>{rtp: {ssrc: (?<id>\d+).+})$", 
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		readonly Regex videoRecvStreamCtrDtrRe = new Regex(
			@"^(?<dtr>~)?VideoReceiveStream: (?<json>{.+?remote_ssrc: (?<id>\d+).+})$",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		readonly Regex videoSendStreamCtrDtrRe = new Regex(
			@"^(?<dtr>~)?(?<id>VideoSendStream)Internal: (?<json>{.+})$",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		static readonly HashSet<string> tags = new HashSet<string>() { "webrtc" }; // todo: have constants container class

		#endregion
	}
}

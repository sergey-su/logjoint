using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
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
			rtcSessionPrefix = matcher.RegisterPrefix("Session:");
			jingleConnPrefix = matcher.RegisterPrefix("Jingle:Conn[");
			prefixlessJingleConnPrefix = matcher.RegisterPrefix("Conn[");
			jinglePortPrefix = matcher.RegisterPrefix("Jingle:Port[");

			jingleConnReGroupNames = GetReGroupNames(jingleConnCreatedRe);
			jinglePortReGroupNames = GetReGroupNames(jinglePortGeneric);
		}


		IEnumerableAsync<Event[]> IWebRtcStateInspector.GetEvents(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			return input.Select<MessagePrefixesPair, Event>(GetEvents, GetFinalEvents, e => e.SetTags(tags));
		}

		void GetEvents(MessagePrefixesPair msgPfx, Queue<Event> buffer)
		{
			var msg = msgPfx.Message;
			Match m;
			if (msgPfx.Prefixes.Contains(rtcSessionPrefix))
			{
				if ((m = rtcSessionStateChangeRe.Match(msg.Text)).Success)
				{
					EnsureWebRtcRootReported(msg, buffer);
					var objId = EnsureSessionReported(m.Groups["sid"].Value, msg, buffer);
					buffer.Enqueue(new PropertyChange(msg, objId, webRtcSessionTypeInfo, "state", m.Groups["new"].Value));
				}
				else if ((m = rtcSessionDestroyedRe.Match(msg.Text)).Success)
				{
					EnsureWebRtcRootReported(msg, buffer);
					var objId = EnsureSessionReported(m.Groups["sid"].Value, msg, buffer);
					buffer.Enqueue(new ObjectDeletion(msg, objId, webRtcSessionTypeInfo));
				}
			}
			else if (msgPfx.Prefixes.Contains(jingleConnPrefix))
			{
				if ((m = jingleConnStateRe.Match(msg.Text)).Success)
				{
					UpdateCommonJingleConnProps(buffer, msg, m);
				}
				else if ((m = jingleConnCreatedRe.Match(msg.Text)).Success)
				{
					UpdateCommonJingleConnProps(buffer, msg, m);
				}
			}
			else if (msgPfx.Prefixes.Contains(prefixlessJingleConnPrefix))
			{
				if ((m = jingleConnDumpRe.Match(msg.Text)).Success)
				{
					UpdateCommonJingleConnProps(buffer, msg, m);
				}
			}
			else if (msgPfx.Prefixes.Contains(jinglePortPrefix))
			{
				if ((m = jinglePortGeneric.Match(msg.Text)).Success)
				{
					UpdateCommonJinglePortProps(buffer, msg, m);
				}
			}
		}

		private string UpdateCommonJingleConnProps(Queue<Event> buffer, Message msg, Match m)
		{
			var objId = EnsureConnectionReported(m.Groups["id"].Value, msg, buffer);
			Dictionary<string, string> propsCache;
			if (!connectionPropsCache.TryGetValue(objId, out propsCache))
			{
				connectionPropsCache.Add(objId, propsCache = new Dictionary<string, string>());
			}
			foreach (var gName in jingleConnReGroupNames)
			{
				string val = m.Groups[gName].Value;
				string oldVal;
				if (!propsCache.TryGetValue(gName, out oldVal) || oldVal != val)
				{
					propsCache[gName] = val;
					switch (gName)
					{
						case "connected": val = val == "C" ? "connected" : "not connected"; break;
						case "receiving": val = val == "C" ? "receiving" : "not receiving"; break;
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
					}
					buffer.Enqueue(new PropertyChange(msg, objId, jingleConnTypeInfo, gName, val));
				}
			}
			return objId;
		}

		string UpdateCommonJinglePortProps(Queue<Event> buffer, Message msg, Match m)
		{
			var objId = EnsurePortReported(m.Groups["id"].Value, msg, buffer);
			Dictionary<string, string> propsCache;
			if (!portPropsCache.TryGetValue(objId, out propsCache))
			{
				portPropsCache.Add(objId, propsCache = new Dictionary<string, string>());
			}
			foreach (var gName in jinglePortReGroupNames)
			{
				string val = m.Groups[gName].Value;
				string oldVal;
				if (!propsCache.TryGetValue(gName, out oldVal) || oldVal != val)
				{
					propsCache[gName] = val;
					buffer.Enqueue(new PropertyChange(msg, objId, jingleConnTypeInfo, gName, val));
				}
			}
			return objId;
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		void EnsureWebRtcRootReported(Message trigger, Queue<Event> buffer)
		{
			if (webRtcRootReported)
				return;
			webRtcRootReported = true;
			buffer.Enqueue(new ObjectCreation(trigger, webRtcRootObjectId, webRtcRootTypeInfo));

			buffer.Enqueue(new ObjectCreation(trigger, webRtcSessionsRootObjectId, rtcSessionsRootTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, webRtcSessionsRootObjectId, rtcSessionsRootTypeInfo, webRtcRootObjectId));

			buffer.Enqueue(new ObjectCreation(trigger, webRtcConnsRootObjectId, rtcConnectionsRootTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, webRtcConnsRootObjectId, rtcConnectionsRootTypeInfo, webRtcRootObjectId));

			buffer.Enqueue(new ObjectCreation(trigger, webRtcPortsRootObjectId, rtcPortsRootTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, webRtcPortsRootObjectId, rtcPortsRootTypeInfo, webRtcRootObjectId));
		}

		string EnsureSessionReported(string sid, Message trigger, Queue<Event> buffer)
		{
			var objId = sid;
			if (!reportedSessions.Add(sid))
				return objId;
			buffer.Enqueue(new ObjectCreation(trigger, objId, webRtcSessionTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, objId, webRtcSessionTypeInfo, webRtcSessionsRootObjectId));
			return objId;
		}

		string EnsureConnectionReported(string id, Message trigger, Queue<Event> buffer)
		{
			var objId = id;
			if (!reportedConnections.Add(id))
				return objId;
			buffer.Enqueue(new ObjectCreation(trigger, objId, jingleConnTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, objId, jingleConnTypeInfo, webRtcConnsRootObjectId));
			return objId;
		}

		string EnsurePortReported(string id, Message trigger, Queue<Event> buffer)
		{
			var objId = id;
			if (!reportedPorts.Add(id))
				return objId;
			buffer.Enqueue(new ObjectCreation(trigger, objId, jinglePortTypeInfo));
			buffer.Enqueue(new ParentChildRelationChange(trigger, objId, jinglePortTypeInfo, webRtcPortsRootObjectId));
			return objId;
		}

		static string[] GetReGroupNames(Regex re)
		{
			return re.GetGroupNames().Where(n => n.Length > 0 && n != "0").Select(n => string.Intern(n)).ToArray();
		}

		#region Mutable state

		bool webRtcRootReported = false;
		HashSet<string> reportedSessions = new HashSet<string>();
		HashSet<string> reportedConnections = new HashSet<string>();
		HashSet<string> reportedPorts = new HashSet<string>();
		Dictionary<string, Dictionary<string, string>> connectionPropsCache = new Dictionary<string, Dictionary<string, string>>();
		Dictionary<string, Dictionary<string, string>> portPropsCache = new Dictionary<string, Dictionary<string, string>>();

		#endregion

		#region Constants

		readonly int rtcSessionPrefix, jingleConnPrefix, prefixlessJingleConnPrefix, jinglePortPrefix;

		readonly string webRtcRootObjectId = "WebRTC";
		readonly string webRtcSessionsRootObjectId = "Sessions";
		readonly string webRtcConnsRootObjectId = "Connections";
		readonly string webRtcPortsRootObjectId = "Ports";
		readonly ObjectTypeInfo webRtcRootTypeInfo = new ObjectTypeInfo("webrtc", isTimeless: true);
		readonly ObjectTypeInfo rtcSessionsRootTypeInfo = new ObjectTypeInfo("webrtc.sessions", isTimeless: true);
		readonly ObjectTypeInfo rtcConnectionsRootTypeInfo = new ObjectTypeInfo("webrtc.conns", isTimeless: true);
		readonly ObjectTypeInfo rtcPortsRootTypeInfo = new ObjectTypeInfo("webrtc.ports", isTimeless: true);
		readonly ObjectTypeInfo webRtcSessionTypeInfo = new ObjectTypeInfo("webrtc.session", primaryPropertyName: "state");
		readonly ObjectTypeInfo jingleConnTypeInfo = new ObjectTypeInfo("webrtc.conn", primaryPropertyName: "ice_state", displayIdPropertyName: "content_name");
		readonly ObjectTypeInfo jinglePortTypeInfo = new ObjectTypeInfo("webrtc.port", displayIdPropertyName: "content_name");

		const string rtcSessionRe = @"^Session:\s*(?<sid>\d+)";
		readonly Regex rtcSessionStateChangeRe = new Regex(rtcSessionRe + @" Old state:(?<old>\w+) New state:(?<new>\w+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex rtcSessionDestroyedRe = new Regex(rtcSessionRe + @" is destroyed", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		readonly static string ipRe = @"\[[^\]]+?\]:\d+";

		readonly static string jingleConnPartyRe = @"(?<{0}_id>\w+):(?<{0}_component>\d+):(?<{0}_generation>\d+):(?<{0}_type>\w+):(?<{0}_proto>\w+):(?<{0}_ip>" + ipRe + @")";
		readonly static string jingleConnRe = 
			@"Conn\["
			+ @"(?<id>\w+):"
			+ @"(?<content_name>\w+):"
			+ string.Format(jingleConnPartyRe, "local") 
			+ @"\-\>" + string.Format(jingleConnPartyRe, "remote")
			+ @"\|(?<connected>[\-C])" 
			+ @"(?<receiving>[\-R])"
			+ @"(?<write_state>[Ww\-x])" 
			+ @"(?<ice_state>[WISF])\|"
			+ @"(?<remote_nomination>\d+)\|" 
			+ @"(?<nomination>\d+)\|" 
			+ @"(?<prio>\d+)\|" 
			+ @"(?<rtt>\-|\d+)"
			+ @"\]";
		readonly Regex jingleConnCreatedRe = new Regex("^Jingle:" + jingleConnRe + @": Connection created", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex jingleConnStateRe = new Regex("^Jingle:" + jingleConnRe + @": UpdateState", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex jingleConnDumpRe = new Regex("^" + jingleConnRe + "$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly string[] jingleConnReGroupNames;

		readonly static string networkRe = @"Net\[(?<net>[^\]]*)\]";
		readonly static string jinglePortRe = @"^Jingle:Port\[(?<id>\w+):(?<content_name>\w*):(?<component>\d+):(?<generation>\d+):(?<type>\w*):" + networkRe + @"\]: ";
		readonly Regex jinglePortCreatedRe = new Regex(jinglePortRe + "Port created", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly Regex jinglePortGeneric = new Regex(jinglePortRe, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		readonly string[] jinglePortReGroupNames;

		static readonly HashSet<string> tags = new HashSet<string>() { "webrtc" }; // todo: have constants container

		#endregion
	}
}

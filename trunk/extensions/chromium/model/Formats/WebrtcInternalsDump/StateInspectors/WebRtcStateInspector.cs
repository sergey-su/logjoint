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
			Match m;
			EnsureWebRtcRootReported(msg, buffer);
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
		}
		
		#region Mutable state

		bool rootReported = false;

		#endregion

		#region Constants

		const string rootObjectId = "WebRTC";
		const string sessionsRootObjectId = "Sessions";
		const string connsRootObjectId = "Connections";
		const string candidatesRootObjectId = "Candidates";
		const string streamsRootObjectId = "Streams";

		readonly static ObjectTypeInfo rootTypeInfo = new ObjectTypeInfo("webrtc", isTimeless: true);
		readonly static ObjectTypeInfo sessionsRootTypeInfo = new ObjectTypeInfo("webrtc.sessions", isTimeless: true);
		readonly static ObjectTypeInfo connectionsRootTypeInfo = new ObjectTypeInfo("webrtc.conns", isTimeless: true);
		readonly static ObjectTypeInfo candidatesRootTypeInfo = new ObjectTypeInfo("webrtc.ports", isTimeless: true);
		readonly static ObjectTypeInfo streamsRootTypeInfo = new ObjectTypeInfo("webrtc.streams", isTimeless: true);

		readonly static ObjectTypeInfo sessionTypeInfo = new ObjectTypeInfo("webrtc.session", primaryPropertyName: "state");
		readonly static ObjectTypeInfo connectionTypeInfo = new ObjectTypeInfo("webrtc.conn", primaryPropertyName: "ice_state", displayIdPropertyName: "content_name");
		readonly static ObjectTypeInfo candidateTypeInfo = new ObjectTypeInfo("webrtc.candidate", displayIdPropertyName: "side", isTimeless: true);
		readonly static ObjectTypeInfo streamTypeInfo = new ObjectTypeInfo("webrtc.stream", displayIdPropertyName: "type");
		readonly static ObjectTypeInfo propertyNodeTypeInfo = new ObjectTypeInfo("webrtc.obj_prop", isTimeless: true);

		#endregion
	}
}

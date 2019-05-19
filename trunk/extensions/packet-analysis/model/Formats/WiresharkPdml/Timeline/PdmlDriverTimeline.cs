using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LogJoint.Wireshark.Dpml
{
	public interface ITimelineEvents
	{
		IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<Message[]> input);
	};

	public class TimelineEvents : ITimelineEvents
	{
		public TimelineEvents()
		{
		}


		IEnumerableAsync<Event[]> ITimelineEvents.GetEvents(IEnumerableAsync<Message[]> input)
		{
			return input.Select<Message, Event>(GetEvents, GetFinalEvents);
		}

		void GetEvents(Message msg, Queue<Event> buffer)
		{
			string makeHttpActivityId(string requestFrameNr) => $"http-{requestFrameNr}";
			string makeTcpActivityId(string streamId) => $"tcp-{streamId}";
			string makeTcpTag(string streamId) => $"tcp-{streamId}";
			string makeDnsActivityId(string requestFrameNr) => $"dns-{requestFrameNr}";

			string tcpStreamId = null;
			if (msg.Protos.TryGetValue("tcp", out var tcp))
			{
				if (tcp.Fields.TryGetValue("tcp.stream", out var streamId))
				{
					tcpStreamId = streamId.Show;
					var tcpActivityId = makeTcpActivityId(tcpStreamId);
					if (!lasteSeenTcpMessages.ContainsKey(tcpActivityId))
					{
						lasteSeenTcpMessages.Add(tcpActivityId, msg);
						var tags = new HashSet<string>
						{
							"tcp",
							makeTcpTag(tcpStreamId)
						};
						string comment = "";
						if (msg.Protos.TryGetValue("ip", out var ip)
						&& ip.Fields.TryGetValue("ip.dst_host", out var host)
						&& tcp.Fields.TryGetValue("tcp.dstport", out var port))
						{
							comment = $" ({host.Show}:{port.Show})";
						}
						buffer.Enqueue(new NetworkMessageEvent(
							msg, $"tcp stream {tcpStreamId}{comment}", tcpActivityId,
							ActivityEventType.Begin, NetworkMessageDirection.Outgoing).SetTags(tags));
					}
					else
					{
						lasteSeenTcpMessages[tcpActivityId] = msg;
					}
				}
			}
			if (msg.Protos.TryGetValue("http", out var http))
			{
				var tags = new HashSet<string>();
				tags.Add("http");
				if (tcpStreamId != null)
					tags.Add(makeTcpTag(tcpStreamId));
				if (http.Fields.TryGetValue("http.request.uri", out var uri)
				 && http.Fields.TryGetValue("http.request_number", out var rqNumer))
				{
					string displayName = uri.Show;
					if (http.Fields.TryGetValue("http.host", out var host))
						tags.Add(host.Show);
					if (http.Fields.TryGetValue("http.request.method", out var method))
						displayName = $"{method.Show} {displayName}";
					buffer.Enqueue(new NetworkMessageEvent(
						msg, displayName, makeHttpActivityId(msg.FrameNum.ToString()), ActivityEventType.Begin, NetworkMessageDirection.Outgoing).SetTags(tags));
					if (tcpStreamId != null)
					{
						buffer.Enqueue(new NetworkMessageEvent(
							msg, $"http request {displayName}", makeTcpActivityId(tcpStreamId),
							ActivityEventType.Milestone, NetworkMessageDirection.Outgoing));
					}
				}
				else if (http.Fields.TryGetValue("http.request_in", out var requestFrameNum))
				{
					var activityStatus = ActivityStatus.Unspecified;
					if (http.Fields.TryGetValue("http.response.code", out var statusField) && int.TryParse(statusField.Show, out var status) && status > 200)
						activityStatus = ActivityStatus.Error;
					buffer.Enqueue(new NetworkMessageEvent(
						msg, "", makeHttpActivityId(requestFrameNum.Show), ActivityEventType.End, NetworkMessageDirection.Outgoing, status: activityStatus).SetTags(tags));
				}
			}
			if (msg.Protos.TryGetValue("dns", out var dns))
			{
				var tags = new HashSet<string>
				{
					"dns"
				};
				if (dns.Fields.TryGetValue("dns.response_to", out var rq))
				{
					buffer.Enqueue(new NetworkMessageEvent(
						msg, "", makeDnsActivityId(rq.Show),
						ActivityEventType.End, NetworkMessageDirection.Outgoing).SetTags(tags));
				}
				else if (dns.Fields.ContainsKey("dns.response_in") && dns.Fields.TryGetValue("dns.qry.name", out var name))
				{
					buffer.Enqueue(new NetworkMessageEvent(
						msg, $"dns {name.Show}", makeDnsActivityId(msg.FrameNum.ToString()),
						ActivityEventType.Begin, NetworkMessageDirection.Outgoing).SetTags(tags));
				}

			}
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
			YieldFinalTcpEvents(buffer);
		}

		private void YieldFinalTcpEvents(Queue<Event> buffer)
		{
			foreach (var tcp in lasteSeenTcpMessages)
			{
				buffer.Enqueue(new NetworkMessageEvent(
					tcp.Value, "", tcp.Key,
					ActivityEventType.End, NetworkMessageDirection.Outgoing));
			}
		}

		Dictionary<string, Message> lasteSeenTcpMessages = new Dictionary<string, Message>();
	}
}

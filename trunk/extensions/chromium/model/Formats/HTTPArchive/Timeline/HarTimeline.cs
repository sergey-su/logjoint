using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace LogJoint.Chromium.HttpArchive
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
			if (msg.ObjectType == Message.ENTRY)
			{
				switch (msg.MessageType.Value)
				{
					case Message.START:
						HashSet<string> tags = null;
						string displayName = msg.Text;
						if (parser.TryParseStart(msg.Text, out var start))
						{
							if (Uri.TryCreate(start.Url, UriKind.Absolute, out var uri))
								tags = GetTags(uri.Host);
							displayName = string.Format("{0} {1}", start.Method, start.Url);
						}
						var startEvt = new NetworkMessageEvent(msg, displayName, msg.ObjectId, ActivityEventType.Begin, NetworkMessageDirection.Outgoing);
						if (tags != null)
							startEvt.SetTags(tags);
						buffer.Enqueue(startEvt);
						phases[msg.ObjectId.Value] = new EntryPhases()
						{
							list = new List<ActivityPhase>(),
							lastPhaseTs = msg.Timestamp
						};
						break;
					case Message.END:
						ActivityStatus status;
						statuses.TryGetValue(msg.ObjectId.Value, out status);
						var endEvt = new NetworkMessageEvent(msg, msg.Text, msg.ObjectId, ActivityEventType.End, NetworkMessageDirection.Outgoing, status: status);
						EntryPhases entryPhases;
						if (phases.TryGetValue(msg.ObjectId.Value, out entryPhases))
						{
							endEvt.Phases = entryPhases.list;
							phases.Remove(msg.ObjectId.Value);
						}
						buffer.Enqueue(endEvt);
						break;
					case Message.BLOCKED:
						RecordPhase(msg, 2);
						break;
					case Message.DNS:
						RecordPhase(msg, 0);
						break;
					case Message.CONNECT:
						RecordPhase(msg, 4);
						break;
					case Message.SEND:
						RecordPhase(msg, 3);
						break;
					case Message.WAIT:
						RecordPhase(msg, 5);
						break;
					case Message.SSL:
						RecordPhase(msg, 1);
						break;
					case Message.RECEIVE:
						RecordStatus(msg);
						break;
				}
			}
		}

		void RecordPhase(Message msg, int phaseType)
		{
			EntryPhases entryPhases;
			if (phases.TryGetValue(msg.ObjectId.Value, out entryPhases))
			{
				var b = entryPhases.list.Count == 0 ? TimeSpan.Zero : entryPhases.list.Last().End;
				var e = b + (msg.Timestamp - entryPhases.lastPhaseTs);
				entryPhases.lastPhaseTs = msg.Timestamp;
				entryPhases.list.Add(new ActivityPhase(b, e, phaseType, msg.MessageType));
			}
		}

		void RecordStatus(Message msg)
		{
			statuses[msg.ObjectId.Value] = (msg.Severity == "W" || msg.Severity == "E") ? ActivityStatus.Error : ActivityStatus.Unspecified;
		}

		void GetFinalEvents(Queue<Event> buffer)
		{
		}

		HashSet<string> GetTags(string host)
		{
			HashSet<string> tags;
			if (!tagsCache.TryGetValue(host, out tags))
			{
				tagsCache.Add(host, tags = new HashSet<string>());
				tags.Add(host);
			}
			return tags;
		}

		class EntryPhases
		{
			public List<ActivityPhase> list;
			public DateTime lastPhaseTs;
		};

		readonly Dictionary<string, EntryPhases> phases = new Dictionary<string, EntryPhases>();
		readonly Dictionary<string, ActivityStatus> statuses = new Dictionary<string, ActivityStatus>();
		readonly Dictionary<string, HashSet<string>> tagsCache = new Dictionary<string, HashSet<string>>();
		readonly Parser parser = new Parser();
	}
}

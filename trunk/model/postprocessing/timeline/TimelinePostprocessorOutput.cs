using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Threading;

namespace LogJoint.Postprocessing.Timeline
{
	class TimelinePostprocessorOutput: ITimelinePostprocessorOutput
	{
		public TimelinePostprocessorOutput(LogSourcePostprocessorDeserializationParams p, ILogPartTokenFactories logPartTokenFactories) :
			this(p, TimelineEntitiesComparer.Instance, logPartTokenFactories)
		{ }

		public TimelinePostprocessorOutput(LogSourcePostprocessorDeserializationParams p, IEntitiesComparer entitiesComparer, ILogPartTokenFactories logPartTokenFactories)
		{
			this.logSource = p.LogSource;

			if (!p.Reader.ReadToFollowing("root"))
				throw new FormatException();
			etag.Read(p.Reader);

			var eventsDeserializer = new EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
			var events = new List<Event>();
			foreach (var elt in p.Reader.ReadChildrenElements())
			{
				if (eventsDeserializer.TryDeserialize(elt, out var evt))
					events.Add(evt);
				else if (logPartTokenFactories.TryReadLogPartToken(elt, out var tmp))
					this.rotatedLogPartToken = tmp;
				p.Cancellation.ThrowIfCancellationRequested();
			}
			this.timelineEvents = events.AsReadOnly();
		}

		public static Task SerializePostprocessorOutput(
			IEnumerableAsync<Event[]> events,
			Task<ILogPartToken> rotatedLogPartToken,
			ILogPartTokenFactories logPartTokenFactories,
			Func<object, TextLogEventTrigger> triggersConverter,
			string contentsEtagAttr,
			Func<Task<Stream>> openOutputStream,
			ITempFilesManager tempFiles,
			CancellationToken cancellation
		)
		{
			return events.SerializePostprocessorOutput<Event, EventsSerializer, IEventsVisitor>(
				triggerSerializer => new EventsSerializer(triggerSerializer),
				rotatedLogPartToken,
				logPartTokenFactories,
				triggersConverter,
				contentsEtagAttr,
				"root",
				openOutputStream,
				tempFiles,
				cancellation
			);
		}


		ILogSource ITimelinePostprocessorOutput.LogSource { get { return logSource; } }

		IList<Event> ITimelinePostprocessorOutput.TimelineEvents { get { return timelineEvents; } }

		TimeSpan ITimelinePostprocessorOutput.TimelineOffset { get { return timelineOffset; } }
		void ITimelinePostprocessorOutput.SetTimelineOffset(TimeSpan value) { timelineOffset = value; }

		string ITimelinePostprocessorOutput.SequenceDiagramName { get { return sequenceDiagramName; } }
		void ITimelinePostprocessorOutput.SetSequenceDiagramName(string value) { sequenceDiagramName = value; }

		ILogPartToken ITimelinePostprocessorOutput.RotatedLogPartToken { get { return rotatedLogPartToken; } }

		string IPostprocessorOutputETag.ETag { get { return etag.Value; } }

		readonly ILogSource logSource;
		readonly ILogPartToken rotatedLogPartToken = new NullLogPartToken();
		readonly PostprocessorOutputETag etag;
		readonly IList<Event> timelineEvents;
		TimeSpan timelineOffset;
		string sequenceDiagramName;

		const string rotatedLogPartTokenEltName = "rotatedLogPartToken";
	};
}

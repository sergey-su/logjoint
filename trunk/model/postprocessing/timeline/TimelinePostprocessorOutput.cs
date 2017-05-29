using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Timeline
{
	public class TimelinePostprocessorOutput: ITimelinePostprocessorOutput
	{
		public TimelinePostprocessorOutput(XDocument doc, ILogSource logSource, ILogPartTokenFactory rotatedLogPartFactory = null) :
			this(doc, logSource, TimelineEntitiesComparer.Instance, rotatedLogPartFactory)
		{ }

		public TimelinePostprocessorOutput(XDocument doc, ILogSource logSource, IEntitiesComparer entitiesComparer, ILogPartTokenFactory rotatedLogPartFactory)
		{
			this.logSource = logSource;
			var eventsDeserializer = new EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
			this.timelineEvents = eventsDeserializer.Deserialize(doc.Root).ToList().AsReadOnly();
			this.rotatedLogPartToken = rotatedLogPartFactory.SafeDeserializeLogPartToken(doc.Root);
		}

		public static XDocument SerializePostprocessorOutput(
			List<Event> events,
			ILogPartToken rotatedLogPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			XAttribute contentsEtagAttr
		)
		{
			var serializer = new EventsSerializer((trigger, elt) =>
			{
				triggersConverter(trigger).Save(elt);
			});
			foreach (var e in events.OrderBy(e => ((IOrderedTrigger)e.Trigger).Index))
				e.Visit(serializer);
			var root = new XElement("root", serializer.Output);
			rotatedLogPartToken.SafeSerializeLogPartToken(root);
			if (contentsEtagAttr != null)
				root.Add(contentsEtagAttr);
			return new XDocument(root);
		}


		ILogSource ITimelinePostprocessorOutput.LogSource { get { return logSource; } }

		IList<Event> ITimelinePostprocessorOutput.TimelineEvents { get { return timelineEvents; } }

		TimeSpan ITimelinePostprocessorOutput.TimelineOffset { get { return timelineOffset; } }
		void ITimelinePostprocessorOutput.SetTimelineOffset(TimeSpan value) { timelineOffset = value; }

		string ITimelinePostprocessorOutput.SequenceDiagramName { get { return sequenceDiagramName; } }
		void ITimelinePostprocessorOutput.SetSequenceDiagramName(string value) { sequenceDiagramName = value; }

		ILogPartToken ITimelinePostprocessorOutput.RotatedLogPartToken { get { return rotatedLogPartToken; } }

		readonly ILogSource logSource;
		readonly ILogPartToken rotatedLogPartToken;
		readonly IList<Event> timelineEvents;
		TimeSpan timelineOffset;
		string sequenceDiagramName;

		const string rotatedLogPartTokenEltName = "rotatedLogPartToken";
	};
}

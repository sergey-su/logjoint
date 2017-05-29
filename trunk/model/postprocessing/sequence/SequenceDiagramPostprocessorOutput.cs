using System;
using System.Collections.Generic;
using System.Linq;
using M = LogJoint.Analytics.Messaging;
using TLBlock = LogJoint.Analytics.Timeline;
using SIBlock = LogJoint.Analytics.StateInspector;
using System.Xml.Linq;
using LogJoint.Analytics;

namespace LogJoint.Postprocessing.SequenceDiagram
{
	public class SequenceDiagramPostprocessorOutput: ISequenceDiagramPostprocessorOutput
	{
		public SequenceDiagramPostprocessorOutput(XDocument doc, ILogSource logSource, ILogPartTokenFactory rotatedLogPartFactory)
		{
			this.logSource = logSource;

			this.events = (new M.EventsDeserializer(TextLogEventTrigger.DeserializerFunction)).Deserialize(
				doc.Root).ToList();
			this.timelineComments = (new TLBlock.EventsDeserializer(TextLogEventTrigger.DeserializerFunction)).Deserialize(
				doc.Root.Element("timeline-comments") ?? new XElement("dummy")).ToList();
			this.stateComments = (new SIBlock.EventsDeserializer(TextLogEventTrigger.DeserializerFunction)).Deserialize(
				doc.Root.Element("state-comments") ?? new XElement("dummy")).ToList();
			this.rotatedLogPartToken = rotatedLogPartFactory.SafeDeserializeLogPartToken(doc.Root);
		}

		public static XDocument SerializePostprocessorOutput(
			List<M.Event> events,
			List<TLBlock.Event> timelineComments,
			List<SIBlock.Event> stateInsectorComments,
			ILogPartToken logPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			XAttribute contentsEtagAttr
		)
		{
			Action<object, XElement> triggerSerializer = (trigger, elt) =>
			{
				triggersConverter(trigger).Save(elt);
			};
			var messagingEventsSerializer = new M.EventsSerializer(triggerSerializer);
			foreach (var e in events)
				e.Visit(messagingEventsSerializer);
			var root = new XElement("root", messagingEventsSerializer.Output);

			var timelineEventsSerializer = new TLBlock.EventsSerializer(triggerSerializer);
			foreach (var e in timelineComments)
				e.Visit(timelineEventsSerializer);
			root.Add(new XElement("timeline-comments", timelineEventsSerializer.Output));

			var stateInspectorEventsSerializer = new SIBlock.EventsSerializer(triggerSerializer);
			foreach (var e in stateInsectorComments)
				e.Visit(stateInspectorEventsSerializer);
			root.Add(new XElement("state-comments", stateInspectorEventsSerializer.Output));

			logPartToken.SafeSerializeLogPartToken(root);

			if (contentsEtagAttr != null)
				root.Add(contentsEtagAttr);

			return new XDocument(root);
		}

		ILogSource ISequenceDiagramPostprocessorOutput.LogSource { get { return logSource; } }

		IEnumerable<M.Event> ISequenceDiagramPostprocessorOutput.Events { get { return events; } }

		IEnumerable<TLBlock.Event> ISequenceDiagramPostprocessorOutput.TimelineComments { get { return timelineComments; } }

		IEnumerable<SIBlock.Event> ISequenceDiagramPostprocessorOutput.StateComments { get { return stateComments; } }

		ILogPartToken ISequenceDiagramPostprocessorOutput.RotatedLogPartToken { get { return rotatedLogPartToken; } }

		readonly ILogSource logSource;
		readonly List<M.Event> events;
		readonly List<TLBlock.Event> timelineComments;
		readonly List<SIBlock.Event> stateComments;
		readonly ILogPartToken rotatedLogPartToken;
	};
}

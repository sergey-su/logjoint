using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;

namespace LogJoint.Postprocessing.StateInspector
{
	public class StateInspectorOutput : IStateInspectorOutput
	{
		public StateInspectorOutput(XDocument doc, ILogSource logSource, ILogPartTokenFactory rotatedLogPartFactory = null)
		{
			this.logSource = logSource;
			var eventsDeserializer = new EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
			this.events = eventsDeserializer
				.Deserialize(doc.Root)
				.ToList();
			this.rotatedLogPartToken = rotatedLogPartFactory.SafeDeserializeLogPartToken(doc.Root);
		}

		public static XDocument SerializePostprocessorOutput(
			List<Event> events,
			ILogPartToken rotatedLogPartToken,
			XAttribute contentsEtagAttr
		)
		{
			var serializer = new EventsSerializer((trigger, elt) => TextLogEventTrigger.FromUnknownTrigger(trigger).Save(elt));
			foreach (var e in events.OrderBy(e => ((ITriggerStreamPosition)e.Trigger).StreamPosition))
				e.Visit(serializer);
			var root = new XElement("root", serializer.Output);
			rotatedLogPartToken.SafeSerializeLogPartToken(root);
			if (contentsEtagAttr != null)
				root.Add(contentsEtagAttr);
			return new XDocument(root);
		}

		ILogSource IStateInspectorOutput.LogSource
		{
			get { return logSource; }
		}

		IList<Event> IStateInspectorOutput.Events
		{
			get { return events; }
		}

		ILogPartToken IStateInspectorOutput.RotatedLogPartToken
		{
			get { return rotatedLogPartToken; }
		}

		readonly ILogSource logSource;
		readonly List<Event> events;
		readonly ILogPartToken rotatedLogPartToken;
	};
}

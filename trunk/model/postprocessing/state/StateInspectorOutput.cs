using System;
using System.Collections.Generic;
using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.StateInspector
{
	public class StateInspectorOutput : IStateInspectorOutput
	{
		public StateInspectorOutput(XmlReader reader, ILogSource logSource, ILogPartTokenFactory rotatedLogPartFactory = null)
		{
			this.logSource = logSource;

			if (!reader.ReadToFollowing("root"))
				throw new FormatException();
			etag.Read(reader);

			var eventsDeserializer = new EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
			foreach (var elt in reader.ReadChildrenElements())
			{
				if (eventsDeserializer.TryDeserialize(elt, out var evt))
					events.Add(evt);
				else if (rotatedLogPartFactory.TryReadLogPartToken(elt, out var tmp))
					this.rotatedLogPartToken = tmp;
			}
		}

		public static Task SerializePostprocessorOutput(
			IEnumerableAsync<Event[]> events,
			Task<ILogPartToken> rotatedLogPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			string contentsEtagAttr,
			string outputFileName,
			ITempFilesManager tempFiles,
			CancellationToken cancellation
		)
		{
			return events.SerializePostprocessorOutput<Event, EventsSerializer, IEventsVisitor>(
				triggerSerializer => new EventsSerializer(triggerSerializer),
				rotatedLogPartToken,
				triggersConverter,
				contentsEtagAttr,
				"root",
				outputFileName,
				tempFiles,
				cancellation
			);
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

		string IPostprocessorOutputETag.ETag { get { return etag.Value; } }

		readonly ILogSource logSource;
		readonly List<Event> events = new List<Event>();
		readonly ILogPartToken rotatedLogPartToken = new NullLogPartToken();
		readonly PostprocessorOutputETag etag;
	};
}

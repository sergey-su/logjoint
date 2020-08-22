using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.StateInspector
{
	class StateInspectorOutput : IStateInspectorOutput
	{
		public StateInspectorOutput(LogSourcePostprocessorDeserializationParams p, ILogPartTokenFactories rotatedLogPartFactories)
		{
			this.logSource = p.LogSource;

			if (!p.Reader.ReadToFollowing("root"))
				throw new FormatException();
			etag.Read(p.Reader);

			var eventsDeserializer = new EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
			foreach (var elt in p.Reader.ReadChildrenElements())
			{
				if (eventsDeserializer.TryDeserialize(elt, out var evt))
					events.Add(evt);
				else if (rotatedLogPartFactories.TryReadLogPartToken(elt, out var tmp))
					this.rotatedLogPartToken = tmp;
				p.Cancellation.ThrowIfCancellationRequested();
			}
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using M = LogJoint.Postprocessing.Messaging;
using TLBlock = LogJoint.Postprocessing.Timeline;
using SIBlock = LogJoint.Postprocessing.StateInspector;
using System.Xml.Linq;
using System.Xml;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.Postprocessing.SequenceDiagram
{
	class SequenceDiagramPostprocessorOutput: ISequenceDiagramPostprocessorOutput
	{
		public SequenceDiagramPostprocessorOutput(LogSourcePostprocessorDeserializationParams p, ILogPartTokenFactory rotatedLogPartFactory)
		{
			this.logSource = p.LogSource;
			var reader = p.Reader;

			events = new List<M.Event>();
			timelineComments = new List<TLBlock.Event>();
			stateComments = new List<SIBlock.Event>();
			rotatedLogPartToken = new NullLogPartToken();

			if (!reader.ReadToFollowing("root"))
				throw new FormatException();
			etag.Read(reader);

			if (reader.ReadToFollowing(messagingEventsElementName))
			{
				var eventsDeserializer = new M.EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
				foreach (var elt in reader.ReadChildrenElements())
					if (eventsDeserializer.TryDeserialize(elt, out var evt))
						events.Add(evt);
			}
			if (reader.ReadToFollowing(timelineCommentsElementName))
			{
				var eventsDeserializer = new TLBlock.EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
				foreach (var elt in reader.ReadChildrenElements())
					if (eventsDeserializer.TryDeserialize(elt, out var evt))
						timelineComments.Add(evt);
			}
			if (reader.ReadToFollowing(stateCommentsElementName))
			{
				var eventsDeserializer = new SIBlock.EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
				foreach (var elt in reader.ReadChildrenElements())
					if (eventsDeserializer.TryDeserialize(elt, out var evt))
						stateComments.Add(evt);
			}
		}

		public static async Task SerializePostprocessorOutput(
			IEnumerableAsync<M.Event[]> events,
			IEnumerableAsync<TLBlock.Event[]> timelineComments,
			IEnumerableAsync<SIBlock.Event[]> stateInspectorComments,
			Task<ILogPartToken> logPartToken,
			ILogPartTokenFactories logPartTokenFactories,
			Func<object, TextLogEventTrigger> triggersConverter,
			string contentsEtagAttr,
			Func<Task<Stream>> openOutputStream,
			ITempFilesManager tempFiles,
			CancellationToken cancellation
		)
		{
			events = events ?? new List<M.Event[]>().ToAsync();
			timelineComments = timelineComments ?? new List<TLBlock.Event[]>().ToAsync();
			stateInspectorComments = stateInspectorComments ?? new List<SIBlock.Event[]>().ToAsync();
			logPartToken = logPartToken ?? Task.FromResult<ILogPartToken>(null);

			var eventsTmpFile = tempFiles.GenerateNewName();
			var timelineCommentsTmpFile = tempFiles.GenerateNewName();
			var stateInsectorCommentsTmpFile = tempFiles.GenerateNewName();

			Func<Task<Stream>> openTempFile(string fileName) => () => Task.FromResult<Stream>(new FileStream(fileName, FileMode.OpenOrCreate));

			var serializeMessagingEvents = events.SerializePostprocessorOutput<M.Event, M.EventsSerializer, M.IEventsVisitor>(
				triggerSerializer => new M.EventsSerializer(triggerSerializer),
				null, logPartTokenFactories, triggersConverter, null, messagingEventsElementName, openTempFile(eventsTmpFile), tempFiles, cancellation
			);

			var serializeTimelineComments = timelineComments.SerializePostprocessorOutput<TLBlock.Event, TLBlock.EventsSerializer, TLBlock.IEventsVisitor>(
				triggerSerializer => new TLBlock.EventsSerializer(triggerSerializer),
				null, logPartTokenFactories, triggersConverter, null, timelineCommentsElementName, openTempFile(timelineCommentsTmpFile), tempFiles, cancellation
			);

			var serializeStateInspectorComments = stateInspectorComments.SerializePostprocessorOutput<SIBlock.Event, SIBlock.EventsSerializer, SIBlock.IEventsVisitor>(
				triggerSerializer => new SIBlock.EventsSerializer(triggerSerializer),
				null, logPartTokenFactories, triggersConverter, null, stateCommentsElementName, openTempFile(stateInsectorCommentsTmpFile), tempFiles, cancellation
			);

			await Task.WhenAll(serializeMessagingEvents, serializeTimelineComments, serializeStateInspectorComments, logPartToken);

			using (var outputWriter = XmlWriter.Create(await openOutputStream(), new XmlWriterSettings() { Indent = true, Async = true, CloseOutput = true }))
			using (var messagingEventsReader = XmlReader.Create(eventsTmpFile))
			using (var timelineCommentsReader = XmlReader.Create(timelineCommentsTmpFile))
			using (var stateInspectorCommentsReader = XmlReader.Create(stateInsectorCommentsTmpFile))
			{
				outputWriter.WriteStartElement("root");

				new PostprocessorOutputETag(contentsEtagAttr).Write(outputWriter);
				logPartTokenFactories.SafeWriteTo(await logPartToken, outputWriter);

				messagingEventsReader.ReadToFollowing(messagingEventsElementName);
				await outputWriter.WriteNodeAsync(messagingEventsReader, false);
				timelineCommentsReader.ReadToFollowing(timelineCommentsElementName);
				await outputWriter.WriteNodeAsync(timelineCommentsReader, false);
				stateInspectorCommentsReader.ReadToFollowing(stateCommentsElementName);
				await outputWriter.WriteNodeAsync(stateInspectorCommentsReader, false);

				outputWriter.WriteEndElement(); // root
			}

			File.Delete(eventsTmpFile);
			File.Delete(timelineCommentsTmpFile);
			File.Delete(stateInsectorCommentsTmpFile);
		}

		ILogSource ISequenceDiagramPostprocessorOutput.LogSource { get { return logSource; } }

		IEnumerable<M.Event> ISequenceDiagramPostprocessorOutput.Events { get { return events; } }

		IEnumerable<TLBlock.Event> ISequenceDiagramPostprocessorOutput.TimelineComments { get { return timelineComments; } }

		IEnumerable<SIBlock.Event> ISequenceDiagramPostprocessorOutput.StateComments { get { return stateComments; } }

		ILogPartToken ISequenceDiagramPostprocessorOutput.RotatedLogPartToken { get { return rotatedLogPartToken; } }

		string IPostprocessorOutputETag.ETag { get { return etag.Value; } }

		private const string messagingEventsElementName = "messaging";
		private const string timelineCommentsElementName = "timeline-comments";
		private const string stateCommentsElementName = "state-comments";

		readonly ILogSource logSource;
		readonly List<M.Event> events;
		readonly List<TLBlock.Event> timelineComments;
		readonly List<SIBlock.Event> stateComments;
		readonly ILogPartToken rotatedLogPartToken;
		readonly PostprocessorOutputETag etag;
	};
}

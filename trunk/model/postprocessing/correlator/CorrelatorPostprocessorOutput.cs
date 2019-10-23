using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using LogJoint.Postprocessing.Messaging.Analisys;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Postprocessing.Correlation
{
	class PostprocessorOutput: ICorrelatorOutput
	{
		readonly NodeId nodeId;
		readonly ILogSource logSource;
		readonly List<M.Event> events;
		readonly ILogPartToken rotatedLogPartToken;
		readonly ISameNodeDetectionToken sameNodeDetectionToken;
		readonly PostprocessorOutputETag etag;
		private const string messagingEventsElementName = "messaging";

		public PostprocessorOutput(
			LogSourcePostprocessorDeserializationParams p,
			ILogPartTokenFactories rotatedLogPartFactories,
			ISameNodeDetectionTokenFactories nodeDetectionTokenFactories)
		{
			this.logSource = p.LogSource;
			var reader = p.Reader;

			events = new List<M.Event>();
			rotatedLogPartToken = new NullLogPartToken();
			sameNodeDetectionToken = new NullSameNodeDetectionToken();

			if (!reader.ReadToFollowing("root"))
				throw new FormatException();
			etag.Read(reader);

			foreach (var elt in p.Reader.ReadChildrenElements())
			{
				if (rotatedLogPartFactories.TryReadLogPartToken(elt, out var tmp))
					this.rotatedLogPartToken = tmp;
				else if (nodeDetectionTokenFactories.TryReadLogPartToken(elt, out var tmp2))
					sameNodeDetectionToken = tmp2;
				else if (elt.Name == NodeId.xmlName)
					nodeId = new NodeId(elt);
				else if (elt.Name == messagingEventsElementName)
				{
					var eventsDeserializer = new M.EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
					foreach (var me in p.Reader.ReadChildrenElements())
						if (eventsDeserializer.TryDeserialize(elt, out var evt))
							events.Add(evt);
				}
				p.Cancellation.ThrowIfCancellationRequested();
			}

			if (nodeId == null)
				throw new FormatException("no node id found");

		}

		public static async Task SerializePostprocessorOutput(
			Task<NodeId> nodeId,
			Task<ILogPartToken> logPartToken,
			ILogPartTokenFactories logPartTokenFactories,
			IEnumerableAsync<M.Event[]> events,
			Task<ISameNodeDetectionToken> sameNodeDetectionTokenTask,
			ISameNodeDetectionTokenFactories nodeDetectionTokenFactories,
			Func<object, TextLogEventTrigger> triggersConverter,
			string contentsEtagAttr,
			string outputFileName,
			ITempFilesManager tempFiles,
			CancellationToken cancellation
		)
		{
			events = events ?? new List<M.Event[]>().ToAsync();
			logPartToken = logPartToken ?? Task.FromResult<ILogPartToken>(null);

			var eventsTmpFile = tempFiles.GenerateNewName();

			var serializeMessagingEvents = events.SerializePostprocessorOutput<M.Event, M.EventsSerializer, M.IEventsVisitor>(
				triggerSerializer => new M.EventsSerializer(triggerSerializer),
				null, logPartTokenFactories, triggersConverter, null, messagingEventsElementName, eventsTmpFile, tempFiles, cancellation
			);

			await Task.WhenAll(serializeMessagingEvents, logPartToken, sameNodeDetectionTokenTask, nodeId);

			using (var outputWriter = XmlWriter.Create(outputFileName, new XmlWriterSettings() { Indent = true, Async = true }))
			using (var messagingEventsReader = XmlReader.Create(eventsTmpFile))
			{
				outputWriter.WriteStartElement("root");

				new PostprocessorOutputETag(contentsEtagAttr).Write(outputWriter);
				logPartTokenFactories.SafeWriteTo(await logPartToken, outputWriter);
				nodeDetectionTokenFactories.SafeWriteTo(await sameNodeDetectionTokenTask, outputWriter);
				(await nodeId).Serialize().WriteTo(outputWriter);

				messagingEventsReader.ReadToFollowing(messagingEventsElementName);
				await outputWriter.WriteNodeAsync(messagingEventsReader, false);

				outputWriter.WriteEndElement(); // root
			}

			File.Delete(eventsTmpFile);
		}

		NodeId ICorrelatorOutput.NodeId => nodeId;

		ILogSource ICorrelatorOutput.LogSource => logSource;

		IEnumerable<M.Event> ICorrelatorOutput.Events => events;

		ILogPartToken ICorrelatorOutput.RotatedLogPartToken => rotatedLogPartToken;

		ISameNodeDetectionToken ICorrelatorOutput.SameNodeDetectionToken => sameNodeDetectionToken;

		string IPostprocessorOutputETag.ETag => etag.Value;
	};
}

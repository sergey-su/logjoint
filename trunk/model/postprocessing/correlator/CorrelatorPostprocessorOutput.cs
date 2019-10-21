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
	class CorrelatorPostprocessorOutput2 // todo: remove 2 from name
	{
		public static async Task SerializePostprocessorOutput(
			NodeId nodeId,
			Task<ILogPartToken> logPartToken,
			ILogPartTokenFactories logPartTokenFactories,
			IEnumerableAsync<M.Event[]> events,
			Task<ISameNodeDetectionToken> sameNodeDetectionTokenTask,
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

			await Task.WhenAll(serializeMessagingEvents, logPartToken, sameNodeDetectionTokenTask);

			using (var outputWriter = XmlWriter.Create(outputFileName, new XmlWriterSettings() { Indent = true, Async = true }))
			using (var messagingEventsReader = XmlReader.Create(eventsTmpFile))
			{
				outputWriter.WriteStartElement("root");

				new PostprocessorOutputETag(contentsEtagAttr).Write(outputWriter);
				logPartTokenFactories.SafeWriteTo(await logPartToken, outputWriter);

				messagingEventsReader.ReadToFollowing(messagingEventsElementName);
				await outputWriter.WriteNodeAsync(messagingEventsReader, false);

				outputWriter.WriteEndElement(); // root
			}

			File.Delete(eventsTmpFile);
		}

		private const string messagingEventsElementName = "messaging";
	};
}

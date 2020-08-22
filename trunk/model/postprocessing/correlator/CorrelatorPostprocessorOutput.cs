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
				else if (elt.Name == messagingEventsElementName)
				{
					var eventsDeserializer = new M.EventsDeserializer(TextLogEventTrigger.DeserializerFunction);
					foreach (var me in elt.Elements())
						if (eventsDeserializer.TryDeserialize(me, out var evt))
							events.Add(evt);
				}
				p.Cancellation.ThrowIfCancellationRequested();
			}
		}

		public static async Task SerializePostprocessorOutput(
			Task<ILogPartToken> logPartToken,
			ILogPartTokenFactories logPartTokenFactories,
			IEnumerableAsync<M.Event[]> events,
			Task<ISameNodeDetectionToken> sameNodeDetectionTokenTask,
			ISameNodeDetectionTokenFactories nodeDetectionTokenFactories,
			Func<object, TextLogEventTrigger> triggersConverter,
			string contentsEtagAttr,
			Func<Task<Stream>> openOutputStream,
			ITempFilesManager tempFiles,
			CancellationToken cancellation
		)
		{
			events = events ?? new List<M.Event[]>().ToAsync();
			logPartToken = logPartToken ?? Task.FromResult<ILogPartToken>(null);
			sameNodeDetectionTokenTask = sameNodeDetectionTokenTask ?? Task.FromResult<ISameNodeDetectionToken>(null);

			var eventsTmpFile = tempFiles.GenerateNewName();

			Func<Task<Stream>> openTempFile(string fileName) => () => Task.FromResult<Stream>(new FileStream(fileName, FileMode.OpenOrCreate));

			var serializeMessagingEvents = events.SerializePostprocessorOutput<M.Event, M.EventsSerializer, M.IEventsVisitor>(
				triggerSerializer => new M.EventsSerializer(triggerSerializer),
				null, logPartTokenFactories, triggersConverter, null, messagingEventsElementName, openTempFile(eventsTmpFile), tempFiles, cancellation
			);

			await Task.WhenAll(serializeMessagingEvents, logPartToken, sameNodeDetectionTokenTask);

			using (var outputWriter = XmlWriter.Create(await openOutputStream(), new XmlWriterSettings() { Indent = true, Async = true, CloseOutput = true }))
			using (var messagingEventsReader = XmlReader.Create(eventsTmpFile))
			{
				outputWriter.WriteStartElement("root");

				new PostprocessorOutputETag(contentsEtagAttr).Write(outputWriter);
				logPartTokenFactories.SafeWriteTo(await logPartToken, outputWriter);
				nodeDetectionTokenFactories.SafeWriteTo(await sameNodeDetectionTokenTask, outputWriter);

				messagingEventsReader.ReadToFollowing(messagingEventsElementName);
				await outputWriter.WriteNodeAsync(messagingEventsReader, false);

				outputWriter.WriteEndElement(); // root
			}

			File.Delete(eventsTmpFile);
		}

		ILogSource ICorrelatorOutput.LogSource => logSource;

		IEnumerable<M.Event> ICorrelatorOutput.Events => events;

		ILogPartToken ICorrelatorOutput.RotatedLogPartToken => rotatedLogPartToken;

		ISameNodeDetectionToken ICorrelatorOutput.SameNodeDetectionToken => sameNodeDetectionToken;

		string IPostprocessorOutputETag.ETag => etag.Value;
	};
}

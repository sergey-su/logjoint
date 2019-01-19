using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.IO;

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

		public static async Task SerializePostprocessorOutput(
			IEnumerableAsync<Event[]> events,
			Task<ILogPartToken> rotatedLogPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			XAttribute contentsEtagAttr,
			string outputFileName,
			ITempFilesManager tempFiles
		)
		{
			var sortKeyAttr = XName.Get("__key");
			var chunks = new List<string>();
			EventsSerializer serializer = null;
			Action resetSerializer = () =>
			{
				if (serializer?.OutputSize > 0)
				{
					string chunkFileName = tempFiles.GenerateNewName();
					chunks.Add(chunkFileName);
					using (var writer = XmlWriter.Create(chunkFileName, new XmlWriterSettings()
					{
						OmitXmlDeclaration = true,
						ConformanceLevel = ConformanceLevel.Fragment
					}))
					{
						foreach (var e in serializer.Output.OrderBy(e => e.AttributeValue(sortKeyAttr)))
							e.WriteTo(writer);
					}
				}
				serializer = new EventsSerializer((trigger, elt) =>
				{
					triggersConverter(trigger).Save(elt);
					elt.SetAttributeValue(sortKeyAttr, ((IOrderedTrigger)trigger).Index.ToString("x8"));
				});
			};
			resetSerializer();
			await events.ForEach(batch =>
			{
				foreach (var e in batch)
				{
					e.Visit(serializer);
					if (serializer.OutputSize >= 8*1024)
						resetSerializer();
				}
				return Task.FromResult(true);
			});
			resetSerializer();

			using (var outputWriter = XmlWriter.Create(outputFileName, new XmlWriterSettings()
			{
				Indent = true
			}))
			{
				outputWriter.WriteStartElement("root");
				contentsEtagAttr?.Save(outputWriter);
				(await rotatedLogPartToken).SafeSerializeLogPartToken(null)?.WriteTo(outputWriter);
				var readersSettings = new XmlReaderSettings()
				{
					ConformanceLevel = ConformanceLevel.Fragment
				};
				var readers = chunks.Select(chunkFileName => XmlReader.Create(chunkFileName, readersSettings)).ToList();
				try
				{
					var q = new VCSKicksCollection.PriorityQueue<KeyValuePair<XmlReader, XElement>>(new ListUtils.LambdaComparer<KeyValuePair<XmlReader, XElement>>((item1, item2) =>
					{
						return string.CompareOrdinal(item1.Value.AttributeValue(sortKeyAttr), item2.Value.AttributeValue(sortKeyAttr));
					}));
					Action<XmlReader> enqueueReader = reader =>
					{
						if (!reader.EOF)
						{
							if (reader.MoveToContent() != XmlNodeType.Element)
								throw new InvalidOperationException("bad chunk");
							q.Enqueue(new KeyValuePair<XmlReader, XElement>(reader, (XElement)XNode.ReadFrom(reader)));
						}
					};
					readers.ForEach(enqueueReader);
					while (q.Count > 0)
					{
						var item = q.Dequeue();
						item.Value.Attribute(sortKeyAttr).Remove();
						item.Value.WriteTo(outputWriter);
						enqueueReader(item.Key);
					}
				}
				finally
				{
					readers.ForEach(r => r.Dispose());
					chunks.ForEach(chunkFileName => File.Delete(chunkFileName));
				}
				outputWriter.WriteEndElement();
			}
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

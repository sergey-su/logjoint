using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using LogJoint.Postprocessing;
using LogJoint.PacketAnalysis;
using System.Collections.Generic;

namespace LogJoint.Wireshark.Dpml
{
	public static class Converters
	{
		public static async Task PcapToPdmp(
			string pcapFile,
			string keyFile,
			string outputFile,
			ITShark tshark,
			CancellationToken cancellation,
			Action<string> reportStatus,
			LJTraceSource trace
		)
		{
			var tsharkArgs = new StringBuilder();
			tsharkArgs.Append($"-r \"{pcapFile}\"");
			tsharkArgs.Append($" -T pdml -2");
			if (!string.IsNullOrEmpty(keyFile) && File.Exists(keyFile))
			{
				tsharkArgs.Append($" -o \"ssl.desegment_ssl_records: TRUE\" -o \"ssl.desegment_ssl_application_data: TRUE\" -o \"ssl.keylog_file:{keyFile}\"");
			}
			using (var process = tshark.Start(tsharkArgs.ToString()))
			using (var xmlReader = XmlReader.Create(process.StandardOutput))
			using (var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false)))
			{
				var packetsRead = 0;
				var processTask = process.GetExitCodeAsync(Timeout.InfiniteTimeSpan);
				using (var statusReportTimer = new Timer(
					_ => reportStatus($"converting to text: {packetsRead} packets"), null,
					TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)))
				{
					await Task.WhenAll(
						processTask,
						Convert(xmlReader, writer, cancellation, val => packetsRead = val, trace)
					);
				}
				if (processTask.Result != 0)
				{
					trace.Error("tshark failed: {0}", process.StandardError.ReadToEnd());
					throw new Exception($"tshark failed with code {processTask.Result}");
				}
			}
		}

		private static void ShortenAttrs(XElement e, string attrName, int maxLen)
		{
			var attrs = e.Descendants("field")
				.SelectMany(f => f.Attributes(attrName))
				.Where(a => a.Value.Length > maxLen)
				.ToArray();
			foreach (var a in attrs)
			{
				a.Parent.SetAttributeValue($"{attrName}-truncated", "true");
				a.Value = a.Value.Substring(0, maxLen);
			}
		}

		private static void DeleteProto(XElement e, string proto)
		{
			var protos = e
				.Elements("proto")
				.Where(p => p.AttributeValue("name") == proto)
				.ToArray();
			foreach (var el in protos)
			{
				el.Remove();
			}
		}

		private static string GetPacketDisplayName(XElement e)
		{

			var frameNum = e
				.Elements("proto")
				.Where(p => p.AttributeValue("name") == "geninfo")
				.Elements("field")
				.Where(f => f.AttributeValue("name") == "num")
				.Attributes("value")
				.FirstOrDefault();
			if (frameNum != null)
				return $"frame {frameNum}";
			return "<packet>";
		}

		private static async Task Convert(XmlReader xmlReader, TextWriter writer, CancellationToken cancellation, Action<int> reportPacketsRead, LJTraceSource trace)
		{
			if (!xmlReader.ReadToFollowing("pdml"))
				throw new Exception("bad pdml");

			var maxElementSize = 1 * 1024 * 512;
			var reductionMethods = new Action<XElement>[]
			{
				(e) => ShortenAttrs(e, "show", 512),
				(e) => ShortenAttrs(e, "value", 512),
				(e) => DeleteProto(e, "json"),
				(e) => ShortenAttrs(e, "show", 64),
				(e) => ShortenAttrs(e, "value", 64),
				(e) => DeleteProto(e, "frame"),
				(e) => DeleteProto(e, "eth"),
			};

			int packetsRead = 0;

			BlockingCollection<XElement> queue = new BlockingCollection<XElement>(1024);

			var producer = Task.Factory.StartNew(() => 
			{
				foreach (var packet in ReadChildrenElements(xmlReader))
				{
					queue.Add(packet);
					if (cancellation.IsCancellationRequested)
						break;
				}
				queue.CompleteAdding();
			});

			var consumer = Task.Factory.StartNew(() => 
			{
				int packetsCompressed = 0;

				writer.WriteLine("<pdml>");
				using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
				{
					Indent = true,
					CloseOutput = false,
					ConformanceLevel = ConformanceLevel.Fragment,
					OmitXmlDeclaration = true
				}))
				{
					foreach (var packet in queue.GetConsumingEnumerable())
					{
						cancellation.ThrowIfCancellationRequested();
						if (packet.Name != "packet")
							throw new Exception($"unexpected node in pdml: {packet.Name}");

						bool reductionUsed = false;
						for (int reductionMethodIdx = 0;;++reductionMethodIdx)
						{
							int packetLen = packet.ToString().Length;
							if (packetLen <= maxElementSize)
								break;
							if (reductionMethodIdx >= reductionMethods.Length)
								throw new Exception($"packet is too large: {GetPacketDisplayName(packet)}");
							reductionMethods[reductionMethodIdx](packet);
							reductionUsed = true;
						}
						if (reductionUsed)
							++packetsCompressed;

						packet.WriteTo(xmlWriter);

						++packetsRead;
						reportPacketsRead(packetsRead);
					}
				}
				writer.WriteLine();
				writer.WriteLine();
				writer.WriteLine("</pdml>");

				trace.Info("PCAP conversion finished. Total packets read: {0}, packets compressed: {1}", 
					packetsRead, packetsCompressed);
			});

			await Task.WhenAll(producer, consumer);
		}

		public static IEnumerable<XElement> ReadChildrenElements(XmlReader inputReader)
		{
			var reader = XmlReader.Create(inputReader, new XmlReaderSettings
			{
				IgnoreWhitespace = true,
				IgnoreComments = true
			});
			if (reader.NodeType != XmlNodeType.Element)
			{
				throw new InvalidOperationException("can not read children of non-element " + reader.NodeType.ToString());
			}
			reader.ReadStartElement();
			for (; reader.Read() && reader.NodeType == XmlNodeType.Element;)
			{
				using (var elementReader = reader.ReadSubtree())
					yield return XElement.Load(elementReader);
			}
		}

	};
}

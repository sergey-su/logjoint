using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using LogJoint.Analytics;
using LogJoint.RegularExpressions;

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
			LJTraceSource trace,
			ITempFilesManager tempFiles
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
			using (var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false)))
			{
				var packetsRead = new Ref<int>();
				var processTask = process.GetExitCodeAsync(TimeSpan.FromMinutes(5));
				using (var statusReportTimer = new Timer(
					_ => reportStatus($"scanning: {packetsRead.Value} packets"), null,
					TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)))
				{
					await Task.WhenAll(
						processTask,
						Task.Run(() => Convert(process.StandardOutput, writer, cancellation, packetsRead))
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

		private static void Convert(StreamReader reader, TextWriter writer, CancellationToken cancellation, Ref<int> packetsRead)
		{
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

			void reduceSize(XElement packet)
			{
				for (int reductionMethodIdx = 0; ; ++reductionMethodIdx)
				{
					int packetLen = packet.ToString().Length;
					if (packetLen <= maxElementSize)
						break;
					if (reductionMethodIdx >= reductionMethods.Length)
						throw new Exception($"packet is too large: {GetPacketDisplayName(packet)}");
					reductionMethods[reductionMethodIdx](packet);
				}
			};


			ITextAccess ta = new StreamTextAccess(reader.BaseStream, reader.CurrentEncoding, new TextStreamPositioningParams(32*1024*1024));
			IMessagesSplitter splitter = new MessagesSplitter(ta, RegexFactory.Instance.Create(@"\<packet\>", ReOptions.None));
			splitter.BeginSplittingSession(new FileRange.Range(0, 1*1024*1024*1024 /*todo*/), 0, MessagesParserDirection.Forward);

			writer.WriteLine("<pdml>");

			var capture = new TextMessageCapture();
			var sb = new StringBuilder();
			while (splitter.GetCurrentMessageAndMoveToNextOne(capture))
			{
				capture.MessageHeaderSlice.Append(sb);
				capture.MessageBodySlice.Append(sb);
				if (sb.Length < maxElementSize)
				{
					writer.Write(sb.ToString());
				}
				else
				{
					var packet = XElement.Parse(sb.ToString(), LoadOptions.PreserveWhitespace);
					reduceSize(packet);
					using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
					{
						Indent = true,
						CloseOutput = false,
						ConformanceLevel = ConformanceLevel.Fragment,
						OmitXmlDeclaration = true
					}))
					{
						packet.WriteTo(xmlWriter);
					}
				}
				sb.Clear();
				++packetsRead.Value;
				if (packetsRead.Value == 6)
					sb.ToString();
				cancellation.ThrowIfCancellationRequested();
			}

			writer.WriteLine("</pdml>");

			splitter.EndSplittingSession();



			/*
			writer.WriteLine("<pdml>");
			using (var xmlReader = XmlReader.Create(reader))
			using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
			{
				Indent = true,
				CloseOutput = false,
				ConformanceLevel = ConformanceLevel.Fragment,
				OmitXmlDeclaration = true
			}))
			{
				foreach (var packet in xmlReader.ReadChildrenElements())
				{
					cancellation.ThrowIfCancellationRequested();
					if (packet.Name != "packet")
						throw new Exception($"unexpected node in pdml: {packet.Name}");

					reduceSize(packet);

					packet.WriteTo(xmlWriter);

					++packetsRead.Value;
				}
			}
			writer.WriteLine();
			writer.WriteLine();
			writer.WriteLine("</pdml>");*/
		}
	};
}

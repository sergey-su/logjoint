using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using LogJoint.Analytics;

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
				var packetsRead = new Ref<int>();
				var processTask = process.GetExitCodeAsync(TimeSpan.FromMinutes(5));
				using (var statusReportTimer = new Timer(
					_ => reportStatus($"scanning: {packetsRead.Value} packets"), null,
					TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)))
				{
					await Task.WhenAll(
						processTask,
						Task.Run(() => Convert(xmlReader, writer, cancellation, packetsRead))
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

		private static void Convert(XmlReader xmlReader, TextWriter writer, CancellationToken cancellation, Ref<int> packetsRead)
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

			writer.WriteLine("<pdml>");
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

					for (int reductionMethodIdx = 0;;++reductionMethodIdx)
					{
						int packetLen = packet.ToString().Length;
						if (packetLen <= maxElementSize)
							break;
						if (reductionMethodIdx >= reductionMethods.Length)
							throw new Exception($"packet is too large: {GetPacketDisplayName(packet)}");
						reductionMethods[reductionMethodIdx](packet);
					}

					packet.WriteTo(xmlWriter);

					++packetsRead.Value;
				}
			}
			writer.WriteLine();
			writer.WriteLine();
			writer.WriteLine("</pdml>");
		}
	};
}

using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Immutable;
using LogJoint.PacketAnalysis;

namespace LogJoint.Wireshark.Dpml
{
	public class Reader : IReader
	{
		readonly ITextLogParser textLogParser;
		readonly CancellationToken cancellation;

		public Reader(ITextLogParser textLogParser, CancellationToken cancellation)
		{
			this.textLogParser = textLogParser;
			this.cancellation = cancellation;
		}

		public IEnumerableAsync<Message[]> Read(string dataFileName, Action<double> progressHandler = null)
		{
			return Read(() => new FileStream(dataFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), s => s.Dispose(), progressHandler);
		}

		public IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null)
		{
			using (var ctx = new Context())
				return EnumerableAsync.Produce<Message[]>(yieldAsync => ctx.Read(yieldAsync, getStream, releaseStream, cancellation, progressHandler, textLogParser), false);
		}

		class Context : IDisposable
		{
			const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;

			readonly Regex logMessageRegex = new Regex(@"^\<packet\>", regexOptions | RegexOptions.Multiline);

			void IDisposable.Dispose()
			{
			}

			public async Task Read(
				IYieldAsync<Message[]> yieldAsync,
				Func<Stream> getStream, Action<Stream> releaseStream,
				CancellationToken cancellation,
				Action<double> progressHandler,
				ITextLogParser textLogParser)
			{
				var inputStream = getStream();
				try
				{
					await textLogParser.ParseStream(
						inputStream,
						textLogParser.CreateRegexHeaderMatcher(logMessageRegex),
						async messagesInfo =>
						{
							var outMessages = new List<Message>();
							for (int i = 0; i < messagesInfo.Count; ++i)
							{
								var mi = messagesInfo[i];
								var xmlStr = $"{mi.HeaderMatch.ToString()}{mi.MessageBoby}";
								using (var textReader = new StringReader(xmlStr))
								using (var xmlReader = XmlReader.Create(textReader, new XmlReaderSettings()
								{
									ConformanceLevel = ConformanceLevel.Fragment
								}))
								{
									if (xmlReader.MoveToContent() != XmlNodeType.Element)
									{
										continue;
									}
									var packetElement = (XElement)XElement.ReadFrom(xmlReader);
									DateTime? timestamp = null;
									long? frameNumber = null;
									var protos = ImmutableDictionary.CreateBuilder<string, Message.Proto>();
									int protoIndex = 0;
									foreach (var protoElement in packetElement.Elements("proto"))
									{
										var nameAttr = protoElement.Attribute("name");
										if (nameAttr == null)
											continue;
										if (nameAttr.Value == "geninfo")
										{
											var tsAttrValue = protoElement.Elements("field").Where(f => f.AttributeValue("name") == "timestamp").Select(f => f.AttributeValue("value")).FirstOrDefault();
											if (tsAttrValue != null && double.TryParse(tsAttrValue, out var tsParsed))
												timestamp = Utils.UnixTimestampMillisToDateTime(tsParsed * 1000);
											var frameNumberAttrValue = protoElement.Elements("field").Where(f => f.AttributeValue("name") == "num").Select(f => f.AttributeValue("value")).FirstOrDefault();
											if (frameNumberAttrValue != null && long.TryParse(frameNumberAttrValue, NumberStyles.HexNumber, null, out var frameNumberParsed))
												frameNumber = frameNumberParsed;
										}
										else
										{
											var fields = ImmutableDictionary.CreateBuilder<string, Message.Field>();
											foreach (var kv in protoElement
												.Descendants("field")
												.Select(fieldElement => new KeyValuePair<string, Message.Field>(fieldElement.AttributeValue("name"),
													new Message.Field(fieldElement.AttributeValue("show", null), fieldElement.AttributeValue("value", null))))
												.Where(kv => !string.IsNullOrEmpty(kv.Key) && (kv.Value.Show != null || kv.Value.Value != null)))
											{
												fields[kv.Key] = kv.Value;
											}
											protos[nameAttr.Value] = new Message.Proto(
												protoElement.AttributeValue("showname"),
												protoIndex++,
												fields.ToImmutable()
											);
										}
									}
									if (timestamp == null || frameNumber == null)
									{
										continue;
									}
									outMessages.Add(new Message(
										mi.MessageIndex,
										mi.StreamPosition,
										timestamp.Value,
										frameNumber.Value,
										protos.ToImmutable()
									));
								}
							}

							if (cancellation.IsCancellationRequested)
								return false;

							return await yieldAsync.YieldAsync(outMessages.ToArray());
						}, new TextLogParserOptions(progressHandler)
						{
							RawBufferSize = 16 * 1024 * 1024
						});
				}
				finally
				{
					releaseStream(inputStream);
				}
			}
		}
	}
}

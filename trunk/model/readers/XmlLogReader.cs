using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using LogJoint.RegularExpressions;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint.XmlFormat 
{
	static class Properties
	{
		public static readonly string LogJointNS = "http://logjoint.codeplex.com/";
	};

	class FactoryWriter : XmlWriter
	{
		public FactoryWriter(IMessagesBuilderCallback callback, ITimeOffsets timeOffsets)
		{
			this.callback = callback;
			this.timeOffsets = timeOffsets;
			states.Push(WriteState.Content);
		}

		public override void Close()
		{
		}

		public override void Flush()
		{
		}

		public override string LookupPrefix(string ns)
		{
			throw new InvalidOperationException("Namespace prefixes are not supportes.");
		}

		public override void WriteBase64(byte[] buffer, int index, int count)
		{
			WriteString(Convert.ToBase64String(buffer, index, count));
		}

		public override void WriteCData(string text)
		{
			WriteString(text);
		}

		public override void WriteCharEntity(char ch)
		{
			WriteString(new string(ch, 1));
		}

		public override void WriteChars(char[] buffer, int index, int count)
		{
			WriteString(new string(buffer, index, count));
		}

		public override void WriteComment(string text)
		{
		}

		public override void WriteDocType(string name, string pubid, string sysid, string subset)
		{
		}

		public override void WriteEntityRef(string name)
		{
			throw new InvalidOperationException("Entities are not supported");
		}

		public override void WriteFullEndElement()
		{
			WriteEndElement();
		}

		public override void WriteProcessingInstruction(string name, string text)
		{
		}

		public override void WriteRaw(string data)
		{
			throw new InvalidOperationException("Raw data is not supported.");
		}

		public override void WriteRaw(char[] buffer, int index, int count)
		{
			WriteRaw("");
		}

		public override void WriteStartDocument(bool standalone)
		{
		}

		public override void WriteStartDocument()
		{
		}

		public override void WriteEndDocument()
		{
		}

		public override void WriteSurrogateCharEntity(char lowChar, char highChar)
		{
			char[] buf = new char[] { lowChar, highChar };
			WriteString(new string(buf));
		}

		public override void WriteWhitespace(string ws)
		{
			WriteString(ws);
		}

		public override void WriteStartElement(string prefix, string localName, string ns)
		{
			states.Push(WriteState.Element);

			localName = string.Intern(localName);
			switch (localName)
			{
				case "m":
				case "f":
				case "ef":
					elemName = localName;
					break;
				default:
					elemName = null;
					break;
			}
			if (elemName != null)
			{
				Reset();
			}
		}

		void Reset()
		{
			content.Length = 0;
			attribName = null;
			thread = null;
			dateTime = new MessageTimestamp();
			severity = SeverityFlag.Info;
		}

		public override void WriteEndElement()
		{
			states.Pop();

			if (elemName == null)
				return;

			if (thread == null)
				thread = callback.GetThread(StringSlice.Empty);
			long position = callback.CurrentPosition;
			long endPosition = callback.CurrentEndPosition;

			output = new Content(position, endPosition, thread, dateTime, new StringSlice(GetAndClearContent()), severity);

			elemName = null;
			Reset();
		}

		public override void WriteStartAttribute(string prefix, string localName, string ns)
		{
			states.Push(WriteState.Attribute);

			localName = string.Intern(localName);
			switch (localName)
			{
				case "t":
				case "d":
				case "s":
					attribName = localName;
					break;
				default:
					attribName = null;
					break;
			}
		}

		static MessageTimestamp ParseDateTime(string str)
		{
			return MessageTimestamp.ParseFromLoselessFormat(str);
		}

		public override void WriteEndAttribute()
		{
			states.Pop();

			switch (attribName)
			{
				case "d":
					dateTime = ParseDateTime(GetAndClearContent()).Adjust(timeOffsets);
					break;
				case "t":
					thread = callback.GetThread(new StringSlice(GetAndClearContent()));
					break;
				case "s":
					switch (GetAndClearContent())
					{
						case "e":
							severity = SeverityFlag.Error;
							break;
						case "w":
							severity = SeverityFlag.Warning;
							break;
					}
					break;
				default:
					// Ended to read unknown attribute. Need to clear content in order not to affect the following attribs/elems.
					GetAndClearContent();
					break;
			}

			attribName = null;
		}

		public override WriteState WriteState
		{
			get { return states.Peek(); }
		}

		public override void WriteString(string text)
		{
			content.Append(text);
		}

		public IMessage GetOutput()
		{
			return output;
		}

		string GetAndClearContent()
		{
			string ret = StringUtils.TrimInsignificantSpace(content.ToString());
			content.Length = 0;
			return ret;
		}

		Stack<WriteState> states = new Stack<WriteState>();
		string elemName;
		string attribName;
		IThread thread;
		MessageTimestamp dateTime;
		StringBuilder content = new StringBuilder();
		SeverityFlag severity = SeverityFlag.Info;
		IMessage output;
		readonly IMessagesBuilderCallback callback;
		readonly ITimeOffsets timeOffsets;
	};

	public class LogJointXSLExtension : UserCodeHelperFunctions
	{
		DateTime __sourceTime;

		protected override DateTime SOURCE_TIME()
		{
			return __sourceTime;
		}


		internal void SetSourceTime(DateTime sourceTime)
		{
			__sourceTime = sourceTime;
		}
	}

	class XmlFormatInfo: StreamBasedFormatInfo
	{
		public readonly XslCompiledTransform Transform;
		public readonly string NSDeclaration = "";
		public readonly string Encoding;
		public readonly LoadedRegex HeadRe;
		public readonly LoadedRegex BodyRe;
		public readonly BoundFinder BeginFinder;
		public readonly BoundFinder EndFinder;
		public readonly TextStreamPositioningParams TextStreamPositioningParams;
		public readonly DejitteringParams? DejitteringParams;
		public readonly IFormatViewOptions ViewOptions;

		public bool IsNativeFormat { get { return Transform == null; } }

		public static readonly XmlFormatInfo NativeFormatInfo = XmlFormatInfo.MakeNativeFormatInfo("utf-8", null, new FormatViewOptions());

		public static XmlFormatInfo MakeNativeFormatInfo(string encoding, DejitteringParams? dejitteringParams, FormatViewOptions viewOptions)
		{
			LoadedRegex headRe;
			headRe.Regex = RegexFactory.Instance.Create(@"\<\s*(m|f|ef)\s", ReOptions.None);
			headRe.SuffersFromPartialMatchProblem = false;
			return new XmlFormatInfo(
				null, headRe, new LoadedRegex(),
				null, null, encoding, null, TextStreamPositioningParams.Default, dejitteringParams, viewOptions);
		}

		public XmlFormatInfo(XmlNode xsl, LoadedRegex headRe, LoadedRegex bodyRe, BoundFinder beginFinder, BoundFinder endFinder, string encoding, MessagesReaderExtensions.XmlInitializationParams extensionsInitData,
				TextStreamPositioningParams textStreamPositioningParams, DejitteringParams? dejitteringParams, IFormatViewOptions viewOptions) :
			base (extensionsInitData)
		{
			Encoding = encoding;
			HeadRe = headRe;
			BodyRe = bodyRe;
			BeginFinder = beginFinder;
			EndFinder = endFinder;
			TextStreamPositioningParams = textStreamPositioningParams;
			DejitteringParams = dejitteringParams;
			ViewOptions = viewOptions;

			if (xsl != null)
			{
				Dictionary<string, string> nsTable = new Dictionary<string, string>();
				foreach (XmlAttribute ns in xsl.SelectNodes(".//namespace::*"))
				{
					if (ns.Value == "http://www.w3.org/XML/1998/namespace")
						continue;
					if (ns.Value == "http://www.w3.org/1999/XSL/Transform")
						continue;
					if (ns.Value == Properties.LogJointNS)
						continue;
					nsTable[ns.Name] = ns.Value;
				}

				StringBuilder nsdeclBuilder = new StringBuilder();
				foreach (KeyValuePair<string, string> ns in nsTable)
				{
					nsdeclBuilder.AppendFormat("{0}='{1}' ", ns.Key, ns.Value);
				}
				NSDeclaration = nsdeclBuilder.ToString();

				Transform = new XslCompiledTransform();
				Transform.Load(xsl);
			}
		}
	};

	class MessagesReader : MediaBasedPositionedMessagesReader
	{
		internal XmlFormatInfo formatInfo;
		readonly XsltArgumentList transformArgs;
		readonly LogJointXSLExtension xslExt;
		readonly ILogSourceThreads threads;

		public MessagesReader(MediaBasedReaderParams readerParams, XmlFormatInfo fmt) :
			base(readerParams.Media, fmt.BeginFinder, fmt.EndFinder, fmt.ExtensionsInitData, fmt.TextStreamPositioningParams, readerParams.Flags, readerParams.SettingsAccessor)
		{
			this.formatInfo = fmt;
			this.threads = readerParams.Threads;
			this.transformArgs = new XsltArgumentList();

			this.xslExt = new LogJointXSLExtension();
			transformArgs.AddExtensionObject(Properties.LogJointNS, this.xslExt);

			foreach (MessagesReaderExtensions.ExtensionData extInfo in this.Extensions.Items)
			{
				transformArgs.AddExtensionObject(Properties.LogJointNS + extInfo.Name, extInfo.Instance());
			}
		}

		protected override Encoding DetectStreamEncoding(Stream stream)
		{
			Encoding ret = EncodingUtils.GetEncodingFromConfigXMLName(formatInfo.Encoding);
			if (ret != null)
				return ret;
			if (formatInfo.Encoding == "BOM")
			{
				ret = EncodingUtils.DetectEncodingFromBOM(stream, Encoding.UTF8);
			}
			else if (formatInfo.Encoding == "PI")
			{
				ret = EncodingUtils.DetectEncodingFromProcessingInstructions(stream);
				if (ret == null)
					ret = EncodingUtils.DetectEncodingFromBOM(stream, Encoding.UTF8);
			}
			return ret;
		}

		internal static readonly XmlReaderSettings xmlReaderSettings = CreateXmlReaderSettings();

		static XmlReaderSettings CreateXmlReaderSettings()
		{
			XmlReaderSettings xrs = new XmlReaderSettings();
			xrs.ConformanceLevel = ConformanceLevel.Fragment;
			xrs.CheckCharacters = false;
			xrs.CloseInput = false;
			return xrs;
		}

		static IMessage MakeMessageInternal(TextMessageCapture capture, XmlFormatInfo formatInfo, IRegex bodyRe, ref IMatch bodyReMatch,
			MessagesBuilderCallback callback, XsltArgumentList transformArgs, DateTime sourceTime, ITimeOffsets timeOffsets)
		{
			int nrOfSequentialFailures = 0;
			int maxNrOfSequentialFailures = 10;
			for (; ; )
			{
				StringBuilder messageBuf = new StringBuilder();
				messageBuf.AppendFormat("<root {0}>", formatInfo.NSDeclaration.ToString());
				messageBuf.Append(capture.HeaderBuffer, capture.HeaderMatch.Index, capture.HeaderMatch.Length);
				if (bodyRe != null)
				{
					if (!bodyRe.Match(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength, ref bodyReMatch))
						return null;
					messageBuf.Append(capture.BodyBuffer, bodyReMatch.Index, bodyReMatch.Length);
				}
				else
				{
					messageBuf.Append(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength);
				}
				messageBuf.Append("</root>");

				callback.SetCurrentPosition(capture.BeginPosition, capture.EndPosition);
				
				//this.owner.xslExt.SetSourceTime(this.owner.MediaLastModified); todo?

				string messageStr = messageBuf.ToString();

				using (FactoryWriter factoryWriter = new FactoryWriter(callback, timeOffsets))
				using (XmlReader xmlReader = XmlReader.Create(new StringReader(messageStr), xmlReaderSettings))
				{
					try
					{
						if (formatInfo.IsNativeFormat)
						{
							factoryWriter.WriteNode(xmlReader, false);
						}
						else
						{
							formatInfo.Transform.Transform(xmlReader, transformArgs, factoryWriter);
						}
						nrOfSequentialFailures = 0;
					}
					catch (XmlException)
					{
						if (capture.IsLastMessage)
						{
							// There might be incomplete XML at the end of the stream. Ignore it.
							return null;
						}
						else
						{
							if (nrOfSequentialFailures < maxNrOfSequentialFailures)
							{
								++nrOfSequentialFailures;
								// Try to parse the next messsage if it's not the end of the stream
								continue;
							}
							else
							{
								throw;
							}
						}
					}
					
					var ret = factoryWriter.GetOutput();
					if (ret == null)
						throw new XsltException(
							"Normalization XSLT produced no output");

					if (formatInfo.ViewOptions.RawViewAllowed)
					{
						ret.SetRawText(StringSlice.Concat(capture.MessageHeaderSlice, capture.MessageBodySlice).Trim());
					}

					if (formatInfo.ViewOptions.WrapLineLength.HasValue)
					{
						ret.WrapsTexts(formatInfo.ViewOptions.WrapLineLength.Value);
					}

					return ret;
				}
			}
		}

		MessagesBuilderCallback CreateMessageBuilderCallback()
		{
			IThread fakeThread = null;
			//fakeThread = threads.GetThread("");
			return new MessagesBuilderCallback(threads, fakeThread);
		}

		class SingleThreadedStrategyImpl : StreamParsingStrategies.SingleThreadedStrategy
		{
			readonly MessagesReader reader;
			readonly MessagesBuilderCallback callback;
			readonly IRegex bodyRegex;

			IMatch bodyMatch;

			public SingleThreadedStrategyImpl(MessagesReader reader) :
				base(reader.LogMedia, reader.StreamEncoding, CloneRegex(reader.formatInfo.HeadRe).Regex,
					reader.formatInfo.HeadRe.GetHeaderReSplitterFlags(), reader.formatInfo.TextStreamPositioningParams)
			{
				this.reader = reader;
				this.callback = reader.CreateMessageBuilderCallback();
				this.bodyRegex = reader.formatInfo.BodyRe.Regex;
			}
			public override void ParserCreated(CreateParserParams p)
			{
				base.ParserCreated(p);
			}
			protected override IMessage MakeMessage(TextMessageCapture capture)
			{
				return MakeMessageInternal(capture, reader.formatInfo, bodyRegex, ref bodyMatch, callback,
					reader.transformArgs, media.LastModified, reader.TimeOffsets);
			}
		};

		protected override StreamParsingStrategies.BaseStrategy CreateSingleThreadedStrategy()
		{
			return new SingleThreadedStrategyImpl(this);
		}

		class ProcessingThreadLocalData
		{
			public LoadedRegex bodyRe;
			public IMatch bodyMatch;
			public MessagesBuilderCallback callback;
		}

		class MultiThreadedStrategyImpl : StreamParsingStrategies.MultiThreadedStrategy<ProcessingThreadLocalData>
		{
			MessagesReader reader;

			public MultiThreadedStrategyImpl(MessagesReader reader) :
				base(reader.LogMedia, reader.StreamEncoding, reader.formatInfo.HeadRe.Regex,
					reader.formatInfo.HeadRe.GetHeaderReSplitterFlags(), reader.formatInfo.TextStreamPositioningParams, null)
			{
				this.reader = reader;
			}
			public override void ParserCreated(CreateParserParams p)
			{
				base.ParserCreated(p);
			}
			public override IMessage MakeMessage(TextMessageCapture capture, ProcessingThreadLocalData threadLocal)
			{
				return MakeMessageInternal(capture, reader.formatInfo, threadLocal.bodyRe.Regex, 
					ref threadLocal.bodyMatch, threadLocal.callback, reader.transformArgs, media.LastModified, reader.TimeOffsets);
			}
			public override ProcessingThreadLocalData InitializeThreadLocalState()
			{
				ProcessingThreadLocalData ret = new ProcessingThreadLocalData();
				ret.bodyRe = CloneRegex(reader.formatInfo.BodyRe);
				ret.callback = reader.CreateMessageBuilderCallback();
				ret.bodyMatch = null;
				return ret;
			}
		};

		protected override StreamParsingStrategies.BaseStrategy CreateMultiThreadedStrategy()
		{
			return new MultiThreadedStrategyImpl(this);
		}

		protected override DejitteringParams? GetDejitteringParams()
		{
			return this.formatInfo.DejitteringParams;
		}

		public override ISearchingParser CreateSearchingParser(CreateSearchingParserParams p)
		{
			return new SearchingParser(
				this,
				p,
				((ITextStreamPositioningParamsProvider)this).TextStreamPositioningParams,
				GetDejitteringParams(),
				VolatileStream,
				StreamEncoding,
				false,
				formatInfo.HeadRe, 
				threads);
		}
	};

	public class NativeXMLFormatFactory : IFileBasedLogProviderFactory, IMediaBasedReaderFactory
	{
		readonly ITempFilesManager tempFiles;

		public NativeXMLFormatFactory(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		IEnumerable<string> IFileBasedLogProviderFactory.SupportedPatterns
		{
			get { yield return "*.xml"; }
		}

		IConnectionParams IFileBasedLogProviderFactory.CreateParams(string fileName)
		{
			return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
		}

		IConnectionParams IFileBasedLogProviderFactory.CreateRotatedLogParams(string folder)
		{
			return ConnectionParamsUtils.CreateRotatedLogConnectionParamsFromFolderPath(folder);
		}

		string ILogProviderFactory.CompanyName
		{
			get { return "LogJoint"; }
		}

		string ILogProviderFactory.FormatName
		{
			get { return "Native XML"; }
		}

		string ILogProviderFactory.FormatDescription
		{
			get { return "XML log files created by LogJoint. LogJoint writes its own logs in files of this format."; }
		}

		string ILogProviderFactory.UITypeKey { get { return StdProviderFactoryUIs.FileBasedProviderUIKey; } }

		string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetFileOrFolderBasedUserFriendlyConnectionName(connectParams);
		}

		string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), tempFiles);
		}

		ILogProvider ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new StreamLogProvider(host, this, connectParams, 
				XmlFormatInfo.NativeFormatInfo, typeof(MessagesReader));
		}

		IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.Default; } }

		LogProviderFactoryFlag ILogProviderFactory.Flags
		{
			get { return LogProviderFactoryFlag.SupportsRotation; }
		}

		IPositionedMessagesReader IMediaBasedReaderFactory.CreateMessagesReader(MediaBasedReaderParams readerParams)
		{
			return new MessagesReader(readerParams, XmlFormat.XmlFormatInfo.NativeFormatInfo);
		}
	};

	public class UserDefinedFormatFactory : 
		UserDefinedFactoryBase, 
		IFileBasedLogProviderFactory, 
		IMediaBasedReaderFactory
	{
		List<string> patterns = new List<string>();
		Lazy<XmlFormatInfo> formatInfo;
		ITempFilesManager tempFilesManager;
		static XmlNamespaceManager nsMgr = new XmlNamespaceManager(new NameTable());
		static readonly string XSLNamespace = "http://www.w3.org/1999/XSL/Transform";

		static UserDefinedFormatFactory()
		{
			nsMgr.AddNamespace("xsl", XSLNamespace);
		}

		public static XmlNamespaceManager NamespaceManager => nsMgr;

		public static void Register(IUserDefinedFormatsManager formatsManager)
		{
			formatsManager.RegisterFormatType(
				"xml", typeof(UserDefinedFormatFactory));
		}

		public UserDefinedFormatFactory(UserDefinedFactoryParams createParams)
			: base(createParams)
		{
			var formatSpecificNode = createParams.FormatSpecificNode;
			ReadPatterns(formatSpecificNode, patterns);
			
			var boundsNodes = formatSpecificNode.Elements("bounds").Take(1);
			var beginFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("begin")).FirstOrDefault());
			var endFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("end")).FirstOrDefault());
			
			this.tempFilesManager = createParams.TempFilesManager;

			formatInfo = new Lazy<XmlFormatInfo>(() => 
			{
				XmlDocument tmpDoc = new XmlDocument();
				tmpDoc.LoadXml(formatSpecificNode.ToString());
				XmlElement xsl = tmpDoc.DocumentElement.SelectSingleNode("xsl:stylesheet", nsMgr) as XmlElement;
				if (xsl == null)
					throw new Exception("Wrong XML-based format definition: xsl:stylesheet is not defined");
				
				LoadedRegex head = ReadRe(formatSpecificNode, "head-re", ReOptions.Multiline);
				LoadedRegex body = ReadRe(formatSpecificNode, "body-re", ReOptions.Singleline);
				string encoding = ReadParameter(formatSpecificNode, "encoding");

				MessagesReaderExtensions.XmlInitializationParams extensionsInitData =
					new MessagesReaderExtensions.XmlInitializationParams(formatSpecificNode.Element("extensions"));

				DejitteringParams? dejitteringParams = DejitteringParams.FromConfigNode(
					formatSpecificNode.Element("dejitter"));

				TextStreamPositioningParams textStreamPositioningParams = TextStreamPositioningParams.FromConfigNode(
					formatSpecificNode);

				return new XmlFormatInfo(xsl, head, body, beginFinder, endFinder,
					encoding, extensionsInitData, textStreamPositioningParams, dejitteringParams, viewOptions);
			});
		}


		#region ILogProviderFactory Members

		public override string UITypeKey { get { return StdProviderFactoryUIs.FileBasedProviderUIKey; } }

		public override IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), tempFilesManager);
		}

		public override string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetFileOrFolderBasedUserFriendlyConnectionName(connectParams);
		}

		public override ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new StreamLogProvider(host, this, connectParams, formatInfo.Value, typeof(MessagesReader));
		}

		public override LogProviderFactoryFlag Flags
		{
			get
			{
				return 
					  LogProviderFactoryFlag.SupportsRotation 
					| LogProviderFactoryFlag.SupportsDejitter 
					| (formatInfo.Value.DejitteringParams.HasValue ? LogProviderFactoryFlag.DejitterEnabled : LogProviderFactoryFlag.None);
			}
		}

		#endregion

		#region IFileReaderFactory Members

		IEnumerable<string> IFileBasedLogProviderFactory.SupportedPatterns
		{
			get
			{
				return patterns;
			}
		}

		IConnectionParams IFileBasedLogProviderFactory.CreateParams(string fileName)
		{
			return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
		}

		IConnectionParams IFileBasedLogProviderFactory.CreateRotatedLogParams(string folder)
		{
			return ConnectionParamsUtils.CreateRotatedLogConnectionParamsFromFolderPath(folder);
		}

		#endregion

		#region IMediaBasedReaderFactory Members
		public IPositionedMessagesReader CreateMessagesReader(MediaBasedReaderParams readerParams)
		{
			return new MessagesReader(readerParams, formatInfo.Value);
		}
		#endregion
	};
}

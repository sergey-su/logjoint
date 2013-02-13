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
		public FactoryWriter(IMessagesBuilderCallback callback, TimeSpan timeOffset)
		{
			this.callback = callback;
			this.timeOffset = timeOffset;
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
			severity = Content.SeverityFlag.Info;
		}

		public override void WriteEndElement()
		{
			states.Pop();

			if (elemName == null)
				return;

			if (thread == null)
				thread = callback.GetThread(StringSlice.Empty);
			long position = callback.CurrentPosition;

			switch (elemName)
			{
				case "m":
					output = new Content(position, thread, dateTime, new StringSlice(GetAndClearContent()), severity);
					break;
				case "f":
					output = new FrameBegin(position, thread, dateTime, new StringSlice(GetAndClearContent()));
					break;
				case "ef":
					output = new FrameEnd(position, thread, dateTime);
					break;
			}

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
					dateTime = ParseDateTime(GetAndClearContent()).Advance(timeOffset);
					break;
				case "t":
					thread = callback.GetThread(new StringSlice(GetAndClearContent()));
					break;
				case "s":
					switch (GetAndClearContent())
					{
						case "e":
							severity = Content.SeverityFlag.Error;
							break;
						case "w":
							severity = Content.SeverityFlag.Warning;
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

		public MessageBase GetOutput()
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
		Content.SeverityFlag severity = Content.SeverityFlag.Info;
		MessageBase output;
		readonly IMessagesBuilderCallback callback;
		readonly TimeSpan timeOffset;
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

		public static readonly XmlFormatInfo NativeFormatInfo = XmlFormatInfo.MakeNativeFormatInfo("utf-8");

		public static XmlFormatInfo MakeNativeFormatInfo(string encoding, DejitteringParams? dejitteringParams = null)
		{
			LoadedRegex headRe;
			headRe.Regex = RegexFactory.Instance.Create(@"\<\s*(m|f|ef)\s", ReOptions.None);
			headRe.SuffersFromPartialMatchProblem = false;
			return new XmlFormatInfo(
				typeof(SimpleFileMedia),
				null, headRe, new LoadedRegex(),
				null, null, encoding, null, TextStreamPositioningParams.Default, dejitteringParams, new FormatViewOptions());
		}

		public XmlFormatInfo(Type mediaType, XmlNode xsl, LoadedRegex headRe, LoadedRegex bodyRe, BoundFinder beginFinder, BoundFinder endFinder, string encoding, MessagesReaderExtensions.XmlInitializationParams extensionsInitData,
				TextStreamPositioningParams textStreamPositioningParams, DejitteringParams? dejitteringParams, IFormatViewOptions viewOptions) :
			base (mediaType, extensionsInitData)
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
		readonly LogSourceThreads threads;

		public MessagesReader(MediaBasedReaderParams readerParams, XmlFormatInfo fmt) :
			base(readerParams.Media, fmt.BeginFinder, fmt.EndFinder, fmt.ExtensionsInitData, fmt.TextStreamPositioningParams)
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

		static MessageBase MakeMessageInternal(TextMessageCapture capture, XmlFormatInfo formatInfo, IRegex bodyRe, ref IMatch bodyReMatch,
			MessagesBuilderCallback callback, XsltArgumentList transformArgs, DateTime sourceTime, TimeSpan timeOffset)
		{
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

				callback.SetCurrentPosition(capture.BeginPosition);
				
				//this.owner.xslExt.SetSourceTime(this.owner.MediaLastModified); todo?

				string messageStr = messageBuf.ToString();

				using (FactoryWriter factoryWriter = new FactoryWriter(callback, timeOffset))
				using (XmlReader xmlReader = XmlTextReader.Create(new StringReader(messageStr), xmlReaderSettings))
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
							// Try to parse the next messsage if it's not the end of the stream
							continue;
						}
					}
					
					var ret = factoryWriter.GetOutput();

					if (formatInfo.ViewOptions.RawViewAllowed)
					{
						ret.__SetRawText(StringSlice.Concat(capture.MessageHeaderSlice, capture.MessageBodySlice).Trim());
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
					GetHeaderReSplitterFlags(reader.formatInfo.HeadRe), reader.formatInfo.TextStreamPositioningParams)
			{
				this.reader = reader;
				this.callback = reader.CreateMessageBuilderCallback();
				this.bodyRegex = reader.formatInfo.BodyRe.Regex;
			}
			public override void ParserCreated(CreateParserParams p)
			{
				base.ParserCreated(p);
			}
			protected override MessageBase MakeMessage(TextMessageCapture capture)
			{
				return MakeMessageInternal(capture, reader.formatInfo, bodyRegex, ref bodyMatch, callback,
					reader.transformArgs, media.LastModified, reader.TimeOffset);
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
					GetHeaderReSplitterFlags(reader.formatInfo.HeadRe), reader.formatInfo.TextStreamPositioningParams)
			{
				this.reader = reader;
			}
			public override void ParserCreated(CreateParserParams p)
			{
				base.ParserCreated(p);
			}
			public override MessageBase MakeMessage(TextMessageCapture capture, ProcessingThreadLocalData threadLocal)
			{
				return MakeMessageInternal(capture, reader.formatInfo, threadLocal.bodyRe.Regex, 
					ref threadLocal.bodyMatch, threadLocal.callback, reader.transformArgs, media.LastModified, reader.TimeOffset);
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

		public override IPositionedMessagesParser CreateSearchingParser(CreateSearchingParserParams p)
		{
			return new SearchingParser(this, p, false, formatInfo.HeadRe, threads);
		}
	};

	class NativeXMLFormatFactory : IFileBasedLogProviderFactory, IMediaBasedReaderFactory
	{
		public static readonly NativeXMLFormatFactory Instance = new NativeXMLFormatFactory();

		static NativeXMLFormatFactory()
		{
			LogProviderFactoryRegistry.DefaultInstance.Register(Instance);
		}

		#region IFileReaderFactory Members

		public IEnumerable<string> SupportedPatterns
		{
			get 
			{
				yield return "*.xml";
			}
		}

		public IConnectionParams CreateParams(string fileName)
		{
			return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
		}

		#endregion

		#region ILogReaderFactory Members

		public string CompanyName
		{
			get { return "LogJoint"; }
		}

		public string FormatName
		{
			get { return "Native XML"; }
		}

		public string FormatDescription
		{
			get { return "XML log files created by LogJoint. LogJoint writes its own logs in files of this format."; }
		}

		public ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory)
		{
			return factory.CreateFileProviderFactoryUI(this);
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetFileBasedUserFriendlyConnectionName(connectParams);
		}

		public string GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		public IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemovePathParamIfItRefersToTemporaryFile(originalConnectionParams.Clone(true), TempFilesManager.GetInstance());
		}

		public ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new StreamLogProvider(host, NativeXMLFormatFactory.Instance, connectParams, 
				XmlFormatInfo.NativeFormatInfo, typeof(MessagesReader));
		}

		public IFormatViewOptions ViewOptions { get { return FormatViewOptions.Default; } }

		#endregion

		#region IMediaBasedReaderFactory Members
		public IPositionedMessagesReader CreateMessagesReader(MediaBasedReaderParams readerParams)
		{
			return new MessagesReader(readerParams, XmlFormat.XmlFormatInfo.NativeFormatInfo);
		}
		#endregion

	};

	class UserDefinedFormatFactory : UserDefinedFormatsManager.UserDefinedFactoryBase, 
		IFileBasedLogProviderFactory, IMediaBasedReaderFactory
	{
		List<string> patterns = new List<string>();
		XmlFormatInfo formatInfo;
		static XmlNamespaceManager nsMgr = new XmlNamespaceManager(new NameTable());
		static readonly string XSLNamespace = "http://www.w3.org/1999/XSL/Transform";

		static UserDefinedFormatFactory()
		{
			nsMgr.AddNamespace("xsl", XSLNamespace);

			Register(UserDefinedFormatsManager.DefaultInstance);
		}

		public static void Register(UserDefinedFormatsManager formatsManager)
		{
			formatsManager.RegisterFormatType(
				"xml", typeof(UserDefinedFormatFactory));
		}


		public UserDefinedFormatFactory(CreateParams createParams)
			: base(createParams)
		{
			var formatSpecificNode = createParams.FormatSpecificNode;
			ReadPatterns(formatSpecificNode, patterns);
			LoadedRegex head = ReadRe(formatSpecificNode, "head-re", ReOptions.Multiline);
			LoadedRegex body = ReadRe(formatSpecificNode, "body-re", ReOptions.Singleline);
			string encoding = ReadParameter(formatSpecificNode, "encoding");
			
			
			XmlDocument tmpDoc = new XmlDocument();
			tmpDoc.LoadXml(formatSpecificNode.ToString());
			XmlElement xsl = tmpDoc.DocumentElement.SelectSingleNode("xsl:stylesheet", nsMgr) as XmlElement;
			if (xsl == null)
				throw new Exception("Wrong XML-based format definition: xsl:stylesheet is not defined");

			var boundsNodes = formatSpecificNode.Elements("bounds").Take(1);
			var beginFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("begin")).FirstOrDefault());
			var endFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("end")).FirstOrDefault());

			Type mediaType = ReadType(formatSpecificNode, "media-type", typeof(SimpleFileMedia));
			MessagesReaderExtensions.XmlInitializationParams extensionsInitData = 
				new MessagesReaderExtensions.XmlInitializationParams(formatSpecificNode.Element("extensions"));

			DejitteringParams? dejitteringParams = DejitteringParams.FromConfigNode(
				formatSpecificNode.Element("dejitter"));

			TextStreamPositioningParams textStreamPositioningParams = TextStreamPositioningParams.FromConfigNode(
				formatSpecificNode);

			formatInfo = new XmlFormatInfo(mediaType, xsl, head, body, beginFinder, endFinder,
				encoding, extensionsInitData, textStreamPositioningParams, dejitteringParams, ViewOptions);
		}


		#region ILogReaderFactory Members

		public override ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory)
		{
			return factory.CreateFileProviderFactoryUI(this);
		}

		public override IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemovePathParamIfItRefersToTemporaryFile(originalConnectionParams.Clone(true), TempFilesManager.GetInstance());
		}

		public override string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetFileBasedUserFriendlyConnectionName(connectParams);
		}

		public override ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new StreamLogProvider(host, this, connectParams, formatInfo, typeof(MessagesReader));
		}

		#endregion

		#region IFileReaderFactory Members

		public IEnumerable<string> SupportedPatterns
		{
			get
			{
				return patterns;
			}
		}

		public new IConnectionParams CreateParams(string fileName)
		{
			return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
		}

		#endregion

		#region IMediaBasedReaderFactory Members
		public IPositionedMessagesReader CreateMessagesReader(MediaBasedReaderParams readerParams)
		{
			return new MessagesReader(readerParams, formatInfo);
		}
		#endregion
	};
}

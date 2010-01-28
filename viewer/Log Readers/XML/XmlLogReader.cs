using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogJoint.XmlFormat 
{
	static class Properties
	{
		public static readonly string LogJointNS = "http://logjoint.codeplex.com/";
	};

	class FactoryWriter : XmlWriter
	{
		public FactoryWriter(IMessagesBuilderCallback callback)
		{
			this.callback = callback;
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
			dateTime = new DateTime();
			severity = Content.SeverityFlag.Info;
		}

		public override void WriteEndElement()
		{
			states.Pop();

			if (elemName == null)
				return;

			if (thread == null)
				thread = callback.GetThread("");
			long position = callback.CurrentPosition;

			switch (elemName)
			{
				case "m":
					output = new Content(position, thread, dateTime, GetAndClearContent(), severity);
					break;
				case "f":
					output = new FrameBegin(position, thread, dateTime, GetAndClearContent());
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

		static DateTime ParseDateTime(string str)
		{
			return XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.Utc);
		}

		public override void WriteEndAttribute()
		{
			states.Pop();

			switch (attribName)
			{
				case "d":
					dateTime = ParseDateTime(GetAndClearContent());
					break;
				case "t":
					thread = callback.GetThread(GetAndClearContent());
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
			string ret = FieldsProcessor.TrimInsignificantSpace(content.ToString());
			content.Length = 0;
			return ret;
		}

		Stack<WriteState> states = new Stack<WriteState>();
		string elemName;
		string attribName;
		IThread thread;
		DateTime dateTime;
		StringBuilder content = new StringBuilder();
		Content.SeverityFlag severity = Content.SeverityFlag.Info;
		MessageBase output;
		readonly IMessagesBuilderCallback callback;
	};

	public class XSLExtension : FieldsProcessor.MessageBuilderFunctions
	{
	}

	class XmlFormatInfo
	{
		public readonly XslCompiledTransform Transform;
		public readonly string NSDeclaration = "";
		public readonly XsltArgumentList TransformArgs;
		public readonly string Encoding;
		public readonly Regex HeadRe;

		public bool IsNativeFormat { get { return Transform == null; } }

		public static readonly XmlFormatInfo NativeFormatInfo = new XmlFormatInfo();

		private XmlFormatInfo()
		{
			HeadRe = new Regex(@"\<\s*(m|f|ef)\s", RegexOptions.Compiled);
			Encoding = "utf-8";
		}

		public XmlFormatInfo(XmlNode xsl, Regex headRe, string encoding)
		{
			Encoding = encoding;
			HeadRe = headRe;

			Dictionary<string, string> nsTable = new Dictionary<string,string>();
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

			TransformArgs = new XsltArgumentList();
			TransformArgs.AddExtensionObject(Properties.LogJointNS, new XSLExtension());
		}
	};

	class LogReader: FileParsingLogReader
	{
		internal XmlFormatInfo formatInfo;

		public LogReader(ILogReaderHost host, ILogReaderFactory factory, XmlFormatInfo fmt, string fileName)
			: base(host, factory, fileName, fmt.HeadRe)
		{
			this.formatInfo = fmt;
			StartAsyncReader("Reader thread: " + fileName);
		}

		class StreamParser : Parser, IMessagesBuilderCallback
		{
			readonly Stream stream;
			readonly LogReader reader;
			long currentPosition;

			public StreamParser(LogReader reader, TextFileStream s, FileRange.Range? range, long startPosition, bool isMainStreamParser)
				:
				base(reader, s, range, startPosition, isMainStreamParser)
			{
				this.reader = reader;
				this.stream = s;
			}

			static DateTime ParseDateTime(string str)
			{
				return XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.Utc);
			}

			public override MessageBase ReadNext()
			{
				for (; ; )
				{
					TextFileStream.TextMessageCapture capture = this.Stream.GetCurrentMessageAndMoveToNextOne();
					if (capture == null)
						return null;

					StringBuilder messageBuf = new StringBuilder();
					messageBuf.AppendFormat("<root {0}>", reader.formatInfo.NSDeclaration.ToString());
					messageBuf.Append(capture.HeaderMatch.Groups[0].Value);
					messageBuf.Append(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength);
					messageBuf.Append("</root>");

					currentPosition = capture.BeginStreamPosition.Value;

					using (FactoryWriter factoryWriter = new FactoryWriter(this))
					using (XmlReader xmlReader = XmlTextReader.Create(new StringReader(messageBuf.ToString()), CreateXmlReaderSettings()))
					{
						try
						{
							if (reader.formatInfo.IsNativeFormat)
							{
								factoryWriter.WriteNode(xmlReader, false);
							}
							else
							{
								reader.formatInfo.Transform.Transform(xmlReader, reader.formatInfo.TransformArgs, factoryWriter);
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
						return factoryWriter.GetOutput();
					}
				}
			}

			public override void Dispose()
			{
				base.Dispose();
			}

			public long CurrentPosition
			{
				get { return currentPosition; }
			}

			public IThread GetThread(string id)
			{
				return reader.GetThread(id);
			}
		};

		protected override Parser CreateReader(TextFileStream s, FileRange.Range? range, long startPosition, bool isMainStreamParser)
		{
			return new StreamParser(this, s, range, startPosition, isMainStreamParser);
		}

		protected override Encoding GetStreamEncoding(TextFileStream stream)
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

	}

	class NativeXMLFormatFactory: IFileReaderFactory
	{
		public static readonly NativeXMLFormatFactory Instance = new NativeXMLFormatFactory();

		static NativeXMLFormatFactory()
		{
			LogReaderFactoryRegistry.Instance.Register(Instance);
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
			ConnectionParams p = new ConnectionParams();
			p["path"] = fileName;
			return p;
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

		public ILogReaderFactoryUI CreateUI()
		{
			return new FileLogFactoryUI(this);
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return connectParams["path"];
		}

		public ILogReader CreateFromConnectionParams(ILogReaderHost host, IConnectionParams connectParams)
		{
			return new LogReader(host, NativeXMLFormatFactory.Instance, XmlFormatInfo.NativeFormatInfo, connectParams["path"]);
		}

		#endregion
	};

	class UserDefinedFormatFactory : UserDefinedFormatsManager.UserDefinedFactoryBase, IFileReaderFactory
	{
		List<string> patterns = new List<string>();
		XmlFormatInfo formatInfo;
		static XmlNamespaceManager nsMgr = new XmlNamespaceManager(new NameTable());

		static UserDefinedFormatFactory()
		{
			nsMgr.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");

			UserDefinedFormatsManager.Instance.RegisterFormatType(
				"xml", typeof(UserDefinedFormatFactory));
		}

		public UserDefinedFormatFactory(string fileName, XmlNode rootNode, XmlNode formatSpecificNode)
			: base(fileName, rootNode, formatSpecificNode)
		{
			ReadPatterns(formatSpecificNode, patterns);
			Regex head = ReadRe(formatSpecificNode, "head-re", RegexOptions.Multiline);
			string encoding = ReadParameter(formatSpecificNode, "encoding");
			XmlNode xsl = formatSpecificNode.SelectSingleNode("xsl:stylesheet", nsMgr);
			if (xsl == null)
				throw new Exception("Wrong XML-based format definition: xsl:stylesheet is not defined");
			formatInfo = new XmlFormatInfo(xsl, head, encoding);
		}


		#region ILogReaderFactory Members

		public override ILogReaderFactoryUI CreateUI()
		{
			return new FileLogFactoryUI(this);
		}

		public override string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return connectParams["path"];
		}

		public override ILogReader CreateFromConnectionParams(ILogReaderHost host, IConnectionParams connectParams)
		{
			return new LogReader(host, this, formatInfo, connectParams["path"]);
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

		public IConnectionParams CreateParams(string fileName)
		{
			ConnectionParams p = new ConnectionParams();
			p["path"] = fileName;
			return p;
		}

		#endregion
	};
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;

namespace LogJoint.RegularGrammar
{
	class LogReader : FileParsingLogReader
	{
		Regex headerRe;
		Regex bodyRe;
		Encoding encoding;
		FieldsProcessor fieldsMapping;

		public LogReader(ILogReaderHost host, ILogReaderFactory factory,
			string fileName, Regex head, Regex body, string encoding, FieldsProcessor fieldsMapping)
			:
			base(host, factory, fileName)
		{
			this.headerRe = head;
			this.bodyRe = body;
			this.encoding = GetEncodingByXMLString(encoding);
			this.fieldsMapping = fieldsMapping;
			StartAsyncReader("Reader thread: " + fileName);
		}

		static Encoding GetEncodingByXMLString(string encoding)
		{
			if (encoding == null)
				encoding = "";
			switch (encoding)
			{
				case "ACP":
				case "":
					return Encoding.Default;
				case "BOM":
					return null;
				default:
					try
					{
						return Encoding.GetEncoding(encoding);
					}
					catch (ArgumentException)
					{
						return Encoding.Default;
					}
			}
		}

		class StreamParser : IStreamParser, IMessagesBuilderCallback
		{
			const int ParserBufferSize = 1024 * 16;
			const long MaximumMessageSize = 1024 * 2;

			readonly FieldsProcessor fieldsMapping;
			readonly LogReader reader;
			readonly Stream stream;
			readonly StringBuilder buf = new StringBuilder();
			readonly byte[] binBuf = new byte[ParserBufferSize];
			readonly long beginPosition;
			readonly long endPosition;
			readonly bool isMainStreamParser;
			readonly DateTime logFileLastModified;

			int headerEnd = 0;
			int headerStart = 0;
			int prevHeaderEnd = 0;
			long streamPos = 0;

			Match currMessageStart = null;

			int MoveBuffer()
			{
				long stmPosTmp = stream.Position;
				int bytesRead = stream.Read(binBuf, 0, binBuf.Length);
				char[] tmp = reader.encoding.GetChars(binBuf, 0, bytesRead);

				streamPos = stmPosTmp - (buf.Length - headerEnd);

				if (tmp.Length == 0)
					return 0;

				int ret = headerEnd;
				buf.Remove(0, headerEnd);
				buf.Append(tmp, 0, tmp.Length);

				headerEnd -= ret;
				headerStart -= ret;
				prevHeaderEnd -= ret;

				return ret;
			}

			public StreamParser(LogReader reader, Stream s, long endPosition, bool isMainStreamParser)
			{
				this.beginPosition = s.Position;
				this.reader = reader;
				this.stream = s;
				this.fieldsMapping = reader.fieldsMapping;
				this.endPosition = endPosition;
				this.isMainStreamParser = isMainStreamParser;
				this.logFileLastModified = File.GetLastWriteTime(reader.FileName);

				if (reader.encoding == null)
				{
					s.Position = 0;
					StreamReader tmpReader = new StreamReader(s, Encoding.Default, true);
					reader.encoding = tmpReader.CurrentEncoding ?? Encoding.Default;
					s.Position = this.beginPosition;
				}

				MoveBuffer();
				FindNextMessageStart();
				if (isMainStreamParser && currMessageStart == null && (endPosition - beginPosition) >= MaximumMessageSize)
					throw new Exception("Unable to parse the stream. The data seems to have incorrect format.");
			}

			Match FindNextMessageStart()
			{
/*				// Protection againts header regexps that can match empty strings.
				// Normally, FindNextMessageStart() returns null when it has reached the end of the stream
				// because the regex can't find the next line. The problem is that regex can be composed so
				// that is can match empty strings. In that case without this check we would never 
				// stop parsing the stream producing more and more empty messages.
				if (???)
				{
					currMessageStart = null;
					return null;
				}*/

				Match m = reader.headerRe.Match(buf.ToString(), headerEnd);
				if (!m.Success)
				{
					if (MoveBuffer() != 0)
					{
						m = reader.headerRe.Match(buf.ToString(), headerEnd);
					}
				}
				if (m.Success)
				{
					prevHeaderEnd = headerEnd;

					headerStart = m.Index;
					headerEnd = m.Index + m.Length;

					currMessageStart = m;
				}
				else
				{
					currMessageStart = null;
				}
				return currMessageStart;
			}

			public MessageBase ReadNext()
			{
				Match start = currMessageStart;
				if (start == null)
					return null;

				fieldsMapping.Reset();
				fieldsMapping.SetSourceTime(logFileLastModified);

				Match body = null;
				
				if (FindNextMessageStart() != null)
				{
					if (reader.bodyRe != null)
						body = reader.bodyRe.Match(buf.ToString(), prevHeaderEnd, headerStart - prevHeaderEnd);
				}
				else
				{	
					if (reader.bodyRe != null)
						body = reader.bodyRe.Match(buf.ToString(), headerEnd, buf.Length - headerEnd);
				}
				if (body != null && !body.Success)
					return null;

				int idx = 0;

				string[] names = reader.headerRe.GetGroupNames();
				for (int i = 1; i < start.Groups.Count; ++i)
					fieldsMapping.SetInputField(idx++, names[i], start.Groups[i].Value);

				if (body != null)
				{
					names = reader.bodyRe.GetGroupNames();
					for (int i = 1; i < body.Groups.Count; ++i)
						fieldsMapping.SetInputField(idx++, names[i], body.Groups[i].Value);
				}

				MessageBase ret = fieldsMapping.MakeMessage(this);

				ret.SetExtraHash(GetPositionBeforeNextMessage());

				return ret;
			}

			public long GetPositionBeforeNextMessage()
			{
				if (currMessageStart == null)
				{
					return streamPos;
				}
				else
				{
					return streamPos + headerStart;
				}
			}

			public long GetPositionOfNextMessage()
			{
				if (currMessageStart == null)
				{
					return stream.Position;
				}
				else
				{
					return streamPos + headerStart;
				}
			}

			public void Dispose()
			{
			}

			public IThread GetThread(string id)
			{
				if (isMainStreamParser)
					return reader.GetThread(id);
				return null;
			}
		};

		protected override IStreamParser CreateParser(Stream s, long endPosition, bool isMainStreamParser)
		{
			return new StreamParser(this, s, endPosition, isMainStreamParser);
		}
	};

	class UserDefinedFormatFactory : UserDefinedFormatsManager.UserDefinedFactoryBase, IFileReaderFactory
	{
		List<string> patterns = new List<string>();
		Regex head;
		Regex body;
		string encoding;
		FieldsProcessor fieldsMapping;

		static UserDefinedFormatFactory()
		{
			UserDefinedFormatsManager.Instance.RegisterFormatType(
				"regular-grammar", typeof(UserDefinedFormatFactory));
		}

		public UserDefinedFormatFactory(string fileName, XmlNode rootNode, XmlNode formatSpecificNode)
			: base(fileName, rootNode, formatSpecificNode)
		{
			ReadPatterns(formatSpecificNode);
			head = ReadRe(formatSpecificNode, "head-re", RegexOptions.Multiline);
			body = ReadRe(formatSpecificNode, "body-re", RegexOptions.Singleline);
			fieldsMapping = new FieldsProcessor(formatSpecificNode.SelectSingleNode("fields-config") as XmlElement);
			encoding = ReadParameter(formatSpecificNode, "encoding");
		}
		
		static Regex ReadRe(XmlNode root, string name, RegexOptions opts)
		{
			string s = ReadParameter(root, name);
			if (string.IsNullOrEmpty(s))
				return null;
			return new Regex(s, opts | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
		}

		void ReadPatterns(XmlNode formatSpecificNode)
		{
			foreach (XmlNode n in formatSpecificNode.SelectNodes("patterns/pattern[text()!='']"))
				patterns.Add(n.InnerText);
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
			return new LogReader(host, this, connectParams["path"], this.head, this.body, encoding,
				new FieldsProcessor(this.fieldsMapping));
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

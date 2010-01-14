using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using LogJoint;

namespace LogJoint.MSLogParser
{
	class MSLogParserXMLReader : FileParsingLogReader
	{
		protected readonly FieldsProcessor fieldsMapping;

		public MSLogParserXMLReader(ILogReaderHost host, 
				ILogReaderFactory factory, string fileName, FieldsProcessor fieldsMapping)
			: base(host, factory, fileName, new Regex(@"\<R\>\s+\<F"))
		{
			this.fieldsMapping = fieldsMapping;
		}

		class StreamParser : Parser, IMessagesBuilderCallback
		{
			readonly Stream stream;
			readonly MSLogParserXMLReader reader;
			readonly FieldsProcessor fieldsMapping;
			long currentPosition;

			public StreamParser(MSLogParserXMLReader reader, TextFileStream s, FileRange.Range? range,
					long startPosition, bool isMainStreamParser):
				base(reader, s, range, startPosition, isMainStreamParser)
			{
				this.reader = reader;
				this.stream = s;
				this.fieldsMapping = reader.fieldsMapping;
			}

			static DateTime ParseDateTime(string str)
			{
				return XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.Utc);
			}

			MessageBase HandleElement(XmlReader tr)
			{
				if (tr.Name != "R")
					return null;

				fieldsMapping.Reset();

				for (int fieldIdx = 0; ; )
				{
					if (!tr.Read())
						return null;

					if (tr.NodeType == XmlNodeType.Element)
					{
						string fieldName = null;
						if (true)
							fieldName = tr.GetAttribute(0);
						string value = null;

						tr.Read(); // read the content of the <F> (field) element or end element if field is empty

						if (tr.NodeType == XmlNodeType.Text)
						{
							value = tr.Value.Trim(); // get the value of the field
							tr.Read(); // end element </F>
						}

						fieldsMapping.SetInputField(fieldIdx, fieldName, value); // Handle the field
						++fieldIdx;
					}
					else if (tr.NodeType == XmlNodeType.EndElement)
					{
						break; // end of R element
					}
				}
				
				return fieldsMapping.MakeMessage(this);
			}

			public override MessageBase ReadNext()
			{
				TextFileStream.TextMessageCapture capture = this.Stream.GetCurrentMessageAndMoveToNextOne();
				if (capture == null)
					return null;

				StringBuilder messageBuf = new StringBuilder();
				messageBuf.Append(capture.HeaderMatch.Groups[0].Value);
				messageBuf.Append(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength);

				currentPosition = capture.BeginStreamPosition.Value;

				using (XmlReader xmlReader = XmlTextReader.Create(new StringReader(messageBuf.ToString()), CreateXmlReaderSettings()))
				{
					try
					{
						xmlReader.Read();
						if (xmlReader.NodeType == XmlNodeType.Element)
							return HandleElement(xmlReader);
					}
					catch (XmlException)
					{
						// There might be incomplete XML at the end of the stream. Ignore it.
					}
				}
				return null;
			}

			public override void Dispose()
			{
				base.Dispose();
			}

			public IThread GetThread(string id)
			{
				return reader.GetThread(id);
			}

			public long CurrentPosition
			{
				get { return currentPosition; }
			}
		};

		protected override Parser CreateReader(TextFileStream s, FileRange.Range? range, long startPosition, bool isMainStreamParser)
		{
			return new StreamParser(this, s, range, startPosition, isMainStreamParser);
		}

		internal static readonly StreamSearch.TrieNode elementStarts = CreateElementStarts();

		protected override Encoding GetStreamEncoding(TextFileStream stream)
		{
			return Encoding.UTF8;
		}

		static StreamSearch.TrieNode CreateElementStarts()
		{
			Encoding encoding = Encoding.UTF8;
			StreamSearch.TrieNode n = new StreamSearch.TrieNode();
			n.Add(encoding.GetBytes("<R>"), 0);
			return n;
		}

		internal static readonly XmlReaderSettings xmlReaderSettings = CreateXmlReaderSettings();

		static XmlReaderSettings CreateXmlReaderSettings()
		{
			XmlReaderSettings xrs = new XmlReaderSettings();
			xrs.ConformanceLevel = ConformanceLevel.Fragment;
			xrs.CheckCharacters = false;
			xrs.IgnoreWhitespace = true;
			xrs.CloseInput = false;
			return xrs;
		}

	}
}

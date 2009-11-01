using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using LogJoint;

namespace LogJoint.MSLogParser
{
	class MSLogParserXMLReader : FileParsingLogReader
	{
		const long NativeXmlStreamGranularity = 256;
		protected readonly FieldsProcessor fieldsMapping;

		public MSLogParserXMLReader(ILogReaderHost host, 
				ILogReaderFactory factory, string fileName, FieldsProcessor fieldsMapping)
			: base(host, factory, fileName)
		{
			this.fieldsMapping = fieldsMapping;
			StreamGranularity = (int)NativeXmlStreamGranularity;
		}

		class StreamParser : IStreamParser, IMessagesBuilderCallback
		{
			XmlReader tr;
			readonly Stream stream;
			readonly MSLogParserXMLReader reader;
			readonly FieldsProcessor fieldsMapping;
			readonly long endPosition;
			bool atTheBeginning;

			public StreamParser(MSLogParserXMLReader reader, Stream s, long endPosition)
			{
				this.reader = reader;
				this.stream = s;
				this.fieldsMapping = reader.fieldsMapping;
				this.endPosition = endPosition;
			}

			public long GetPositionOfNextMessage()
			{
				long pos = elementStarts.Find(stream);
				if (pos == -1)
					pos = endPosition;
				stream.Position = pos;
				atTheBeginning = true;
				return pos;
			}

			public long GetPositionBeforeNextMessage()
			{
				return stream.Position - NativeXmlStreamGranularity;
			}

			static DateTime ParseDateTime(string str)
			{
				return XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.Utc);
			}

			MessageBase HandleElement()
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

			public MessageBase ReadNext()
			{
				MessageBase msg = null;

				if (tr == null)
				{
					if (!atTheBeginning)
						GetPositionOfNextMessage();
					tr = XmlTextReader.Create(stream, MSLogParserXMLReader.xmlReaderSettings);
				}

				for (; ; )
				{
					try
					{
						tr.Read();

						if (tr.EOF)
							break;

						if (tr.NodeType == XmlNodeType.Element)
							if ((msg = HandleElement()) != null)
								break;
					}
					catch (XmlException)
					{
						break;
					}
				}

				return msg;
			}

			public void Dispose()
			{
				if (tr != null)
					tr.Close();
			}

			public IThread GetThread(string id)
			{
				return reader.GetThread(id);
			}
		};

		protected override IStreamParser CreateParser(Stream s, long endPosition, bool isMainStreamParser)
		{
			return new StreamParser(this, s, endPosition);
		}

		internal static readonly StreamSearch.TrieNode elementStarts = CreateElementStarts();

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

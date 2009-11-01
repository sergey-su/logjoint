using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace LogJoint.NativeFormat
{
	class LogReader: FileParsingLogReader
	{
		const long NativeXmlStreamGranularity = 256;

		public LogReader(ILogReaderHost host, ILogReaderFactory factory, string fileName)
			: base(host, factory, fileName)
		{
			StreamGranularity = (int)NativeXmlStreamGranularity;
			StartAsyncReader("Reader thread: " + fileName);
		}

		class StreamParser : IStreamParser
		{
			XmlReader tr;
			readonly Stream stream;
			readonly LogReader reader;
			readonly long endPosition;
			bool atTheBeginning;

			public StreamParser(LogReader reader, Stream s, long endPosition)
			{
				this.reader = reader;
				this.stream = s;
				this.endPosition = endPosition;
			}

			public long GetPositionOfNextMessage()
			{
				long pos = nativeLogElementStarts.Find(stream);
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

			ExceptionContent.ExceptionInfo ReadException()
			{
				string msg = "";
				string stack = "";
				ExceptionContent.ExceptionInfo inner = null;

				tr.Read();

				if (tr.NodeType == XmlNodeType.Text) // exception message
				{
					msg = TrimInsignificantSpace(tr.Value);
					tr.Read();
				}
				if (tr.NodeType == XmlNodeType.Element && tr.Name == "stack") // stack
				{
					if (tr.IsEmptyElement)
					{
						tr.Read();
					}
					else
					{
						tr.Read();
						if (tr.NodeType == XmlNodeType.Text)
							stack = TrimInsignificantSpace(tr.Value);
						while (!(tr.NodeType == XmlNodeType.EndElement && tr.Name == "stack"))
							tr.Read();
						tr.Read();
					}
				}
				if (tr.NodeType == XmlNodeType.Element && tr.Name == "exception") // inner exception 
					inner = ReadException();
				while (!(tr.NodeType == XmlNodeType.EndElement && tr.Name == "exception")) // end of exception
					tr.Read();
				return new ExceptionContent.ExceptionInfo(msg, stack, inner);
			}
			
			bool HandleElement(out MessageBase msg)
			{
				msg = null;
				switch (tr.Name)
				{
					case "log":
						break;
					case "thread":
						IThread t = reader.GetThread(tr.GetAttribute("id"));
						if (t.IsInitialized)
							break;
						string threadDescr = "";
						tr.Read();
						if (tr.NodeType == XmlNodeType.Text)
						{
							threadDescr = TrimInsignificantSpace(tr.Value);
							tr.Read();
						}
						t.Init(threadDescr);
						break;
					case "f":
						IThread ftd = reader.GetThread(tr.GetAttribute("t"));
						DateTime ftime = ParseDateTime(tr.GetAttribute("d"));
						tr.Read();
						string frameName = "";
						if (tr.NodeType == XmlNodeType.Text)
						{
							frameName = TrimInsignificantSpace(tr.Value);
							tr.Read();
						}
						msg = new FrameBegin(
							ftd,
							ftime,
							frameName
						);
						break;
					case "ef":
						IThread eft = reader.GetThread(tr.GetAttribute("t"));
						msg = new FrameEnd(eft, ParseDateTime(tr.GetAttribute("d")));
						break;
					case "m":
						IThread mtd = reader.GetThread(tr.GetAttribute("t"));
						DateTime time = ParseDateTime(tr.GetAttribute("d"));
						Content.SeverityFlag sev = Content.SeverityFlag.Info;
						switch (string.Intern(tr.GetAttribute("s") ?? ""))
						{
							case "w":
								sev = Content.SeverityFlag.Warning;
								break;
							case "e":
								sev = Content.SeverityFlag.Error;
								break;
						}
						tr.Read(); // Read the next token. 
						// It might be </m> or message text or <exception>.
						string messageText = "";
						if (tr.NodeType == XmlNodeType.Text)
						{
							messageText = TrimInsignificantSpace(tr.Value);
							tr.Read(); // Read the next token. 
							// It might be </m> or <exception>.
						}
						if (tr.NodeType == XmlNodeType.Element &&
							tr.Name == "exception")
						{
							msg = new ExceptionContent(
								mtd,
								time,
								messageText,
								ReadException()
							);
						}
						else
						{
							msg = new Content(
								mtd,
								time,
								messageText,
								sev
							);
						}
						break;
					case "eol":
						return false;
				}
				return true;
			}
				
			public MessageBase ReadNext()
			{
				MessageBase msg = null;
				// todo: add support for end-of-log
				//bool endOfLogReached = false; 

				if (tr == null)
				{
					if (!atTheBeginning)
						GetPositionOfNextMessage();
					tr = XmlTextReader.Create(stream, LogReader.xmlReaderSettings);
				}

				for (; ; )
				{
					tr.Read();

					if (tr.EOF)
						break;

					if (tr.NodeType == XmlNodeType.Element) // an element found
					{
						// try to handle it
						if (!HandleElement(out msg))
						{
							//endOfLogReached = true;
						}
						if (msg != null)
						{
							break;
						}
					}
				}

				return msg;
			}

			public void Dispose()
			{
				if (tr != null)
					tr.Close();
			}
		};

		protected override IStreamParser CreateParser(Stream s, long endPosition, bool isMainStreamParser)
		{
			return new StreamParser(this, s, endPosition);
		}

		internal static readonly StreamSearch.TrieNode nativeLogElementStarts = CreateNativeLogElementStarts();

		static StreamSearch.TrieNode CreateNativeLogElementStarts()
		{
			Encoding encoding = Encoding.UTF8;

			StreamSearch.TrieNode n = new StreamSearch.TrieNode();
			foreach (string s in new string[] { "m", "f", "ef", "thread" })
			{
				n.Add(encoding.GetBytes('<' + s + ' '), 0);
				n.Add(encoding.GetBytes('<' + s + '\t'), 0);
				n.Add(encoding.GetBytes('<' + s + "/>"), 0);
				n.Add(encoding.GetBytes('<' + s + ">"), 0);
			}

			return n;
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

	class Factory: IFileReaderFactory
	{
		public static readonly Factory Instance = new Factory();

		static Factory()
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
			return new LogReader(host, Factory.Instance, connectParams["path"]);
		}

		#endregion
	};
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Diagnostics;
using LogJoint.MessagesContainers;

namespace LogJoint.RegularGrammar
{
	class LogReader : FileParsingLogReader
	{
		readonly Regex headerRe;
		readonly Regex bodyRe;
		readonly FieldsProcessor fieldsMapping;
		readonly string encoding;
		readonly bool timeDependsOnSourceTime;

		public LogReader(ILogReaderHost host, ILogReaderFactory factory,
			string fileName, Regex head, Regex body, string encoding, FieldsProcessor fieldsMapping)
			:
			base(host, factory, fileName, head, null, null)
		{
			using (host.Trace.NewFrame)
			{
				this.headerRe = head;
				this.bodyRe = body;
				this.fieldsMapping = fieldsMapping;
				this.encoding = encoding;
				this.timeDependsOnSourceTime = fieldsMapping.TimeDependsOnSourceTime;
				if (timeDependsOnSourceTime)
				{
					tracer.Info("Time depends on source time");
				}
				StartAsyncReader("Reader thread: " + fileName);
			}
		}

		protected override Encoding GetStreamEncoding(TextFileStream stream)
		{
			Encoding ret = EncodingUtils.GetEncodingFromConfigXMLName(encoding);
			if (ret == null)
				ret = EncodingUtils.DetectEncodingFromBOM(stream, Encoding.Default);
			return ret;
		}

		class StreamParser : Parser, IMessagesBuilderCallback
		{
			readonly FieldsProcessor fieldsMapping;
			readonly LogReader reader;
			long? currentPosition;

			public StreamParser(LogReader reader, TextFileStream s, FileRange.Range? range, long startPosition, bool isMainStreamParser)
				: base(reader, s, range, startPosition, isMainStreamParser)
			{
				this.reader = reader;
				this.fieldsMapping = reader.fieldsMapping;
			}

			public override MessageBase ReadNext()
			{
				TextFileStream.TextMessageCapture capture = this.Stream.GetCurrentMessageAndMoveToNextOne();
				if (capture == null)
					return null;

				currentPosition = capture.BeginStreamPosition.Value;
				try
				{
					fieldsMapping.Reset();
					fieldsMapping.SetSourceTime(this.Stream.LastModified);

					Match body = null;
					if (reader.bodyRe != null)
						body = reader.bodyRe.Match(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength);

					if (body != null && !body.Success)
						return null;

					int idx = 0;
					string[] names;
					GroupCollection groups;

					names = reader.headerRe.GetGroupNames();
					groups = capture.HeaderMatch.Groups;
					for (int i = 1; i < groups.Count; ++i)
						fieldsMapping.SetInputField(idx++, names[i], groups[i].Value);

					if (body != null)
					{
						names = reader.bodyRe.GetGroupNames();
						groups = body.Groups;
						for (int i = 1; i < groups.Count; ++i)
							fieldsMapping.SetInputField(idx++, names[i], groups[i].Value);
					}

					MessageBase ret = fieldsMapping.MakeMessage(this);

					return ret;

				}
				finally
				{
					currentPosition = null;
				}
			}

			public IThread GetThread(string id)
			{
				return reader.GetThread(id);
			}

			public long CurrentPosition
			{
				get 
				{
					if (!currentPosition.HasValue)
						throw new InvalidOperationException("CurrentPosition cannot be read now");
					return currentPosition.Value;
				}
			}
		};

		protected override Parser CreateReader(TextFileStream s, FileRange.Range? range, long startPosition, bool isMainStreamReader)
		{
			return new StreamParser(this, s, range, startPosition, isMainStreamReader);
		}

		public override LogReaderTraits Traits
		{
			get 
			{
				LogReaderTraits ret = LogReaderTraits.None;
				if (!timeDependsOnSourceTime)
					ret |= LogReaderTraits.MessageTimeIsPersistent;
				return ret;
			}
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
			Tests.Run();
		}

		public UserDefinedFormatFactory(string fileName, XmlNode rootNode, XmlNode formatSpecificNode)
			: base(fileName, rootNode, formatSpecificNode)
		{
			ReadPatterns(formatSpecificNode, patterns);
			head = ReadRe(formatSpecificNode, "head-re", RegexOptions.Multiline);
			body = ReadRe(formatSpecificNode, "body-re", RegexOptions.Singleline);
			fieldsMapping = new FieldsProcessor(formatSpecificNode.SelectSingleNode("fields-config") as XmlElement, true);
			encoding = ReadParameter(formatSpecificNode, "encoding");
			foreach (XmlElement e in formatSpecificNode.SelectNodes("extensions/extension"))
			{
				fieldsMapping.AddExtension(new FieldsProcessor.ProcessorExtention(e.GetAttribute("name"),
					e.GetAttribute("class-name")));
			}
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

	static class Tests
	{
		//void PrepareDecoder(byte[] data,  Decoder d,

		public static void Run()
		{
			// utf8 bytes
			byte[] bytes = { 
				0x20, 0xe2, 0x98, 0xa3, 0x20, 0x20, 0x20, 0x20,
				0xe2, 0x98, 0xa3 // biohazard symbol
			};
			char[] chars = new char[10];
			Decoder d = Encoding.UTF8.GetDecoder();

			// Decode biohazard symbol
			d.Reset();
			Debug.Assert(d.GetChars(bytes, 8, 3, chars, 0) == 1 && chars[0]=='\u2623');

			// Decoding the biohazard symbol from the midde must produce 
			// two simbols. Biohazard won't be recognized of-course.
			d.Reset();
			Debug.Assert(d.GetChars(bytes, 9, 2, chars, 0) == 2);

			d.Reset();
			// Read 9 bytes. The last bytes is the beginning of biohazard symbol
			Debug.Assert(d.GetChars(bytes, 0, 9, chars, 0) == 6);
			// Read the biohazard symbol from the rest of the buffer
			Debug.Assert(d.GetChars(bytes, 9, 2, chars, 0) == 1 && chars[0]=='\u2623');
			
			d.Reset();
			// Read 7 bytes starting from 4-th one. 4-th byte is the middle of the biohazard symbol.
			// This 4-th byte causes fallback and produces substitution simbol.
			Debug.Assert(d.GetChars(bytes, 3, 7, chars, 0) == 5);
			// Read the biohazard symbol from the rest of the buffer
			Debug.Assert(d.GetChars(bytes, 10, 1, chars, 0) == 1 && chars[0]=='\u2623');
		}
	};
}

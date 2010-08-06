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
	class FormatInfo : StreamBasedFormatInfo
	{
		public readonly Regex HeadRe;
		public readonly Regex BodyRe;
		public readonly string Encoding;
		public readonly FieldsProcessor FieldsMapping;
		public FormatInfo(Type logMediaType, Regex headRe, Regex bodyRe, string encoding, FieldsProcessor fieldsMap):
			base(logMediaType)
		{
			this.HeadRe = headRe;
			this.BodyRe = bodyRe;
			this.Encoding = encoding;
			this.FieldsMapping = fieldsMap;
		}
	};

	class MessagesProvider : StreamBasedPositionedMessagesProvider
	{
		readonly LogSourceThreads threads;
		readonly FormatInfo fmtInfo;
		readonly FieldsProcessor fieldsProcessor;

		public MessagesProvider(LogSourceThreads threads, ILogMedia media, FormatInfo fmt):
			base(media, fmt.HeadRe, null, null)
		{
			this.threads = threads;
			this.fmtInfo = fmt;
			this.fieldsProcessor = new FieldsProcessor(fmt.FieldsMapping);
		}

		class StreamParser : Parser, IMessagesBuilderCallback
		{
			readonly FieldsProcessor fieldsMapping;
			readonly MessagesProvider reader;
			long? currentPosition;

			public StreamParser(MessagesProvider reader, TextFileStream s, FileRange.Range? range, long startPosition, bool isMainStreamParser)
				: base(reader, s, range, startPosition, isMainStreamParser)
			{
				this.reader = reader;
				this.fieldsMapping = reader.fieldsProcessor;
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
					if (reader.fmtInfo.BodyRe != null)
						body = reader.fmtInfo.BodyRe.Match(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength);

					if (body != null && !body.Success)
						return null;

					int idx = 0;
					string[] names;
					GroupCollection groups;

					names = reader.fmtInfo.HeadRe.GetGroupNames();
					groups = capture.HeaderMatch.Groups;
					for (int i = 1; i < groups.Count; ++i)
						fieldsMapping.SetInputField(idx++, names[i], groups[i].Value);

					if (body != null)
					{
						names = reader.fmtInfo.BodyRe.GetGroupNames();
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
				return reader.threads.GetThread(id);
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

		public override IPositionedMessagesParser CreateParser(long startPosition, FileRange.Range? range, bool isMainStreamReader)
		{
			return new StreamParser(this, base.FStream, range, startPosition, isMainStreamReader);
		}

		protected override Encoding GetStreamEncoding(TextFileStreamBase stream)
		{
			Encoding ret = EncodingUtils.GetEncodingFromConfigXMLName(fmtInfo.Encoding);
			if (ret == null)
				ret = EncodingUtils.DetectEncodingFromBOM(stream, Encoding.Default);
			return ret;
		}
	};

	class UserDefinedFormatFactory : UserDefinedFormatsManager.UserDefinedFactoryBase, IFileReaderFactory
	{
		List<string> patterns = new List<string>();
		FormatInfo fmtInfo;

		static UserDefinedFormatFactory()
		{
			UserDefinedFormatsManager.Instance.RegisterFormatType(
				"regular-grammar", typeof(UserDefinedFormatFactory));
		}

		public UserDefinedFormatFactory(string fileName, XmlNode rootNode, XmlNode formatSpecificNode)
			: base(fileName, rootNode, formatSpecificNode)
		{
			ReadPatterns(formatSpecificNode, patterns);
			FieldsProcessor fieldsMapping = new FieldsProcessor(formatSpecificNode.SelectSingleNode("fields-config") as XmlElement, true);
			foreach (XmlElement e in formatSpecificNode.SelectNodes("extensions/extension"))
			{
				fieldsMapping.AddExtension(new FieldsProcessor.ProcessorExtention(e.GetAttribute("name"),
					e.GetAttribute("class-name")));
			}
			Type mediaType = ReadType(formatSpecificNode, "media-type", typeof(SimpleFileMedia));
			fmtInfo = new FormatInfo(
				mediaType,
				ReadRe(formatSpecificNode, "head-re", RegexOptions.Multiline),
				ReadRe(formatSpecificNode, "body-re", RegexOptions.Singleline),
				ReadParameter(formatSpecificNode, "encoding"),
				fieldsMapping
			);
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
			return new StreamLogReader(host, this, connectParams, fmtInfo, typeof(MessagesProvider));
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

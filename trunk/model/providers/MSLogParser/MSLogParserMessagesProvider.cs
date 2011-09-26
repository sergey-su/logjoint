using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using LogJoint;
using LogJoint.RegularExpressions;

namespace LogJoint.MSLogParser
{
	class FormatInfo : StreamBasedFormatInfo
	{
		public FormatInfo(FieldsProcessor.InitializationParams fieldsParams):
			base(typeof(SimpleFileMedia), MessagesProviderExtensions.XmlInitializationParams.Empty)
		{
			this.fieldsParams = fieldsParams;
		}

		public FieldsProcessor.InitializationParams FieldsProcessorParams
		{
			get { return fieldsParams; }
		}

		readonly FieldsProcessor.InitializationParams fieldsParams;
	};

	class MessagesProvider : MediaBasedPositionedMessagesProvider
	{
		readonly LogSourceThreads threads;
		readonly FormatInfo formatInfo;
		readonly IRegex headerRe = RegexFactory.Instance.Create(@"\<R\>\s+\<F", ReOptions.Multiline);

		public MessagesProvider(LogSourceThreads threads, ILogMedia media, FormatInfo fmtInfo)
			: base(media, null, null, fmtInfo.ExtensionsInitData)
		{
			this.threads = threads;
			this.formatInfo = fmtInfo;
		}

		//FieldsProcessor CreateNewFieldsProcessor()
		//{
		//    return new FieldsProcessor(
		//        formatInfo.FieldsProcessorParams,
		//        fmtInfo.HeadRe.Regex.GetGroupNames().Skip(1),
		//        Extensions.Items.Select(ext => new FieldsProcessor.ExtensionInfo(ext.Name, ext.Instance))
		//    );
		//}

		MessagesBuilderCallback CreateMessageBuilderCallback()
		{
			IThread fakeThread = null;
			//fakeThread = threads.GetThread("");
			return new MessagesBuilderCallback(threads, fakeThread);
		}

		class SingleThreadedStrategyImpl : StreamParsingStrategies.SingleThreadedStrategy
		{
			readonly MessagesProvider provider;
			readonly MessagesBuilderCallback callback;

			FieldsProcessor fieldsProcessor;
			MakeMessageFlags currentParserFlags;

			public SingleThreadedStrategyImpl(MessagesProvider provider) :
				base(provider.LogMedia, provider.StreamEncoding, provider.headerRe, MessagesSplitterFlags.None)
			{
				this.provider = provider;
				this.callback = provider.CreateMessageBuilderCallback();
			}
			public override void ParserCreated(CreateParserParams p)
			{
				base.ParserCreated(p);
				currentParserFlags = ParserFlagsToMakeMessageFlags(p.Flags);
			}
			protected override MessageBase MakeMessage(TextMessageCapture capture)
			{
				return MakeMessageInternal(capture, ref fieldsProcessor, callback, currentParserFlags, media.LastModified);
			}
		};

		protected override StreamParsingStrategies.BaseStrategy CreateSingleThreadedStrategy()
		{
			return new SingleThreadedStrategyImpl(this);
		}

		static DateTime ParseDateTime(string str)
		{
			return XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.Utc);
		}

		static MessageBase HandleElement(XmlReader tr, MessagesBuilderCallback callback, MakeMessageFlags makeMessageFlags, ref FieldsProcessor fieldsProcessor)
		{
			if (tr.Name != "R")
				return null;

			fieldsProcessor.Reset();

			for (int fieldIdx = 0; ; )
			{
				if (!tr.Read())
					return null;

				if (tr.NodeType == XmlNodeType.Element)
				{
					string fieldName = null;
					if (true)
						fieldName = tr.GetAttribute(0);
					string value = "";

					tr.Read(); // read the content of the <F> (field) element or end element if field is empty

					if (tr.NodeType == XmlNodeType.Text)
					{
						value = tr.Value; // get the value of the field
						tr.Read(); // end element </F>
					}

					fieldsProcessor.SetInputField(fieldIdx, new StringSlice(value).Trim()); // Handle the field
					++fieldIdx;
				}
				else if (tr.NodeType == XmlNodeType.EndElement)
				{
					break; // end of R element
				}
			}

			return fieldsProcessor.MakeMessage(callback, makeMessageFlags);
		}

		static internal MessageBase MakeMessageInternal(TextMessageCapture capture, ref FieldsProcessor fieldsProcessor, 
			MessagesBuilderCallback callback, MakeMessageFlags makeMessageFlags, DateTime sourceTime)
		{
			StringBuilder messageBuf = new StringBuilder();
			capture.MessageHeaderSlice.Append(messageBuf);
			capture.MessageBodySlice.Append(messageBuf);

			callback.SetCurrentPosition(capture.BeginPosition);

			using (XmlReader xmlReader = XmlTextReader.Create(new StringReader(messageBuf.ToString()), xmlReaderSettings))
			{
				try
				{
					xmlReader.Read();
					if (xmlReader.NodeType == XmlNodeType.Element)
						return HandleElement(xmlReader, callback, makeMessageFlags, ref fieldsProcessor);
				}
				catch (XmlException)
				{
					// There might be incomplete XML at the end of the stream. Ignore it.
				}
			}

			return null;
		}

		protected override Encoding DetectStreamEncoding(Stream stream)
		{
			return Encoding.UTF8;
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

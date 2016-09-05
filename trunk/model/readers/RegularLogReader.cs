using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.IO;
using System.Xml;
using System.Diagnostics;
using LogJoint.MessagesContainers;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;

namespace LogJoint.RegularGrammar
{
	public class FormatInfo : StreamBasedFormatInfo
	{
		[Flags]
		public enum FormatFlags
		{
			None = 0,
			AllowPlainTextSearchOptimization = 1
		};

		public readonly LoadedRegex HeadRe;
		public readonly LoadedRegex BodyRe;
		public readonly string Encoding;
		public readonly FieldsProcessor.InitializationParams FieldsProcessorParams;
		public readonly DejitteringParams? DejitteringParams;
		public readonly TextStreamPositioningParams TextStreamPositioningParams;
		public readonly static string EmptyBodyReEquivalientTemplate = "^(?<body>.*)$";
		public readonly FormatFlags Flags;
		public readonly RotationParams RotationParams;
		public FormatInfo(
			LoadedRegex headRe, LoadedRegex bodyRe, 
			string encoding, FieldsProcessor.InitializationParams fieldsParams, 
			MessagesReaderExtensions.XmlInitializationParams extensionsInitData,
			DejitteringParams? dejitteringParams,
			TextStreamPositioningParams textStreamPositioningParams,
			FormatFlags flags,
			RotationParams rotationParams
		) :
			base(extensionsInitData)
		{
			this.HeadRe = headRe;
			this.BodyRe = bodyRe;
			this.Encoding = encoding;
			this.FieldsProcessorParams = fieldsParams;
			this.DejitteringParams = dejitteringParams;
			this.TextStreamPositioningParams = textStreamPositioningParams;
			this.Flags = flags;
			this.RotationParams = rotationParams;
		}
	};

	public class MessagesReader : MediaBasedPositionedMessagesReader
	{
		readonly ILogSourceThreads threads;
		readonly FormatInfo fmtInfo;
		readonly ITempFilesManager tempFilesManager;

		public MessagesReader(MediaBasedReaderParams readerParams, FormatInfo fmt) :
			base(readerParams.Media, null, null, fmt.ExtensionsInitData, fmt.TextStreamPositioningParams, readerParams.Flags, readerParams.SettingsAccessor)
		{
			if (readerParams.Threads == null)
				throw new ArgumentNullException("threads");
			this.threads = readerParams.Threads;
			this.fmtInfo = fmt;
			this.tempFilesManager = readerParams.TempFilesManager;

			base.Extensions.AttachExtensions();
		}

		FieldsProcessor CreateNewFieldsProcessor()
		{
			return CreateNewFieldsProcessor(fmtInfo, Extensions, tempFilesManager);
		}

		internal static FieldsProcessor CreateNewFieldsProcessor(FormatInfo fmtInfo, MessagesReaderExtensions extensions, ITempFilesManager tempFilesManager)
		{
			return new FieldsProcessor(
				fmtInfo.FieldsProcessorParams,
				fmtInfo.HeadRe.Regex.GetGroupNames().Skip(1).Concat(
					fmtInfo.BodyRe.Regex != null ? fmtInfo.BodyRe.Regex.GetGroupNames().Skip(1) : Enumerable.Repeat("body", 1)),
				extensions.Items.Select(ext => new FieldsProcessor.ExtensionInfo(ext.Name, ext.AssemblyName, ext.ClassName, ext.Instance)),
				tempFilesManager
			);
		}

		MessagesBuilderCallback CreateMessageBuilderCallback()
		{
			IThread fakeThread = null;
			//fakeThread = threads.GetThread("");
			return new MessagesBuilderCallback(threads, fakeThread);
		}

		static IMessage MakeMessageInternal(
			TextMessageCapture capture,
			IRegex headRe,
			IRegex bodyRe,
			ref IMatch bodyMatch,
			FieldsProcessor fieldsProcessor,
			MakeMessageFlags makeMessageFlags,
			DateTime sourceTime,
			ITimeOffsets timeOffsets,
			MessagesBuilderCallback threadLocalCallbackImpl)
		{
			if (bodyRe != null)
				if (!bodyRe.Match(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength, ref bodyMatch))
					return null;

			int idx = 0;
			Group[] groups;

			fieldsProcessor.Reset();
			fieldsProcessor.SetSourceTime(sourceTime);
			fieldsProcessor.SetPosition(capture.BeginPosition);
			fieldsProcessor.SetTimeOffsets(timeOffsets);

			groups = capture.HeaderMatch.Groups;
			for (int i = 1; i < groups.Length; ++i)
			{
				var g = groups[i];
				fieldsProcessor.SetInputField(idx++, new StringSlice(capture.HeaderBuffer, g.Index, g.Length));
			}

			if (bodyRe != null)
			{
				groups = bodyMatch.Groups;
				for (int i = 1; i < groups.Length; ++i)
				{
					var g = groups[i];
					fieldsProcessor.SetInputField(idx++, new StringSlice(capture.BodyBuffer, g.Index, g.Length));
				}
			}
			else
			{
				fieldsProcessor.SetInputField(idx++, new StringSlice(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength));
			}

			threadLocalCallbackImpl.SetCurrentPosition(capture.BeginPosition, capture.EndPosition);

			IMessage ret;
			ret = fieldsProcessor.MakeMessage(threadLocalCallbackImpl, makeMessageFlags);

			ret.SetRawText(StringSlice.Concat(capture.MessageHeaderSlice, capture.MessageBodySlice).Trim());

			return ret;
		}

		class SingleThreadedStrategyImpl : StreamParsingStrategies.SingleThreadedStrategy
		{
			readonly MessagesReader reader;
			readonly FieldsProcessor fieldsProcessor;
			readonly MessagesBuilderCallback callback;
			readonly IRegex headerRegex, bodyRegex;
			IMatch bodyMatch;

			MakeMessageFlags currentParserFlags;

			public SingleThreadedStrategyImpl(MessagesReader reader) : base(
				reader.LogMedia,
				reader.StreamEncoding,
				CloneRegex(reader.fmtInfo.HeadRe, reader.IsQuickFormatDetectionMode ? ReOptions.Timeboxed : ReOptions.None).Regex,
				GetHeaderReSplitterFlags(reader.fmtInfo.HeadRe),
				reader.fmtInfo.TextStreamPositioningParams
			)
			{
				this.reader = reader;
				this.fieldsProcessor = reader.CreateNewFieldsProcessor();
				this.callback = reader.CreateMessageBuilderCallback();
				this.headerRegex = headerRe;
				this.bodyRegex = CloneRegex(reader.fmtInfo.BodyRe).Regex;
			}
			public override void ParserCreated(CreateParserParams p)
			{
				base.ParserCreated(p);
				currentParserFlags = ParserFlagsToMakeMessageFlags(p.Flags);
			}
			protected override IMessage MakeMessage(TextMessageCapture capture)
			{
				return MakeMessageInternal(capture, headerRegex, bodyRegex, ref bodyMatch, fieldsProcessor, currentParserFlags, 
					media.LastModified, reader.TimeOffsets, callback);
			}
		};

		protected override StreamParsingStrategies.BaseStrategy CreateSingleThreadedStrategy()
		{
			return new SingleThreadedStrategyImpl(this);
		}

#if !SILVERLIGHT

		class ProcessingThreadLocalData
		{
			public LoadedRegex headRe;
			public LoadedRegex bodyRe;
			public IMatch bodyMatch;
			public FieldsProcessor fieldsProcessor;
			public MessagesBuilderCallback callback;
		}

		class MultiThreadedStrategyImpl : StreamParsingStrategies.MultiThreadedStrategy<ProcessingThreadLocalData>
		{
			MessagesReader reader;
			MakeMessageFlags flags;

			public MultiThreadedStrategyImpl(MessagesReader reader) :
				base(reader.LogMedia, reader.StreamEncoding, reader.fmtInfo.HeadRe.Regex,
					GetHeaderReSplitterFlags(reader.fmtInfo.HeadRe), reader.fmtInfo.TextStreamPositioningParams)
			{
				this.reader = reader;
			}
			public override void ParserCreated(CreateParserParams p)
			{
				base.ParserCreated(p);
				flags = ParserFlagsToMakeMessageFlags(p.Flags);
			}
			public override IMessage MakeMessage(TextMessageCapture capture, ProcessingThreadLocalData threadLocal)
			{
				return MakeMessageInternal(capture, threadLocal.headRe.Regex, threadLocal.bodyRe.Regex, ref threadLocal.bodyMatch, threadLocal.fieldsProcessor, flags, media.LastModified, 
					reader.TimeOffsets, threadLocal.callback);
			}
			public override ProcessingThreadLocalData InitializeThreadLocalState()
			{
				ProcessingThreadLocalData ret = new ProcessingThreadLocalData();
				ret.headRe = CloneRegex(reader.fmtInfo.HeadRe, reader.IsQuickFormatDetectionMode ? ReOptions.Timeboxed : ReOptions.None);
				ret.bodyRe = CloneRegex(reader.fmtInfo.BodyRe);
				ret.fieldsProcessor = reader.CreateNewFieldsProcessor();
				ret.callback = reader.CreateMessageBuilderCallback();
				ret.bodyMatch = null;
				return ret;
			}
		};

		protected override StreamParsingStrategies.BaseStrategy CreateMultiThreadedStrategy()
		{
			return new MultiThreadedStrategyImpl(this);
		}
#else

		protected override StreamParsingStrategies.BaseStrategy CreateMultiThreadedStrategy()
		{
			return null;
		}

#endif

		protected override Encoding DetectStreamEncoding(Stream stream)
		{
			Encoding ret = EncodingUtils.GetEncodingFromConfigXMLName(fmtInfo.Encoding);
			if (ret == null)
				ret = EncodingUtils.DetectEncodingFromBOM(stream, EncodingUtils.GetDefaultEncoding());
			return ret;
		}

		protected override DejitteringParams? GetDejitteringParams()
		{
			return fmtInfo.DejitteringParams;
		}

		public override IPositionedMessagesParser CreateSearchingParser(CreateSearchingParserParams p)
		{
			return new SearchingParser(this, p, (fmtInfo.Flags & FormatInfo.FormatFlags.AllowPlainTextSearchOptimization) != 0, 
				fmtInfo.HeadRe, threads);
		}
	};

	public class UserDefinedFormatFactory : 
		UserDefinedFactoryBase,
		IFileBasedLogProviderFactory, IMediaBasedReaderFactory, IUserCodePrecompile
	{
		List<string> patterns = new List<string>();
		Lazy<FormatInfo> fmtInfo;
		readonly string uiKey;
		readonly ITempFilesManager tempFilesManager;

		[RegistrationMethod]
		public static void Register(IUserDefinedFormatsManager formatsManager)
		{
			formatsManager.RegisterFormatType(
				"regular-grammar", typeof(UserDefinedFormatFactory));
		}

		public UserDefinedFormatFactory(UserDefinedFactoryParams createParams)
			: base(createParams)
		{
			var formatSpecificNode = createParams.FormatSpecificNode;
			ReadPatterns(formatSpecificNode, patterns);
			tempFilesManager = createParams.TempFilesManager;
			fmtInfo = new Lazy<FormatInfo>(() =>
			{
				Type precompiledUserCode = ReadPrecompiledUserCode(createParams.RootNode);
				FieldsProcessor.InitializationParams fieldsInitParams = new FieldsProcessor.InitializationParams(
					formatSpecificNode.Element("fields-config"), true, precompiledUserCode);
				MessagesReaderExtensions.XmlInitializationParams extensionsInitData = new MessagesReaderExtensions.XmlInitializationParams(
					formatSpecificNode.Element("extensions"));
				DejitteringParams? dejitteringParams = DejitteringParams.FromConfigNode(
					formatSpecificNode.Element("dejitter"));
				TextStreamPositioningParams textStreamPositioningParams = TextStreamPositioningParams.FromConfigNode(
					formatSpecificNode);
				RotationParams rotationParams = RotationParams.FromConfigNode(
					formatSpecificNode.Element("rotation"));
				FormatInfo.FormatFlags flags = FormatInfo.FormatFlags.None;
				if (formatSpecificNode.Element("plain-text-search-optimization").AttributeValue("allowed") == "yes")
					flags |= FormatInfo.FormatFlags.AllowPlainTextSearchOptimization;
				return new FormatInfo(
					ReadRe(formatSpecificNode, "head-re", ReOptions.Multiline),
					ReadRe(formatSpecificNode, "body-re", ReOptions.Singleline),
					ReadParameter(formatSpecificNode, "encoding"),
					fieldsInitParams,
					extensionsInitData,
					dejitteringParams,
					textStreamPositioningParams,
					flags,
					rotationParams
				);
			});
			uiKey = ReadParameter(formatSpecificNode, "ui-key");
		}

		public IPositionedMessagesReader CreateMessagesReader(MediaBasedReaderParams readerParams)
		{
			return new MessagesReader(readerParams, fmtInfo.Value);
		}
		
		#region ILogReaderFactory Members

		public override string UITypeKey { get { return string.IsNullOrEmpty(uiKey) ? StdProviderFactoryUIs.FileBasedProviderUIKey : uiKey; } }

		public override string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetFileOrFolderBasedUserFriendlyConnectionName(connectParams);
		}

		public override IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), tempFilesManager);
		}

		public override ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new StreamLogProvider(host, this, connectParams, fmtInfo.Value, typeof(MessagesReader));
		}

		public override LogProviderFactoryFlag Flags
		{
			get
			{
				var ret = LogProviderFactoryFlag.SupportsDejitter | LogProviderFactoryFlag.SupportsReordering;
				if (fmtInfo.Value.DejitteringParams.HasValue)
					ret |= LogProviderFactoryFlag.DejitterEnabled;
				if (fmtInfo.Value.RotationParams.IsSupported)
					ret |= LogProviderFactoryFlag.SupportsRotation;
				return ret;
			}
		}

		#endregion

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


		public Type CompileUserCodeToType(CompilationTargetFx targetFx, Func<string, string> assemblyLocationResolver)
		{
			using (MessagesReaderExtensions extensions = new MessagesReaderExtensions(null, fmtInfo.Value.ExtensionsInitData))
			{
				var fieldsProcessor = MessagesReader.CreateNewFieldsProcessor(this.fmtInfo.Value, extensions, tempFilesManager);
				var type = fieldsProcessor.CompileUserCodeToType(targetFx, assemblyLocationResolver);
				return type;
			}
		}
	};
}

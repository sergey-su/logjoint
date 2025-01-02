using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

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
        public readonly FieldsProcessor.IInitializationParams FieldsProcessorParams;
        public readonly DejitteringParams? DejitteringParams;
        public readonly TextStreamPositioningParams TextStreamPositioningParams;
        public readonly static string EmptyBodyReEquivalientTemplate = "^(?<body>.*)$";
        public readonly FormatFlags Flags;
        public readonly RotationParams RotationParams;
        public readonly BoundFinder BeginFinder;
        public readonly BoundFinder EndFinder;

        public FormatInfo(
            LoadedRegex headRe, LoadedRegex bodyRe,
            string encoding, FieldsProcessor.IInitializationParams fieldsParams,
            MessagesReaderExtensions.XmlInitializationParams extensionsInitData,
            DejitteringParams? dejitteringParams,
            TextStreamPositioningParams textStreamPositioningParams,
            FormatFlags flags,
            RotationParams rotationParams,
            BoundFinder beginFinder,
            BoundFinder endFinder
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
            this.BeginFinder = beginFinder;
            this.EndFinder = endFinder;
        }

        public IEnumerable<string> InputFieldNames =>
                HeadRe.Regex.GetGroupNames().Skip(1).Concat(
                    BodyRe.Regex != null ? BodyRe.Regex.GetGroupNames().Skip(1) :
                    HeadRe.Regex.GetGroupNames().Contains("body") ? Enumerable.Empty<string>() :
                    Enumerable.Repeat("body", 1)
                );
    };

    public class MessagesReader : MediaBasedPositionedMessagesReader
    {
        readonly ILogSourceThreadsInternal threads;
        readonly FormatInfo fmtInfo;
        readonly FieldsProcessor.IFactory fieldsProcessorFactory;
        readonly ITraceSourceFactory traceSourceFactory;
        readonly IRegexFactory regexFactory;
        readonly Lazy<ValueTask<bool>> isBodySingleFieldExpression;

        public MessagesReader(
            MediaBasedReaderParams readerParams,
            FormatInfo fmt,
            FieldsProcessor.IFactory fieldsProcessorFactory,
            IRegexFactory regexFactory,
            ITraceSourceFactory traceSourceFactory,
            Settings.IGlobalSettingsAccessor settings
        ) :
            base(readerParams.Media, fmt.BeginFinder, fmt.EndFinder, fmt.ExtensionsInitData, fmt.TextStreamPositioningParams,
                readerParams.QuickFormatDetectionMode, settings, traceSourceFactory, readerParams.ParentLoggingPrefix)
        {
            if (readerParams.Threads == null)
                throw new ArgumentNullException(nameof(readerParams) + ".Threads");
            this.threads = readerParams.Threads;
            this.traceSourceFactory = traceSourceFactory;
            this.regexFactory = regexFactory;
            this.fmtInfo = fmt;
            this.fieldsProcessorFactory = fieldsProcessorFactory;

            base.Extensions.AttachExtensions();

            this.isBodySingleFieldExpression = new Lazy<ValueTask<bool>>(async () =>
            {
                return (await CreateNewFieldsProcessor()).IsBodySingleFieldExpression();
            });
        }

        ValueTask<FieldsProcessor.IFieldsProcessor> CreateNewFieldsProcessor()
        {
            return fieldsProcessorFactory.CreateProcessor(
                fmtInfo.FieldsProcessorParams,
                fmtInfo.InputFieldNames,
                Extensions.Items.Select(ext => new FieldsProcessor.ExtensionInfo(ext.Name, ext.AssemblyName, ext.ClassName, ext.Instance)),
                Trace
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
            IRegex bodyRe,
            ref IMatch bodyMatch,
            FieldsProcessor.IFieldsProcessor fieldsProcessor,
            FieldsProcessor.MakeMessageFlags makeMessageFlags,
            DateTime sourceTime,
            ITimeOffsets timeOffsets,
            MessagesBuilderCallback threadLocalCallbackImpl
        )
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
            threadLocalCallbackImpl.SetRawText(StringSlice.Concat(capture.MessageHeaderSlice, capture.MessageBodySlice).Trim());

            var ret = fieldsProcessor.MakeMessage(threadLocalCallbackImpl, makeMessageFlags | FieldsProcessor.MakeMessageFlags.LazyBody);

            return ret;
        }

        class SingleThreadedStrategyImpl : StreamReadingStrategies.SingleThreadedStrategy
        {
            readonly MessagesReader reader;
            readonly MessagesBuilderCallback callback;
            readonly IRegex bodyRegex;
            readonly Profiling.Counters.Writer perfWriter;
            FieldsProcessor.IFieldsProcessor fieldsProcessor;
            IMatch bodyMatch;

            FieldsProcessor.MakeMessageFlags currentParserFlags;

            public SingleThreadedStrategyImpl(MessagesReader reader) : base(
                reader.LogMedia,
                reader.StreamEncoding,
                reader.IsQuickFormatDetectionMode ? reader.fmtInfo.HeadRe.Regex.ToTimeboxed() : reader.fmtInfo.HeadRe.Regex,
                reader.fmtInfo.HeadRe.GetHeaderReSplitterFlags(),
                reader.fmtInfo.TextStreamPositioningParams
            )
            {
                this.reader = reader;
                this.callback = reader.CreateMessageBuilderCallback();
                this.bodyRegex = reader.fmtInfo.BodyRe.Regex;
                this.perfWriter = reader.PerfCounters.GetWriter();
            }
            public override async Task ParserCreated(ReadMessagesParams p)
            {
                this.fieldsProcessor = await reader.CreateNewFieldsProcessor();
                await base.ParserCreated(p);
                currentParserFlags = ParserFlagsToMakeMessageFlags(p.Flags);
            }
            protected override IMessage MakeMessage(TextMessageCapture capture)
            {
                using var perfop = perfWriter.IncrementTicks(reader.ReadMessageCounter);
                var result = MakeMessageInternal(capture, bodyRegex, ref bodyMatch, fieldsProcessor, currentParserFlags,
                    media.LastModified, reader.TimeOffsets, callback);
                return result;
            }
        };

        protected override StreamReadingStrategies.BaseStrategy CreateSingleThreadedStrategy()
        {
            return new SingleThreadedStrategyImpl(this);
        }

#if !SILVERLIGHT

        class ProcessingThreadLocalData
        {
            public LoadedRegex headRe;
            public LoadedRegex bodyRe;
            public IMatch bodyMatch;
            public FieldsProcessor.IFieldsProcessor fieldsProcessor;
            public MessagesBuilderCallback callback;
        }

        class MultiThreadedStrategyImpl : StreamReadingStrategies.MultiThreadedStrategy<ProcessingThreadLocalData>
        {
            readonly MessagesReader reader;
            FieldsProcessor.MakeMessageFlags flags;

            public MultiThreadedStrategyImpl(MessagesReader reader) :
                base(reader.LogMedia, reader.StreamEncoding, reader.fmtInfo.HeadRe.Regex,
                        reader.fmtInfo.HeadRe.GetHeaderReSplitterFlags(), reader.fmtInfo.TextStreamPositioningParams,
                        reader.Trace.Prefix, reader.traceSourceFactory)
            {
                this.reader = reader;
            }
            public override async Task ParserCreated(ReadMessagesParams p)
            {
                await base.ParserCreated(p);
                flags = ParserFlagsToMakeMessageFlags(p.Flags);
            }
            public override IMessage MakeMessage(TextMessageCapture capture, ProcessingThreadLocalData threadLocal)
            {
                return MakeMessageInternal(capture, threadLocal.bodyRe.Regex, ref threadLocal.bodyMatch, threadLocal.fieldsProcessor, flags, media.LastModified,
                    reader.TimeOffsets, threadLocal.callback);
            }
            public override ProcessingThreadLocalData InitializeThreadLocalState()
            {
                ProcessingThreadLocalData ret = new ProcessingThreadLocalData
                {
                    headRe = reader.fmtInfo.HeadRe.WithRegex(
                        reader.IsQuickFormatDetectionMode ? reader.fmtInfo.HeadRe.Regex.ToTimeboxed() : reader.fmtInfo.HeadRe.Regex),
                    bodyRe = reader.fmtInfo.BodyRe,
                    fieldsProcessor = reader.CreateNewFieldsProcessor().Result,
                    callback = reader.CreateMessageBuilderCallback(),
                    bodyMatch = null
                };
                return ret;
            }
        };

        protected override StreamReadingStrategies.BaseStrategy CreateMultiThreadedStrategy()
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
            Encoding ret = EncodingUtils.GetEncodingFromConfigXMLName(fmtInfo.Encoding, Trace);
            if (ret == null)
                ret = EncodingUtils.DetectEncodingFromBOM(stream, EncodingUtils.GetDefaultEncoding());
            return ret;
        }

        protected override DejitteringParams? GetDejitteringParams()
        {
            return fmtInfo.DejitteringParams;
        }

        public override async Task<ISearchingParser> CreateSearchingParser(SearchMessagesParams p)
        {
            var allowPlainTextSearchOptimization =
                   (fmtInfo.Flags & FormatInfo.FormatFlags.AllowPlainTextSearchOptimization) != 0
                || p.SearchParams.SearchInRawText
                || await isBodySingleFieldExpression.Value;
            return new SearchingParser(
                this,
                p,
                ((ITextStreamPositioningParamsProvider)this).TextStreamPositioningParams,
                GetDejitteringParams(),
                VolatileStream,
                StreamEncoding,
                allowPlainTextSearchOptimization,
                fmtInfo.HeadRe,
                traceSourceFactory,
                regexFactory
            );
        }
    };

    public class UserDefinedFormatFactory :
        UserDefinedFactoryBase,
        IFileBasedLogProviderFactory,
        IPrecompilingLogProviderFactory,
        IMediaBasedReaderFactory
    {
        readonly List<string> patterns = new List<string>();
        readonly Lazy<FormatInfo> fmtInfo;
        readonly string uiKey;
        readonly ITempFilesManager tempFilesManager;
        readonly ProviderFactory providerFactory;
        readonly ReaderFactory readerFactory;
        readonly FieldsProcessor.IFactory fieldsProcessorFactory;

        public static string ConfigNodeName => "regular-grammar";

        public static UserDefinedFormatFactory Create(
            UserDefinedFactoryParams createParams, ITempFilesManager tempFilesManager, IRegexFactory regexFactory,
            FieldsProcessor.IFactory fieldsProcessorFactory, ITraceSourceFactory traceSourceFactory,
            ISynchronizationContext modelSynchronizationContext, Settings.IGlobalSettingsAccessor globalSettingsAccessor,
            LogMedia.IFileSystem fileSystem)
        {
            return new UserDefinedFormatFactory(createParams, tempFilesManager, regexFactory, fieldsProcessorFactory,
                (host, connectParams, factory, readerFactory) => new StreamLogProvider(host, factory, connectParams, readerFactory,
                    tempFilesManager, traceSourceFactory, modelSynchronizationContext, globalSettingsAccessor, fileSystem),
                (@params, fmtInfo) => new MessagesReader(@params, fmtInfo, fieldsProcessorFactory, regexFactory,
                    traceSourceFactory, globalSettingsAccessor));
        }

        private delegate ILogProvider ProviderFactory(
            ILogProviderHost host, IConnectionParams connectionParams, UserDefinedFormatFactory factory,
            Func<MediaBasedReaderParams, IPositionedMessagesReader> readerFactory);
        private delegate IPositionedMessagesReader ReaderFactory(
            MediaBasedReaderParams @params, FormatInfo fmtInfo);


        private UserDefinedFormatFactory(UserDefinedFactoryParams createParams, ITempFilesManager tempFilesManager, IRegexFactory regexFactory,
            FieldsProcessor.IFactory fieldsProcessorFactory, ProviderFactory providerFactory, ReaderFactory readerFactory)
            : base(createParams, regexFactory)
        {
            var formatSpecificNode = createParams.FormatSpecificNode;
            ReadPatterns(formatSpecificNode, patterns);
            var boundsNodes = formatSpecificNode.Elements("bounds").Take(1);
            var beginFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("begin")).FirstOrDefault());
            var endFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("end")).FirstOrDefault());
            this.tempFilesManager = tempFilesManager;
            this.fieldsProcessorFactory = fieldsProcessorFactory;
            this.providerFactory = providerFactory;
            this.readerFactory = readerFactory;
            fmtInfo = new Lazy<FormatInfo>(() =>
            {
                FieldsProcessor.IInitializationParams fieldsInitParams = fieldsProcessorFactory.CreateInitializationParams(
                    formatSpecificNode.Element("fields-config"), performChecks: true);
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
                    ReadRe(formatSpecificNode, "head-re", ReOptions.Multiline, extensionsInitData),
                    ReadRe(formatSpecificNode, "body-re", ReOptions.Singleline, extensionsInitData),
                    ReadParameter(formatSpecificNode, "encoding"),
                    fieldsInitParams,
                    extensionsInitData,
                    dejitteringParams,
                    textStreamPositioningParams,
                    flags,
                    rotationParams,
                    beginFinder,
                    endFinder
                );
            });
            uiKey = ReadParameter(formatSpecificNode, "ui-key");
        }

        IPositionedMessagesReader IMediaBasedReaderFactory.CreateMessagesReader(MediaBasedReaderParams readerParams)
        {
            return readerFactory(readerParams, fmtInfo.Value);
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

        public override Task<ILogProvider> CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
        {
            return Task.FromResult(providerFactory(host, connectParams, this, @params => readerFactory(@params, fmtInfo.Value)));
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

        IEnumerable<string> IFileBasedLogProviderFactory.SupportedPatterns => patterns;

        IConnectionParams IFileBasedLogProviderFactory.CreateParams(string fileName)
        {
            return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
        }

        IConnectionParams IFileBasedLogProviderFactory.CreateRotatedLogParams(string folder, IEnumerable<string> patterns)
        {
            return ConnectionParamsUtils.CreateRotatedLogConnectionParamsFromFolderPath(folder, this, patterns);
        }

        byte[] IPrecompilingLogProviderFactory.Precompile(string assemblyName, LJTraceSource trace)
        {
            return fieldsProcessorFactory.CreatePrecompiledAssembly(
                fmtInfo.Value.FieldsProcessorParams,
                fmtInfo.Value.InputFieldNames,
                fmtInfo.Value.ExtensionsInitData.Items.Select(
                    i => new FieldsProcessor.ExtensionInfo(
                        i.name, i.assemblyName, i.className,
                        () => throw new InvalidOperationException(
                            $"Attempted to instantiate format extension {i.name} while precompiling")
                    )
                ),
                assemblyName,
                trace
            );
        }
    };
}

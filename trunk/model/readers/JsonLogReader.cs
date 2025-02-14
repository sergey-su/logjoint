using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Xml;
using JUST;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace LogJoint.JsonFormat
{
    class JsonFormatInfo : StreamBasedFormatInfo
    {
        public readonly string Encoding;
        public readonly LoadedRegex HeadRe;
        public readonly LoadedRegex BodyRe;
        public readonly BoundFinder BeginFinder;
        public readonly BoundFinder EndFinder;
        public readonly TextStreamPositioningParams TextStreamPositioningParams;
        public readonly StreamReorderingParams? DejitteringParams;
        public readonly IFormatViewOptions ViewOptions;
        public readonly JObject Transform;

        public JsonFormatInfo(
                string transform, LoadedRegex headRe, LoadedRegex bodyRe,
                BoundFinder beginFinder, BoundFinder endFinder, string encoding,
                TextStreamPositioningParams textStreamPositioningParams,
                StreamReorderingParams? dejitteringParams, IFormatViewOptions viewOptions) :
            base(MessagesReaderExtensions.XmlInitializationParams.Empty)
        {
            Encoding = encoding;
            HeadRe = headRe;
            BodyRe = bodyRe;
            BeginFinder = beginFinder;
            EndFinder = endFinder;
            TextStreamPositioningParams = textStreamPositioningParams;
            DejitteringParams = dejitteringParams;
            ViewOptions = viewOptions;
            Transform = JObject.Parse(transform);
        }
    };

    class MessagesReader : MediaBasedMessagesReader
    {
        internal JsonFormatInfo formatInfo;
        readonly ILogSourceThreadsInternal threads;
        readonly ITraceSourceFactory traceSourceFactory;
        readonly IRegexFactory regexFactory;

        public MessagesReader(
            MediaBasedReaderParams readerParams,
            JsonFormatInfo fmt,
            IRegexFactory regexFactory,
            ITraceSourceFactory traceSourceFactory,
            Settings.IGlobalSettingsAccessor settings
        ) :
            base(readerParams.Media, fmt.BeginFinder, fmt.EndFinder, fmt.ExtensionsInitData, fmt.TextStreamPositioningParams, readerParams.QuickFormatDetectionMode,
                settings, traceSourceFactory, readerParams.ParentLoggingPrefix)
        {
            this.formatInfo = fmt;
            this.threads = readerParams.Threads;
            this.traceSourceFactory = traceSourceFactory;
            this.regexFactory = regexFactory;
        }

        protected override Encoding DetectStreamEncoding(Stream stream)
        {
            Encoding ret = EncodingUtils.GetEncodingFromConfigXMLName(formatInfo.Encoding, Trace);
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

        static IMessage MakeMessageInternal(TextMessageCapture capture, JsonFormatInfo formatInfo, IRegex bodyRe, ref IMatch bodyReMatch,
            MessagesBuilderCallback callback)
        {
            StringBuilder messageBuf = new StringBuilder();
            messageBuf.Append(capture.HeaderBuffer, capture.HeaderMatch.Index, capture.HeaderMatch.Length);
            if (bodyRe != null)
            {
                if (!bodyRe.Match(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength, ref bodyReMatch))
                    return null;
                messageBuf.Append(capture.BodyBuffer, bodyReMatch.Index, bodyReMatch.Length);
            }
            else
            {
                messageBuf.Append(capture.BodyBuffer, capture.BodyIndex, capture.BodyLength);
            }

            callback.SetCurrentPosition(capture.BeginPosition, capture.EndPosition);

            string messageStr = messageBuf.ToString();

            JObject messageObj;
            try
            {
                messageObj = JObject.Parse(messageStr);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                if (!TryRemoveAdditionalText(messageStr, out var fixerdMessageStr))
                    throw;
                try
                {
                    messageObj = JObject.Parse(fixerdMessageStr);
                }
                catch
                {
                    throw e;
                }
            }

            var transfromed = JsonTransformer.Transform(
                formatInfo.Transform.DeepClone() as JObject,
                messageObj
            );
            var d = transfromed.Property("d")?.Value;
            DateTime date;
            if (d != null && d.Type == JTokenType.String)
                date = DateTime.Parse(d.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            else if (d != null && d.Type == JTokenType.Date)
                date = (DateTime)((JValue)d).Value;
            else
                throw new Exception("Bad time property \"d\"");

            var t = transfromed.Property("t")?.Value?.ToString();

            var m = transfromed.Property("m")?.Value;
            var msg = "";
            if (m != null)
                msg = m.ToString();

            var s = transfromed.Property("s")?.Value?.ToString();
            var sev = !string.IsNullOrEmpty(s) ? char.ToLower(s[0]) : 'i';

            Message ret = new Message(
                capture.BeginPosition,
                capture.EndPosition,
                callback.GetThread(new StringSlice(t ?? "")),
                new MessageTimestamp(date),
                new StringSlice(msg),
                sev == 'i' ? SeverityFlag.Info :
                sev == 'e' ? SeverityFlag.Error :
                sev == 'w' ? SeverityFlag.Warning :
                SeverityFlag.Info,
                formatInfo.ViewOptions.RawViewAllowed ? StringSlice.Concat(capture.MessageHeaderSlice, capture.MessageBodySlice).Trim() : new StringSlice(),
                maxLineLen: formatInfo.ViewOptions.WrapLineLength
            );

            return ret;
        }

        static bool TryRemoveAdditionalText(string str, out string fixedString)
        {
            int depth = 0;
            int objectBegin = 0;
            int objectEnd = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (c == '{')
                {
                    ++depth;
                    if (depth == 1)
                    {
                        objectBegin = i;
                    }
                }
                else if (c == '}')
                {
                    --depth;
                    if (depth == 0)
                    {
                        objectEnd = i + 1;
                        break;
                    }
                    else if (depth < 0)
                    {
                        break;
                    }
                }
            }
            if (objectEnd == objectBegin)
            {
                fixedString = null;
                return false;
            }
            else
            {
                fixedString = str.Substring(objectBegin, objectEnd - objectBegin);
                return true;
            }
        }

        MessagesBuilderCallback CreateMessageBuilderCallback()
        {
            IThread fakeThread = null;
            //fakeThread = threads.GetThread("");
            return new MessagesBuilderCallback(threads, fakeThread);
        }

        class SingleThreadedStrategyImpl : StreamReadingStrategies.SingleThreadedStrategy
        {
            readonly MessagesReader reader;
            readonly MessagesBuilderCallback callback;
            readonly IRegex bodyRegex;

            IMatch bodyMatch;

            public SingleThreadedStrategyImpl(MessagesReader reader) :
                base(reader.LogMedia, reader.StreamEncoding, reader.formatInfo.HeadRe.Regex,
                    reader.formatInfo.HeadRe.GetHeaderReSplitterFlags(), reader.formatInfo.TextStreamPositioningParams)
            {
                this.reader = reader;
                this.callback = reader.CreateMessageBuilderCallback();
                this.bodyRegex = reader.formatInfo.BodyRe.Regex;
            }
            public override Task ParserCreated(ReadMessagesParams p)
            {
                return base.ParserCreated(p);
            }
            protected override IMessage MakeMessage(TextMessageCapture capture)
            {
                return MakeMessageInternal(capture, reader.formatInfo, bodyRegex, ref bodyMatch, callback);
            }
        };

        protected override StreamReadingStrategies.BaseStrategy CreateSingleThreadedStrategy()
        {
            return new SingleThreadedStrategyImpl(this);
        }

        class ProcessingThreadLocalData
        {
            public LoadedRegex bodyRe;
            public IMatch bodyMatch;
            public MessagesBuilderCallback callback;
        }

        class MultiThreadedStrategyImpl : StreamReadingStrategies.MultiThreadedStrategy<ProcessingThreadLocalData>
        {
            readonly MessagesReader reader;

            public MultiThreadedStrategyImpl(MessagesReader reader) :
                base(reader.LogMedia, reader.StreamEncoding, reader.formatInfo.HeadRe.Regex,
                    reader.formatInfo.HeadRe.GetHeaderReSplitterFlags(), reader.formatInfo.TextStreamPositioningParams, null, reader.traceSourceFactory)
            {
                this.reader = reader;
            }
            public override Task ParserCreated(ReadMessagesParams p)
            {
                return base.ParserCreated(p);
            }
            public override IMessage MakeMessage(TextMessageCapture capture, ProcessingThreadLocalData threadLocal)
            {
                return MakeMessageInternal(capture, reader.formatInfo, threadLocal.bodyRe.Regex,
                    ref threadLocal.bodyMatch, threadLocal.callback);
            }
            public override ProcessingThreadLocalData InitializeThreadLocalState()
            {
                ProcessingThreadLocalData ret = new ProcessingThreadLocalData
                {
                    bodyRe = reader.formatInfo.BodyRe,
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

        protected override StreamReorderingParams? GetDejitteringParams()
        {
            return this.formatInfo.DejitteringParams;
        }

        public override IAsyncEnumerable<SearchResultMessage> Search(SearchMessagesParams p)
        {
            return StreamSearching.Search(
                this,
                p,
                ((ITextStreamPositioningParamsProvider)this).TextStreamPositioningParams,
                GetDejitteringParams(),
                VolatileStream,
                StreamEncoding,
                false,
                formatInfo.HeadRe,
                traceSourceFactory,
                regexFactory
            );
        }
    };

    public class UserDefinedFormatFactory :
        UserDefinedFactoryBase,
        IFileBasedLogProviderFactory,
        IMediaBasedReaderFactory
    {
        readonly List<string> patterns = new List<string>();
        readonly Lazy<JsonFormatInfo> formatInfo;
        readonly ITempFilesManager tempFilesManager;
        readonly ReaderFactory readerFactory;
        readonly ProviderFactory providerFactory;

        public static string ConfigNodeName => "json";

        public static UserDefinedFormatFactory Create(UserDefinedFactoryParams createParams,
            ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory,
            ISynchronizationContext modelSynchronizationContext, Settings.IGlobalSettingsAccessor globalSettings,
            IRegexFactory regexFactory, LogMedia.IFileSystem fileSystem, IFiltersList displayFilters,
            FilteringStats filteringStats)
        {
            return new UserDefinedFormatFactory(createParams, tempFilesManager, regexFactory,
                (readerParams, formatInfo, hermeticReader) => new FilteringMessagesReader(
                    new MessagesReader(readerParams, formatInfo, regexFactory, traceSourceFactory, globalSettings),
                    readerParams, hermeticReader ? null : displayFilters, tempFilesManager, fileSystem, regexFactory,
                    traceSourceFactory, globalSettings, modelSynchronizationContext, filteringStats
                ),
                (host, connectParams, factory, readerFactory) => new StreamLogProvider(host, factory, connectParams, readerFactory,
                    tempFilesManager, traceSourceFactory, modelSynchronizationContext, globalSettings, fileSystem));
        }

        private delegate ILogProvider ProviderFactory(
            ILogProviderHost host, IConnectionParams connectionParams, UserDefinedFormatFactory factory,
            Func<MediaBasedReaderParams, IMessagesReader> readerFactory);
        private delegate IMessagesReader ReaderFactory(
            MediaBasedReaderParams @params, JsonFormatInfo fmtInfo, bool hermeticReader);

        private UserDefinedFormatFactory(UserDefinedFactoryParams createParams, ITempFilesManager tempFilesManager,
            IRegexFactory regexFactory, ReaderFactory readerFactory, ProviderFactory providerFactory)
            : base(createParams, regexFactory)
        {
            var formatSpecificNode = createParams.FormatSpecificNode;
            ReadPatterns(formatSpecificNode, patterns);

            var boundsNodes = formatSpecificNode.Elements("bounds").Take(1);
            var beginFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("begin")).FirstOrDefault());
            var endFinder = BoundFinder.CreateBoundFinder(boundsNodes.Select(n => n.Element("end")).FirstOrDefault());

            this.tempFilesManager = tempFilesManager;
            this.readerFactory = readerFactory;
            this.providerFactory = providerFactory;

            formatInfo = new Lazy<JsonFormatInfo>(() =>
            {
                string transform = ReadParameter(formatSpecificNode, "transform");
                if (transform == null)
                    throw new Exception("Wrong JSON format definition: transform is not defined");

                LoadedRegex head = ReadRe(formatSpecificNode, "head-re", ReOptions.Multiline, null);
                LoadedRegex body = ReadRe(formatSpecificNode, "body-re", ReOptions.Singleline, null);
                string encoding = ReadParameter(formatSpecificNode, "encoding");

                StreamReorderingParams? dejitteringParams = StreamReorderingParams.FromConfigNode(
                    formatSpecificNode.Element("dejitter"));

                TextStreamPositioningParams textStreamPositioningParams = TextStreamPositioningParams.FromConfigNode(
                    formatSpecificNode);

                return new JsonFormatInfo(transform, head, body, beginFinder, endFinder,
                    encoding, textStreamPositioningParams, dejitteringParams, viewOptions);
            });
        }


        #region ILogProviderFactory Members

        public override string UITypeKey { get { return StdProviderFactoryUIs.FileBasedProviderUIKey; } }

        public override IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
        {
            return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), tempFilesManager);
        }

        public override string GetUserFriendlyConnectionName(IConnectionParams connectParams)
        {
            return ConnectionParamsUtils.GetFileOrFolderBasedUserFriendlyConnectionName(connectParams);
        }

        public override Task<ILogProvider> CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
        {
            return Task.FromResult(providerFactory(host, connectParams, this,
                @params => readerFactory(@params, formatInfo.Value, hermeticReader: false)));
        }

        public override LogProviderFactoryFlag Flags
        {
            get
            {
                return
                      LogProviderFactoryFlag.SupportsRotation
                    | LogProviderFactoryFlag.SupportsDejitter
                    | (formatInfo.Value.DejitteringParams.HasValue ? LogProviderFactoryFlag.DejitterEnabled : LogProviderFactoryFlag.None);
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

        IMessagesReader IMediaBasedReaderFactory.CreateMessagesReader(MediaBasedReaderParams readerParams)
        {
            return readerFactory(readerParams, formatInfo.Value, hermeticReader: true);
        }
    };
}


namespace LogJoint.Json
{
    static public class Functions
    {
        class Impl : UserCodeHelperFunctions
        {
            protected override DateTime SOURCE_TIME()
            {
                throw new NotImplementedException();
            }
        };

        static readonly Impl impl = new Impl();

        static public DateTime TO_DATETIME(string value, string format)
        {
            return impl.TO_DATETIME(value, format);
        }

        public static DateTime EPOCH_TIME(double epochTime)
        {
            return impl.EPOCH_TIME(epochTime);
        }

        public static string MATCH(string value, string pattern)
        {
            return impl.MATCH(value, pattern);
        }

        public static string MATCH_GROUP(string value, string pattern, int groupIndex)
        {
            return impl.MATCH(value, pattern, groupIndex);
        }

        public static int TO_INT(string str)
        {
            return impl.TO_INT(str);
        }

        public static int HEX_TO_INT(string str)
        {
            return impl.HEX_TO_INT(str);
        }

        public static string TRIM(string str)
        {
            return impl.TRIM(str);
        }

        public static int PARSE_YEAR(string str)
        {
            return impl.PARSE_YEAR(str);
        }

        public static DateTime DATETIME_FROM_TIMEOFDAY(DateTime dt)
        {
            return impl.DATETIME_FROM_TIMEOFDAY(dt);
        }

        public static string NEW_LINE()
        {
            return impl.NEW_LINE();
        }
    }
}

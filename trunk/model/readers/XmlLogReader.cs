using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using LogJoint.RegularExpressions;
using System.Xml.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.XmlFormat
{
    static class Properties
    {
        public static readonly string LogJointNS = "http://logjoint.codeplex.com/";
    };

    class FactoryWriter : XmlWriter
    {
        public FactoryWriter(
            FieldsProcessor.IMessagesBuilderCallback callback,
            ITimeOffsets timeOffsets,
            int? maxLineLen,
            bool useEmbeddedAttributes
        )
        {
            this.callback = callback;
            this.timeOffsets = timeOffsets;
            this.maxLineLen = maxLineLen;
            this.useEmbeddedAttributes = useEmbeddedAttributes;
            states.Push(WriteState.Content);
        }

        public override void Close()
        {
        }

        public override void Flush()
        {
        }

        public override string LookupPrefix(string ns)
        {
            throw new InvalidOperationException("Namespace prefixes are not supportes.");
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            WriteString(Convert.ToBase64String(buffer, index, count));
        }

        public override void WriteCData(string text)
        {
            WriteString(text);
        }

        public override void WriteCharEntity(char ch)
        {
            WriteString(new string(ch, 1));
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count));
        }

        public override void WriteComment(string text)
        {
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
        }

        public override void WriteEntityRef(string name)
        {
            throw new InvalidOperationException("Entities are not supported");
        }

        public override void WriteFullEndElement()
        {
            WriteEndElement();
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
        }

        public override void WriteRaw(string data)
        {
            throw new InvalidOperationException("Raw data is not supported.");
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            WriteRaw("");
        }

        public override void WriteStartDocument(bool standalone)
        {
        }

        public override void WriteStartDocument()
        {
        }

        public override void WriteEndDocument()
        {
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            char[] buf = new char[] { lowChar, highChar };
            WriteString(new string(buf));
        }

        public override void WriteWhitespace(string ws)
        {
            WriteString(ws);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            states.Push(WriteState.Element);

            localName = string.Intern(localName);
            switch (localName)
            {
                case "m":
                case "f":
                case "ef":
                    elemName = localName;
                    break;
                default:
                    elemName = null;
                    break;
            }
            if (elemName != null)
            {
                Reset();
            }
        }

        void Reset()
        {
            content.Length = 0;
            attribName = null;
            thread = null;
            dateTime = new MessageTimestamp();
            severity = SeverityFlag.Info;
            embeddedPositon = null;
            embeddedEndPositon = null;
            embeddedRawText = null;
        }

        public override void WriteEndElement()
        {
            states.Pop();

            if (elemName == null)
                return;

            if (thread == null)
                thread = callback.GetThread(StringSlice.Empty);

            output = new Message(
                callback.CurrentPosition,
                callback.CurrentEndPosition,
                thread,
                dateTime,
                new StringSlice(GetAndClearContent()),
                severity,
                useEmbeddedAttributes ? new StringSlice(embeddedRawText ?? "") : callback.CurrentRawText,
                maxLineLen: this.maxLineLen
            );

            if (useEmbeddedAttributes)
            {
                if (embeddedPositon == null)
                    throw new Exception("No embedded position was found in a message");
                if (embeddedEndPositon == null)
                    throw new Exception("No embedded end position was found in a message");
                output.SetEmbeddedPositions(new Message.EmbeddedPositions(
                    embeddedPositon.Value, embeddedEndPositon.Value));
            }

            elemName = null;
            Reset();
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            states.Push(WriteState.Attribute);

            localName = string.Intern(localName);
            switch (localName)
            {
                case "t":
                case "d":
                case "s":
                case "p":
                case "ep":
                case "r":
                    attribName = localName;
                    break;
                default:
                    attribName = null;
                    break;
            }
        }

        static MessageTimestamp ParseDateTime(string str)
        {
            return MessageTimestamp.ParseFromLoselessFormat(str);
        }

        public override void WriteEndAttribute()
        {
            states.Pop();

            switch (attribName)
            {
                case "d":
                    dateTime = ParseDateTime(GetAndClearContent()).Adjust(timeOffsets);
                    break;
                case "t":
                    thread = callback.GetThread(new StringSlice(GetAndClearContent()));
                    break;
                case "s":
                    switch (GetAndClearContent())
                    {
                        case "e":
                            severity = SeverityFlag.Error;
                            break;
                        case "w":
                            severity = SeverityFlag.Warning;
                            break;
                    }
                    break;
                case "p":
                    if (long.TryParse(GetAndClearContent(), out long p))
                    {
                        embeddedPositon = p;
                    }
                    break;
                case "ep":
                    if (long.TryParse(GetAndClearContent(), out long ep))
                    {
                        embeddedEndPositon = ep;
                    }
                    break;
                case "r":
                    embeddedRawText = GetAndClearContent();
                    break;
                default:
                    // Ended to read unknown attribute. Need to clear content in order not to affect the following attribs/elems.
                    GetAndClearContent();
                    break;
            }

            attribName = null;
        }

        public override WriteState WriteState
        {
            get { return states.Peek(); }
        }

        public override void WriteString(string text)
        {
            content.Append(text);
        }

        public Message GetOutput()
        {
            return output;
        }

        string GetAndClearContent()
        {
            string ret = StringUtils.TrimInsignificantSpace(content.ToString());
            content.Length = 0;
            return ret;
        }

        readonly Stack<WriteState> states = new Stack<WriteState>();
        string elemName;
        string attribName;
        IThread thread;
        MessageTimestamp dateTime;
        readonly StringBuilder content = new StringBuilder();
        SeverityFlag severity = SeverityFlag.Info;
        Message output;
        readonly FieldsProcessor.IMessagesBuilderCallback callback;
        readonly ITimeOffsets timeOffsets;
        readonly int? maxLineLen;
        readonly bool useEmbeddedAttributes;
        long? embeddedPositon;
        long? embeddedEndPositon;
        string embeddedRawText;
    };

    public class LogJointXSLExtension : UserCodeHelperFunctions
    {
        DateTime __sourceTime;

        protected override DateTime SOURCE_TIME()
        {
            return __sourceTime;
        }


        internal void SetSourceTime(DateTime sourceTime)
        {
            __sourceTime = sourceTime;
        }
    }

    class XmlFormatInfo : StreamBasedFormatInfo
    {
        public readonly XslCompiledTransform Transform;
        public readonly string NSDeclaration = "";
        public readonly string Encoding;
        public readonly LoadedRegex HeadRe;
        public readonly LoadedRegex BodyRe;
        public readonly BoundFinder BeginFinder;
        public readonly BoundFinder EndFinder;
        public readonly TextStreamPositioningParams TextStreamPositioningParams;
        public readonly StreamReorderingParams? DejitteringParams;
        public readonly IFormatViewOptions ViewOptions;

        public bool IsNativeFormat { get { return Transform == null; } }

        public static XmlFormatInfo MakeNativeFormatInfo(string encoding, StreamReorderingParams? dejitteringParams,
            FormatViewOptions viewOptions, IRegexFactory regexFactory)
        {
            var headRe = new LoadedRegex(
                regexFactory.Create(@"\<\s*(m|f|ef)\s", ReOptions.None),
                suffersFromPartialMatchProblem: false);
            return new XmlFormatInfo(
                null, headRe, new LoadedRegex(),
                null, null, encoding, null, TextStreamPositioningParams.Default, dejitteringParams, viewOptions);
        }

        public XmlFormatInfo(XmlNode xsl, LoadedRegex headRe, LoadedRegex bodyRe, BoundFinder beginFinder, BoundFinder endFinder, string encoding, MessagesReaderExtensions.XmlInitializationParams extensionsInitData,
                TextStreamPositioningParams textStreamPositioningParams, StreamReorderingParams? dejitteringParams, IFormatViewOptions viewOptions) :
            base(extensionsInitData)
        {
            Encoding = encoding;
            HeadRe = headRe;
            BodyRe = bodyRe;
            BeginFinder = beginFinder;
            EndFinder = endFinder;
            TextStreamPositioningParams = textStreamPositioningParams;
            DejitteringParams = dejitteringParams;
            ViewOptions = viewOptions;

            if (xsl != null)
            {
                Dictionary<string, string> nsTable = new Dictionary<string, string>();
                foreach (XmlAttribute ns in xsl.SelectNodes(".//namespace::*"))
                {
                    if (ns.Value == "http://www.w3.org/XML/1998/namespace")
                        continue;
                    if (ns.Value == "http://www.w3.org/1999/XSL/Transform")
                        continue;
                    if (ns.Value == Properties.LogJointNS)
                        continue;
                    nsTable[ns.Name] = ns.Value;
                }

                StringBuilder nsdeclBuilder = new StringBuilder();
                foreach (KeyValuePair<string, string> ns in nsTable)
                {
                    nsdeclBuilder.AppendFormat("{0}='{1}' ", ns.Key, ns.Value);
                }
                NSDeclaration = nsdeclBuilder.ToString();

                Transform = new XslCompiledTransform();
                Transform.Load(xsl);
            }
        }
    };

    class MessagesReader : MediaBasedMessagesReader
    {
        internal XmlFormatInfo formatInfo;
        readonly XsltArgumentList transformArgs;
        readonly LogJointXSLExtension xslExt;
        readonly ILogSourceThreadsInternal threads;
        readonly ITraceSourceFactory traceSourceFactory;
        readonly IRegexFactory regexFactory;
        readonly bool useEmbeddedAttributes;

        public MessagesReader(
            MediaBasedReaderParams readerParams,
            XmlFormatInfo fmt,
            IRegexFactory regexFactory,
            ITraceSourceFactory traceSourceFactory,
            Settings.IGlobalSettingsAccessor settings,
            bool useEmbeddedAttributes
        ) :
            base(readerParams.Media, fmt.BeginFinder, fmt.EndFinder, fmt.ExtensionsInitData, fmt.TextStreamPositioningParams,
                readerParams.QuickFormatDetectionMode, settings, traceSourceFactory, readerParams.ParentLoggingPrefix)
        {
            this.formatInfo = fmt;
            this.threads = readerParams.Threads;
            this.traceSourceFactory = traceSourceFactory;
            this.regexFactory = regexFactory;
            this.transformArgs = new XsltArgumentList();
            this.useEmbeddedAttributes = useEmbeddedAttributes;

            this.xslExt = new LogJointXSLExtension();
            transformArgs.AddExtensionObject(Properties.LogJointNS, this.xslExt);

            foreach (MessagesReaderExtensions.ExtensionData extInfo in this.Extensions.Items)
            {
                transformArgs.AddExtensionObject(Properties.LogJointNS + extInfo.Name, extInfo.Instance());
            }
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

        internal static readonly XmlReaderSettings xmlReaderSettings = CreateXmlReaderSettings();

        static XmlReaderSettings CreateXmlReaderSettings()
        {
            XmlReaderSettings xrs = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                CheckCharacters = false,
                CloseInput = false
            };
            return xrs;
        }

        static IMessage MakeMessageInternal(TextMessageCapture capture, XmlFormatInfo formatInfo, IRegex bodyRe, ref IMatch bodyReMatch,
            MessagesBuilderCallback callback, XsltArgumentList transformArgs, ITimeOffsets timeOffsets, bool useEmbeddedAttributes)
        {
            int nrOfSequentialFailures = 0;
            int maxNrOfSequentialFailures = 10;
            for (; ; )
            {
                StringBuilder messageBuf = new StringBuilder();
                messageBuf.AppendFormat("<root {0}>", formatInfo.NSDeclaration.ToString());
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
                messageBuf.Append("</root>");

                callback.SetCurrentPosition(capture.BeginPosition, capture.EndPosition);
                if (formatInfo.ViewOptions.RawViewAllowed)
                {
                    callback.SetRawText(StringSlice.Concat(capture.MessageHeaderSlice, capture.MessageBodySlice).Trim());
                }

                //this.owner.xslExt.SetSourceTime(this.owner.MediaLastModified); todo?

                string messageStr = messageBuf.ToString();

                using FactoryWriter factoryWriter = new FactoryWriter(callback, timeOffsets, formatInfo.ViewOptions.WrapLineLength, useEmbeddedAttributes);
                using XmlReader xmlReader = XmlReader.Create(new StringReader(messageStr), xmlReaderSettings);
                try
                {
                    if (formatInfo.IsNativeFormat)
                    {
                        factoryWriter.WriteNode(xmlReader, false);
                    }
                    else
                    {
                        formatInfo.Transform.Transform(xmlReader, transformArgs, factoryWriter);
                    }
                    nrOfSequentialFailures = 0;
                }
                catch (XmlException)
                {
                    if (capture.IsLastMessage)
                    {
                        // There might be incomplete XML at the end of the stream. Ignore it.
                        return null;
                    }
                    else
                    {
                        if (nrOfSequentialFailures < maxNrOfSequentialFailures)
                        {
                            ++nrOfSequentialFailures;
                            // Try to parse the next message if it's not the end of the stream
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                var ret = factoryWriter.GetOutput();
                if (ret == null)
                    throw new XsltException(
                        "Normalization XSLT produced no output");

                return ret;
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
                return MakeMessageInternal(capture, reader.formatInfo, bodyRegex, ref bodyMatch, callback,
                    reader.transformArgs, reader.TimeOffsets, reader.useEmbeddedAttributes);
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
                    ref threadLocal.bodyMatch, threadLocal.callback, reader.transformArgs, reader.TimeOffsets, reader.useEmbeddedAttributes);
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

    public class NativeXMLFormatFactory : IFileBasedLogProviderFactory, IMediaBasedReaderFactory
    {
        readonly ITempFilesManager tempFiles;
        readonly XmlFormatInfo nativeFormatInfo;
        readonly IRegexFactory regexFactory;
        readonly ITraceSourceFactory traceSourceFactory;
        readonly ISynchronizationContext modelSynchronizationContext;
        readonly Settings.IGlobalSettingsAccessor globalSettings;
        readonly LogMedia.IFileSystem fileSystem;
        readonly IFiltersList displayFilters;

        public NativeXMLFormatFactory(ITempFilesManager tempFiles, IRegexFactory regexFactory, ITraceSourceFactory traceSourceFactory,
            ISynchronizationContext modelSynchronizationContext, Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem, 
            IFiltersList displayFilters)
        {
            this.tempFiles = tempFiles;
            this.regexFactory = regexFactory;
            this.traceSourceFactory = traceSourceFactory;
            this.modelSynchronizationContext = modelSynchronizationContext;
            this.globalSettings = globalSettings;
            this.fileSystem = fileSystem;
            this.displayFilters = displayFilters;
            this.nativeFormatInfo = XmlFormatInfo.MakeNativeFormatInfo("utf-8", null, new FormatViewOptions(), regexFactory);
        }

        IEnumerable<string> IFileBasedLogProviderFactory.SupportedPatterns
        {
            get { yield return "*.xml"; }
        }

        IConnectionParams IFileBasedLogProviderFactory.CreateParams(string fileName)
        {
            return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
        }

        IConnectionParams IFileBasedLogProviderFactory.CreateRotatedLogParams(string folder, IEnumerable<string> patterns)
        {
            return ConnectionParamsUtils.CreateRotatedLogConnectionParamsFromFolderPath(folder, this, patterns);
        }

        string ILogProviderFactory.CompanyName
        {
            get { return "LogJoint"; }
        }

        string ILogProviderFactory.FormatName
        {
            get { return "Native XML"; }
        }

        string ILogProviderFactory.FormatDescription
        {
            get { return "XML log files created by LogJoint. LogJoint writes its own logs in files of this format."; }
        }

        string ILogProviderFactory.UITypeKey { get { return StdProviderFactoryUIs.FileBasedProviderUIKey; } }

        string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
        {
            return ConnectionParamsUtils.GetFileOrFolderBasedUserFriendlyConnectionName(connectParams);
        }

        string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
        {
            return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
        }

        IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
        {
            return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), tempFiles);
        }

        Task<ILogProvider> ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
        {
            return Task.FromResult<ILogProvider>(new StreamLogProvider(host, this, connectParams,
                @params => new FilteringMessagesReader(
                    new MessagesReader(@params, nativeFormatInfo, regexFactory, traceSourceFactory, globalSettings, useEmbeddedAttributes: false),
                    @params, displayFilters, tempFiles, fileSystem, regexFactory,
                    traceSourceFactory, globalSettings, modelSynchronizationContext
                ),
                tempFiles, traceSourceFactory, modelSynchronizationContext, globalSettings, fileSystem));
        }

        IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.Default; } }

        LogProviderFactoryFlag ILogProviderFactory.Flags
        {
            get { return LogProviderFactoryFlag.SupportsRotation; }
        }

        IMessagesReader IMediaBasedReaderFactory.CreateMessagesReader(MediaBasedReaderParams readerParams)
        {
            return new MessagesReader(readerParams, nativeFormatInfo, regexFactory, traceSourceFactory,
                globalSettings, useEmbeddedAttributes: false);
        }
    };

    public class UserDefinedFormatFactory :
        UserDefinedFactoryBase,
        IFileBasedLogProviderFactory,
        IMediaBasedReaderFactory
    {
        readonly List<string> patterns = new List<string>();
        readonly Lazy<XmlFormatInfo> formatInfo;
        readonly ITempFilesManager tempFilesManager;
        readonly ReaderFactory readerFactory;
        readonly ProviderFactory providerFactory;
        static readonly XmlNamespaceManager nsMgr = new XmlNamespaceManager(new NameTable());
        static readonly string XSLNamespace = "http://www.w3.org/1999/XSL/Transform";
        readonly string uiKey;

        static UserDefinedFormatFactory()
        {
            nsMgr.AddNamespace("xsl", XSLNamespace);
        }

        public static XmlNamespaceManager NamespaceManager => nsMgr;
        public static string ConfigNodeName => "xml";

        public static UserDefinedFormatFactory Create(UserDefinedFactoryParams createParams,
            ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory,
            ISynchronizationContext modelSynchronizationContext, Settings.IGlobalSettingsAccessor globalSettings,
            IRegexFactory regexFactory, LogMedia.IFileSystem fileSystem, IFiltersList displayFilters)
        {
            return new UserDefinedFormatFactory(createParams, tempFilesManager, regexFactory,
                (readerParams, formatInfo) =>
                new FilteringMessagesReader(
                    new MessagesReader(readerParams, formatInfo, regexFactory, traceSourceFactory,
                        globalSettings, useEmbeddedAttributes: false),
                    readerParams, displayFilters, tempFilesManager, fileSystem, regexFactory, traceSourceFactory, globalSettings,
                    modelSynchronizationContext
                ),
                (host, connectParams, factory, readerFactory) => new StreamLogProvider(host, factory, connectParams, readerFactory,
                    tempFilesManager, traceSourceFactory, modelSynchronizationContext, globalSettings, fileSystem));
        }

        private delegate ILogProvider ProviderFactory(
            ILogProviderHost host, IConnectionParams connectionParams, UserDefinedFormatFactory factory,
            Func<MediaBasedReaderParams, IMessagesReader> readerFactory);
        private delegate IMessagesReader ReaderFactory(
            MediaBasedReaderParams @params, XmlFormatInfo fmtInfo);

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

            formatInfo = new Lazy<XmlFormatInfo>(() =>
            {
                XmlDocument tmpDoc = new XmlDocument();
                tmpDoc.LoadXml(formatSpecificNode.ToString());
                XmlElement xsl =
                    tmpDoc.DocumentElement.SelectSingleNode("xsl:stylesheet", nsMgr) as XmlElement ??
                    throw new Exception("Wrong XML-based format definition: xsl:stylesheet is not defined");

                MessagesReaderExtensions.XmlInitializationParams extensionsInitData =
                    new MessagesReaderExtensions.XmlInitializationParams(formatSpecificNode.Element("extensions"));

                LoadedRegex head = ReadRe(formatSpecificNode, "head-re", ReOptions.Multiline, extensionsInitData);
                LoadedRegex body = ReadRe(formatSpecificNode, "body-re", ReOptions.Singleline, extensionsInitData);
                string encoding = ReadParameter(formatSpecificNode, "encoding");

                StreamReorderingParams? dejitteringParams = StreamReorderingParams.FromConfigNode(
                    formatSpecificNode.Element("dejitter"));

                TextStreamPositioningParams textStreamPositioningParams = TextStreamPositioningParams.FromConfigNode(
                    formatSpecificNode);

                return new XmlFormatInfo(xsl, head, body, beginFinder, endFinder,
                    encoding, extensionsInitData, textStreamPositioningParams, dejitteringParams, viewOptions);
            });
            uiKey = ReadParameter(formatSpecificNode, "ui-key");
        }


        #region ILogProviderFactory Members

        public override string UITypeKey { get { return string.IsNullOrEmpty(uiKey) ? StdProviderFactoryUIs.FileBasedProviderUIKey : uiKey; } }

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
            return Task.FromResult(providerFactory(host, connectParams, this, @params => readerFactory(@params, formatInfo.Value)));
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

        #region IFileReaderFactory Members

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

        IConnectionParams IFileBasedLogProviderFactory.CreateRotatedLogParams(string folder, IEnumerable<string> patterns)
        {
            return ConnectionParamsUtils.CreateRotatedLogConnectionParamsFromFolderPath(folder, this, patterns);
        }

        #endregion

        IMessagesReader IMediaBasedReaderFactory.CreateMessagesReader(MediaBasedReaderParams readerParams)
        {
            return readerFactory(readerParams, formatInfo.Value);
        }
    };
}

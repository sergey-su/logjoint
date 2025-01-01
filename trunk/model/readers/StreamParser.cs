using System;
using LogJoint.StreamParsingStrategies;
using LogJoint.Settings;
using System.Threading.Tasks;

namespace LogJoint
{
    class StreamParser : IPositionedMessagesParser
    {
        private bool disposed;
        private readonly bool isSequentialReadingParser;
        private readonly bool multithreadingDisabled;
        private readonly CreateParserParams initialParams;
        private readonly StreamParsingStrategies.BaseStrategy strategy;

        public static async Task<StreamParser> Create(
            IPositionedMessagesReader owner,
            CreateParserParams p,
            TextStreamPositioningParams textStreamPositioningParams,
            IGlobalSettingsAccessor globalSettings,
            StrategiesCache strategiesCache)
        {
            var parser = new StreamParser(owner, p, textStreamPositioningParams, globalSettings, strategiesCache);
            await parser.strategy.ParserCreated(parser.initialParams);
            return parser;
        }

        private StreamParser(
            IPositionedMessagesReader owner,
            CreateParserParams p,
            TextStreamPositioningParams textStreamPositioningParams,
            IGlobalSettingsAccessor globalSettings,
            StrategiesCache strategiesCache
        )
        {
            p.EnsureRangeIsSet(owner);

            this.initialParams = p;

            this.isSequentialReadingParser = (p.Flags & MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading) != 0;
            this.multithreadingDisabled = (p.Flags & MessagesParserFlag.DisableMultithreading) != 0
                || globalSettings.MultithreadedParsingDisabled;

            this.strategy = CreateParsingStrategy(p, textStreamPositioningParams, strategiesCache);
        }

        public struct StrategiesCache
        {
            public Lazy<BaseStrategy> MultiThreadedStrategy;
            public Lazy<BaseStrategy> SingleThreadedStrategy;
        };

        static bool HeuristicallyDetectWhetherMultithreadingMakesSense(CreateParserParams parserParams,
            TextStreamPositioningParams textStreamPositioningParams)
        {
            if (IsBrowser.Value)
            {
                return false;
            }
            if (System.Environment.ProcessorCount == 1)
            {
                return false;
            }

            long approxBytesToRead;
            if (parserParams.Direction == MessagesParserDirection.Forward)
            {
                approxBytesToRead = new TextStreamPosition(parserParams.Range.Value.End, textStreamPositioningParams).StreamPositionAlignedToBlockSize
                    - new TextStreamPosition(parserParams.StartPosition, textStreamPositioningParams).StreamPositionAlignedToBlockSize;
            }
            else
            {
                approxBytesToRead = new TextStreamPosition(parserParams.StartPosition, textStreamPositioningParams).StreamPositionAlignedToBlockSize
                    - new TextStreamPosition(parserParams.Range.Value.Begin, textStreamPositioningParams).StreamPositionAlignedToBlockSize;
            }
            if (approxBytesToRead < MultiThreadedStrategy<int>.GetBytesToParsePerThread(textStreamPositioningParams) * 2)
            {
                return false;
            }

            return true;
        }

        StreamParsingStrategies.BaseStrategy CreateParsingStrategy(
            CreateParserParams parserParams,
            TextStreamPositioningParams textStreamPositioningParams,
            StrategiesCache strategiesCache)
        {
            bool useMultithreadedStrategy;

            if (multithreadingDisabled)
                useMultithreadedStrategy = false;
            else if (!isSequentialReadingParser)
                useMultithreadedStrategy = false;
            else
                useMultithreadedStrategy = HeuristicallyDetectWhetherMultithreadingMakesSense(parserParams, textStreamPositioningParams);

            //useMultithreadedStrategy = false;

            Lazy<BaseStrategy> strategyToTryFirst;
            Lazy<BaseStrategy> strategyToTrySecond;
            if (useMultithreadedStrategy)
            {
                strategyToTryFirst = strategiesCache.MultiThreadedStrategy;
                strategyToTrySecond = strategiesCache.SingleThreadedStrategy;
            }
            else
            {
                strategyToTryFirst = strategiesCache.SingleThreadedStrategy;
                strategyToTrySecond = strategiesCache.MultiThreadedStrategy;
            }

            var strategy = strategyToTryFirst.Value;
            if (strategy == null)
                strategy = strategyToTrySecond.Value;
            return strategy;
        }

        public bool IsDisposed
        {
            get { return disposed; }
        }

        public virtual ValueTask DisposeAsync()
        {
            if (disposed)
                return ValueTask.CompletedTask;
            disposed = true;
            strategy.ParserDestroyed();
            return ValueTask.CompletedTask;
        }

        public ValueTask<PostprocessedMessage> ReadNextAndPostprocess()
        {
            return strategy.ReadNextAndPostprocess();
        }
    };
}

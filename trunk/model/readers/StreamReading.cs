using System;
using LogJoint.Settings;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LogJoint
{
    static class StreamReading
    {
        public struct StrategiesCache
        {
            public Lazy<StreamReadingStrategies.BaseStrategy> MultiThreadedStrategy;
            public Lazy<StreamReadingStrategies.BaseStrategy> SingleThreadedStrategy;
        };

        public static async IAsyncEnumerable<PostprocessedMessage> Read(
            IMessagesReader owner,
            ReadMessagesParams readParams,
            TextStreamPositioningParams textStreamPositioningParams,
            IGlobalSettingsAccessor globalSettings,
            StrategiesCache strategiesCache)
        {
            using var impl = new Impl(owner, readParams, textStreamPositioningParams, globalSettings, strategiesCache);
            await impl.Init();
            for (; ; )
            {
                PostprocessedMessage message = await impl.ReadNextAndPostprocess();
                if (message.Message == null)
                    break;
                yield return message;
            }
        }

        private class Impl : IDisposable
        {
            private readonly bool isSequentialReadingParser;
            private readonly bool multithreadingDisabled;
            private readonly ReadMessagesParams readParams;
            private readonly StreamReadingStrategies.BaseStrategy strategy;

            public Impl(
                IMessagesReader owner,
                ReadMessagesParams readParams,
                TextStreamPositioningParams textStreamPositioningParams,
                IGlobalSettingsAccessor globalSettings,
                StrategiesCache strategiesCache
            )
            {
                readParams.EnsureRangeIsSet(owner);

                this.readParams = readParams;

                this.isSequentialReadingParser = (readParams.Flags & ReadMessagesFlag.HintMassiveSequentialReading) != 0;
                this.multithreadingDisabled = (readParams.Flags & ReadMessagesFlag.DisableMultithreading) != 0
                    || globalSettings.MultithreadedParsingDisabled;

                this.strategy = CreateParsingStrategy(readParams, textStreamPositioningParams, strategiesCache);
            }

            void IDisposable.Dispose() => strategy.ParserDestroyed();

            public async ValueTask Init() => await strategy.ParserCreated(readParams);

            public ValueTask<PostprocessedMessage> ReadNextAndPostprocess() => strategy.ReadNextAndPostprocess();

            static bool HeuristicallyDetectWhetherMultithreadingMakesSense(ReadMessagesParams parserParams,
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
                if (parserParams.Direction == ReadMessagesDirection.Forward)
                {
                    approxBytesToRead = new TextStreamPosition(parserParams.Range.Value.End, textStreamPositioningParams).StreamPositionAlignedToBlockSize
                        - new TextStreamPosition(parserParams.StartPosition, textStreamPositioningParams).StreamPositionAlignedToBlockSize;
                }
                else
                {
                    approxBytesToRead = new TextStreamPosition(parserParams.StartPosition, textStreamPositioningParams).StreamPositionAlignedToBlockSize
                        - new TextStreamPosition(parserParams.Range.Value.Begin, textStreamPositioningParams).StreamPositionAlignedToBlockSize;
                }
                if (approxBytesToRead < StreamReadingStrategies.MultiThreadedStrategy<int>.GetBytesToParsePerThread(textStreamPositioningParams) * 2)
                {
                    return false;
                }

                return true;
            }

            StreamReadingStrategies.BaseStrategy CreateParsingStrategy(
                ReadMessagesParams parserParams,
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

                Lazy<StreamReadingStrategies.BaseStrategy> strategyToTryFirst;
                Lazy<StreamReadingStrategies.BaseStrategy> strategyToTrySecond;
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
        }
    };
}

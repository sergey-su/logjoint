using System;
using LogJoint.StreamParsingStrategies;
using LogJoint.Settings;

namespace LogJoint
{
	class StreamParser : IPositionedMessagesParser
	{
		private bool disposed;
		private readonly bool isSequentialReadingParser;
		private readonly bool multithreadingDisabled;
		protected readonly CreateParserParams InitialParams;
		protected readonly StreamParsingStrategies.BaseStrategy Strategy;

		public StreamParser(
			IPositionedMessagesReader owner,
			CreateParserParams p,
			TextStreamPositioningParams textStreamPositioningParams,
			IGlobalSettingsAccessor globalSettings,
			StrategiesCache strategiesCache
		)
		{
			p.EnsureRangeIsSet(owner);

			this.InitialParams = p;

			this.isSequentialReadingParser = (p.Flags & MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading) != 0;
			this.multithreadingDisabled = (p.Flags & MessagesParserFlag.DisableMultithreading) != 0
				|| globalSettings.MultithreadedParsingDisabled;

			CreateParsingStrategy(p, textStreamPositioningParams, strategiesCache, out this.Strategy);

			this.Strategy.ParserCreated(p);
		}

		public struct StrategiesCache
		{
			public Lazy<BaseStrategy> MultiThreadedStrategy;
			public Lazy<BaseStrategy> SingleThreadedStrategy;
		};

		static bool HeuristicallyDetectWhetherMultithreadingMakesSense(CreateParserParams parserParams,
			TextStreamPositioningParams textStreamPositioningParams)
		{
#if SILVERLIGHT
				return false;
#else
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
#endif
		}

		void CreateParsingStrategy(
			CreateParserParams parserParams,
			TextStreamPositioningParams textStreamPositioningParams,
			StrategiesCache strategiesCache,
			out BaseStrategy strategy)
		{
			bool useMultithreadedStrategy;

			if (multithreadingDisabled)
				useMultithreadedStrategy = false;
			else if (!isSequentialReadingParser)
				useMultithreadedStrategy = false;
			else
				useMultithreadedStrategy = HeuristicallyDetectWhetherMultithreadingMakesSense(parserParams, textStreamPositioningParams);

			useMultithreadedStrategy = false;

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

			strategy = strategyToTryFirst.Value;
			if (strategy == null)
				strategy = strategyToTrySecond.Value;
		}

		public bool IsDisposed
		{
			get { return disposed; }
		}

		public virtual void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			Strategy.ParserDestroyed();
		}

		public IMessage ReadNext()
		{
			return Strategy.ReadNext();
		}

		public PostprocessedMessage ReadNextAndPostprocess()
		{
			return Strategy.ReadNextAndPostprocess();
		}
	};
}

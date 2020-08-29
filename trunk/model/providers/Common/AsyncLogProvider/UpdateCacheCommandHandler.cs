using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	internal class UpdateCacheCommandHandler : IAsyncLogProviderCommandHandler
	{
		public UpdateCacheCommandHandler(
			IAsyncLogProvider owner,
			LJTraceSource tracer,
			MessagesContainers.RangesManagingCollection buffer,
			Settings.IGlobalSettingsAccessor settingsAccessor
		)
		{
			this.owner = owner;
			this.buffer = buffer;
			this.tracer = tracer;
			this.settingsAccessor = settingsAccessor;
		}

		public override string ToString()
		{
			return string.Format("ap={0}", owner.ActivePositionHint);
		}

		bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
		{
			if (ctx.Cache == null)
				return false;
			var currentRange = ctx.Cache.MessagesRange;
			var avaRange = ctx.Stats.PositionsRange;
			long cacheSize = CalcMaxActiveRangeSize(settingsAccessor, avaRange);
			if (currentRange.IsEmpty)
				return false;
			if (!currentRange.IsInRange(owner.ActivePositionHint))
				return false;
			var delta = owner.ActivePositionHint - (currentRange.Begin + currentRange.End) / 2;
			if (Math.Abs(delta) < cacheSize / 6)
				return true;
			if (delta < 0 && currentRange.Begin == avaRange.Begin)
				return true;
			if (delta > 0 && currentRange.End == avaRange.End)
				return true;
			return false;
		}

		async Task IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
		{
			this.reader = ctx.Reader;
			this.currentStats = owner.Stats;
			long cacheSize = CalcMaxActiveRangeSize(settingsAccessor, 
				new FileRange.Range(ctx.Reader.BeginPosition, ctx.Reader.EndPosition));
			var startFrom = owner.ActivePositionHint;
			ConstrainedNavigate(
				startFrom - cacheSize / 2,
				startFrom + cacheSize / 2 + (cacheSize % 2) // add remainder to ensure that the diff between positions equals exactly cacheSize
			);
			await FillCacheRanges(ctx.Preemption);
		}

		void IAsyncLogProviderCommandHandler.Complete(Exception e)
		{
		}

		static long CalcMaxActiveRangeSize(Settings.IGlobalSettingsAccessor settings, FileRange.Range availableRange)
		{
			long MB = 1024 * 1024;
			long sizeThreshold = settings.FileSizes.Threshold * MB;
			long partialLoadingSize = settings.FileSizes.WindowSize * MB;
			if (IsBrowser.Value)
			{
				long kB = 1024;
				sizeThreshold = 128 * kB;
				partialLoadingSize = 64 * kB;
			}

			long currentSize = availableRange.End - availableRange.Begin;

			if (currentSize < sizeThreshold)
				return currentSize;
			else
				return partialLoadingSize;
		}

		bool ConstrainedNavigate(long p1, long p2)
		{
			var availableRange = currentStats.PositionsRange;
			if (p1 < availableRange.Begin)
			{
				p2 = Math.Min(availableRange.End, p2 + availableRange.Begin - p1);
				p1 = availableRange.Begin;
			}
			if (p2 >= availableRange.End)
			{
				p1 = Math.Max(availableRange.Begin, p1 - p2 + availableRange.End);
				p2 = availableRange.End;
			}

			//var pos1 = PositionedMessagesUtils.NormalizeMessagePosition(reader, p1);
			//var pos2 = PositionedMessagesUtils.NormalizeMessagePosition(reader, p2);
			var pos1 = p1;
			var pos2 = p2;

			tracer.Info("setting new active range {0}-{1} (aligned {2}-{3})", p1, p2, pos1, pos2);
			tracer.Info("messages before changing the active range: {0}", buffer);

			if (buffer.SetActiveRange(pos1, pos2))
			{
				tracer.Info("messages changed. new messages: {0}", buffer);

				owner.StatsTransaction(stats =>
				{
					stats.MessagesCount = buffer.Count;
					return LogProviderStatsFlag.CachedMessagesCount;
				});
				return true;
			}
			else
			{
				tracer.Info("setting a new active range didn't make any change in messages");
				return false;
			}
		}

		async Task FillCacheRanges(CancellationToken cancellationToken)
		{
			using (tracer.NewFrame)
			using (var perfop = new Profiling.Operation(tracer, "FillRanges"))
			{
				bool updateStarted = false;

				// Iterate through the ranges
				for (; ; )
				{
					cancellationToken.ThrowIfCancellationRequested();
					currentRange = buffer.GetNextRangeToFill();
					if (currentRange == null) // Nothing to fill
					{
						break;
					}
					currentMessagesContainer = buffer;
					tracer.Info("currentRange={0}", currentRange);

					bool failed = false;
					try
					{
						if (!updateStarted)
						{
							tracer.Info("Starting to update the messages.");

							updateStarted = true;
						}

						long messagesRead = 0;

						ResetFlags();

						await DisposableAsync.Using(await reader.CreateParser(new CreateParserParams(
								currentRange.GetPositionToStartReadingFrom(), currentRange.DesirableRange,
								MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading,
								MessagesParserDirection.Forward,
								postprocessor: null,
								cancellation: cancellationToken)), async parser =>
						{
							tracer.Info("parser created");
							for (; ; )
							{
								cancellationToken.ThrowIfCancellationRequested();

								ResetFlags();

								if (!await ReadNextMessage(parser))
								{
									cancellationToken.ThrowIfCancellationRequested();
									break;
								}

								ProcessLastReadMessageAndFlush();

								++messagesRead;
							}
						});

						tracer.Info("reading finished");

						FlushBuffer();
					}
					catch
					{
						failed = true;
						throw;
					}
					finally
					{
						if (!failed)
						{
							tracer.Info("Loading of the range finished successfully. Completing the range.");
							currentRange.Complete();
							tracer.Info("Disposing the range.");
						}
						else
						{
							tracer.Info("Loading failed. Disposing the range without completion.");
						}
						currentRange.Dispose();
						currentRange = null;
						currentMessagesContainer = null;
						perfop.Milestone("range completed");
					}
				}

				UpdateLoadedTimeStats(reader);

				if (updateStarted)
				{
					perfop.Milestone("great success");
					owner.SetMessagesCache(new AsyncLogProviderDataCache()
					{
						Messages = new MessagesContainers.ListBasedCollection(
							buffer.Forward(0, int.MaxValue).Select(m => m.Message)),
						MessagesRange = buffer.ActiveRange,
					});
				}
			}
		}

		void UpdateLoadedTimeStats(IPositionedMessagesReader reader)
		{
			MessagesContainers.RangesManagingCollection tmp = buffer;

			tracer.Info("Current messages: {0}", tmp);

			long bytesCount = 0;
			foreach (MessagesContainers.MessagesRange lr in tmp.Ranges)
				bytesCount += reader.PositionRangeToBytes(lr.LoadedRange);

			DateRange loadedTime;
			int c = tmp.Count;
			if (c != 0)
			{
				DateTime begin = new DateTime();
				DateTime end = new DateTime();
				foreach (IndexedMessage l in tmp.Forward(0, 1))
				{
					tracer.Info("First message: {0}, {1}", l.Message.Time, l.Message.Text);
					begin = l.Message.Time.ToLocalDateTime();
				}
				foreach (IndexedMessage l in tmp.Reverse(c - 1, c - 2))
				{
					tracer.Info("Last message: {0}, {1}", l.Message.Time, l.Message.Text);
					end = l.Message.Time.ToLocalDateTime();
				}
				loadedTime = DateRange.MakeFromBoundaryValues(begin, end);
			}
			else
			{
				loadedTime = DateRange.MakeEmpty();
			}

			tracer.Info("Calculated statistics: LoadedTime={0}, BytesLoaded={1}", loadedTime, bytesCount);

			owner.StatsTransaction(stats =>
			{
				stats.LoadedTime = loadedTime;
				stats.LoadedBytes = bytesCount;
				return LogProviderStatsFlag.CachedTime | LogProviderStatsFlag.BytesCount;
			});
		}

		void ResetFlags()
		{
			lastReadMessage = null;
		}

		async Task<bool> ReadNextMessage(IPositionedMessagesParser parser)
		{
			var tmp = await parser.ReadNextAndPostprocess();
			lastReadMessage = tmp.Message;
			return lastReadMessage != null;
		}

		private void ProcessLastReadMessageAndFlush()
		{
			if (lastReadMessage != null)
			{
				readBuffer.Add(lastReadMessage);

				if (readBuffer.Count >= 1024)
				{
					FlushBuffer();
					return;
				}
			}
		}

		bool FlushBuffer()
		{
			if (readBuffer.Count == 0)
				return false;

			bool messagesChanged = false;
			int newMessagesCount = 0;
			IMessage firstMessageWithTimeConstraintViolation = null;
			
			foreach (IMessage m in readBuffer)
			{
				try
				{
					currentRange.Add(m, false);
					messagesChanged = true;
				}
				catch (TimeConstraintViolationException e)
				{
					tracer.Warning("Time constraint violation. New rejected message: {0} {1}. Existing conflicting message: {2} {3}",
						e.ConflictingMessage2.Time, e.ConflictingMessage2.Text, e.ConflictingMessage1.Time, e.ConflictingMessage1.Text);
					if (firstMessageWithTimeConstraintViolation == null)
						firstMessageWithTimeConstraintViolation = m;
				}
			}
			if (messagesChanged)
			{
				newMessagesCount = currentMessagesContainer.Count;
			}
			readBuffer.Clear();
			if (messagesChanged)
			{
				if (currentMessagesContainer == buffer)
				{
					owner.StatsTransaction(stats =>
					{
						stats.MessagesCount = newMessagesCount;
						return LogProviderStatsFlag.CachedMessagesCount;
					});
				}
			}
			owner.StatsTransaction(stats =>
			{
				if (firstMessageWithTimeConstraintViolation != null && stats.FirstMessageWithTimeConstraintViolation == null)
				{
					stats.FirstMessageWithTimeConstraintViolation = firstMessageWithTimeConstraintViolation;
					return LogProviderStatsFlag.FirstMessageWithTimeConstraintViolation;
				}
				return LogProviderStatsFlag.None;
			});

			return true;
		}

		readonly IAsyncLogProvider owner;
		readonly LJTraceSource tracer;
		readonly MessagesContainers.RangesManagingCollection buffer;
		readonly Settings.IGlobalSettingsAccessor settingsAccessor;
		IPositionedMessagesReader reader;
		LogProviderStats currentStats;

		MessagesContainers.MessagesRange currentRange;
		MessagesContainers.RangesManagingCollection currentMessagesContainer; // todo: get rid of it
		List<IMessage> readBuffer = new List<IMessage>();
		IMessage lastReadMessage;
	};
}
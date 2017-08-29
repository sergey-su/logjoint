using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

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

		bool IAsyncLogProviderCommandHandler.RunSynchroniously(CommandContext ctx)
		{
			if (ctx.Cache == null)
				return false;
			var currentRange = ctx.Cache.MessagesRange;
			long cacheSize = CalcMaxActiveRangeSize(settingsAccessor, ctx.Cache.AvailableRange);
			bool moveCacheRange = currentRange.IsEmpty ||
				Math.Abs((currentRange.Begin + currentRange.End) / 2 - owner.ActivePositionHint) > cacheSize / 6;
			if (!moveCacheRange)
				return true;
			return false;
		}

		void IAsyncLogProviderCommandHandler.ContinueAsynchroniously(CommandContext ctx)
		{
			this.reader = ctx.Reader;
			this.currentStats = owner.Stats;
			long cacheSize = CalcMaxActiveRangeSize(settingsAccessor, 
				new FileRange.Range(ctx.Reader.BeginPosition, ctx.Reader.EndPosition));
			var startFrom = owner.ActivePositionHint;
			ConstrainedNavigate(
				startFrom - cacheSize / 2,
				startFrom + cacheSize / 2
			);
			FillCacheRanges(ctx.Preemption);
		}

		void IAsyncLogProviderCommandHandler.Complete(Exception e)
		{
		}

		static long CalcMaxActiveRangeSize(Settings.IGlobalSettingsAccessor settings, FileRange.Range availableRange)
		{
			long MB = 1024 * 1024;
			long sizeThreshold = settings.FileSizes.Threshold * MB;
			long partialLoadingSize = settings.FileSizes.WindowSize * MB;

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

		void FillCacheRanges(CancellationToken preemptionToken)
		{
			using (tracer.NewFrame)
			using (var perfop = new Profiling.Operation(tracer, "FillRanges"))
			{
				bool updateStarted = false;
				try
				{
					// Iterate through the ranges
					for (; ; )
					{
						currentRange = buffer.GetNextRangeToFill();
						if (currentRange == null) // Nothing to fill
						{
							break;
						}
						currentMessagesContainer = buffer;
						tracer.Info("currentRange={0}", currentRange);

						try
						{
							if (!updateStarted)
							{
								tracer.Info("Starting to update the messages.");

								updateStarted = true;
							}

							long messagesRead = 0;

							ResetFlags();

							// Start reading elements
							using (IPositionedMessagesParser parser = reader.CreateParser(new CreateParserParams(
									currentRange.GetPositionToStartReadingFrom(), currentRange.DesirableRange,
									MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading,
									MessagesParserDirection.Forward)))
							{
								tracer.Info("parser created");
								for (; ; )
								{
									if (preemptionToken.IsCancellationRequested)
									{
										loadingInterrupted = true;
										preemptionToken.ThrowIfCancellationRequested();
									}

									ResetFlags();

									ReadNextMessage(parser);

									ProcessLastReadMessageAndFlush();

									ReportLoadErrorIfAny();

									if (breakAlgorithm)
									{
										break;
									}

									++messagesRead;
								}
							}

							tracer.Info("reading finished");

							FlushBuffer();
						}
						finally
						{
							if (!loadingInterrupted)
							{
								tracer.Info("Loading of the range finished completly. Completing the range.");
								currentRange.Complete();
								tracer.Info("Disposing the range.");
							}
							else
							{
								tracer.Info("Loading was interrupted. Disposing the range without completion.");
							}
							currentRange.Dispose();
							currentRange = null;
							currentMessagesContainer = null;
							perfop.Milestone("range completed");
						}

						if (loadingInterrupted)
						{
							tracer.Info("Loading interrupted. Stopping to read ranges.");
							break;
						}
					}

					UpdateLoadedTimeStats(reader);
				}
				catch (Exception e)
				{
					tracer.Error(e, "failed to update cache");
					loadingInterrupted = true;
				}
				finally
				{
					if (updateStarted)
					{
						tracer.Info("Finishing to update the messages.");
						if (!loadingInterrupted)
						{
							owner.SetMessagesCache(new AsyncLogProviderDataCache()
							{
								Messages = new MessagesContainers.ListBasedCollection(
									buffer.Forward(0, int.MaxValue).Select(m => m.Message)),
								MessagesRange = buffer.ActiveRange,
								AvailableRange = currentStats.PositionsRange,
								AvailableTime = currentStats.AvailableTime,
							});
						}
					}
					else
					{
						tracer.Info("There were no ranges to read");
					}
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
			breakAlgorithm = false;
			loadingInterrupted = false;
			lastReadMessage = null;
		}

		void ReadNextMessage(IPositionedMessagesParser parser)
		{
			try
			{
				var tmp = parser.ReadNextAndPostprocess();
				lastReadMessage = tmp.Message;
				if (lastReadMessage == null)
				{
					breakAlgorithm = true;
				}
			}
			catch (Exception e)
			{
				loadError = e;
				breakAlgorithm = true;
			}
		}

		private void ReportLoadErrorIfAny()
		{
			if (loadError != null)
			{
				owner.StatsTransaction(stats =>
				{
					stats.Error = loadError;
					stats.State = LogProviderState.LoadError;
					return LogProviderStatsFlag.State | LogProviderStatsFlag.Error;
				});
			}
		}

		private bool ProcessLastReadMessageAndFlush()
		{
			if (lastReadMessage != null)
			{
				readBuffer.Add(lastReadMessage);

				if (readBuffer.Count >= 1024)
				{
					FlushBuffer();
					return true;
				}
			}
			return false;
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
				catch (TimeConstraintViolationException)
				{
					tracer.Warning("Time constraint violation. Message: %s %s", m.Time.ToString(), m.Text);
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
		Exception loadError;
		bool breakAlgorithm;
		bool loadingInterrupted;
	};
}
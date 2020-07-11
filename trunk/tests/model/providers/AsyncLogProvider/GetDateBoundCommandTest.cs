using System;
using System.Linq;
using LogJoint;
using System.Collections.Generic;
using NSubstitute;
using System.Threading;
using Range = LogJoint.FileRange.Range;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LogJoint.Tests.Providers.AsyncLogProvider
{
	[TestFixture]
	public class GetDateBoundCommandTest
	{
		static string[] expectedSyncResultsWhenMiddleOfAvailableRangeIsCached = new[]
		{
			"0LR", "0UR",
			"1LR", "1UR",
			"2UR",
			"6LR", "6U",
			"7L", "7LR", "7U", "7UR",
			"8L", "8LR", "8U", "8UR",
			"9L", "9UR",
			"14L", "14U",
			"15L", "15U",
			"16L", "16U"
		};

		[Test]
		public async Task TestSyncResultsWhenMiddleOfAvailableRangeIsCached_CacheSz3()
		{
			await TestSyncResults(new Range(20, 45), expectedSyncResultsWhenMiddleOfAvailableRangeIsCached);
		}

		[Test]
		public async Task TestSyncResultsWhenMiddleOfAvailableRangeIsCached_CacheSz2()
		{
			await TestSyncResults(new Range(20, 40), expectedSyncResultsWhenMiddleOfAvailableRangeIsCached);
		}

		[Test]
		public async Task TestSyncResultsWhenMiddleOfAvailableRangeIsCached_CacheSz1()
		{
			// cache of size 1 is useless. it can be in the middle of date equal range
			await TestSyncResults(new Range(30, 40),
				"0LR", "0UR",
				"1LR", "1UR",
				"2UR",
				"14L", "14U",
				"15L", "15U",
				"16L", "16U"
			);
		}

		static string[] expectedSyncResultsWhenBeginningOfAvailableRangeIsCached = new[]
		{
			"0L", "0U", "0LR", "0UR",
			"1L", "1U", "1LR", "1UR",
			"2L", "2U", "2LR", "2UR",
			"3L", "3U", "3LR", "3UR",
			"4L", "4U", "4LR", "4UR",
			"5L", "5U", "5LR", "5UR",
			"6L", "6U", "6LR", "6UR",
			"7L", "7LR", "7U", "7UR",
			"8L", "8LR", "8U", "8UR",
			"9L", "9UR",
			"14L", "14U",
			"15L", "15U",
			"16L", "16U"
		};

		[Test]
		public async Task TestSyncResultsWhenCacheIsAtBeginningOfLog_CacheSz3()
		{
			await TestSyncResults(new Range(0, 30), expectedSyncResultsWhenBeginningOfAvailableRangeIsCached);
		}

		[Test]
		public async Task TestSyncResultsWhenCacheIsAtBeginningOfLog_CacheSz2()
		{
			await TestSyncResults(new Range(0, 20), expectedSyncResultsWhenBeginningOfAvailableRangeIsCached);
		}

		[Test]
		public async Task TestSyncResultsWhenCacheIsAtBeginningOfLog_CacheSz1()
		{
			await TestSyncResults(new Range(0, 10),
				"0L", "0U", "0LR", "0UR",
				"1L", "1U", "1LR", "1UR",
				"2L", "2UR",
				"4L", "4UR",
				"5L", "5U", "5LR", "5UR",
				"6L", "6UR",
				"8L", "8UR",
				"9L", "9UR",
				"14L", "14U",
				"15L", "15U",
				"16L", "16U"
			);
		}

		static string[] expectedSyncResultsWhenEndOfAvailableRangeIsCached = new[]
		{
			"0LR", "0UR",
			"1LR", "1UR",
			"2UR",
			"6LR", "6U",
			"7L", "7LR", "7U", "7UR",
			"8L", "8LR", "8U", "8UR",
			"9L", "9LR", "9U", "9UR",
			"10L", "10LR", "10U", "10UR",
			"11L", "11LR", "11U", "11UR",
			"12L", "12LR", "12U", "12UR",
			"13L", "13LR", "13U", "13UR",
			"14L", "14LR", "14U", "14UR",
			"15L", "15LR", "15U", "15UR",
			"16L", "16LR", "16U", "16UR",
		};

		[Test]
		public async Task TestSyncResultsWhenCacheIsAtEndOfLog_CacheSz3()
		{
			await TestSyncResults(new Range(40, 61), expectedSyncResultsWhenEndOfAvailableRangeIsCached);
		}

		[Test]
		public async Task TestSyncResultsWhenCacheIsAtEndOfLog_CacheSz2()
		{
			await TestSyncResults(new Range(50, 61), expectedSyncResultsWhenEndOfAvailableRangeIsCached);
		}

		[Test]
		public async Task TestSyncResultsWhenCacheIsAtEndOfLog_CacheSz1()
		{
			await TestSyncResults(new Range(60, 61), 
				"0LR", "0UR",
				"1LR", "1UR",
				"2UR",
				"6LR", "6U",
				"7L", "7LR", "7U", "7UR",
				"8LR", "8U",
				"9LR", "9U",
				"10L", "10LR", "10U", "10UR",
				"11L", "11LR", "11U", "11UR",
				"12L", "12LR", "12U", "12UR",
				"13LR", "13U",
				"14L", "14LR", "14U", "14UR",
				"15L", "15LR", "15U", "15UR",
				"16L", "16LR", "16U", "16UR"
			);
		}

		[Test]
		public async Task TestSyncResultsWhenEverythingIsCache()
		{
			await TestSyncResults(new Range(0, 61),
				Enumerable.Range(0, 17).SelectMany(d => new [] {"L", "U", "LR", "UR"}.Select(b => string.Format("{0}{1}", d, b))).ToArray()
			);
		}


		static DateTime Mid(DateTime d1, DateTime d2)
		{
			return d1.AddTicks((d2 - d1).Ticks / 2);
		}

		static string ToString(ValueBound bound)
		{
			switch (bound)
			{
				case LogJoint.ValueBound.Lower: return "L";
				case LogJoint.ValueBound.LowerReversed: return "LR";
				case LogJoint.ValueBound.Upper: return "U";
				case LogJoint.ValueBound.UpperReversed: return "UR";
				default: Assert.Fail(); return "";
			}
		}

		async Task TestSyncResults(CommandContext ctx, params string[] expectedSyncResults)
		{
			var cache = ctx.Cache;
			var cachedRange = cache.Messages.DatesRange;
			var fullLogRange = ctx.Stats.AvailableTime;
			var datesToTest = new[]
			{
				fullLogRange.Begin.AddSeconds(-1),
				fullLogRange.Begin.AddTicks(-1),
				fullLogRange.Begin,
				fullLogRange.Begin.AddTicks(+1),
				Mid(fullLogRange.Begin, cachedRange.Begin),
				cachedRange.Begin.AddTicks(-1),
				cachedRange.Begin,
				cachedRange.Begin.AddTicks(+1),
				Mid(cachedRange.Begin, cachedRange.End),
				cachedRange.End.AddTicks(-1),
				cachedRange.End,
				cachedRange.End.AddTicks(+1),
				Mid(cachedRange.End, fullLogRange.End),
				fullLogRange.End.AddTicks(-1),
				fullLogRange.End,
				fullLogRange.End.AddTicks(+1),
				fullLogRange.End.AddSeconds(+1)
			};
			var boundsCache = Substitute.For<IDateBoundsCache>();
			boundsCache.Get(new DateTime()).ReturnsForAnyArgs((DateBoundPositionResponseData)null);
			int dateIdx = 0;
			foreach (var dateToTest in datesToTest)
			{
				foreach (var bound in new[] { ValueBound.Lower, ValueBound.LowerReversed, ValueBound.Upper, ValueBound.UpperReversed })
				{
					var testId = string.Format("{0}{1}", dateIdx, ToString(bound));

					var cmd = new GetDateBoundCommand(dateToTest, false, bound, boundsCache);
					IAsyncLogProviderCommandHandler cmdIntf = cmd;
					DateBoundPositionResponseData syncResult = null;
					if (cmdIntf.RunSynchronously(ctx))
					{
						cmdIntf.Complete(null);
						Assert.IsTrue(cmd.Task.IsCompleted);
						syncResult = cmd.Task.Result;
					}
					bool expectSyncResult = expectedSyncResults.IndexOf(i => i == testId) != null;
					Assert.AreEqual(expectSyncResult, syncResult != null, "Result must be sync for test " + testId);

					cmd = new GetDateBoundCommand(dateToTest, false, bound, boundsCache);
					cmdIntf = cmd;
					cmdIntf.ContinueAsynchronously(ctx).Wait();
					cmdIntf.Complete(null);
					Assert.IsTrue(cmd.Task.IsCompleted);
					DateBoundPositionResponseData asyncResult = cmd.Task.Result;

					if (syncResult != null)
					{
						Assert.AreEqual(
							await PositionedMessagesUtils.NormalizeMessagePosition(ctx.Reader, syncResult.Position),
							await PositionedMessagesUtils.NormalizeMessagePosition(ctx.Reader, asyncResult.Position),
							"Position mismatch " + testId
						);
						Assert.AreEqual(syncResult.IsBeforeBeginPosition, asyncResult.IsBeforeBeginPosition, "IsBeforeBeginPosition mismatch " + testId);
						Assert.AreEqual(syncResult.IsEndPosition, asyncResult.IsEndPosition, "IsEndPosition mismatch " + testId);
					}
				}
				++dateIdx;
			}
		}

		async Task TestSyncResults(FileRange.Range cachedRange, params string[] expectedSyncResults)
		{
			IPositionedMessagesReader reader = new PositionedMessagesUtilsTests.TestReader(new long[] { 0, 10, 20, 30, 40, 50, 60 });
			var (firstMsg, lastMsg) = await PositionedMessagesUtils.GetBoundaryMessages(reader, null);
			var availableTime = DateRange.MakeFromBoundaryValues(
				firstMsg.Time.ToLocalDateTime(),
				lastMsg.Time.ToLocalDateTime()
			);
			var availableRange = new FileRange.Range(reader.BeginPosition, reader.EndPosition);
			var ctx = new CommandContext()
			{
				Cache = new AsyncLogProviderDataCache()
				{
					Messages = new LogJoint.MessagesContainers.ListBasedCollection(),
					MessagesRange = cachedRange,
				},
				Stats = new LogProviderStats()
				{
					PositionsRange = availableRange,
					AvailableTime = availableTime,
				},
				Cancellation = CancellationToken.None,
				Preemption = CancellationToken.None,
				Reader = reader,
				Tracer = LJTraceSource.EmptyTracer
			};
			await DisposableAsync.Using(await reader.CreateParser(new CreateParserParams()
			{
				Direction = MessagesParserDirection.Forward,
				StartPosition = cachedRange.Begin,
				Range = cachedRange
			}), async parser =>
			{
				for (; ; )
				{
					IMessage msg = await parser.ReadNext();
					if (msg == null)
						break;
					else
						ctx.Cache.Messages.Add(msg);
				}
			});
			await TestSyncResults(ctx, expectedSyncResults);
		}
	}
}

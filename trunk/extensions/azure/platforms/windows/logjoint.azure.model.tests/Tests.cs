using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace LogJoint.Azure
{
	[TestClass]
	public class Tests
	{
		class AzureDiagnosticLogsTableMock : IAzureDiagnosticLogsTable
		{
			public AzureDiagnosticLogsTableMock Add(long entryTicks, string entryMessage)
			{
				Assert.IsNotNull(entryMessage);
				Assert.IsFalse(completed);
				entries.Add(new KeyValuePair<long, string>(entryTicks, entryMessage));
				return this;
			}

			public AzureDiagnosticLogEntry GetFirstEntry()
			{
				EnsureCompleted();
				return MakeWADEntry(entries.FirstOrDefault());
			}

			public AzureDiagnosticLogEntry GetFirstEntryOlderThan(string partitionKey)
			{
				EnsureCompleted();
				return MakeWADEntry(entries.FirstOrDefault(entry => 
					string.Compare(AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(entry.Key), partitionKey) > 0));
			}

			public IEnumerable<AzureDiagnosticLogEntry> GetEntriesInRange(string beginPartitionKey, string endPartitionKey, int? limit)
			{
				EnsureCompleted();
				var x =
					from entry in entries
					let pk = AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(entry.Key)
					where string.Compare(pk, beginPartitionKey) >= 0 && string.Compare(pk, endPartitionKey) < 0
					select MakeWADEntry(entry);
				if (limit != null)
					x = x.Take(limit.Value);
				return x;
			}

			static WADLogsTableEntry MakeWADEntry(KeyValuePair<long, string> entry)
			{
				if (entry.Value == null)
					return null;
				return new WADLogsTableEntry()
				{
					PartitionKey = AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(entry.Key),
					EventTickCount = entry.Key,
					Timestamp = new DateTime(entry.Key, DateTimeKind.Utc),
					Message = entry.Value
				};
			}

			void EnsureCompleted()
			{
				if (completed)
					return;
				completed = true;
				entries.Sort((e1, e2) => Math.Sign(e1.Key - e2.Key));
			}

			List<KeyValuePair<long, string>> entries = new List<KeyValuePair<long, string>>();
			bool completed;
		};

		static long TestEventTimestampFromMinutes(int minutes)
		{
			return (new DateTime(0600000000000000000)).AddMinutes(minutes).Ticks;
		}

		[TestMethod]
		public void EventTickCountToEventPartitionKeyTest()
		{
			// sample numbers are taken from read WADLogsTable

			Assert.AreEqual("0634923055200000000", AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(634923055356353096));
			Assert.AreEqual("0634904775000000000", AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(634904775152901748));
			Assert.AreEqual("0634903933800000000", AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(634903934382982291));
		}

		[TestMethod]
		public void FindLastMessage_LastMessageIsBeforeNow()
		{
			var lastEntryPK = AzureDiagnosticsUtils.FindLastMessagePartitionKey(
				new AzureDiagnosticLogsTableMock().Add(TestEventTimestampFromMinutes(1), "hey").Add(TestEventTimestampFromMinutes(3), "there"),
				new DateTime(TestEventTimestampFromMinutes(4), DateTimeKind.Utc));
			Assert.AreEqual(AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(TestEventTimestampFromMinutes(3)), lastEntryPK.ToString());
		}

		[TestMethod]
		public void FindLastMessage_LastMessageIsAfterNow()
		{
			var lastEntryPK = AzureDiagnosticsUtils.FindLastMessagePartitionKey(
				new AzureDiagnosticLogsTableMock().Add(TestEventTimestampFromMinutes(1), "hey").Add(TestEventTimestampFromMinutes(10), "there"),
				new DateTime(TestEventTimestampFromMinutes(7), DateTimeKind.Utc));
			Assert.AreEqual(AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(TestEventTimestampFromMinutes(10)), lastEntryPK.ToString());
		}

		[TestMethod]
		public void FindLastMessage_ManyMessagesAtTheSameSecond()
		{
			var lastEntryPK = AzureDiagnosticsUtils.FindLastMessagePartitionKey(
				new AzureDiagnosticLogsTableMock()
					.Add(TestEventTimestampFromMinutes(2), "hey")
					.Add(TestEventTimestampFromMinutes(9), "there")
					.Add(TestEventTimestampFromMinutes(9)+1, "there2")
					.Add(TestEventTimestampFromMinutes(9)+2, "there3"),
				new DateTime(TestEventTimestampFromMinutes(15), DateTimeKind.Utc));
			Assert.AreEqual(AzureDiagnosticsUtils.EventTickCountToEventPartitionKey(TestEventTimestampFromMinutes(9)), lastEntryPK.ToString());
		}

		[TestMethod]
		public void LoadMessagesRange_EntriesAlreadySortedWithingPartitions()
		{
			var msgs = AzureDiagnosticsUtils.LoadWADLogsTableMessagesRange(
				new AzureDiagnosticLogsTableMock()
					.Add(TestEventTimestampFromMinutes(2), "1")
					.Add(TestEventTimestampFromMinutes(2) + 1, "2")
					.Add(TestEventTimestampFromMinutes(3), "3")
					.Add(TestEventTimestampFromMinutes(3) + 1, "4"),
				new LogJoint.LogSourceThreads(), EntryPartition.MinValue, EntryPartition.MaxValue, null).ToArray();
			var str = string.Join("|", msgs.Select(m => m.Text.ToString()).ToArray());
			Assert.AreEqual("1|2|3|4", str);
		}

		[TestMethod]
		public void LoadMessagesRange_EntriesAreUnsortedWithingPartitions()
		{
			var msgs = AzureDiagnosticsUtils.LoadWADLogsTableMessagesRange(
				new AzureDiagnosticLogsTableMock()
					.Add(TestEventTimestampFromMinutes(2) + 1, "1")
					.Add(TestEventTimestampFromMinutes(2), "2")
					.Add(TestEventTimestampFromMinutes(3), "3")
					.Add(TestEventTimestampFromMinutes(4) + 10, "4")
					.Add(TestEventTimestampFromMinutes(5) + 50, "5"),
				new LogJoint.LogSourceThreads(), EntryPartition.MinValue, EntryPartition.MaxValue, null).ToArray();
			var str = string.Join("|", msgs.Select(m => m.Text.ToString()).ToArray());
			Assert.AreEqual("2|1|3|4|5", str);
		}

		static void FindDateBoundTest(PositionedMessagesUtils.ValueBound bound, long dateTicks, 
			string expectedMessage, EntryPartition? searchRangeBegin = null, EntryPartition? searchRangeEnd = null)
		{
			var entry = AzureDiagnosticsUtils.FindDateBound(
				new AzureDiagnosticLogsTableMock()
					.Add(TestEventTimestampFromMinutes(1), "1")
					.Add(TestEventTimestampFromMinutes(1) + 2, "1+2")
					.Add(TestEventTimestampFromMinutes(1) + 2, "1+2.2")
					.Add(TestEventTimestampFromMinutes(1) + 3, "1+3")
					.Add(TestEventTimestampFromMinutes(1) + 10, "1+10")
					.Add(TestEventTimestampFromMinutes(2), "2")
					.Add(TestEventTimestampFromMinutes(3) + 100, "3+100")
					.Add(TestEventTimestampFromMinutes(3) + 200, "3+200")
					.Add(TestEventTimestampFromMinutes(3) + 300, "3+300")
					.Add(TestEventTimestampFromMinutes(3) + 300, "3+300.2")
					.Add(TestEventTimestampFromMinutes(1000) + 10, "1000+10")
					.Add(TestEventTimestampFromMinutes(1000) + 20, "1000+20")
					.Add(TestEventTimestampFromMinutes(100000), "100000")
					.Add(TestEventTimestampFromMinutes(100000) + 100, "100000+100"),
				new DateTime(dateTicks, DateTimeKind.Utc), bound, 
					searchRangeBegin.GetValueOrDefault(EntryPartition.MinValue), 
					searchRangeEnd.GetValueOrDefault(EntryPartition.MaxValue), 
					CancellationToken.None);
			if (expectedMessage == null)
			{
				Assert.IsTrue(!entry.HasValue);
			}
			else
			{
				Assert.IsTrue(entry.HasValue);
				Assert.AreEqual(expectedMessage, (entry.Value.Entry as WADLogsTableEntry).Message);
			}
		}

		[TestMethod]
		public void FindDateBound_LowerBound()
		{
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.Lower, TestEventTimestampFromMinutes(0), "1");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.Lower, TestEventTimestampFromMinutes(1) + 3, "1+3");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.Lower, TestEventTimestampFromMinutes(1) + 4, "1+10");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.Lower, TestEventTimestampFromMinutes(3) + 300, "3+300");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.Lower, TestEventTimestampFromMinutes(3) + 301, "1000+10");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.Lower, TestEventTimestampFromMinutes(1000) + 50, "100000");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.Lower, TestEventTimestampFromMinutes(300000), null);
		}

		[TestMethod]
		public void FindDateBound_LowerReversedBound()
		{
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(300000), "100000+100");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(100000) + 101, "100000+100");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(100000) + 100, "100000+100");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(100000) + 99, "100000");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(3) + 300, "3+300.2");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(3) + 400, "3+300.2");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(1), "1");
			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(0), null);

			FindDateBoundTest(PositionedMessagesUtils.ValueBound.LowerReversed, TestEventTimestampFromMinutes(3) + 150, "3+100", 
				new EntryTimestamp(TestEventTimestampFromMinutes(3)).Partition);
		}

		[TestMethod]
		public void FindDateBound_LowerReversedBound_BugFromRealLogs1()
		{
			var tableMock = new AzureDiagnosticLogsTableMock()
					.Add(634929032152764926, "1")
					.Add(634929034677682953, "2")
					.Add(634929034680972605, "3")
					.Add(634931678260882377, "4");
			var bound = AzureDiagnosticsUtils.FindDateBound(tableMock, 
				new DateTime(634929210892764926, DateTimeKind.Utc), PositionedMessagesUtils.ValueBound.LowerReversed,
				new EntryPartition(0634929031800000000), new EntryPartition(0634932574800000000), CancellationToken.None);
			Assert.IsTrue(bound.HasValue && (bound.Value.Entry as WADLogsTableEntry).Message == "3");
		}

		[TestMethod]
		public void FindDateBound_LowerBound_BugFromRealLogs2()
		{
			var tableMock = new AzureDiagnosticLogsTableMock()
					.Add(634929032152764926, "1")
					.Add(634929034677682953, "2")
					.Add(634929034680972605, "3")
					.Add(634931678260882377, "4");
			var bound = AzureDiagnosticsUtils.FindDateBound(tableMock, new DateTime(634929213420972605, DateTimeKind.Utc), 
				PositionedMessagesUtils.ValueBound.Lower,
				new EntryPartition(0634929031800000000), new EntryPartition(0634932574800000000), CancellationToken.None);
			Assert.IsTrue(bound.HasValue && (bound.Value.Entry as WADLogsTableEntry).Message == "4");
		}

	}
}

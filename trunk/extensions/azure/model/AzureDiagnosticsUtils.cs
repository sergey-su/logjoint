using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogJoint.Azure
{
	public struct EntryPartition
	{
		public EntryPartition(string partitionKey)
		{
			partitionKeyTicks = RemoveSeconds(long.Parse(partitionKey));
		}

		public EntryPartition(EntryTimestamp timestamp)
		{
			partitionKeyTicks = RemoveSeconds(timestamp.Ticks);
		}

		public EntryPartition(long eventTicks)
		{
			partitionKeyTicks = RemoveSeconds(eventTicks);
		}

		public static readonly EntryPartition MaxValue = new EntryPartition() { partitionKeyTicks = long.MaxValue };
		public static readonly EntryPartition MinValue = new EntryPartition() { partitionKeyTicks = 0 };

		public override string ToString()
		{
			return partitionKeyTicks.ToString("0000000000000000000");
		}

		public long Ticks { get { return partitionKeyTicks; } }

		public long MakeMessagePosition(int messageIndexWithinPartition)
		{
			return partitionKeyTicks + messageIndexWithinPartition;
		}

		public EntryPartition Advance(long distance = 1)
		{
			if (distance > 0)
			{
				distance = Math.Min(distance, (MaxValue.Ticks - partitionKeyTicks) / AzureDiagnosticsUtils.ticksPerMinute);
				return new EntryPartition() { partitionKeyTicks = this.partitionKeyTicks + distance * AzureDiagnosticsUtils.ticksPerMinute };
			}
			else if (distance < 0)
			{
				return new EntryPartition() { partitionKeyTicks = Math.Max(0, this.partitionKeyTicks + distance * AzureDiagnosticsUtils.ticksPerMinute) };
			}
			else
			{
				return this;
			}
		}

		public static EntryPartition Max(EntryPartition p1, EntryPartition p2)
		{
			return new EntryPartition() { partitionKeyTicks = Math.Max(p1.Ticks, p2.Ticks) };
		}

		public static int Compare(EntryPartition p1, EntryPartition p2)
		{
			return Math.Sign(p1.Ticks - p2.Ticks);
		}

		public static int Distance(EntryPartition begin, EntryPartition end)
		{
			return AzureDiagnosticsUtils.TicksToMinutes(end.Ticks - begin.Ticks);
		}

		static long RemoveSeconds(long ticks)
		{
			return (ticks / AzureDiagnosticsUtils.ticksPerMinute) * AzureDiagnosticsUtils.ticksPerMinute;
		}

		long partitionKeyTicks;
	};

	public struct EntryTimestamp
	{
		public EntryTimestamp(long utcTicks)
		{
			this.utcTicks = utcTicks;
		}
		public EntryTimestamp(DateTime dateTime)
		{
			this.utcTicks = dateTime.ToUniversalTime().Ticks;
		}
		public EntryTimestamp(MessageTimestamp messageTimestamp):
			this(messageTimestamp.ToUniversalTime().Ticks)
		{
		}
		public long Ticks
		{
			get { return utcTicks; }
		}
		public EntryPartition Partition
		{
			get { return new EntryPartition(this); }
		}

		readonly long utcTicks;
	};

	public static class AzureDiagnosticsUtils
	{
		public static EntryPartition? FindFirstMessagePartitionKey(IAzureDiagnosticLogsTable wadTable)
		{
			var firstEntry = wadTable.GetFirstEntry();
			if (firstEntry == null)
				return null;
			return new EntryPartition(firstEntry.PartitionKey);
		}

		public static EntryPartition? FindLastMessagePartitionKey(IAzureDiagnosticLogsTable wadTable, DateTime utcNow)
		{
			if (utcNow.Kind != DateTimeKind.Utc)
				throw new ArgumentException("time must be of UTC kind", "utcNow");
			var rangeForBinarySearch = GetRangeForLastMessageBinarySearch(wadTable, utcNow);
			if (rangeForBinarySearch == null)
				return null;
			
			EntryPartition begin = rangeForBinarySearch.Item1;
			EntryPartition end = rangeForBinarySearch.Item2;

			int searchRangeDuration = EntryPartition.Distance(begin, end);
			int pos = ListUtils.BinarySearch(
				new ListUtils.VirtualList<int>(searchRangeDuration, i => i), 0, searchRangeDuration,
				i => wadTable.GetFirstEntryOlderThan(begin.Advance(i).ToString()) != null);
			
			if (pos == searchRangeDuration)
				return null;

			return begin.Advance(pos);
		}

		internal static int TicksToMinutes(long ticks)
		{
			return (int)(ticks / ticksPerMinute);
		}

		static long MinutesToTicks(int minutes)
		{
			return minutes * ticksPerMinute;
		}

		static Tuple<EntryPartition, EntryPartition> GetRangeForLastMessageBinarySearch(IAzureDiagnosticLogsTable wadTable, DateTime utcNow)
		{
			EntryPartition initialPartition = new EntryTimestamp(utcNow).Partition;

			bool thereAreEntriesOlderThanNow = wadTable.GetFirstEntryOlderThan(initialPartition.ToString()) != null;
			bool searchingForward = thereAreEntriesOlderThanNow;

			EntryPartition currentPartition = initialPartition;

			for (long step = searchingForward ? 1 : -1; ; step *= 2)
			{
				EntryPartition t = initialPartition.Advance(step);
				if (EntryPartition.Compare(currentPartition, t) == 0)
					return null;
				var tmp = wadTable.GetFirstEntryOlderThan(t.ToString());
				if (searchingForward)
				{
					if (tmp == null)
						return new Tuple<EntryPartition, EntryPartition>(currentPartition.Advance(), t);
				}
				else
				{
					if (tmp != null)
						return new Tuple<EntryPartition, EntryPartition>(new EntryPartition(tmp.EventTickCount), currentPartition.Advance());
				}
				currentPartition = t;
			}
		}

		/// <summary>
		/// Loads a range of messages from Logs Table. The range is specified by two partition keys.
		/// </summary>
		/// <param name="wadTable">Table to load messages from</param>
		/// <param name="threads">Threads container that will store loaded threads</param>
		/// <param name="beginPartitionKey">Begin of the range. Messages with PartitionKey GREATER THAN or EQUAL to <paramref name="beginPartitionKey"/> are included to the range</param>
		/// <param name="endPartitionKey">End of the range. Messages with PartitionKey LESS THAN <paramref name="endPartitionKey"/> are included to the range</param>
		/// <param name="entriesLimit">If specified limits the number of items to return</param>
		/// <returns>Sequence of messages sorted by EventTickCount</returns>
		public static IEnumerable<MessageBase> LoadMessagesRange(
			IAzureDiagnosticLogsTable wadTable, 
			LogSourceThreads threads, 
			EntryPartition beginPartition,
			EntryPartition endPartition,
			int? entriesLimit)
		{
			foreach (var entryAndIndex in LoadEntriesRange(wadTable, beginPartition, endPartition, entriesLimit))
			{
				var entry = entryAndIndex.Entry;
				yield return new Content(
					new EntryPartition(entry.EventTickCount).MakeMessagePosition(entryAndIndex.IndexWithinPartition),
					threads.GetThread(new StringSlice(string.Format("{0}-{1}", entry.Pid, entry.Tid))),
					new MessageTimestamp(new DateTime(entry.EventTickCount, DateTimeKind.Utc)),
					new StringSlice(entry.Message),
					Content.SeverityFlag.Info
				);
			}
		}

		static IEnumerable<IndexedAzureDiagnosticLogEntry> LoadEntriesRange(
			IAzureDiagnosticLogsTable wadTable,
			EntryPartition beginPartition,
			EntryPartition endPartition,
			int? entriesLimit)
		{
			string currentPartitionKey = null;
			var currentPartitionEntries = new List<AzureDiagnosticLogEntry>();
			Comparison<AzureDiagnosticLogEntry> compareEntries = (e1, e2) => Math.Sign(e1.EventTickCount - e2.EventTickCount);
			foreach (var entry in wadTable.GetEntriesInRange(beginPartition.ToString(), endPartition.ToString(), entriesLimit))
			{
				if (entry.PartitionKey != currentPartitionKey)
				{
					currentPartitionEntries.Sort(compareEntries);
					for (var i = 0; i < currentPartitionEntries.Count; ++i)
						yield return new IndexedAzureDiagnosticLogEntry(currentPartitionEntries[i], i);
					currentPartitionEntries.Clear();
					currentPartitionKey = entry.PartitionKey;
				}
				currentPartitionEntries.Add(entry);
			}
			currentPartitionEntries.Sort(compareEntries);
			for (var i = 0; i < currentPartitionEntries.Count; ++i)
				yield return new IndexedAzureDiagnosticLogEntry(currentPartitionEntries[i], i);
		}

		public static IndexedAzureDiagnosticLogEntry? FindDateBound(
			IAzureDiagnosticLogsTable wadTable,
			DateTime date,
			PositionedMessagesUtils.ValueBound bound,
			EntryPartition searchRangeBegin,
			EntryPartition searchRangeEnd,
			CancellationToken cancellationToken)
		{
			switch (bound)
			{
				case PositionedMessagesUtils.ValueBound.Lower:
					return FindLowerDateBound(wadTable, date, searchRangeEnd);
				case PositionedMessagesUtils.ValueBound.LowerReversed:
					return FindLowerReversedDateBound(wadTable, date, searchRangeBegin, cancellationToken);
				default:
					throw new NotImplementedException("Searching for " + bound.ToString() + " bound in Azure Diagnostics Logs is not implemented");
			}
		}

		static IndexedAzureDiagnosticLogEntry? FindLowerDateBound(
			IAzureDiagnosticLogsTable wadTable,
			DateTime date,
			EntryPartition searchRangeEnd)
		{
			var dateTimestamp = new EntryTimestamp(date);
			var ret = LoadEntriesRange(wadTable, dateTimestamp.Partition, searchRangeEnd, null)
				.FirstOrDefault(e => e.Entry.EventTickCount >= dateTimestamp.Ticks);
			return ret.Entry != null ? ret : new IndexedAzureDiagnosticLogEntry?();
		}

		static IndexedAzureDiagnosticLogEntry FindLowerReversedDateBoundInPartition(IAzureDiagnosticLogsTable wadTable,
			EntryPartition partition, long dateTicks)
		{
			var ret = LoadEntriesRange(wadTable, partition, partition.Advance(), null)
				.LastOrDefault(e => e.Entry.EventTickCount <= dateTicks);
			return ret;
		}

		static IndexedAzureDiagnosticLogEntry? FindLowerReversedDateBound(
			IAzureDiagnosticLogsTable wadTable,
			DateTime date,
			EntryPartition searchRangeBegin,
			CancellationToken cancellationToken)
		{
			EntryTimestamp dateTimestamp = new EntryTimestamp(date);
			var ret = FindLowerReversedDateBoundInPartition(wadTable, dateTimestamp.Partition, dateTimestamp.Ticks);
			if (ret.Entry != null)
				return ret;

			var rangeForBinarySearch = GetRangeForLowerReversedDateBoundBinarySearch(wadTable, dateTimestamp.Ticks, 
				searchRangeBegin, cancellationToken);
			if (rangeForBinarySearch == null)
				return null;

			EntryPartition begin = rangeForBinarySearch.Item1;
			EntryPartition end = rangeForBinarySearch.Item2;

			int searchRangeDuration = (int)EntryPartition.Distance(begin, end);
			int pos = ListUtils.BinarySearch(
				new ListUtils.VirtualList<int>(searchRangeDuration, i => i), 0, searchRangeDuration,
				timeBeingTested => 
				{
					cancellationToken.ThrowIfCancellationRequested();
					var firstEntryFollowingTimeBeingTested = wadTable.GetFirstEntryOlderThan(begin.Advance(timeBeingTested).ToString());
					bool lessThanBoundaryBeingSearched;
					if (firstEntryFollowingTimeBeingTested == null)
						lessThanBoundaryBeingSearched = false;
					else
						lessThanBoundaryBeingSearched = firstEntryFollowingTimeBeingTested.EventTickCount < dateTimestamp.Ticks;
					return lessThanBoundaryBeingSearched;
				});
			if (pos == searchRangeDuration)
				return null;

			ret = FindLowerReversedDateBoundInPartition(wadTable, begin.Advance(pos), dateTimestamp.Ticks);
			if (ret.Entry != null)
				return ret;

			return null;
		}

		static Tuple<EntryPartition, EntryPartition> GetRangeForLowerReversedDateBoundBinarySearch(
			IAzureDiagnosticLogsTable wadTable, 
			long dateTicks,
			EntryPartition searchRangeBegin,
			CancellationToken cancellationToken)
		{
			EntryPartition datePartition = new EntryPartition(new EntryTimestamp(dateTicks));
			EntryPartition lastStepPartition = datePartition;
			for (int step = 1; ; step *= 2)
			{
				EntryPartition p = EntryPartition.Max(searchRangeBegin, datePartition.Advance(step));
				if (EntryPartition.Compare(p, lastStepPartition) == 0)
					return null;
				var tmp = wadTable.GetFirstEntryOlderThan(p.ToString());
				if (tmp != null && tmp.EventTickCount < dateTicks)
					return new Tuple<EntryPartition, EntryPartition>(
						new EntryPartition(tmp.PartitionKey),
						lastStepPartition.Advance());
				lastStepPartition = p;
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		static readonly long ticksPerSecond = 10000 * 1000;
		internal static readonly long ticksPerMinute = ticksPerSecond * 60;

		public static string GetNextPartitionKey(string pk)
		{
			return new EntryPartition(pk).Advance().ToString();
		}

		public static string EventTickCountToEventPartitionKey(long ticks)
		{
			return new EntryTimestamp(ticks).Partition.ToString();
		}
	}
}

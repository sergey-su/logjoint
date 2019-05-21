using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LogJoint
{
	// todo: move next to sources
	public class TimeOffsets : ITimeOffsets, IEquatable<ITimeOffsets>
	{
		public class Builder : ITimeOffsetsBuilder
		{
			bool baseOffsetSet;
			List<Entry> entries = new List<Entry>();
			TimeOffsets result;

			void ITimeOffsetsBuilder.SetBaseOffset(TimeSpan value)
			{
				AssertNotCompleted();
				if (baseOffsetSet)
					throw new InvalidOperationException();
				baseOffsetSet = true;
				entries.Add(new Entry() { at = DateTime.MinValue, offset = value });
			}

			void ITimeOffsetsBuilder.AddOffset(DateTime at, TimeSpan offset)
			{
				AssertNotCompleted();
				entries.Add(new Entry() { at = at, offset = offset });
			}

			ITimeOffsets ITimeOffsetsBuilder.ToTimeOffsets()
			{
				if (result != null)
					return result;

				if (!baseOffsetSet)
					entries.Add(new Entry() { at = DateTime.MinValue });
				entries.Sort((e1, e2) => Math.Sign(e2.at.Ticks - e1.at.Ticks));
				for (int i = entries.Count - 2; i >= 0; --i)
					entries[i].offset += entries[i + 1].offset;

				result = new TimeOffsets(entries);
				
				return result;
			}

			void AssertNotCompleted()
			{
				if (result != null)
					throw new InvalidOperationException("can not modify completed TimeOffsets");
			}
		};

		public static ITimeOffsets Empty { get { return emptyOffsets; } }

		public static bool TryParse(string str, out ITimeOffsets value)
		{
			value = null;
			if (str == null)
				return false;
			var entries = new List<Entry>();
			bool firstPart = true;
			foreach (var part in str.Split(','))
			{
				var entry = new Entry();
				if (firstPart)
				{
					firstPart = false;
					if (!TimeSpan.TryParseExact(part, "c", null, out entry.offset))
						return false;
					entry.at = DateTime.MinValue;
				}
				else
				{
					var components = part.Split('=');
					if (components.Length != 2)
						return false;
					if (!TimeSpan.TryParseExact(components[1], "c", null, out entry.offset))
						return false;
					entry.at = MessageTimestamp.ParseFromLoselessFormat(components[0]).ToUnspecifiedTime();
				}
				entries.Add(entry);
			}
			if (entries.Count == 0)
				return false;
			entries.Sort((e1, e2) => Math.Sign(e2.at.Ticks - e1.at.Ticks));
			value = new TimeOffsets(entries);
			return true;
		}

		public override string ToString()
		{
			return
				((IEnumerable<Entry>)entries)
				.Reverse()
				.Aggregate(
					new StringBuilder(),
					(sb, e) => sb.AppendFormat(e.IsBaseOffsetEntry ? "{1:c}" : ",{0}={1:c}", 
						new MessageTimestamp(e.at).StoreToLoselessFormat(), e.offset),
					sb => sb.ToString()
				);
		}

		DateTime ITimeOffsets.Get(DateTime dateTime)
		{
			var offset = offsetGetter(dateTime);
			try
			{
				return dateTime + offset;
			}
			catch (ArgumentOutOfRangeException)
			{
				if (offset.Ticks < 0)
				{
					if ((dateTime - DateTime.MinValue) < -offset)
						return DateTime.MinValue;
				}
				if (offset.Ticks > 0)
				{
					if ((DateTime.MaxValue - dateTime) < offset)
						return DateTime.MaxValue;
				}
				throw new ArgumentOutOfRangeException(
					string.Format("Time offset {0} can not be applied to DateTime {1}", offset, dateTime));
			}
		}

		ITimeOffsets ITimeOffsets.Inverse()
		{
			var inverseEntries = new List<Entry>();
			foreach (var entry in entries)
				inverseEntries.Add(new Entry() { 
				at = entry.at != DateTime.MinValue ? entry.at + entry.offset : entry.at, 
				offset = -entry.offset 
			});
			return new TimeOffsets(inverseEntries);
		}

		TimeSpan ITimeOffsets.BaseOffset
		{
			get { return entries[entries.Count - 1].offset; }
		}

		bool ITimeOffsets.IsEmpty
		{
			get { return entries.All(e => e.offset == TimeSpan.Zero); }
		}

		bool IEquatable<ITimeOffsets>.Equals(ITimeOffsets obj)
		{
			TimeOffsets other = obj as TimeOffsets;
			if (other == null)
				return false;
			return EqualsInternal(other);
		}

		public override bool Equals(object obj)
		{
			TimeOffsets other = obj as TimeOffsets;
			if (other == null)
				return false;
			return EqualsInternal(other);
		}

		public override int GetHashCode()
		{
			return entries.Aggregate(967, (h, e) => h ^ Entry.comparer.GetHashCode(e));
		}


		private TimeOffsets(List<Entry> entries)
		{
			this.entries = entries;

			var baseOffset = entries[entries.Count - 1].offset;

			// generate optimized offset getters for typical cases
			// fallback to generic getter for other cases
			if (entries.Count == 1) // base offset only
			{
				offsetGetter = dateTime => baseOffset;
			}
			else if (entries.Count == 2) // one offset
			{
				var offset1 = entries[0];
				offsetGetter = dateTime => dateTime < offset1.at ? baseOffset : offset1.offset;
			}
			else
			{
				// generic getter
				offsetGetter = dateTime =>
					entries[ListUtils.BinarySearch(entries, 0, entries.Count, e => e.at > dateTime)].offset;
			}
		}

		bool EqualsInternal(TimeOffsets other)
		{
			return entries.SequenceEqual(other.entries, Entry.comparer);
		}


		[DebuggerDisplay("at {at} {offset}")]
		class Entry
		{
			public DateTime at;
			public TimeSpan offset;

			class Comparer : IEqualityComparer<Entry>
			{
				bool IEqualityComparer<Entry>.Equals(Entry x, Entry y)
				{
					return x.at == y.at && x.offset == y.offset;
				}

				int IEqualityComparer<Entry>.GetHashCode(Entry obj)
				{
					return obj.at.GetHashCode() ^ obj.offset.GetHashCode();
				}
			};

			public bool IsBaseOffsetEntry { get { return at == DateTime.MinValue; } }

			public static IEqualityComparer<Entry> comparer = new Comparer();
		};


		readonly static ITimeOffsets emptyOffsets = ((ITimeOffsetsBuilder)new Builder()).ToTimeOffsets();

		readonly List<Entry> entries;
		readonly Func<DateTime, TimeSpan> offsetGetter;
	};
}
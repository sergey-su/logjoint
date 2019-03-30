using System;
using System.Collections.Generic;

namespace LogJoint
{
	public class LRUCache<K, V>
	{
		private readonly int capacity;
		private readonly Dictionary<K, Entry> map = new Dictionary<K, Entry>();
		private readonly LinkedList<K> lruKeys = new LinkedList<K>();

		public LRUCache(int capacity)
		{
			if (capacity <= 0)
				throw new ArgumentException("bad LRUCache capacity");
			this.capacity = capacity;
		}

		public V this[K key]
		{
			get
			{
				if (!TryGetValue(key, out var value))
					throw new KeyNotFoundException();
				return value;
			}
			set => Set(key, value);
		}

		public bool TryGetValue(K key, out V value)
		{
			if (!map.TryGetValue(key, out var entry))
			{
				value = default;
				return false;
			}
			Touch(entry.keyNode);
			value = entry.value;
			return true;
		}

		public bool ContainsKey(K key)
		{
			return map.ContainsKey(key);
		}

		public void Set(K key, V value)
		{
			if (map.TryGetValue(key, out var node))
			{
				Touch(node.keyNode);
				node.value = value;
			}
			else
			{
				if (map.Count >= capacity)
				{
					map.Remove(lruKeys.First.Value);
					lruKeys.RemoveFirst();
				}
				map.Add(key, new Entry()
				{
					keyNode = lruKeys.AddLast(key),
					value = value
				});
			}
		}

		public void Clear()
		{
			lruKeys.Clear();
			map.Clear();
		}

		public int Count => map.Count;

		private void Touch(LinkedListNode<K> i)
		{
			lruKeys.Remove(i);
			lruKeys.AddLast(i);
		}

		private class Entry
		{
			public V value;
			public LinkedListNode<K> keyNode;
		}
	}
}


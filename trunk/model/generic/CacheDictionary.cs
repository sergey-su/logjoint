using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogJoint
{
	public class CacheDictionary<K, V>
	{
		public class Entry
		{
			public bool valid;
			public V value;
		};

		readonly Dictionary<K, Entry> cache = new Dictionary<K, Entry>();

		public void MarkAllInvalid()
		{
			foreach (var x in cache)
				x.Value.valid = false;
		}

		public V Get(K key, Func<K, V> factory)
		{
			Entry entry;
			if (!cache.TryGetValue(key, out entry))
				cache.Add(key, entry = new Entry() { value = factory(key) });
			entry.valid = true;
			return entry.value;
		}

		public V Get(K key)
		{
			Entry entry;
			if (!cache.TryGetValue(key, out entry))
				return default(V);
			return entry.value;
		}

		public void Cleanup()
		{
			foreach (var k in cache.Where(x => !x.Value.valid).Select(x => x.Key).ToArray())
				cache.Remove(k);
		}
	};
}

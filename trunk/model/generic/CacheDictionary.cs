using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogJoint
{
    public class CacheDictionary<K, V> where K: notnull
    {
        public class Entry
        {
            public bool valid;
            required public V value;
        };

        readonly Dictionary<K, Entry> cache = new Dictionary<K, Entry>();

        public void MarkAllInvalid()
        {
            foreach (var x in cache)
                x.Value.valid = false;
        }

        public V Get(K key, Func<K, V> factory)
        {
            Entry? entry;
            if (!cache.TryGetValue(key, out entry))
                cache.Add(key, entry = new Entry() { value = factory(key) });
            entry.valid = true;
            return entry.value;
        }

        public V? Get(K key)
        {
            Entry? entry;
            if (!cache.TryGetValue(key, out entry))
                return default;
            return entry.value;
        }

        public void Cleanup(Action<KeyValuePair<K, V>>? finalizer = null)
        {
            var deadEntries = cache.Where(x => !x.Value.valid).ToList();
            foreach (var e in deadEntries)
                cache.Remove(e.Key);
            if (finalizer != null)
                deadEntries.ForEach(e => finalizer(new KeyValuePair<K, V>(e.Key, e.Value.value)));
        }
    };
}

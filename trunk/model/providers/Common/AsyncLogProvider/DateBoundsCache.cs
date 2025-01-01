using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
    public interface IDateBoundsCache
    {
        DateBoundPositionResponseData Get(DateTime d);
        void Set(DateTime d, DateBoundPositionResponseData data);
        void Invalidate();
    };

    class DateBoundsCache : IDateBoundsCache
    {
        object sync = new object();
        class Entry
        {
            public int lastUsed;
            public DateBoundPositionResponseData value;
        };
        Dictionary<DateTime, Entry> data = new Dictionary<DateTime, Entry>();
        int time;
        int hitsCount;
        int requestsCount;

        DateBoundPositionResponseData IDateBoundsCache.Get(DateTime d)
        {
            lock (sync)
            {
                requestsCount++;
                DateBoundsCache.Entry e;
                if (data.TryGetValue(d, out e))
                {
                    hitsCount++;
                    e.lastUsed = ++time;
                    return e.value;
                }
            }
            return null;
        }

        void IDateBoundsCache.Set(DateTime d, DateBoundPositionResponseData rsp)
        {
            lock (sync)
            {
                data[d] = new DateBoundsCache.Entry()
                {
                    lastUsed = ++time,
                    value = rsp
                };
                int sizeThreshold = 512;
                if (data.Count > sizeThreshold)
                {
                    // drop half of oldest entries when the threshold is reached
                    data = data
                        .OrderByDescending(x => x.Value.lastUsed)
                        .Take(sizeThreshold / 2)
                        .ToDictionary(x => x.Key, x => x.Value);
                }
            }
        }

        void IDateBoundsCache.Invalidate()
        {
            lock (sync)
            {
                data.Clear();
            }
        }
    };
}

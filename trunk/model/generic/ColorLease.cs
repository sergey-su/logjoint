using System;
using System.Collections.Generic;

namespace LogJoint
{
    public interface IColorLease
    {
        int GetNextColor(int? preferredColor = null);
        void ReleaseColor(int index);
        void Reset();
    };

    public interface IColorLeaseConfig
    {
        Func<int> ColorsCountSelector { get; set; }
    };

    public class ColorLease : IColorLease, IColorLeaseConfig
    {
        public ColorLease(int colorsCount)
        {
            this.colorsCountSelector = () => colorsCount;
            this.refCounters = new List<int>();
        }

        int IColorLease.GetNextColor(int? preferredColor)
        {
            int retIdx = 0;
            lock (sync)
            {
                var (colorsCount, counters) = this.GetState();
                int minRefcounter = int.MaxValue;
                for (int idx = 0; idx < colorsCount; ++idx)
                {
                    if (preferredColor != null && preferredColor.Value == idx)
                    {
                        retIdx = idx;
                        break;
                    }
                    int refCount = counters[idx];
                    if (refCount < minRefcounter)
                    {
                        minRefcounter = counters[idx];
                        retIdx = idx;
                    }
                }
                ++counters[retIdx];
                return retIdx;
            }
        }

        void IColorLease.ReleaseColor(int index)
        {
            lock (sync)
            {
                var (colorsCount, counters) = this.GetState();
                if (counters[index] > 0)
                    --counters[index];
            }
        }

        void IColorLease.Reset()
        {
            lock (sync)
            {
                for (int idx = 0; idx < refCounters.Count; ++idx)
                    refCounters[idx] = 0;
            }
        }

        Func<int> IColorLeaseConfig.ColorsCountSelector
        {
            get => colorsCountSelector;
            set => colorsCountSelector = value ?? throw new NullReferenceException();
        }

        private (int colorsCount, List<int> counters) GetState()
        {
            var colorsCount = colorsCountSelector();
            if (colorsCount < 0)
                throw new InvalidOperationException("color count can be negative, got " + colorsCount.ToString());
            if (colorsCount > refCounters.Count)
            {
                refCounters.Capacity = colorsCount;
                for (int i = refCounters.Count; i < colorsCount; ++i)
                    refCounters.Add(0);
            }
            return (colorsCount, refCounters);
        }

        readonly object sync = new object();
        Func<int> colorsCountSelector;
        List<int> refCounters;
    };
}

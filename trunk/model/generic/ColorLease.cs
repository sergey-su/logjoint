namespace LogJoint
{
	public interface IColorLease
	{
		int GetNextColor(int? preferredColor = null);
		void ReleaseColor(int index);
		void Reset();
	};

	public class ColorLease : IColorLease
	{
		public ColorLease(int colorsCount)
		{
			this.refCounters = new int[colorsCount];
		}

		int IColorLease.GetNextColor(int? preferredColor)
		{
			int retIdx = 0;
			lock (sync)
			{
				int minRefcounter = int.MaxValue;
				for (int idx = 0; idx < refCounters.Length; ++idx)
				{
					if (preferredColor != null && preferredColor.Value == idx)
					{
						retIdx = idx;
						break;
					}
					int refCount = refCounters[idx];
					if (refCount < minRefcounter)
					{
						minRefcounter = refCounters[idx];
						retIdx = idx;
					}
				}
				++refCounters[retIdx];
				return retIdx;
			}
		}

		void IColorLease.ReleaseColor(int index)
		{
			lock (sync)
			{
				if (refCounters[index] > 0)
					--refCounters[index];
			}
		}

		void IColorLease.Reset()
		{
			lock (sync)
			{
				for (int idx = 0; idx < refCounters.Length; ++idx)
					refCounters[idx] = 0;
			}
		}

		readonly object sync = new object();
		readonly int[] refCounters;
	};
}

using System.Threading;

namespace LogJoint
{
	public class LazyUpdateFlag
	{
		public void Invalidate()
		{
			updateRequired = 1;
		}

		public bool Validate()
		{
			return Interlocked.CompareExchange(ref updateRequired, 0, 1) != 0;
		}

		int updateRequired;
	};
};
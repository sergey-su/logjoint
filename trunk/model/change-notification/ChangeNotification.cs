using System;
using System.Threading;

namespace LogJoint
{
	public class ChangeNotification: IChangeNotification
	{
		private readonly ISynchronizationContext invoke;
		private int changePosted;

		public ChangeNotification(ISynchronizationContext invoke)
		{
			this.invoke = invoke;
		}

		void IChangeNotification.Post()
		{
			if (Interlocked.CompareExchange(ref changePosted, 1, 0) == 0)
			{
				invoke.Post(() =>
				{
					changePosted = 0;
					OnChange?.Invoke(this, EventArgs.Empty);
				});
			}
		}

		public event EventHandler OnChange;
	};
}

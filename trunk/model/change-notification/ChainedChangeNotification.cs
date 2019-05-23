using System;

namespace LogJoint
{
	class ChainedChangeNotification : IChainedChangeNotification, IChangeNotification, IDisposable
	{
		readonly IChangeNotification parentChangeNotification;
		readonly ISubscription subscription;

		public ChainedChangeNotification(IChangeNotification parentChangeNotification, bool initiallyActive)
		{
			this.parentChangeNotification = parentChangeNotification;
			this.subscription = parentChangeNotification.CreateSubscription(() => OnChange?.Invoke(this, EventArgs.Empty), initiallyActive);
		}

		bool IChainedChangeNotification.Active { get => subscription.Active; set => subscription.Active = value; }

		public event EventHandler OnChange;

		void IDisposable.Dispose()
		{
			this.subscription.Dispose();
		}

		void IChangeNotification.Post()
		{
			parentChangeNotification.Post();
		}

		ISubscription IChangeNotification.CreateSubscription(Action sideEffect, bool initiallyActive)
		{
			return new Subscription(this, sideEffect, initiallyActive);
		}

		IChainedChangeNotification IChangeNotification.CreateChainedChangeNotification(bool initiallyActive)
		{
			return new ChainedChangeNotification(this, initiallyActive);
		}
	};
}

using System;

namespace LogJoint
{
	class Subscription : ISubscription
	{
		private readonly IChangeNotification changeNotification;
		private bool disposed;
		private bool active;
		private readonly EventHandler changeHandler;

		public Subscription(IChangeNotification changeNotification, Action sideEffect, bool initiallyActive)
		{
			this.changeNotification = changeNotification;
			this.SideEffect = sideEffect;
			this.changeHandler = (sender, evt) => SideEffect?.Invoke();
			if (initiallyActive)
				Active = true;
		}

		public void Dispose()
		{
			if (!disposed)
			{
				Active = false;
				disposed = true;
			}
		}

		public bool Active
		{
			get
			{
				return active;
			}
			set
			{
				if (disposed)
					throw new ObjectDisposedException("Subscription");
				if (active == value)
					return;
				active = value;
				if (active)
				{
					changeNotification.OnChange += this.changeHandler;
					this.changeHandler(this, EventArgs.Empty);
				}
				else
				{
					changeNotification.OnChange -= this.changeHandler;
				}
			}
		}

		public Action SideEffect { get; set; }
	};
}

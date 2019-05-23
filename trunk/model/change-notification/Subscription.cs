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

		ISubscription IChangeNotification.CreateSubscription(Action sideEffect, bool initiallyActive)
		{
			return new Subscription(this, sideEffect, initiallyActive);
		}

		IChainedChangeNotification IChangeNotification.CreateChainedChangeNotification(bool initiallyActive)
		{
			return new ChainedChangeNotification(this, initiallyActive);
		}
	};

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

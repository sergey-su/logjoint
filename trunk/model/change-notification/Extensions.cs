using System;

namespace LogJoint
{
	public static class ChangeNotificationExtensions
	{
		public static ISubscription CreateSubscription(this IChangeNotification changeNotification, Action sideEffect, bool initiallyActive = true)
		{
			return new Subscription(changeNotification, sideEffect, initiallyActive);
		}

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

		public static IChainedChangeNotification CreateChainedChangeNotification(this IChangeNotification changeNotification, bool initiallyActive = true)
		{
			return new ChainedChangeNotification(changeNotification, initiallyActive);
		}

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
		};
	};
}

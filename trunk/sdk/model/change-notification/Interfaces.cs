using System;

namespace LogJoint
{
	public interface IChangeNotification
	{
		void Post();
		event EventHandler OnChange;
		bool IsEmittingEvents { get; }
		ISubscription CreateSubscription(Action sideEffect, bool initiallyActive = true);
		IChainedChangeNotification CreateChainedChangeNotification(bool initiallyActive = true);
	};

	public interface ISubscription : IDisposable
	{
		bool Active { get; set; }
		Action SideEffect { get; set; }
	};

	public interface IChainedChangeNotification: IChangeNotification, IDisposable
	{
		bool Active { get; set; }
	};
}

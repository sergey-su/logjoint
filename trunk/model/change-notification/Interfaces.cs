using System;

namespace LogJoint
{
	public interface IChangeNotification
	{
		void Post();
		event EventHandler OnChange;
	};

	public interface ISubscription : IDisposable
	{
		bool Active { get; set; }
		Action SideEffect { get; set; }
	};
}

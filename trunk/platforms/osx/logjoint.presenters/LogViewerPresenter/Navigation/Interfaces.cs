using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal interface INavigationManager
	{
		bool NavigationIsInProgress { get; }
		Task NavigateView(Func<CancellationToken, Task> navigate);

		event EventHandler NavigationIsInProgressChanged;
	};
};
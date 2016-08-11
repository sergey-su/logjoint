using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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

	internal class NavigationManager : INavigationManager
	{
		readonly LJTraceSource tracer;
		readonly Telemetry.ITelemetryCollector telemetry;

		Task currentNavigationTask;
		CancellationTokenSource currentNavigationTaskCancellation;
		int currentNavigationTaskId;

		public NavigationManager(
			LJTraceSource tracer,
			Telemetry.ITelemetryCollector telemetry
		)
		{
			this.tracer = tracer;
			this.telemetry = telemetry;
		}

		public event EventHandler NavigationIsInProgressChanged;

		bool INavigationManager.NavigationIsInProgress
		{
			get { return currentNavigationTask != null; }
		}

		Task INavigationManager.NavigateView(Func<CancellationToken, Task> navigate)
		{
			bool wasInProgress = false;
			if (currentNavigationTask != null)
			{
				wasInProgress = true;
				currentNavigationTaskCancellation.Cancel();
				currentNavigationTask = null;
			}
			var taskId = ++currentNavigationTaskId;
			currentNavigationTaskCancellation = new CancellationTokenSource();
			Func<Task> wrapper = async () => 
			{
				// todo: have perf op for navigation
				tracer.Info("nav begin {0} ", taskId);
				var cancellation = currentNavigationTaskCancellation.Token;
				try
				{
					await navigate(cancellation);
				}
				catch (OperationCanceledException)
				{
					throw; // fail navigation task with same exception. don't telemetrize it.
				}
				catch (Exception e)
				{
					telemetry.ReportException(e, "LogViewer navigation");
					throw;
				}
				finally
				{
					tracer.Info("nav end {0}{1}", taskId, cancellation.IsCancellationRequested ? " (cancelled)" : "");
					if (taskId == currentNavigationTaskId && currentNavigationTask != null)
					{
						currentNavigationTask = null;
						if (NavigationIsInProgressChanged != null)
							NavigationIsInProgressChanged(this, EventArgs.Empty);
					}
				}
			};
			var tmp = wrapper();
			if (!tmp.IsCompleted)
			{
				currentNavigationTask = tmp;
			}
			if (wasInProgress != (currentNavigationTask != null))
			if (NavigationIsInProgressChanged != null)
				NavigationIsInProgressChanged(this, EventArgs.Empty);
			return tmp;
		}
	};
};
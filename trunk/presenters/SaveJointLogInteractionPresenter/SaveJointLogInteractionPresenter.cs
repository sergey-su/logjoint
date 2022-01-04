using System;
using System.Threading;

namespace LogJoint.UI.Presenters.SaveJointLogInteractionPresenter
{
	public class Presenter : IPresenter
	{
		readonly ILogSourcesManager logSources;
		readonly IShutdown shutdown;
		readonly Progress.IProgressAggregatorFactory progressFactory;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly StatusReports.IPresenter statusReport;

		public Presenter(
			ILogSourcesManager logSources,
			IShutdown shutdown, 
			Progress.IProgressAggregatorFactory progressFactory,
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			StatusReports.IPresenter statusReport
		)
		{
			this.logSources = logSources;
			this.shutdown = shutdown;
			this.progressFactory = progressFactory;
			this.alerts = alerts;
			this.fileDialogs = fileDialogs;
			this.statusReport = statusReport;
		}

		bool IPresenter.IsInteractionInProgress
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		async void IPresenter.StartInteraction()
		{
			string filename = fileDialogs.SaveFileDialog(new SaveFileDialogParams()
			{
				SuggestedFileName = "joint-log.log"
			});
			if (filename == null)
				return;
			try
			{
				using var status = statusReport.CreateNewStatusReport();
				using var progress = progressFactory.CreateProgressAggregator();
				using var manualCancellation = new CancellationTokenSource();
				using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(manualCancellation.Token, shutdown.ShutdownToken);
				status.SetCancellationHandler(() => manualCancellation.Cancel());
				void setStatusText(int percentCompleted) =>
					status.ShowStatusText(string.Format("Saving joint log {0}%", percentCompleted), autoHide: false);
				setStatusText(0);
				progress.ProgressChanged += (s, e) => setStatusText(e.ProgressPercentage);
				await LogSourcesManagerExtensions.SaveJoinedLog(logSources, cancellation.Token, progress, filename);
			}
			catch (Exception e)
			{
				alerts.ShowPopup("Failed to save joint log", e.Message,
					AlertFlags.Ok | AlertFlags.WarningIcon);
			}
		}
	};
};
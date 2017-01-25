using System;

namespace LogJoint.UI.Presenters.IssueReportDialogPresenter
{
	public class Presenter : IPresenter
	{
		readonly IPromptDialog promptDialog;
		readonly Telemetry.ITelemetryCollector telemetryCollector;
		readonly Telemetry.ITelemetryUploader telemetryUploader;
	
		public Presenter(
			Telemetry.ITelemetryCollector telemetryCollector,
			Telemetry.ITelemetryUploader telemetryUploader,
			IPromptDialog promptDialog
		)
		{
			this.telemetryCollector = telemetryCollector;
			this.telemetryUploader = telemetryUploader;
			this.promptDialog = promptDialog;
		}

		void IPresenter.ShowDialog()
		{
			if (!telemetryUploader.IsIssuesReportingConfigured)
				return;
			var text = promptDialog.ExecuteDialog(
				"Report issue",
				"Enter problem's description and press OK to upload issue report." 
					+ Environment.NewLine + "Logs will be attached automatically",
				""
			);
			if (text == null)
				return;
			telemetryCollector.ReportIssue(text);
		}
	};
};
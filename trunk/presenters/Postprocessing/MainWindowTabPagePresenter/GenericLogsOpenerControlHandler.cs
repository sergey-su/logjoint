namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	class GenericLogsOpenerControlHandler : IViewControlHandler
	{
		readonly NewLogSourceDialog.IPresenter newLogSourceDialog;
		readonly ControlData controlData;

		public GenericLogsOpenerControlHandler(
			NewLogSourceDialog.IPresenter newLogSourceDialog
		)
		{
			this.newLogSourceDialog = newLogSourceDialog;
			this.controlData = new ControlData(false, "*1 Open* logs");
		}

		ControlData IViewControlHandler.GetCurrentData() => controlData;

		void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
		{
			if (newLogSourceDialog != null)
				newLogSourceDialog.ShowTheDialog(newLogSourceDialog.FormatDetectorPageName);
		}
	};
}

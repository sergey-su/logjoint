namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	class GenericLogsOpenerControlHandler : IViewControlHandler
	{
		readonly NewLogSourceDialog.IPresenter newLogSourceDialog;

		public GenericLogsOpenerControlHandler(
			NewLogSourceDialog.IPresenter newLogSourceDialog
		)
		{
			this.newLogSourceDialog = newLogSourceDialog;
		}

		ControlData IViewControlHandler.GetCurrentData()
		{
			return new ControlData()
			{
				Content = "*1 Open* logs"
			};
		}

		void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
		{
			if (newLogSourceDialog != null)
				newLogSourceDialog.ShowTheDialog(newLogSourceDialog.FotmatDetectorPageName);
		}
	};
}

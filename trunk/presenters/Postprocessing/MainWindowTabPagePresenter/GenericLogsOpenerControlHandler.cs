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
            this.controlData = new ControlData(disabled: IsBrowser.Value, content: "*1 Open* logs");
        }

        ControlData IViewControlHandler.GetCurrentData() => controlData;

        void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
        {
            if (!this.controlData.Disabled)
                newLogSourceDialog.ShowTheDialog(newLogSourceDialog.FormatDetectorPageName);
        }
    };
}

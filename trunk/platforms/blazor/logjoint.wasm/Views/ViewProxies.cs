using LogJoint.UI.Presenters;

namespace LogJoint.Wasm
{
    // todo: eventually make all views to reactive and get rid of view proxies
    public class ViewProxies : Factory.IViewsFactory, LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory
    {
        public UI.LogViewer.ViewProxy LoadedMessagesLogViewerViewProxy = new();
        public UI.LoadedMessages.ViewProxy LoadedMessagesViewProxy;
        public UI.SourcesListViewProxy SourcesListViewProxy = new();
        public UI.SourcesManagerViewProxy SourcesManagerViewProxy = new();
        public UI.Postprocessing.ViewProxy PostprocessingTabPage = new();
        public UI.Postprocesssing.StateInspector.ViewProxy PostprocesssingStateInspectorViewProxy = new();
        public UI.Postprocesssing.Timeline.ViewProxy PostprocesssingTimelineViewProxy = new();
        public UI.SearchPanelViewProxy SearchPanel = new();
        public UI.LogViewer.ViewProxy SearchResultLogViewer = new();
        public UI.SearchResultViewProxy SearchResult;
        public UI.HistoryDialogViewProxy HistoryDialog = new();
        public UI.PreprocessingUserInteractionsViewProxy PreprocessingUserInteractions = new();
        public UI.MessagePropertiesViewProxy MessageProperties = new();
        public UI.TimelineViewProxy Timeline = new();
        public UI.SourcePropertiesWindowViewProxy SourcePropertiesWindow = new();
        public UI.StatusReportViewProxy StatusReport = new();
        public UI.TimelinePanelViewProxy TimelinePanel = new();
        public UI.FilterDialogViewProxy FilterDialog = new();
        public UI.MainFormViewProxy MainForm = new();

        public ViewProxies()
        {
            this.LoadedMessagesViewProxy = new UI.LoadedMessages.ViewProxy(LoadedMessagesLogViewerViewProxy);
            this.SearchResult = new UI.SearchResultViewProxy(SearchResultLogViewer);
        }

        LogJoint.UI.Presenters.FormatsWizard.Factory.IViewsFactory Factory.IViewsFactory.FormatsWizardViewFactory => null;

        LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory Factory.IViewsFactory.PostprocessingViewsFactory => this;

        LogJoint.UI.Presenters.About.IView Factory.IViewsFactory.CreateAboutView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.IView Factory.IViewsFactory.CreateDebugOutputFormatView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.IView Factory.IViewsFactory.CreateFileBasedFormatView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.IView Factory.IViewsFactory.CreateFormatDetectionView() => null;

        LogJoint.UI.Presenters.HistoryDialog.IView Factory.IViewsFactory.CreateHistoryDialogView() => HistoryDialog;

        LogJoint.UI.Presenters.FilterDialog.IView Factory.IViewsFactory.CreateHlFilterDialogView() => FilterDialog;

        LogJoint.UI.Presenters.FiltersManager.IView Factory.IViewsFactory.CreateHlFiltersManagerView() => null;

        LogJoint.UI.Presenters.LoadedMessages.IView Factory.IViewsFactory.CreateLoadedMessagesView() => LoadedMessagesViewProxy;

        LogJoint.UI.Presenters.MainForm.IView Factory.IViewsFactory.CreateMainFormView() => MainForm;

        LogJoint.UI.Presenters.MessagePropertiesDialog.IView Factory.IViewsFactory.CreateMessagePropertiesDialogView() => MessageProperties;


        LogJoint.UI.Presenters.NewLogSourceDialog.IView Factory.IViewsFactory.CreateNewLogSourceDialogView() => null;

        LogJoint.UI.Presenters.Options.Dialog.IView Factory.IViewsFactory.CreateOptionsDialogView() => null;

        LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.IView Factory.IViewsFactory.CreatePostprocessingTabPage() => PostprocessingTabPage;

        LogJoint.UI.Presenters.PreprocessingUserInteractions.IView Factory.IViewsFactory.CreatePreprocessingView() => PreprocessingUserInteractions;

        LogJoint.UI.Presenters.SearchEditorDialog.IView Factory.IViewsFactory.CreateSearchEditorDialogView() => null;

        LogJoint.UI.Presenters.SearchesManagerDialog.IView Factory.IViewsFactory.CreateSearchesManagerDialogView() => null;

        LogJoint.UI.Presenters.FilterDialog.IView Factory.IViewsFactory.CreateSearchFilterDialogView(LogJoint.UI.Presenters.SearchEditorDialog.IDialogView parentView) => FilterDialog;

        LogJoint.UI.Presenters.SearchPanel.IView Factory.IViewsFactory.CreateSearchPanelView() => SearchPanel;

        LogJoint.UI.Presenters.SearchResult.IView Factory.IViewsFactory.CreateSearchResultView() => SearchResult;

        LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateSequenceDiagramView() => null;

        LogJoint.UI.Presenters.SharingDialog.IView Factory.IViewsFactory.CreateSharingDialogView() => null;

        LogJoint.UI.Presenters.SourcePropertiesWindow.IView Factory.IViewsFactory.CreateSourcePropertiesWindowView() => SourcePropertiesWindow;

        LogJoint.UI.Presenters.SourcesList.IView Factory.IViewsFactory.CreateSourcesListView() => SourcesListViewProxy;

        LogJoint.UI.Presenters.SourcesManager.IView Factory.IViewsFactory.CreateSourcesManagerView() => SourcesManagerViewProxy;

        LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateStateInspectorView() => PostprocesssingStateInspectorViewProxy;

        LogJoint.UI.Presenters.StatusReports.IView Factory.IViewsFactory.CreateStatusReportsView() => StatusReport;

        LogJoint.UI.Presenters.ThreadsList.IView Factory.IViewsFactory.CreateThreadsListView() => null;

        LogJoint.UI.Presenters.TimelinePanel.IView Factory.IViewsFactory.CreateTimelinePanelView() => TimelinePanel;

        LogJoint.UI.Presenters.Timeline.IView Factory.IViewsFactory.CreateTimelineView() => Timeline;

        LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateTimelineView() => PostprocesssingTimelineViewProxy;

        LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateTimeSeriesView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.IView Factory.IViewsFactory.CreateWindowsEventsLogFormatView() => null;
    }
}

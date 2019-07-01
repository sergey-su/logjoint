using System;
using AppKit;

namespace LogJoint.UI.Presenters
{
	public class ViewsFactory:
		Factory.IViewsFactory,
		FormatsWizard.Factory.IViewsFactory,
		Postprocessing.Factory.IViewsFactory,
		Mac.IReactive
	{
		readonly MainWindowAdapter mainWindow;
		readonly ModelObjects model;

		public ViewsFactory (
			MainWindowAdapter mainWindow,
			ModelObjects model
		)
		{
			this.mainWindow = mainWindow;
			this.model = model;
		}

		FormatsWizard.Factory.IViewsFactory Factory.IViewsFactory.FormatsWizardViewFactory => this;

		FormatsWizard.IView FormatsWizard.Factory.IViewsFactory.CreateFormatsWizardView() => new FormatsWizardDialogController ();
		FormatsWizard.ChooseOperationPage.IView FormatsWizard.Factory.IViewsFactory.CreateChooseOperationPageView() => new ChooseOperationPageController ();
		FormatsWizard.ImportLog4NetPage.IView FormatsWizard.Factory.IViewsFactory.CreateImportLog4NetPagePageView() => new ImportLog4NetPageController ();
		FormatsWizard.FormatIdentityPage.IView FormatsWizard.Factory.IViewsFactory.CreateFormatIdentityPageView() => new FormatIdentityPageController ();
		FormatsWizard.FormatAdditionalOptionsPage.IView FormatsWizard.Factory.IViewsFactory.CreateFormatAdditionalOptionsPage() => new FormatAdditionalOptionsPageController ();
		FormatsWizard.SaveFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateSaveFormatPageView() => new SaveFormatPageController ();
		FormatsWizard.NLogGenerationLogPage.IView FormatsWizard.Factory.IViewsFactory.CreateNLogGenerationLogPageView () => new NLogGenerationLogPageController ();
		FormatsWizard.ImportNLogPage.IView FormatsWizard.Factory.IViewsFactory.CreateImportNLogPage() => new ImportNLogPageController ();
		FormatsWizard.ChooseExistingFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateChooseExistingFormatPageView() => new ChooseExistingFormatPageController ();
		FormatsWizard.FormatDeleteConfirmPage.IView FormatsWizard.Factory.IViewsFactory.CreateFormatDeleteConfirmPageView() => new FormatDeletionConfirmationPageController ();
		FormatsWizard.RegexBasedFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateRegexBasedFormatPageView() => new RegexBasedFormatPageController ();
		FormatsWizard.EditSampleDialog.IView FormatsWizard.Factory.IViewsFactory.CreateEditSampleDialogView() => new EditSampleLogDialogController ();
		FormatsWizard.TestDialog.IView FormatsWizard.Factory.IViewsFactory.CreateTestDialogView() => new TestFormatDialogController ();
		FormatsWizard.EditRegexDialog.IView FormatsWizard.Factory.IViewsFactory.CreateEditRegexDialog() => new EditRegexDialogController ();
		FormatsWizard.EditFieldsMapping.IView FormatsWizard.Factory.IViewsFactory.CreateEditFieldsMappingDialog() => new FieldsMappingDialogController ();
		FormatsWizard.CustomTransformBasedFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateXmlBasedFormatPageView() => new XmlBasedFormatPageController ();
		FormatsWizard.CustomTransformBasedFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateJsonBasedFormatPageView() => new XmlBasedFormatPageController ();
		FormatsWizard.CustomCodeEditorDialog.IView FormatsWizard.Factory.IViewsFactory.CreateXsltEditorDialog() => new XsltEditorDialogController ();
		FormatsWizard.CustomCodeEditorDialog.IView FormatsWizard.Factory.IViewsFactory.CreateJUSTEditorDialog() => new XsltEditorDialogController ();

		Postprocessing.Factory.IViewsFactory Factory.IViewsFactory.PostprocessingViewsFactory => this;

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.StateInspectorVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateStateInspectorViewObjects ()
		{
			var wnd = new UI.Postprocessing.StateInspector.StateInspectorWindowController ();
			return (wnd, wnd);
		}

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.TimelineVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateTimelineViewObjects ()
		{
			var wnd = new UI.Postprocessing.TimelineVisualizer.TimelineWindowController ();
			return (wnd, wnd);
		}

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.SequenceDiagramVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateSequenceDiagramViewObjects ()
		{
			var wnd = new UI.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramWindowController ();
			return (wnd, wnd);
		}

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.TimeSeriesVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateTimeSeriesViewObjects ()
		{
			var wnd = new UI.Postprocessing.TimeSeriesVisualizer.TimeSeriesWindowController ();
			return (wnd, wnd);
		}

		About.IView Factory.IViewsFactory.CreateAboutView () => new AboutDialogAdapter ();

		BookmarksList.IView Factory.IViewsFactory.CreateBookmarksListView () => mainWindow.BookmarksManagementControlAdapter.ListView;

		BookmarksManager.IView Factory.IViewsFactory.CreateBookmarksManagerView () => mainWindow.BookmarksManagementControlAdapter;

		NewLogSourceDialog.Pages.FileBasedFormat.IView Factory.IViewsFactory.CreateFileBasedFormatView () => new FileBasedFormatPageController ();

		NewLogSourceDialog.Pages.FormatDetection.IView Factory.IViewsFactory.CreateFormatDetectionView () => new FormatDetectionPageController();

		FilterDialog.IView Factory.IViewsFactory.CreateSearchFilterDialogView(SearchEditorDialog.IDialogView parentView)
		{
			return new FilterDialogController ((AppKit.NSWindowController)parentView);
		}

		FilterDialog.IView Factory.IViewsFactory.CreateHlFilterDialogView () => new FilterDialogController (mainWindow);

		FiltersManager.IView Factory.IViewsFactory.CreateHlFiltersManagerView () => mainWindow.HighlightingFiltersManagerControlAdapter;

		LoadedMessages.IView Factory.IViewsFactory.CreateLoadedMessagesView () => mainWindow.LoadedMessagesControlAdapter;

		MainForm.IView Factory.IViewsFactory.CreateMainFormView () => mainWindow;

		MessagePropertiesDialog.IView Factory.IViewsFactory.CreateMessagePropertiesDialogView () => new MessagePropertiesDialogView ();

		NewLogSourceDialog.IView Factory.IViewsFactory.CreateNewLogSourceDialogView () => new NewLogSourceDialogView();

		Options.Dialog.IView Factory.IViewsFactory.CreateOptionsDialogView () => throw new NotImplementedException();

		Postprocessing.MainWindowTabPage.IView Factory.IViewsFactory.CreatePostprocessingTabPage (MainForm.IPresenter presenter)
		{
			return new UI.Postprocessing.MainWindowTabPage.MainWindowTabPageAdapter (presenter);
		}

		SearchEditorDialog.IView Factory.IViewsFactory.CreateSearchEditorDialogView () => new SearchEditorDialogView ();

		SearchesManagerDialog.IView Factory.IViewsFactory.CreateSearchesManagerDialogView () => new SearchesManagerDialogView ();

		SearchPanel.IView Factory.IViewsFactory.CreateSearchPanelView () => mainWindow.SearchPanelControlAdapter;

		SearchPanel.ISearchResultsPanelView Factory.IViewsFactory.CreateSearchResultsPanelView () => mainWindow;

		SearchResult.IView Factory.IViewsFactory.CreateSearchResultView () => mainWindow.SearchResultsControlAdapter;

		SharingDialog.IView Factory.IViewsFactory.CreateSharingDialogView () => new SharingDialogController ();

		HistoryDialog.IView Factory.IViewsFactory.CreateHistoryDialogView () => new HistoryDialogAdapter ();

		SourcePropertiesWindow.IView Factory.IViewsFactory.CreateSourcePropertiesWindowView () => new SourcePropertiesDialogView ();

		SourcesList.IView Factory.IViewsFactory.CreateSourcesListView () => mainWindow.SourcesManagementControlAdapter.SourcesListControlAdapter;

		SourcesManager.IView Factory.IViewsFactory.CreateSourcesManagerView () => mainWindow.SourcesManagementControlAdapter;

		StatusReports.IView Factory.IViewsFactory.CreateStatusReportsView () => mainWindow.StatusPopupControlAdapter;

		ThreadsList.IView Factory.IViewsFactory.CreateThreadsListView () => throw new NotImplementedException ();

		TimelinePanel.IView Factory.IViewsFactory.CreateTimelinePanelView () => mainWindow.TimelinePanelControlAdapter;

		Timeline.IView Factory.IViewsFactory.CreateTimelineView () => mainWindow.TimelinePanelControlAdapter.TimelineControlAdapter;

		PreprocessingUserInteractions.IView Factory.IViewsFactory.CreatePreprocessingView () => new LogsPreprocessorUI (model.SynchronizationContext, this);

		UI.Reactive.INSOutlineViewController Mac.IReactive.CreateOutlineViewController (NSOutlineView outlineView)
		{
			return new UI.Reactive.NSOutlineViewController (outlineView, model.TelemetryCollector);
		}

		UI.Reactive.INSTableViewController Mac.IReactive.CreateTableViewController (NSTableView tableView)
		{
			return new UI.Reactive.NSTableViewController (tableView, model.TelemetryCollector);
		}
	}
}

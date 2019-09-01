using LogJoint.UI.Windows;

namespace LogJoint.UI.Presenters
{
	class ViewsFactory :
		Factory.IViewsFactory,
		FormatsWizard.Factory.IViewsFactory,
		Postprocessing.Factory.IViewsFactory,
		Windows.Reactive.IReactive
	{
		readonly UI.MainForm mainForm;
		readonly IWinFormsComponentsInitializer winFormsComponentsInitializer;
		readonly ModelObjects model;

		public ViewsFactory(UI.MainForm mainForm, ModelObjects model)
		{
			this.mainForm = mainForm;
			this.winFormsComponentsInitializer = mainForm;
			this.model = model;
		}

		LoadedMessages.IView Factory.IViewsFactory.CreateLoadedMessagesView() => mainForm.loadedMessagesControl;
		StatusReports.IView Factory.IViewsFactory.CreateStatusReportsView() => new StatusReportView(
			mainForm,
			mainForm.toolStripStatusLabel,
			mainForm.cancelLongRunningProcessDropDownButton,
			mainForm.cancelLongRunningProcessLabel
		);
		Timeline.IView Factory.IViewsFactory.CreateTimelineView() => mainForm.timeLinePanel.TimelineControl;
		TimelinePanel.IView Factory.IViewsFactory.CreateTimelinePanelView() => mainForm.timeLinePanel;
		SearchResult.IView Factory.IViewsFactory.CreateSearchResultView() => mainForm.searchResultView;
		ThreadsList.IView Factory.IViewsFactory.CreateThreadsListView() => mainForm.threadsListView;
		SearchEditorDialog.IView Factory.IViewsFactory.CreateSearchEditorDialogView() => new SearchEditorDialogView();
		FilterDialog.IView Factory.IViewsFactory.CreateHlFilterDialogView() => new FilterDialogView();
		FilterDialog.IView Factory.IViewsFactory.CreateSearchFilterDialogView(SearchEditorDialog.IDialogView parentView) => new FilterDialogView();
		SearchesManagerDialog.IView Factory.IViewsFactory.CreateSearchesManagerDialogView() => new SearchesManagerDialogView();
		SearchPanel.IView Factory.IViewsFactory.CreateSearchPanelView() => mainForm.searchPanelView;
		SearchPanel.ISearchResultsPanelView Factory.IViewsFactory.CreateSearchResultsPanelView() => new SearchResultsPanelView() { container = mainForm.splitContainer_Log_SearchResults };
		SourcePropertiesWindow.IView Factory.IViewsFactory.CreateSourcePropertiesWindowView() => new SourceDetailsWindowView();
		SourcesList.IView Factory.IViewsFactory.CreateSourcesListView() => mainForm.sourcesListView.SourcesListView;
		SharingDialog.IView Factory.IViewsFactory.CreateSharingDialogView() => new ShareDialog();
		HistoryDialog.IView Factory.IViewsFactory.CreateHistoryDialogView() => new UI.HistoryDialog();
		NewLogSourceDialog.IView Factory.IViewsFactory.CreateNewLogSourceDialogView() => new UI.NewLogSourceDialogView();
		NewLogSourceDialog.Pages.FormatDetection.IView Factory.IViewsFactory.CreateFormatDetectionView() => new NewLogSourceDialog.Pages.FormatDetection.AnyLogFormatUI();
		NewLogSourceDialog.Pages.FileBasedFormat.IView Factory.IViewsFactory.CreateFileBasedFormatView() => new NewLogSourceDialog.Pages.FileBasedFormat.FileLogFactoryUI();
#if WIN
		NewLogSourceDialog.Pages.DebugOutput.IView Factory.IViewsFactory.CreateDebugOutputFormatView() => new NewLogSourceDialog.Pages.DebugOutput.DebugOutputFactoryUI();
		NewLogSourceDialog.Pages.WindowsEventsLog.IView Factory.IViewsFactory.CreateWindowsEventsLogFormatView() => new NewLogSourceDialog.Pages.WindowsEventsLog.EVTFactoryUI();
#endif
		FormatsWizard.Factory.IViewsFactory Factory.IViewsFactory.FormatsWizardViewFactory => this;
		FormatsWizard.IView FormatsWizard.Factory.IViewsFactory.CreateFormatsWizardView() => new ManageFormatsWizard();
		FormatsWizard.ChooseOperationPage.IView FormatsWizard.Factory.IViewsFactory.CreateChooseOperationPageView() => new ChooseOperationPage();
		FormatsWizard.ImportLog4NetPage.IView FormatsWizard.Factory.IViewsFactory.CreateImportLog4NetPagePageView() => new ImportLog4NetPage();
		FormatsWizard.FormatIdentityPage.IView FormatsWizard.Factory.IViewsFactory.CreateFormatIdentityPageView() => new FormatIdentityPage();
		FormatsWizard.FormatAdditionalOptionsPage.IView FormatsWizard.Factory.IViewsFactory.CreateFormatAdditionalOptionsPage() => new FormatAdditionalOptionsPage();
		FormatsWizard.SaveFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateSaveFormatPageView() => new SaveFormatPage();
		FormatsWizard.ImportNLogPage.IView FormatsWizard.Factory.IViewsFactory.CreateImportNLogPage() => new ImportNLogPage();
		FormatsWizard.NLogGenerationLogPage.IView FormatsWizard.Factory.IViewsFactory.CreateNLogGenerationLogPageView() => new NLogGenerationLogPage();
		FormatsWizard.ChooseExistingFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateChooseExistingFormatPageView() => new ChooseExistingFormatPage();
		FormatsWizard.FormatDeleteConfirmPage.IView FormatsWizard.Factory.IViewsFactory.CreateFormatDeleteConfirmPageView() => new FormatDeleteConfirmPage();
		FormatsWizard.RegexBasedFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateRegexBasedFormatPageView() => new RegexBasedFormatPage();
		FormatsWizard.EditSampleDialog.IView FormatsWizard.Factory.IViewsFactory.CreateEditSampleDialogView() => new EditSampleLogForm();
		FormatsWizard.TestDialog.IView FormatsWizard.Factory.IViewsFactory.CreateTestDialogView() => new TestParserForm();
		FormatsWizard.EditRegexDialog.IView FormatsWizard.Factory.IViewsFactory.CreateEditRegexDialog() => new EditRegexForm();
		FormatsWizard.EditFieldsMapping.IView FormatsWizard.Factory.IViewsFactory.CreateEditFieldsMappingDialog() => new FieldsMappingForm();
		FormatsWizard.CustomTransformBasedFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateXmlBasedFormatPageView() => new XmlBasedFormatPage();
		FormatsWizard.CustomTransformBasedFormatPage.IView FormatsWizard.Factory.IViewsFactory.CreateJsonBasedFormatPageView() => new XmlBasedFormatPage();
		FormatsWizard.CustomCodeEditorDialog.IView FormatsWizard.Factory.IViewsFactory.CreateXsltEditorDialog() => new EditXsltForm();
		FormatsWizard.CustomCodeEditorDialog.IView FormatsWizard.Factory.IViewsFactory.CreateJUSTEditorDialog() => new EditXsltForm();
		SourcesManager.IView Factory.IViewsFactory.CreateSourcesManagerView() => mainForm.sourcesListView;
		MessagePropertiesDialog.IView Factory.IViewsFactory.CreateMessagePropertiesDialogView() => new MessagePropertiesDialogView(mainForm);
		FiltersManager.IView Factory.IViewsFactory.CreateHlFiltersManagerView() => mainForm.hlFiltersManagementView;
		BookmarksList.IView Factory.IViewsFactory.CreateBookmarksListView() => mainForm.bookmarksManagerView.ListView;
		BookmarksManager.IView Factory.IViewsFactory.CreateBookmarksManagerView() => mainForm.bookmarksManagerView;
		Options.Dialog.IView Factory.IViewsFactory.CreateOptionsDialogView() => new OptionsDialogView(this);
		About.IView Factory.IViewsFactory.CreateAboutView() => new AboutBox();
		MainForm.IView Factory.IViewsFactory.CreateMainFormView() => mainForm;
		Postprocessing.MainWindowTabPage.IView Factory.IViewsFactory.CreatePostprocessingTabPage() => new UI.Postprocessing.MainWindowTabPage.TabPage();
		Postprocessing.Factory.IViewsFactory Factory.IViewsFactory.PostprocessingViewsFactory => this;
		PreprocessingUserInteractions.IView Factory.IViewsFactory.CreatePreprocessingView() => new LogsPreprocessorUI(mainForm, model.SynchronizationContext, this);

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.StateInspectorVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateStateInspectorViewObjects()
		{
			var impl = new UI.Postprocessing.StateInspector.StateInspectorForm(this);
			winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
			return (impl, impl);
		}

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.TimelineVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateTimelineViewObjects()
		{
			var impl = new UI.Postprocessing.TimelineVisualizer.TimelineForm();
			winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
			return (impl, impl.TimelineVisualizerView);
		}

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.SequenceDiagramVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateSequenceDiagramViewObjects()
		{
			var impl = new UI.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramForm();
			winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
			return (impl, impl.SequenceDiagramVisualizerView);
		}

		(Postprocessing.IPostprocessorOutputForm, Postprocessing.TimeSeriesVisualizer.IView) Postprocessing.Factory.IViewsFactory.CreateTimeSeriesViewObjects()
		{
			var impl = new UI.Postprocessing.TimeSeriesVisualizer.TimeSeriesForm();
			winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
			return (impl, impl.TimeSeriesVisualizerView);
		}

		Windows.Reactive.ITreeViewController Windows.Reactive.IReactive.CreateTreeViewController(MultiselectTreeView treeView)
		{
			return new Windows.Reactive.TreeViewController(treeView);
		}

		Windows.Reactive.IListBoxController Windows.Reactive.IReactive.CreateListBoxController(System.Windows.Forms.ListBox listBox)
		{
			return new Windows.Reactive.ListBoxController(listBox);
		}
	};
}

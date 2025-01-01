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

        ThreadsList.IView Factory.IViewsFactory.CreateThreadsListView() => mainForm.threadsListView;
        SourcePropertiesWindow.IView Factory.IViewsFactory.CreateSourcePropertiesWindowView() => new SourceDetailsWindowView();
        SharingDialog.IView Factory.IViewsFactory.CreateSharingDialogView() => new ShareDialog();
        NewLogSourceDialog.IView Factory.IViewsFactory.CreateNewLogSourceDialogView() => new UI.NewLogSourceDialogView();
        NewLogSourceDialog.Pages.FormatDetection.IView Factory.IViewsFactory.CreateFormatDetectionView() => new NewLogSourceDialog.Pages.FormatDetection.AnyLogFormatUI();
        NewLogSourceDialog.Pages.FileBasedFormat.IView Factory.IViewsFactory.CreateFileBasedFormatView() => new NewLogSourceDialog.Pages.FileBasedFormat.FileLogFactoryUI();
        NewLogSourceDialog.Pages.DebugOutput.IView Factory.IViewsFactory.CreateDebugOutputFormatView() => new NewLogSourceDialog.Pages.DebugOutput.DebugOutputFactoryUI();
        NewLogSourceDialog.Pages.WindowsEventsLog.IView Factory.IViewsFactory.CreateWindowsEventsLogFormatView() => new NewLogSourceDialog.Pages.WindowsEventsLog.EVTFactoryUI();
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
        MessagePropertiesDialog.IView Factory.IViewsFactory.CreateMessagePropertiesDialogView() => new MessagePropertiesDialogView(mainForm);
        Options.Dialog.IView Factory.IViewsFactory.CreateOptionsDialogView() => new OptionsDialogView(this);
        About.IView Factory.IViewsFactory.CreateAboutView() => new AboutBox();
        MainForm.IView Factory.IViewsFactory.CreateMainFormView() => mainForm;
        Postprocessing.Factory.IViewsFactory Factory.IViewsFactory.PostprocessingViewsFactory => this;
        PreprocessingUserInteractions.IView Factory.IViewsFactory.CreatePreprocessingView() => new LogsPreprocessorUI(mainForm, model.SynchronizationContext, this);

        Postprocessing.StateInspectorVisualizer.IView Postprocessing.Factory.IViewsFactory.CreateStateInspectorView()
        {
            var impl = new UI.Postprocessing.StateInspector.StateInspectorForm(this);
            winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
            return impl;
        }

        Postprocessing.TimelineVisualizer.IView Postprocessing.Factory.IViewsFactory.CreateTimelineView()
        {
            var impl = new UI.Postprocessing.TimelineVisualizer.TimelineForm();
            winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
            return impl.TimelineVisualizerView;
        }

        Postprocessing.SequenceDiagramVisualizer.IView Postprocessing.Factory.IViewsFactory.CreateSequenceDiagramView()
        {
            var impl = new UI.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramForm();
            winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
            return impl.SequenceDiagramVisualizerView;
        }

        Postprocessing.TimeSeriesVisualizer.IView Postprocessing.Factory.IViewsFactory.CreateTimeSeriesView()
        {
            var impl = new UI.Postprocessing.TimeSeriesVisualizer.TimeSeriesForm();
            winFormsComponentsInitializer.InitOwnedForm(impl, takeOwnership: false);
            return impl.TimeSeriesVisualizerView;
        }

        Windows.Reactive.ITreeViewController<Node> Windows.Reactive.IReactive.CreateTreeViewController<Node>(MultiselectTreeView treeView)
        {
            return new Windows.Reactive.TreeViewController<Node>(treeView);
        }

        Windows.Reactive.IListBoxController<Item> Windows.Reactive.IReactive.CreateListBoxController<Item>(System.Windows.Forms.ListBox listBox)
        {
            return new Windows.Reactive.ListBoxController<Item>(listBox);
        }
    };
}

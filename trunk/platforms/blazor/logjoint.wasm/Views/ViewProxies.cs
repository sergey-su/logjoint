using LogJoint.UI.Presenters;

namespace LogJoint.Wasm
{
    // todo: eventually make all views to reactive and get rid of view proxies
    public class ViewProxies : Factory.IViewsFactory, LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory
    {
        public UI.Postprocesssing.StateInspector.ViewProxy PostprocesssingStateInspectorViewProxy = new();
        public UI.Postprocesssing.Timeline.ViewProxy PostprocesssingTimelineViewProxy = new();
        public UI.PreprocessingUserInteractionsViewProxy PreprocessingUserInteractions = new();
        public UI.MessagePropertiesViewProxy MessageProperties = new();
        public UI.SourcePropertiesWindowViewProxy SourcePropertiesWindow = new();
        public UI.MainFormViewProxy MainForm = new();

        LogJoint.UI.Presenters.FormatsWizard.Factory.IViewsFactory Factory.IViewsFactory.FormatsWizardViewFactory => null;

        LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory Factory.IViewsFactory.PostprocessingViewsFactory => this;

        LogJoint.UI.Presenters.About.IView Factory.IViewsFactory.CreateAboutView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.IView Factory.IViewsFactory.CreateDebugOutputFormatView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.IView Factory.IViewsFactory.CreateFileBasedFormatView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.IView Factory.IViewsFactory.CreateFormatDetectionView() => null;

        LogJoint.UI.Presenters.MainForm.IView Factory.IViewsFactory.CreateMainFormView() => MainForm;

        LogJoint.UI.Presenters.MessagePropertiesDialog.IView Factory.IViewsFactory.CreateMessagePropertiesDialogView() => MessageProperties;


        LogJoint.UI.Presenters.NewLogSourceDialog.IView Factory.IViewsFactory.CreateNewLogSourceDialogView() => null;

        LogJoint.UI.Presenters.Options.Dialog.IView Factory.IViewsFactory.CreateOptionsDialogView() => null;

        LogJoint.UI.Presenters.PreprocessingUserInteractions.IView Factory.IViewsFactory.CreatePreprocessingView() => PreprocessingUserInteractions;


        LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateSequenceDiagramView() => null;

        LogJoint.UI.Presenters.SharingDialog.IView Factory.IViewsFactory.CreateSharingDialogView() => null;

        LogJoint.UI.Presenters.SourcePropertiesWindow.IView Factory.IViewsFactory.CreateSourcePropertiesWindowView() => SourcePropertiesWindow;

        LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateStateInspectorView() => PostprocesssingStateInspectorViewProxy;

        LogJoint.UI.Presenters.ThreadsList.IView Factory.IViewsFactory.CreateThreadsListView() => null;

        LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateTimelineView() => PostprocesssingTimelineViewProxy;

        LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateTimeSeriesView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.IView Factory.IViewsFactory.CreateWindowsEventsLogFormatView() => null;
    }
}

using LogJoint.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace LogJoint
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(WireupDependenciesAndCreateMainForm());
		}

		static Form WireupDependenciesAndCreateMainForm()
		{
			var tracer = new LJTraceSource("App", "app");

			using (tracer.NewFrame)
			{
				var mainForm = new UI.MainForm();
				tracer.Info("main form created");
				Properties.WebContentConfig webContentConfig = new Properties.WebContentConfig();
				ISynchronizationContext modelSynchronizationContext = new WinFormsSynchronizationContext(mainForm);

				var model = ModelFactory.Create(
					tracer,
					new ModelConfig
					{
						WorkspacesUrl = Properties.Settings.Default.WorkspacesUrl,
						TelemetryUrl = Properties.Settings.Default.TelemetryUrl,
						IssuesUrl = Properties.Settings.Default.IssuesUrl,
						AutoUpdateUrl = Properties.Settings.Default.AutoUpdateUrl,
						WebContentCacheConfig = webContentConfig,
						LogsDownloaderConfig = webContentConfig
					},
					modelSynchronizationContext,
					new ColorLease(16), // todo: dynamic lease
					(storageManager) => new UI.LogsPreprocessorCredentialsCache(
						modelSynchronizationContext,
						storageManager.GlobalSettingsEntry,
						mainForm
					),
					(shutdown, webContentCache) => new UI.Presenters.WebBrowserDownloader.Presenter(
						new UI.WebBrowserDownloader.WebBrowserDownloaderForm(),
						modelSynchronizationContext,
						webContentCache,
						shutdown
					)
				);

				var pluggableProtocolManager = new PluggableProtocolManager(
					model.instancesCounter,
					model.shutdown,
					model.telemetryCollector,
					model.firstStartDetector,
					model.launchUrlParser
				);

				Telemetry.WinFormsUnhandledExceptionsReporter.Setup(model.telemetryCollector);

				var postprocessingViewsFactory = new UI.Postprocessing.PostprocessorOutputFormFactory();

				var presentation = UI.Presenters.Factory.Create(
					tracer,
					model,
					new ClipboardAccess(model.telemetryCollector),
					new ShellOpen(),
					new Alerts(),
					new Alerts(),
					new UI.PromptDialog.Presenter(),


					mainForm.loadedMessagesControl,
					new UI.StatusReportView(
						mainForm,
						mainForm.toolStripStatusLabel,
						mainForm.cancelLongRunningProcessDropDownButton,
						mainForm.cancelLongRunningProcessLabel
					),
					mainForm.timeLinePanel.TimelineControl,
					mainForm.timeLinePanel,
					mainForm.searchResultView,
					mainForm.threadsListView,
					new SearchEditorDialogView(),
					() => new UI.FilterDialogView(),
					new UI.SearchesManagerDialogView(),
					mainForm.searchPanelView,
					new UI.SearchResultsPanelView() { container = mainForm.splitContainer_Log_SearchResults },
					new UI.SourceDetailsWindowView(),
					mainForm.sourcesListView.SourcesListView,
					new UI.ShareDialog(),
					new UI.HistoryDialog(),
					new UI.NewLogSourceDialogView(),
					() => new UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.AnyLogFormatUI(),
					() => new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.FileLogFactoryUI(),
					() => new UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.DebugOutputFactoryUI(),
					() => new UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.EVTFactoryUI(),
					new UI.Presenters.FormatsWizard.ObjectsFactory.ViewFactories()
					{
						CreateFormatsWizardView = () => new ManageFormatsWizard(),
						CreateChooseOperationPageView = () => new ChooseOperationPage(),
						CreateImportLog4NetPagePageView = () => new ImportLog4NetPage(),
						CreateFormatIdentityPageView = () => new FormatIdentityPage(),
						CreateFormatAdditionalOptionsPage = () => new FormatAdditionalOptionsPage(),
						CreateSaveFormatPageView = () => new SaveFormatPage(),
						CreateImportNLogPage = () => new ImportNLogPage(),
						CreateNLogGenerationLogPageView = () => new NLogGenerationLogPage(),
						CreateChooseExistingFormatPageView = () => new ChooseExistingFormatPage(),
						CreateFormatDeleteConfirmPageView = () => new FormatDeleteConfirmPage(),
						CreateRegexBasedFormatPageView = () => new RegexBasedFormatPage(),
						CreateEditSampleDialogView = () => new EditSampleLogForm(),
						CreateTestDialogView = () => new TestParserForm(),
						CreateEditRegexDialog = () => new EditRegexForm(),
						CreateEditFieldsMappingDialog = () => new FieldsMappingForm(),
						CreateXmlBasedFormatPageView = () => new XmlBasedFormatPage(),
						CreateJsonBasedFormatPageView = () => new XmlBasedFormatPage(),
						CreateXsltEditorDialog = () => new EditXsltForm(),
						CreateJUSTEditorDialog = () => new EditXsltForm(),
					},
					mainForm.sourcesListView,
					new MessagePropertiesDialogView(mainForm, model.changeNotification),
					mainForm.hlFiltersManagementView,
					mainForm.bookmarksManagerView.ListView,
					mainForm.bookmarksManagerView,
					new OptionsDialogView(),
					new AboutBox(),
					new AboutDialogConfig(),
					mainForm,
					new DragDropHandler(
						model.logSourcesController,
						model.logSourcesPreprocessings,
						model.preprocessingStepsFactory
					),
					mainFormPresenter => new UI.Postprocessing.MainWindowTabPage.TabPage(mainFormPresenter),
					postprocessingViewsFactory
				);

				UI.LogsPreprocessorUI logsPreprocessorUI = new UI.LogsPreprocessorUI(
					model.logSourcesPreprocessings,
					mainForm,
					presentation.statusReportsPresenter);


				var pluginEntryPoint = new Extensibility.Application(
					model.expensibilityEntryPoint,
					presentation.expensibilityEntryPoint,
					new Extensibility.View(
						mainForm
					)
				);

				var pluginsManager = new Extensibility.PluginsManager(
					pluginEntryPoint,
					model.telemetryCollector,
					model.shutdown,
					model.expensibilityEntryPoint
				);
				tracer.Info("plugin manager created");

				AppInitializer.WireUpCommandLineHandler(presentation.mainFormPresenter, model.commandLineHandler);

				postprocessingViewsFactory.Init(
					pluginEntryPoint,
					model.logSourceNamesProvider,
					model.analyticsShortNames,
					presentation.sourcesManagerPresenter,
					presentation.loadedMessagesPresenter,
					presentation.clipboardAccess,
					presentation.presentersFacade,
					presentation.alertPopup
				);

				return mainForm;
			}
		}
	}
}
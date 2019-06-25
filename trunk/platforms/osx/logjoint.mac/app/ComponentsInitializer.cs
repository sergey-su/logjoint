using System;

namespace LogJoint.UI
{
	public static class ComponentsInitializer
	{
		public static void WireupDependenciesAndInitMainWindow(MainWindowAdapter mainWindow)
		{
			var tracer = new LJTraceSource("App", "app");
			tracer.Info("starting app");


			using (tracer.NewFrame)
			{
				ISynchronizationContext invokingSynchronization = new NSInvokeSynchronization ();
				Properties.WebContentConfig webContentConfig = new Properties.WebContentConfig ();

				var model = ModelFactory.Create (
					tracer,
					new ModelConfig {
						WorkspacesUrl = Properties.Settings.Default.WorkspacesUrl,
						TelemetryUrl = Properties.Settings.Default.TelemetryUrl,
						IssuesUrl = Properties.Settings.Default.IssuesUrl,
						AutoUpdateUrl = Properties.Settings.Default.AutoUpdateUrl,
						WebContentCacheConfig = webContentConfig,
						LogsDownloaderConfig = webContentConfig
					},
					invokingSynchronization,
					(storageManager) => new PreprocessingCredentialsCache (
						mainWindow.Window,
						storageManager.GlobalSettingsEntry,
						invokingSynchronization
					),
					(shutdown, webContentCache) => new Presenters.WebBrowserDownloader.Presenter (
						new WebBrowserDownloaderWindowController (),
						invokingSynchronization,
						webContentCache,
						shutdown
					)
				);

				var presentation = Presenters.Factory.Create (
					tracer,
					model,
					new ClipboardAccess (),
					new ShellOpen (),
					new AlertPopup (),
					new FileDialogs (),
					new PromptDialogController (),
					new AboutDialogConfig (),
					new DragDropHandler (
						model.LogSourcesPreprocessings,
						model.PreprocessingStepsFactory,
						model.LogSourcesController
					),
					mainWindow,
					new Presenters.ViewsFactory(mainWindow)
				);


				new UI.LogsPreprocessorUI(
					model.LogSourcesPreprocessings,
					presentation.StatusReportsPresenter
				);

				mainWindow.InstancesCounter = model.InstancesCounter;

				tracer.Info("main form presenter created");

				CustomURLSchemaEventsHandler.Instance.Init(
					presentation.MainFormPresenter,
					model.CommandLineHandler,
					invokingSynchronization
				);
				// todo: consider not depending on mono in the system.
				// It's required 2 times:
				//  1. Formats' user code compilation
				//  2. Start of updater tool during auto-update
				var monoChecker = new MonoChecker (
					presentation.MainFormPresenter,
					presentation.AlertPopup,
					model.TelemetryCollector,
					presentation.ShellOpen
				);

				model.PluginsManager.LoadPlugins (new Application (
					model.ExpensibilityEntryPoint,
					presentation.ExpensibilityEntryPoint,
					model.TelemetryCollector
				));
			}
		}
	}
}


using System;

namespace LogJoint.UI
{
	public static class ComponentsInitializer
	{
		public static void WireupDependenciesAndInitMainWindow(MainWindowAdapter mainWindow)
		{
			ISynchronizationContext invokingSynchronization = new NSInvokeSynchronization ();
			Properties.WebContentConfig webContentConfig = new Properties.WebContentConfig ();

			var model = ModelFactory.Create (
				new ModelConfig {
					WorkspacesUrl = Properties.Settings.Default.WorkspacesUrl,
					TelemetryUrl = Properties.Settings.Default.TelemetryUrl,
					IssuesUrl = Properties.Settings.Default.IssuesUrl,
					AutoUpdateUrl = Properties.Settings.Default.AutoUpdateUrl,
					PluginsUrl = Properties.Settings.Default.PluginsUrl,
					WebContentCacheConfig = webContentConfig,
					LogsDownloaderConfig = webContentConfig,
					TraceListeners = Properties.Settings.Default.TraceListenerConfig != null ?
						new [] { new TraceListener(Properties.Settings.Default.TraceListenerConfig) }
						: null
				},
				invokingSynchronization,
				(storageManager) => new PreprocessingCredentialsCache (
					mainWindow.Window,
					storageManager.GlobalSettingsEntry,
					invokingSynchronization
				),
				(shutdown, webContentCache, traceSourceFactory) => new Presenters.WebViewTools.Presenter (
					new WebBrowserDownloaderWindowController (),
					invokingSynchronization,
					webContentCache,
					shutdown,
					traceSourceFactory
				),
				new Drawing.Matrix.Factory()
			);

			var viewsFactory = new Presenters.ViewsFactory (mainWindow, model);
			mainWindow.Init (model.InstancesCounter);
			mainWindow.SourcesManagementControlAdapter.SourcesListControlAdapter.Init (viewsFactory);
			var presentation = Presenters.Factory.Create (
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
					model.LogSourcesManager
				),
				mainWindow,
				viewsFactory
			);

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
				viewsFactory
			), Properties.Settings.Default.LocalPlugins);

			foreach (var asm in model.PluginsManager.PluginAssemblies)
				ObjCRuntime.Runtime.RegisterAssembly(asm);
		}
	}
}


using LogJoint.UI;
using System;
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

				var viewsFactory = new UI.Presenters.ViewsFactory(mainForm, model);

				var presentation = UI.Presenters.Factory.Create(
					tracer,
					model,
					new ClipboardAccess(model.TelemetryCollector),
					new ShellOpen(),
					new Alerts(),
					new Alerts(),
					new UI.PromptDialog.Presenter(),
					new AboutDialogConfig(),
					new DragDropHandler(
						model.LogSourcesController,
						model.LogSourcesPreprocessings,
						model.PreprocessingStepsFactory
					),
					new UI.Presenters.StaticSystemThemeDetector(UI.Presenters.ColorThemeMode.Light),
					viewsFactory
				);

				var pluginEntryPoint = new Extensibility.Application(
					model.ExpensibilityEntryPoint,
					presentation.ExpensibilityEntryPoint,
					new Extensibility.View(
						mainForm,
						viewsFactory
					)
				);

				model.PluginsManager.LoadPlugins(pluginEntryPoint);

				new PluggableProtocolManager(
					model.InstancesCounter,
					model.Shutdown,
					model.TelemetryCollector,
					model.FirstStartDetector,
					model.LaunchUrlParser
				);

				Telemetry.WinFormsUnhandledExceptionsReporter.Setup(model.TelemetryCollector);

				AppInitializer.WireUpCommandLineHandler(presentation.MainFormPresenter, model.CommandLineHandler);

				return mainForm;
			}
		}
	}
}
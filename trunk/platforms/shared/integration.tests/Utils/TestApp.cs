using NSubstitute;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
	public class Mocks
	{
		public Preprocessing.ICredentialsCache CredentialsCache;
		public WebBrowserDownloader.IDownloader WebBrowserDownloader;
		public Persistence.IWebContentCacheConfig WebContentCacheConfig;
		public Preprocessing.ILogsDownloaderConfig LogsDownloaderConfig;

		public UI.Presenters.IClipboardAccess ClipboardAccess;
		public UI.Presenters.IShellOpen ShellOpen;
		public UI.Presenters.IAlertPopup AlertPopup;
		public UI.Presenters.IFileDialogs FileDialogs;
		public UI.Presenters.IPromptDialog PromptDialog;
		public UI.Presenters.About.IAboutConfig AboutConfig;
		public UI.Presenters.MainForm.IDragDropHandler DragDropHandler;
		public UI.Presenters.ISystemThemeDetector SystemThemeDetector;

		public UI.Presenters.Factory.IViewsFactory Views;
	};

	public class ViewModelObjects
	{
		public UI.Presenters.LogViewer.IViewModel LoadedMessagesLogViewer;
		public UI.Presenters.MainForm.IViewEvents MainForm;
		public UI.Presenters.PreprocessingUserInteractions.IViewModel PreprocessingUserInteractions;
		public UI.Presenters.Postprocessing.MainWindowTabPage.IViewModel PostprocessingTabPage;
		public string PostprocessingTabPageId;
	};

	public class TestAppConfig
	{
		public int LogViewerViewSize = 20;
	};

	public class TestAppInstance
	{
		private bool disposed;
		private TraceListener traceListener;

		public ISynchronizationContext SynchronizationContext { get; private set; }
		public ModelObjects Model { get; private set; }
		public UI.Presenters.PresentationObjects Presentation { get; private set; }
		public ViewModelObjects ViewModel { get; private set; }
		public Mocks Mocks { get; private set; }

		public static async Task<TestAppInstance> Create(TestAppConfig config = null)
		{
			config = config ?? new TestAppConfig();

			var mocks = new Mocks
			{
				CredentialsCache = Substitute.For<Preprocessing.ICredentialsCache>(),
				WebBrowserDownloader = Substitute.For<WebBrowserDownloader.IDownloader>(),
				WebContentCacheConfig = Substitute.For<Persistence.IWebContentCacheConfig>(),
				LogsDownloaderConfig = Substitute.For<Preprocessing.ILogsDownloaderConfig>(),

				ClipboardAccess = Substitute.For<UI.Presenters.IClipboardAccess>(),
				ShellOpen = Substitute.For<UI.Presenters.IShellOpen>(),
				AlertPopup = Substitute.For<UI.Presenters.IAlertPopup>(),
				FileDialogs = Substitute.For<UI.Presenters.IFileDialogs>(),
				PromptDialog = Substitute.For<UI.Presenters.IPromptDialog>(),
				AboutConfig = Substitute.For<UI.Presenters.About.IAboutConfig>(),
				DragDropHandler = Substitute.For<UI.Presenters.MainForm.IDragDropHandler>(),
				SystemThemeDetector = Substitute.For<UI.Presenters.ISystemThemeDetector>(),
				Views = Substitute.For<UI.Presenters.Factory.IViewsFactory>(),
			};

			var viewModel = new ViewModelObjects();

			InitializeMocks(config, mocks, viewModel);

			var appDataDir = Path.Combine(Path.GetTempPath(),
				$"logjoint.int.test.workdir.{DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss'.'fff")}");

			Directory.CreateDirectory(appDataDir);
			var traceListener = new TraceListener(Path.Combine(appDataDir, "test-debug.log"));
			LJTraceSource.SetTestListeners(new[] { traceListener });

			ISynchronizationContext serialSynchronizationContext = new SerialSynchronizationContext();

			var (model, presentation) = await serialSynchronizationContext.Invoke(() =>
			{
				var tracer = new LJTraceSource("app", "test");

				var modelObjects = ModelFactory.Create(
					tracer,
					new ModelConfig
					{
						WorkspacesUrl = "",
						TelemetryUrl = "",
						IssuesUrl = "",
						AutoUpdateUrl = "",
						WebContentCacheConfig = mocks.WebContentCacheConfig,
						LogsDownloaderConfig = mocks.LogsDownloaderConfig,
						AppDataDirectory = appDataDir
					},
					serialSynchronizationContext,
					(_1) => mocks.CredentialsCache,
					(_1, _2) => mocks.WebBrowserDownloader
				);

				var presentationObjects = UI.Presenters.Factory.Create(
					tracer,
					modelObjects,
					mocks.ClipboardAccess,
					mocks.ShellOpen,
					mocks.AlertPopup,
					mocks.FileDialogs,
					mocks.PromptDialog,
					mocks.AboutConfig,
					mocks.DragDropHandler,
					mocks.SystemThemeDetector,
					mocks.Views
				);

				return (modelObjects, presentationObjects);
			});

			return new TestAppInstance
			{
				SynchronizationContext = serialSynchronizationContext,
				Mocks = mocks,
				Model = model,
				Presentation = presentation,
				ViewModel = viewModel,
				traceListener = traceListener
			};
		}

		private static void InitializeMocks(TestAppConfig config, Mocks mocks, ViewModelObjects viewModel)
		{
			mocks.Views.CreateLoadedMessagesView().MessagesView.SetViewModel(
				Arg.Do<UI.Presenters.LogViewer.IViewModel>(x => viewModel.LoadedMessagesLogViewer = x));
			mocks.Views.CreateLoadedMessagesView().MessagesView.DisplayLinesPerPage.Returns(config.LogViewerViewSize);

			mocks.Views.CreateMainFormView().SetPresenter(
				Arg.Do<UI.Presenters.MainForm.IViewEvents>(x => viewModel.MainForm = x));
			mocks.Views.CreateMainFormView().AddTab(
				Arg.Do<string>(tabId => viewModel.PostprocessingTabPageId = tabId),
				UI.Presenters.Postprocessing.MainWindowTabPage.Presenter.TabCaption,
				Arg.Any<object>()
			);

			mocks.Views.CreatePreprocessingView().SetViewModel(
				Arg.Do<UI.Presenters.PreprocessingUserInteractions.IViewModel>(x => viewModel.PreprocessingUserInteractions = x));

			mocks.Views.CreatePostprocessingTabPage().SetViewModel(
				Arg.Do<UI.Presenters.Postprocessing.MainWindowTabPage.IViewModel>(x => viewModel.PostprocessingTabPage = x));
		}

		public async Task Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			try
			{
				var tcs = new TaskCompletionSource<int>();
				var mainFormView = Mocks.Views.CreateMainFormView();
				mainFormView.When(x => x.ForceClose()).Do(x => tcs.SetResult(0));
				ViewModel.MainForm.OnClosing();
				await tcs.Task;
				traceListener.Flush();
			}
			finally
			{
				LJTraceSource.SetTestListeners(null);
			}
		}
	};
}

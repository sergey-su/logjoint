using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
	public class Mocks: IMocks // todo: make internal
	{
		public Preprocessing.ICredentialsCache CredentialsCache;
		public WebViewTools.IWebViewTools WebBrowserDownloader;
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

		UI.Presenters.IPromptDialog IMocks.PromptDialog => PromptDialog;
		UI.Presenters.IClipboardAccess IMocks.ClipboardAccess => ClipboardAccess;
	};

	public class ViewModelObjects // todo: make internal
	{
		public UI.Presenters.LogViewer.IViewModel LoadedMessagesLogViewer;
		public UI.Presenters.MainForm.IViewModel MainForm;
		public UI.Presenters.PreprocessingUserInteractions.IViewModel PreprocessingUserInteractions;
		public UI.Presenters.Postprocessing.MainWindowTabPage.IViewModel PostprocessingTabPage;
		public string PostprocessingTabPageId;
		public UI.Presenters.LoadedMessages.IViewModel LoadedMessages;
		public UI.Presenters.LogViewer.IViewModel SearchResultLogViewer;
		public UI.Presenters.MessagePropertiesDialog.IDialogViewModel MessagePropertiesDialog;
		public UI.Presenters.SourcesManager.IViewModel SourcesManager;
		public UI.Presenters.SourcesList.IViewModel SourcesList;
		public UI.Presenters.SourcePropertiesWindow.IViewModel SourcePropertiesWindow;
	};

	public class TestAppConfig
	{
		public int LogViewerViewSize = 20;
		public string LocalPluginsList;
	};

	public class TestAppInstance: IContext, IRegistry // todo: make internal
	{
		private bool disposed;
		private TraceListener traceListener;
		private Dictionary<Type, object> registry = new Dictionary<Type, object>();

		public ISynchronizationContext SynchronizationContext { get; private set; }
		public ModelObjects Model { get; private set; }
		public UI.Presenters.PresentationObjects Presentation { get; private set; }
		public ViewModelObjects ViewModel { get; private set; }
		public Mocks Mocks { get; private set; }
		public ISamples Samples { get; private set; }

		/// <summary>
		/// Temporary folder where this instance of application stores its state.
		/// </summary>
		public string AppDataDirectory { get; private set; }

		IModel IContext.Model => Model.ExpensibilityEntryPoint;
		UI.Presenters.IPresentation IContext.Presentation => Presentation.ExpensibilityEntryPoint;
		IMocks IContext.Mocks => Mocks;
		IRegistry IContext.Registry => this;
		ISamples IContext.Samples => Samples;
		IUtils IContext.Utils => new TestAppExtensions.UtilsImpl(this); // todo: have one object
		string IContext.AppDataDirectory => AppDataDirectory;

		T IRegistry.Get<T>() => (T)registry[typeof(T)];
		void IRegistry.Set<T>(T value) => registry[typeof(T)] = value;

		public static async Task<TestAppInstance> Create(TestAppConfig config = null)
		{
			config = config ?? new TestAppConfig();

			var mocks = new Mocks
			{
				CredentialsCache = Substitute.For<Preprocessing.ICredentialsCache>(),
				WebBrowserDownloader = Substitute.For<WebViewTools.IWebViewTools>(),
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
			var traceListener = new TraceListener(Path.Combine(appDataDir, "test-debug.log") + ";logical-thread=1");

			ISynchronizationContext serialSynchronizationContext = new SerialSynchronizationContext();

			var (model, presentation) = await serialSynchronizationContext.Invoke(() =>
			{
				var modelObjects = ModelFactory.Create(
					new ModelConfig
					{
						WorkspacesUrl = "",
						TelemetryUrl = "",
						IssuesUrl = "",
						AutoUpdateUrl = "",
						WebContentCacheConfig = mocks.WebContentCacheConfig,
						LogsDownloaderConfig = mocks.LogsDownloaderConfig,
						AppDataDirectory = appDataDir,
						TraceListeners = new[] { traceListener }
					},
					serialSynchronizationContext,
					(_1) => mocks.CredentialsCache,
					(_1, _2, _3) => mocks.WebBrowserDownloader,
					Substitute.For<Drawing.IMatrixFactory>(), // todo: won't work for SequenceDiagram presenter tests
					RegularExpressions.FCLRegexFactory.Instance,
					() => Substitute.For<Postprocessing.Correlation.Solver.ISolver>() // todo: won't work if correlation is to be tested
				);

				var presentationObjects = UI.Presenters.Factory.Create(
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

				if (config.LocalPluginsList != null)
				{
					modelObjects.PluginsManager.LoadPlugins(new TestApplicationEntryPoint
					{
						Model = modelObjects.ExpensibilityEntryPoint,
						Presentation = presentationObjects.ExpensibilityEntryPoint
					}, config.LocalPluginsList);
				}

				return (modelObjects, presentationObjects);
			});

			return new TestAppInstance
			{
				SynchronizationContext = serialSynchronizationContext,
				Mocks = mocks,
				Model = model,
				Presentation = presentation,
				ViewModel = viewModel,
				Samples = new SamplesUtils(),
				traceListener = traceListener,
				AppDataDirectory = appDataDir
			};
		}

		private static void InitializeMocks(TestAppConfig config, Mocks mocks, ViewModelObjects viewModel)
		{
			mocks.Views.CreateLoadedMessagesView().MessagesView.SetViewModel(
				Arg.Do<UI.Presenters.LogViewer.IViewModel>(x => viewModel.LoadedMessagesLogViewer = x));
			mocks.Views.CreateLoadedMessagesView().MessagesView.DisplayLinesPerPage.Returns(config.LogViewerViewSize);

			mocks.Views.CreateMainFormView().SetViewModel(
				Arg.Do<UI.Presenters.MainForm.IViewModel>(x => viewModel.MainForm = x));
			mocks.Views.CreateMainFormView().AddTab(
				Arg.Do<string>(tabId => viewModel.PostprocessingTabPageId = tabId),
				UI.Presenters.Postprocessing.MainWindowTabPage.Presenter.TabCaption,
				Arg.Any<object>()
			);

			mocks.Views.CreatePreprocessingView().SetViewModel(
				Arg.Do<UI.Presenters.PreprocessingUserInteractions.IViewModel>(x => viewModel.PreprocessingUserInteractions = x));

			mocks.Views.CreatePostprocessingTabPage().SetViewModel(
				Arg.Do<UI.Presenters.Postprocessing.MainWindowTabPage.IViewModel>(x => viewModel.PostprocessingTabPage = x));

			mocks.Views.CreateLoadedMessagesView().SetViewModel(
				Arg.Do<UI.Presenters.LoadedMessages.IViewModel>(x => viewModel.LoadedMessages = x));

			mocks.Views.CreateSearchResultView().MessagesView.SetViewModel(
				Arg.Do<UI.Presenters.LogViewer.IViewModel>(x => viewModel.SearchResultLogViewer = x));
			mocks.Views.CreateSearchResultView().MessagesView.DisplayLinesPerPage.Returns(config.LogViewerViewSize);

			mocks.Views.CreateMessagePropertiesDialogView().CreateDialog(
				Arg.Do<UI.Presenters.MessagePropertiesDialog.IDialogViewModel>(x => viewModel.MessagePropertiesDialog = x));

			mocks.Views.CreateSourcesManagerView().SetViewModel(
				Arg.Do<UI.Presenters.SourcesManager.IViewModel>(x => viewModel.SourcesManager = x));
			mocks.Views.CreateSourcesListView().SetViewModel(
				Arg.Do<UI.Presenters.SourcesList.IViewModel>(x => viewModel.SourcesList = x));
			mocks.Views.CreateSourcePropertiesWindowView().SetViewModel(
				Arg.Do<UI.Presenters.SourcePropertiesWindow.IViewModel>(x => viewModel.SourcePropertiesWindow = x));
		}

		public async Task Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			var tcs = new TaskCompletionSource<int>();
			await this.Model.SynchronizationContext.Invoke(() =>
			{
				var mainFormView = Mocks.Views.CreateMainFormView();
				mainFormView.When(x => x.ForceClose()).Do(x => tcs.SetResult(0));
				ViewModel.MainForm.OnClosing();
			});
			await tcs.Task;
			traceListener.Flush();
		}

		class TestApplicationEntryPoint
		{
			public IModel Model { get; internal set; }
			public UI.Presenters.IPresentation Presentation { get; internal set; }
		};
	};
}

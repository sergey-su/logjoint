using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogJoint;
using LogJoint.Preprocessing;
using LogJoint.Persistence;
using NSubstitute;
using Microsoft.JSInterop;

namespace LogJoint
{

	public class ViewModelObjects
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

    class Mocks
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

        public Mocks(ViewModelObjects viewModel)
        {
            CredentialsCache = Substitute.For<Preprocessing.ICredentialsCache>();
            WebBrowserDownloader = Substitute.For<WebViewTools.IWebViewTools>();
            WebContentCacheConfig = Substitute.For<Persistence.IWebContentCacheConfig>();
            LogsDownloaderConfig = Substitute.For<Preprocessing.ILogsDownloaderConfig>();

            ClipboardAccess = Substitute.For<UI.Presenters.IClipboardAccess>();
            ShellOpen = Substitute.For<UI.Presenters.IShellOpen>();
            AlertPopup = Substitute.For<UI.Presenters.IAlertPopup>();
            FileDialogs = Substitute.For<UI.Presenters.IFileDialogs>();
            PromptDialog = Substitute.For<UI.Presenters.IPromptDialog>();
            AboutConfig = Substitute.For<UI.Presenters.About.IAboutConfig>();
            DragDropHandler = Substitute.For<UI.Presenters.MainForm.IDragDropHandler>();
            SystemThemeDetector = Substitute.For<UI.Presenters.ISystemThemeDetector>();
            Views = Substitute.For<UI.Presenters.Factory.IViewsFactory>();

			Views.CreateLoadedMessagesView().MessagesView.SetViewModel(
				Arg.Do<UI.Presenters.LogViewer.IViewModel>(x => viewModel.LoadedMessagesLogViewer = x));
            Views.CreateLoadedMessagesView().MessagesView.DisplayLinesPerPage.Returns(10);
            Views.CreateLoadedMessagesView().MessagesView.HasInputFocus.Returns(true);
			Views.CreateSourcesManagerView().SetViewModel(
				Arg.Do<UI.Presenters.SourcesManager.IViewModel>(x => viewModel.SourcesManager = x));
			Views.CreateSourcesListView().SetViewModel(
				Arg.Do<UI.Presenters.SourcesList.IViewModel>(x => viewModel.SourcesList = x));
        }
    };
}

namespace logjoint.wasm
{
    public class Program
    {
        class BlazorSynchronizationContext: ISynchronizationContext
        {
            void ISynchronizationContext.Post(Action action)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ => action(), null);
            }
        };

        class WebContentConfig : IWebContentCacheConfig, ILogsDownloaderConfig
        {
            bool IWebContentCacheConfig.IsCachingForcedForHost(string hostName) => false;
            LogDownloaderRule ILogsDownloaderConfig.GetLogDownloaderConfig(Uri forUri) => null;
            void ILogsDownloaderConfig.AddRule(Uri uri, LogDownloaderRule rule) {}
        };


        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSingleton<ModelObjects>(serviceProvider =>
            {
                ISynchronizationContext invokingSynchronization = new BlazorSynchronizationContext();
                WebContentConfig webContentConfig = new WebContentConfig();

                var model = ModelFactory.Create(
                    new ModelConfig
                    {
                        WorkspacesUrl = "",
                        TelemetryUrl = "",
                        IssuesUrl = "",
                        AutoUpdateUrl = "",
                        PluginsUrl = "",
                        WebContentCacheConfig = webContentConfig,
                        LogsDownloaderConfig = webContentConfig,
                        TraceListeners = new[] { new TraceListener(";console=1") },
                        FormatsRepositoryAssembly = System.Reflection.Assembly.GetExecutingAssembly(),
                        FileSystem = new LogJoint.Wasm.FileSystem(serviceProvider.GetService<IJSRuntime>())
                    },
                        invokingSynchronization,
                        (storageManager) => null /*new PreprocessingCredentialsCache (
                        mainWindow.Window,
                        storageManager.GlobalSettingsEntry,
                        invokingSynchronization
                    )*/,
                        (shutdown, webContentCache, traceSourceFactory) => null /*new Presenters.WebViewTools.Presenter (
                        new WebBrowserDownloaderWindowController (),
                        invokingSynchronization,
                        webContentCache,
                        shutdown,
                        traceSourceFactory
                    )*/,
                    null/*new Drawing.Matrix.Factory()*/,
                    LogJoint.RegularExpressions.FCLRegexFactory.Instance
                );
                return model;
            });
            builder.Services.AddSingleton<ViewModelObjects>(serviceProvider =>
            {
                var model = serviceProvider.GetService<ModelObjects>();

                var viewModel = new ViewModelObjects();
                var mocks = new Mocks(viewModel);

                var presentationObjects = LogJoint.UI.Presenters.Factory.Create(
                    model,
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

                return viewModel;
            });

            await builder.Build().RunAsync();
        }
    }
}

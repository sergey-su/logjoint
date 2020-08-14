using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogJoint;
using LogJoint.Preprocessing;
using LogJoint.Persistence;
using NSubstitute;
using Microsoft.JSInterop;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.CodeAnalysis;
using LogJoint.FieldsProcessor;

namespace LogJoint.Wasm
{

	public class ViewModelObjects // todo: rename to ViewProxies
	{
        public LogJoint.UI.Presenters.PresentationObjects PresentationObjects;

        public UI.LogViewer.ViewProxy LoadedMessagesLogViewerViewProxy = new UI.LogViewer.ViewProxy();
        public UI.LoadedMessages.ViewProxy LoadedMessagesViewProxy;

        public LogJoint.UI.Presenters.MainForm.IViewModel MainForm;
		public LogJoint.UI.Presenters.PreprocessingUserInteractions.IViewModel PreprocessingUserInteractions;
		public LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.IViewModel PostprocessingTabPage;
		public string PostprocessingTabPageId;
		public LogJoint.UI.Presenters.LogViewer.IViewModel SearchResultLogViewer;
		public LogJoint.UI.Presenters.MessagePropertiesDialog.IDialogViewModel MessagePropertiesDialog;
		public LogJoint.UI.Presenters.SourcesManager.IViewModel SourcesManager;
		public LogJoint.UI.Presenters.SourcesList.IViewModel SourcesList;
		public LogJoint.UI.Presenters.SourcePropertiesWindow.IViewModel SourcePropertiesWindow;

        public ViewModelObjects()
        {
            this.LoadedMessagesViewProxy = new UI.LoadedMessages.ViewProxy(LoadedMessagesLogViewerViewProxy);
        }
	};

    class Mocks
    {
        public Preprocessing.ICredentialsCache CredentialsCache;
        public WebViewTools.IWebViewTools WebBrowserDownloader;
        public Persistence.IWebContentCacheConfig WebContentCacheConfig;
        public Preprocessing.ILogsDownloaderConfig LogsDownloaderConfig;

        public LogJoint.UI.Presenters.IClipboardAccess ClipboardAccess;
        public LogJoint.UI.Presenters.IShellOpen ShellOpen;
        public LogJoint.UI.Presenters.IAlertPopup AlertPopup;
        public LogJoint.UI.Presenters.IFileDialogs FileDialogs;
        public LogJoint.UI.Presenters.IPromptDialog PromptDialog;
        public LogJoint.UI.Presenters.About.IAboutConfig AboutConfig;
        public LogJoint.UI.Presenters.MainForm.IDragDropHandler DragDropHandler;
        public LogJoint.UI.Presenters.ISystemThemeDetector SystemThemeDetector;

        public LogJoint.UI.Presenters.Factory.IViewsFactory Views;

        public Mocks(ViewModelObjects viewModel)
        {
            CredentialsCache = Substitute.For<Preprocessing.ICredentialsCache>();
            WebBrowserDownloader = Substitute.For<WebViewTools.IWebViewTools>();
            WebContentCacheConfig = Substitute.For<Persistence.IWebContentCacheConfig>();
            LogsDownloaderConfig = Substitute.For<Preprocessing.ILogsDownloaderConfig>();

            ClipboardAccess = Substitute.For<LogJoint.UI.Presenters.IClipboardAccess>();
            ShellOpen = Substitute.For<LogJoint.UI.Presenters.IShellOpen>();
            AlertPopup = Substitute.For<LogJoint.UI.Presenters.IAlertPopup>();
            FileDialogs = Substitute.For<LogJoint.UI.Presenters.IFileDialogs>();
            PromptDialog = Substitute.For<LogJoint.UI.Presenters.IPromptDialog>();
            AboutConfig = Substitute.For<LogJoint.UI.Presenters.About.IAboutConfig>();
            DragDropHandler = Substitute.For<LogJoint.UI.Presenters.MainForm.IDragDropHandler>();
            SystemThemeDetector = Substitute.For<LogJoint.UI.Presenters.ISystemThemeDetector>();
            Views = Substitute.For<LogJoint.UI.Presenters.Factory.IViewsFactory>();

            Views.CreateLoadedMessagesView().Returns(viewModel.LoadedMessagesViewProxy);
			Views.CreateSourcesManagerView().SetViewModel(
				Arg.Do<LogJoint.UI.Presenters.SourcesManager.IViewModel>(x => viewModel.SourcesManager = x));
			Views.CreateSourcesListView().SetViewModel(
				Arg.Do<LogJoint.UI.Presenters.SourcesList.IViewModel>(x => viewModel.SourcesList = x));
        }
    };
}

namespace LogJoint.Wasm
{
    namespace Extensibility
    {
        public interface IApplication
        {
            IModel Model { get; }
            LogJoint.UI.Presenters.IPresentation Presentation { get; }
            object View { get; }
        };

        class Application : IApplication
        {
            public Application(
                IModel model,
                LogJoint.UI.Presenters.IPresentation presentation
            )
            {
                this.Model = model;
                this.Presentation = presentation;
            }

            public LogJoint.UI.Presenters.IPresentation Presentation { get; private set; }
            public IModel Model { get; private set; }
            public object View { get; private set; }
        }
    }

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

        class MetadataReferencesProvider : FieldsProcessor.IMetadataReferencesProvider
        {
            List<MetadataReference> references = new List<MetadataReference>();

            public async Task Init(IJSRuntime jsRuntime)
            {
                var httpClient = new HttpClient();
                async Task<MetadataReference> resolve(string asmName) => MetadataReference.CreateFromStream(
                    await httpClient.GetStreamAsync(
                        await jsRuntime.InvokeAsync<string>("logjoint.getResourceUrl", $"_framework/_bin/{asmName}")));
                references.AddRange(await Task.WhenAll(
                    resolve("mscorlib.dll"),
                    // resolve("System.Runtime.dll"),
                    resolve("netstandard.dll"),
                    resolve("logjoint.model.dll"),
                    resolve("logjoint.model.sdk.dll")
                ));
            }

            IReadOnlyList<MetadataReference> IMetadataReferencesProvider.GetMetadataReferences(IEnumerable<string> assemblyNames) => references;
        };


        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            var fieldsProcessorMetadataReferencesProvider = new MetadataReferencesProvider();

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSingleton<TraceListener>(serviceProvider => new TraceListener(";membuf=1"));
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
                        TraceListeners = new[] { serviceProvider.GetService<TraceListener>() },
                        FormatsRepositoryAssembly = System.Reflection.Assembly.GetExecutingAssembly(),
                        FileSystem = new LogJoint.Wasm.LogMediaFileSystem(serviceProvider.GetService<IJSRuntime>()),
                        FieldsProcessorMetadataReferencesProvider = fieldsProcessorMetadataReferencesProvider,
                        PersistenceFileSystem = new LogJoint.Wasm.PersistenceFileSystem((IJSInProcessRuntime)serviceProvider.GetService<IJSRuntime>()),
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

                model.GlobalSettingsAccessor.FileSizes = new Settings.FileSizes() { Threshold = 1, WindowSize = 1 };

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

                viewModel.PresentationObjects = presentationObjects;

                return viewModel;
            });

            var wasmHost = builder.Build();

            await fieldsProcessorMetadataReferencesProvider.Init(wasmHost.Services.GetService<IJSRuntime>());

            {
                var pluginsDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "plugins"); // folder in memory, powered by emscripten MEMFS.
                var resourcesAssembly = Assembly.GetExecutingAssembly();
                foreach (string resourceName in resourcesAssembly.GetManifestResourceNames().Where(f => f.StartsWith("LogJoint.Wasm.Plugins")))
                {
                    Console.WriteLine("Found plugin in resources: {0}", resourceName);
                    var fz = new FastZip();
                    fz.ExtractZip(resourcesAssembly.GetManifestResourceStream(resourceName), pluginsDir,
                        FastZip.Overwrite.Always, null, null, null, false, false);
                    Console.WriteLine("Exactracted plugin: {0}", resourceName);
                }
                var model = wasmHost.Services.GetService<ModelObjects>();
                var view = wasmHost.Services.GetService<ViewModelObjects>();
                model.PluginsManager.LoadPlugins(new Extensibility.Application(
                    model.ExpensibilityEntryPoint,
                    view.PresentationObjects.ExpensibilityEntryPoint), pluginsDir, false);
            }

            await wasmHost.RunAsync();
        }
    }
}

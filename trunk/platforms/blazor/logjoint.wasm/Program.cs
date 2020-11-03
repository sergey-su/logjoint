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
using Microsoft.CodeAnalysis;
using LogJoint.FieldsProcessor;
using LogJoint.Wasm.UI;
using System.IO;
using System.IO.Compression;

namespace LogJoint.Wasm
{

	public class ViewModelObjects // todo: rename to ViewProxies
	{
        public LogJoint.UI.Presenters.PresentationObjects PresentationObjects;

        public UI.LogViewer.ViewProxy LoadedMessagesLogViewerViewProxy = new UI.LogViewer.ViewProxy();
        public UI.LoadedMessages.ViewProxy LoadedMessagesViewProxy;
        public UI.SourcesListViewProxy SourcesListViewProxy = new UI.SourcesListViewProxy();
        public UI.SourcesManagerViewProxy SourcesManagerViewProxy = new UI.SourcesManagerViewProxy();
        public UI.Postprocessing.ViewProxy PostprocessingTabPage = new UI.Postprocessing.ViewProxy();
        public UI.Postprocesssing.StateInspector.ViewProxy PostprocesssingStateInspectorViewProxy = new UI.Postprocesssing.StateInspector.ViewProxy();
        public UI.SearchPanelViewProxy SearchPanel;
        public UI.SearchResultViewProxy SearchResult;
        public UI.LogViewer.ViewProxy SearchResultLogViewer = new UI.LogViewer.ViewProxy();
        public UI.BookmarksListViewProxy BookmarksList = new UI.BookmarksListViewProxy();
        public UI.HistoryDialogViewProxy HistoryDialog;
        public UI.PreprocessingUserInteractionsViewProxy PreprocessingUserInteractions = new PreprocessingUserInteractionsViewProxy();
        public UI.MessagePropertiesViewProxy MessageProperties = new MessagePropertiesViewProxy();

        public ViewModelObjects()
        {
            this.LoadedMessagesViewProxy = new UI.LoadedMessages.ViewProxy(LoadedMessagesLogViewerViewProxy);
            this.SearchPanel = new UI.SearchPanelViewProxy();
            this.SearchResult = new UI.SearchResultViewProxy(SearchResultLogViewer);
            this.HistoryDialog = new UI.HistoryDialogViewProxy();
        }
	};

    class Mocks
    {
        public Preprocessing.ICredentialsCache CredentialsCache;
        public WebViewTools.IWebViewTools WebBrowserDownloader;
        public Persistence.IWebContentCacheConfig WebContentCacheConfig;
        public Preprocessing.ILogsDownloaderConfig LogsDownloaderConfig;

        public LogJoint.UI.Presenters.IShellOpen ShellOpen;
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

            ShellOpen = Substitute.For<LogJoint.UI.Presenters.IShellOpen>();
            FileDialogs = Substitute.For<LogJoint.UI.Presenters.IFileDialogs>();
            PromptDialog = Substitute.For<LogJoint.UI.Presenters.IPromptDialog>();
            AboutConfig = Substitute.For<LogJoint.UI.Presenters.About.IAboutConfig>();
            DragDropHandler = Substitute.For<LogJoint.UI.Presenters.MainForm.IDragDropHandler>();
            SystemThemeDetector = Substitute.For<LogJoint.UI.Presenters.ISystemThemeDetector>();
            Views = Substitute.For<LogJoint.UI.Presenters.Factory.IViewsFactory>();

            Views.CreateLoadedMessagesView().Returns(viewModel.LoadedMessagesViewProxy);
            Views.CreateSourcesManagerView().Returns(viewModel.SourcesManagerViewProxy);
            Views.CreateSourcesListView().Returns(viewModel.SourcesListViewProxy);
            Views.CreatePostprocessingTabPage().Returns(viewModel.PostprocessingTabPage);
            Views.PostprocessingViewsFactory.CreateStateInspectorView().Returns(viewModel.PostprocesssingStateInspectorViewProxy);
            Views.CreatePreprocessingView().Returns(viewModel.PreprocessingUserInteractions);
            Views.CreateSearchPanelView().Returns(viewModel.SearchPanel);
            Views.CreateSearchResultView().Returns(viewModel.SearchResult);
            Views.CreateBookmarksListView().Returns(viewModel.BookmarksList);
            Views.CreateHistoryDialogView().Returns(viewModel.HistoryDialog);
            Views.CreateMessagePropertiesDialogView().Returns(viewModel.MessageProperties);
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

        class AssemblyLoader: FieldsProcessor.IAssemblyLoader
        {
            Assembly FieldsProcessor.IAssemblyLoader.Load(byte[] image)
            {
                var context = System.Runtime.Loader.AssemblyLoadContext.Default;
                using (var ms = new MemoryStream(image))
                    return context.LoadFromStream(ms);
            }
        };

        class MetadataReferencesProvider : FieldsProcessor.IMetadataReferencesProvider
        {
            List<MetadataReference> references = new List<MetadataReference>();

            public async Task Init(IJSRuntime jsRuntime)
            {
                var httpClient = new HttpClient();
                async Task<MetadataReference> resolve(string asmName) => MetadataReference.CreateFromStream(
                    await httpClient.GetStreamAsync(
                        await jsRuntime.InvokeAsync<string>("logjoint.getResourceUrl", $"_framework/{asmName}")));
                references.AddRange(await Task.WhenAll(
                    resolve("mscorlib.dll"),
                    resolve("System.Runtime.dll"),
                    resolve("System.Private.CoreLib.dll"),
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
            builder.Services.AddSingleton<JsInterop>(serviceProvider => new JsInterop(serviceProvider.GetService<IJSRuntime>()));
            builder.Services.AddSingleton<TraceListener>(serviceProvider => new TraceListener(";membuf=1"));
            builder.Services.AddSingleton<ModelObjects>(serviceProvider =>
            {
                ISynchronizationContext invokingSynchronization = new BlazorSynchronizationContext();
                WebContentConfig webContentConfig = new WebContentConfig();
                var logMediaFileSystem = new LogJoint.Wasm.LogMediaFileSystem(serviceProvider.GetService<IJSRuntime>());

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
                        RemoveDefaultTraceListener = true, // it's expensive in wasm
                        FormatsRepositoryAssembly = System.Reflection.Assembly.GetExecutingAssembly(),
                        FileSystem = logMediaFileSystem,
                        FieldsProcessorMetadataReferencesProvider = fieldsProcessorMetadataReferencesProvider,
                        FieldsProcessorAssemblyLoader = new AssemblyLoader(),
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

                logMediaFileSystem.Init(model.TraceSourceFactory);
                model.GlobalSettingsAccessor.FileSizes = new Settings.FileSizes() { Threshold = 1, WindowSize = 1 };

                return model;
            });
            builder.Services.AddSingleton<ViewModelObjects>(serviceProvider =>
            {
                var model = serviceProvider.GetService<ModelObjects>();
                var jsRuntime = serviceProvider.GetService<IJSRuntime>();

                var viewModel = new ViewModelObjects();
                var mocks = new Mocks(viewModel);
                mocks.ShellOpen.When(s => s.OpenInTextEditor(Arg.Any<string>())).Do(x =>
                {
                    serviceProvider.GetService<JsInterop>().SaveAs.SaveAs(File.ReadAllText(x.Arg<string>()), x.Arg<string>());
                });

                var presentationObjects = LogJoint.UI.Presenters.Factory.Create(
                    model,
                    new Clipboard(jsRuntime),
                    mocks.ShellOpen,
                    /*alertPopup=*/null,
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
            builder.Services.AddSingleton<TreeStyles>();

            var wasmHost = builder.Build();

            var jsInterop = wasmHost.Services.GetService<JsInterop>();
            await jsInterop.Init();

            var jsRuntime = wasmHost.Services.GetService<IJSRuntime>();
            await fieldsProcessorMetadataReferencesProvider.Init(jsRuntime);

            {
                var pluginsDirsList = new List<string>();
                var pluginsDir = Path.Combine(Path.GetTempPath(), "plugins"); // folder in memory, powered by emscripten MEMFS.
                var resourcesAssembly = Assembly.GetExecutingAssembly();

                foreach (string resourceName in resourcesAssembly.GetManifestResourceNames().Where(f => f.StartsWith("LogJoint.Wasm.Plugins")))
                {
                    var resourceStream = resourcesAssembly.GetManifestResourceStream(resourceName);
                    var pluginDir = Path.Combine(pluginsDir, resourceName);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    using (var archive = new ZipArchive(resourceStream, ZipArchiveMode.Read, leaveOpen: true))
                    {
                        var createdDirectories = new HashSet<string>();
                        void ensureDirectoryCreated(string dir)
                        {
                            if (createdDirectories.Add(dir))
                                Directory.CreateDirectory(dir);
                        };
                        foreach (var e in archive.Entries)
                        {
                            var fileName = Path.Combine(pluginDir, e.FullName);
                            ensureDirectoryCreated(Path.GetDirectoryName(fileName));
                            using (var sourceStream = e.Open())
                            using (var targetStream = File.OpenWrite(fileName))
                            {
                                sourceStream.CopyTo(targetStream);
                            }
                        }
                    }
                    Console.WriteLine("Extracted plugin: {0}, took {1}", resourceName, sw.Elapsed);
                    pluginsDirsList.Add(pluginDir);
                }

                var model = wasmHost.Services.GetService<ModelObjects>();
                var view = wasmHost.Services.GetService<ViewModelObjects>();
                model.PluginsManager.LoadPlugins(new Extensibility.Application(
                    model.ExpensibilityEntryPoint,
                    view.PresentationObjects.ExpensibilityEntryPoint), string.Join(',', pluginsDirsList), false);
            }

            await wasmHost.RunAsync();
        }
    }
}

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
        public UI.QuickSearchTextBoxViewProxy SearchPanelQuickSearchTextBox = new UI.QuickSearchTextBoxViewProxy();
        public UI.SearchPanelViewProxy SearchPanel;
        public UI.SearchResultViewProxy SearchResult;
        public UI.LogViewer.ViewProxy SearchResultLogViewer = new UI.LogViewer.ViewProxy();
        public UI.BookmarksListViewProxy BookmarksList = new UI.BookmarksListViewProxy();

        public LogJoint.UI.Presenters.MainForm.IViewModel MainForm;
		public LogJoint.UI.Presenters.PreprocessingUserInteractions.IViewModel PreprocessingUserInteractions;
		public string PostprocessingTabPageId;
		public LogJoint.UI.Presenters.MessagePropertiesDialog.IDialogViewModel MessagePropertiesDialog;
		public LogJoint.UI.Presenters.SourcePropertiesWindow.IViewModel SourcePropertiesWindow;

        public ViewModelObjects()
        {
            this.LoadedMessagesViewProxy = new UI.LoadedMessages.ViewProxy(LoadedMessagesLogViewerViewProxy);
            this.SearchPanel = new UI.SearchPanelViewProxy(SearchPanelQuickSearchTextBox);
            this.SearchResult = new UI.SearchResultViewProxy(SearchResultLogViewer);
        }
	};

    class Mocks
    {
        public Preprocessing.ICredentialsCache CredentialsCache;
        public WebViewTools.IWebViewTools WebBrowserDownloader;
        public Persistence.IWebContentCacheConfig WebContentCacheConfig;
        public Preprocessing.ILogsDownloaderConfig LogsDownloaderConfig;

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

            ShellOpen = Substitute.For<LogJoint.UI.Presenters.IShellOpen>();
            AlertPopup = Substitute.For<LogJoint.UI.Presenters.IAlertPopup>();
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
            Views.CreateSearchPanelView().Returns(viewModel.SearchPanel);
            Views.CreateSearchResultView().Returns(viewModel.SearchResult);
            Views.CreateBookmarksListView().Returns(viewModel.BookmarksList);
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
                    serviceProvider.GetService<IJSRuntime>().InvokeVoidAsync("alert", File.ReadAllText(x.Arg<string>()));
                });

                var presentationObjects = LogJoint.UI.Presenters.Factory.Create(
                    model,
                    new Clipboard(jsRuntime),
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
            builder.Services.AddSingleton<TreeStyles>();

            var wasmHost = builder.Build();

            var jsRuntime = wasmHost.Services.GetService<IJSRuntime>();
            await fieldsProcessorMetadataReferencesProvider.Init(jsRuntime);
            // Put pre-compiled assemblies to the cache.
            // todo: remove when blazor RC2 fixes the SHA-xxx encryption classes which will allow using roslyn to compile the code at run time.
            await jsRuntime.InvokeVoidAsync("logjoint.setLocalStorageItem", "/ljpfs/e-FFFFFFFF4BB58F48-user-code-cache/b-FFFFFFFFBB3B7F3C-builder-code--777507682", // chrome debug log
                "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AElNZXNzYWdlc0J1aWxkZXJDYWxsYmFjawBfX2NhbGxiYWNrAGxvZ2pvaW50Lm1vZGVsLnNkawBMb2dKb2ludC5JbnRlcm5hbABsb2dqb2ludC5tb2RlbABVc2VyQ29kZWE0MTY1MTExNTNkOTRmNWNhNzc5OThiZTEwMmM2ODZhLmRsbABTeXN0ZW0AZ2V0X0N1cnJlbnRFbmRQb3NpdGlvbgBnZXRfQ3VycmVudFBvc2l0aW9uAE1lc3NhZ2VUaW1lc3RhbXAAX19NZXNzYWdlQnVpbGRlcgBHZW5lcmF0ZWRNZXNzYWdlQnVpbGRlcgBMb2dKb2ludC5GaWVsZHNQcm9jZXNzb3IALmN0b3IALmNjdG9yAFN5c3RlbS5EaWFnbm9zdGljcwBTeXN0ZW0uUnVudGltZS5Db21waWxlclNlcnZpY2VzAERlYnVnZ2luZ01vZGVzAFJlc2V0RmllbGRWYWx1ZXMATWFrZU1lc3NhZ2VGbGFncwBfX2ZsYWdzAFN0cmluZ1NsaWNlQXdhcmVVc2VyQ29kZUhlbHBlckZ1bmN0aW9ucwBPYmplY3QAX19BcHBseVRpbWVPZmZzZXQATG9nSm9pbnQAZ2V0X0N1cnJlbnRSYXdUZXh0AF9fZXh0AHNldgBTZXRJbnB1dEZpZWxkQnlJbmRleABfX2luZGV4AGJvZHkAb3BfRXF1YWxpdHkAX19HZXRfU2V2ZXJpdHkARW1wdHkAAAAAB3AAaQBkAAAHdABpAGQAAAl0AGkAbQBlAAAHcwBlAHYAAAliAG8AZAB5AAAlTQBNAGQAZAAvAEgASABtAG0AcwBzAC4ARgBGAEYARgBGAEYAAA9XAEEAUgBOAEkATgBHAAALRQBSAFIATwBSAAALRgBBAFQAQQBMAAAAH+Aw5Nxrq0Cx5ek+AUEllgAEIAEBCAMgAAEFIAEBEREDIAAOAwYRGQQHAREZAgYOEQcGERkRLRExERkSNRUROQEIBSABAREZByACETERGQ4DBhExBiABERkRGQYgARI1ERkGIAERMRExAyAACgUgAQERMQQgABEZBRUROQEIFCAIAQoKEjURPREZEU0RGRUROQEIBgACAhEZDgsHAxE9ERkVETkBCAh87IXXvqd5jgMGEh0GIAIBCBEZAyAACAUgAREZCAQgAQ4IBSACAQ4cCCACEh0SJREpBCAAEhUEIAARLQMAAAEDKAAOCAEACAAAAAAAHgEAAQBUAhZXcmFwTm9uRXhjZXB0aW9uVGhyb3dzAQgBAAIAAAAAAAB8LAAAAAAAAAAAAACWLAAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAiCwAAAAAAAAAAAAAAABfQ29yRGxsTWFpbgBtc2NvcmVlLmRsbAAAAAAA/yUAIAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAMAAAAqDwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            await jsRuntime.InvokeVoidAsync("logjoint.setLocalStorageItem", "/ljpfs/e-FFFFFFFF4BB58F48-user-code-cache/b-FFFFFFFFBFFA7C77-builder-code-1423217803", // analog
                "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ZXRfc3JjU3RyaW5nAGdldF90aWRTdHJpbmcAZ2V0X3RuYW1lU3RyaW5nAGdldF9kYXRlU3RyaW5nAGdldF9zZXZTdHJpbmcAZ2V0X2JvZHlTdHJpbmcAZmFrZU1zZwBJTWVzc2FnZXNCdWlsZGVyQ2FsbGJhY2sAX19jYWxsYmFjawBsb2dqb2ludC5tb2RlbC5zZGsATG9nSm9pbnQuSW50ZXJuYWwAbG9nam9pbnQubW9kZWwAVXNlckNvZGU5YjQ5Y2IxZDYwMTI0OTk0YmY4NTU2M2UyMDA3OTc4Ni5kbGwAZ2V0X0l0ZW0AU3lzdGVtAGdldF9DdXJyZW50RW5kUG9zaXRpb24AZ2V0X0N1cnJlbnRQb3NpdGlvbgBNZXNzYWdlVGltZXN0YW1wAF9fTWVzc2FnZUJ1aWxkZXIAR2VuZXJhdGVkTWVzc2FnZUJ1aWxkZXIATG9nSm9pbnQuRmllbGRzUHJvY2Vzc29yAC5jdG9yAC5jY3RvcgBTeXN0ZW0uRGlhZ25vc3RpY3MAU3lzdGVtLlJ1bnRpbWUuQ29tcGlsZXJTZXJ2aWNlcwBEZWJ1Z2dpbmdNb2RlcwBSZXNldEZpZWxkVmFsdWVzAE1ha2VNZXNzYWdlRmxhZ3MAX19mbGFncwBTdHJpbmdTbGljZUF3YXJlVXNlckNvZGVIZWxwZXJGdW5jdGlvbnMAT2JqZWN0AF9fQXBwbHlUaW1lT2Zmc2V0AExvZ0pvaW50AGdldF9DdXJyZW50UmF3VGV4dABfX2V4dABzZXYAU2V0SW5wdXRGaWVsZEJ5SW5kZXgAX19pbmRleABib2R5AF9fR2V0X1NldmVyaXR5AEVtcHR5AAAAAAAHcwBlAHYAAAlkAGEAdABlAAAHdABpAGQAAAt0AG4AYQBtAGUAAAdzAHIAYwAACWIAbwBkAHkAAC9NAE0AZABkACAASABIAFwAOgBtAG0AXAA6AHMAcwBcAC4AZgBmAGYAZgBmAGYAAAAAAFd4ttrvZZlDndE11T1/ZooABCABAQgDIAABBSABARERAyAADgMGERkEBwERGQIGDhEHBhEtERkRMREZEjUVETkBCAcgAhEtERkOAwYRLQUgAQERGQYgAREZERkGIAESNREZBiABES0RLQMgAAoFIAEBES0EIAARGQUVETkBCBQgCAEKChI1ET0RGRFNERkVETkBCAMHAQMEIAEDCAsHAxE9ERkVETkBCAi3elxWGTTgiQMGEh0GIAIBCBEZAyAACAUgAREZCAQgAQ4IBSACAQ4cCCACEh0SJREpBCAAEhUEIAARMQMAAAEDKAAOCAEACAAAAAAAHgEAAQBUAhZXcmFwTm9uRXhjZXB0aW9uVGhyb3dzAQgBAAIAAAAAAAAAAMwsAAAAAAAAAAAAAOYsAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAADYLAAAAAAAAAAAAAAAAF9Db3JEbGxNYWluAG1zY29yZWUuZGxsAAAAAAD/JQAgABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAMAAAA+DwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

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

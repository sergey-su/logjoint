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
using Microsoft.JSInterop;
using System.Reflection;
using Microsoft.CodeAnalysis;
using LogJoint.FieldsProcessor;
using LogJoint.Wasm.UI;
using System.IO;
using System.IO.Compression;
using LogJoint.UI.Presenters;

namespace LogJoint.Wasm
{
    // todo: get rid of proxies
    public class ViewProxies: Factory.IViewsFactory, LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory
    {
        public UI.LogViewer.ViewProxy LoadedMessagesLogViewerViewProxy = new();
        public UI.LoadedMessages.ViewProxy LoadedMessagesViewProxy;
        public UI.SourcesListViewProxy SourcesListViewProxy = new();
        public UI.SourcesManagerViewProxy SourcesManagerViewProxy = new();
        public UI.Postprocessing.ViewProxy PostprocessingTabPage = new();
        public UI.Postprocesssing.StateInspector.ViewProxy PostprocesssingStateInspectorViewProxy = new();
        public UI.Postprocesssing.Timeline.ViewProxy PostprocesssingTimelineViewProxy = new();
        public UI.SearchPanelViewProxy SearchPanel = new();
        public UI.LogViewer.ViewProxy SearchResultLogViewer = new();
        public UI.SearchResultViewProxy SearchResult;
        public UI.BookmarksListViewProxy BookmarksList = new();
        public UI.HistoryDialogViewProxy HistoryDialog = new();
        public UI.PreprocessingUserInteractionsViewProxy PreprocessingUserInteractions = new();
        public UI.MessagePropertiesViewProxy MessageProperties = new();
        public UI.TimelineViewProxy Timeline = new();
        public UI.SourcePropertiesWindowViewProxy SourcePropertiesWindow = new();
        public UI.StatusReportViewProxy StatusReport = new();
        public UI.TimelinePanelViewProxy TimelinePanel = new();
        public UI.FilterDialogViewProxy FilterDialog = new();
        public UI.BookmarksManagerViewProxy BookmarksManager = new();
        public UI.MainFormViewProxy MainForm = new();

        public ViewProxies()
        {
            this.LoadedMessagesViewProxy = new UI.LoadedMessages.ViewProxy(LoadedMessagesLogViewerViewProxy);
            this.SearchResult = new UI.SearchResultViewProxy(SearchResultLogViewer);
        }

        LogJoint.UI.Presenters.FormatsWizard.Factory.IViewsFactory Factory.IViewsFactory.FormatsWizardViewFactory => null;

        LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory Factory.IViewsFactory.PostprocessingViewsFactory => this;

        LogJoint.UI.Presenters.About.IView Factory.IViewsFactory.CreateAboutView() => null;

        LogJoint.UI.Presenters.BookmarksList.IView Factory.IViewsFactory.CreateBookmarksListView() => BookmarksList;

        LogJoint.UI.Presenters.BookmarksManager.IView Factory.IViewsFactory.CreateBookmarksManagerView() => BookmarksManager;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.IView Factory.IViewsFactory.CreateDebugOutputFormatView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.IView Factory.IViewsFactory.CreateFileBasedFormatView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.IView Factory.IViewsFactory.CreateFormatDetectionView() => null;

        LogJoint.UI.Presenters.HistoryDialog.IView Factory.IViewsFactory.CreateHistoryDialogView() => HistoryDialog;

        LogJoint.UI.Presenters.FilterDialog.IView Factory.IViewsFactory.CreateHlFilterDialogView() => FilterDialog;

        LogJoint.UI.Presenters.FiltersManager.IView Factory.IViewsFactory.CreateHlFiltersManagerView() => null;

        LogJoint.UI.Presenters.LoadedMessages.IView Factory.IViewsFactory.CreateLoadedMessagesView() => LoadedMessagesViewProxy;

        LogJoint.UI.Presenters.MainForm.IView Factory.IViewsFactory.CreateMainFormView() => MainForm;

        LogJoint.UI.Presenters.MessagePropertiesDialog.IView Factory.IViewsFactory.CreateMessagePropertiesDialogView() => MessageProperties;


        LogJoint.UI.Presenters.NewLogSourceDialog.IView Factory.IViewsFactory.CreateNewLogSourceDialogView() => null;

        LogJoint.UI.Presenters.Options.Dialog.IView Factory.IViewsFactory.CreateOptionsDialogView() => null;

        LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.IView Factory.IViewsFactory.CreatePostprocessingTabPage() => PostprocessingTabPage;

        LogJoint.UI.Presenters.PreprocessingUserInteractions.IView Factory.IViewsFactory.CreatePreprocessingView() => PreprocessingUserInteractions;

        LogJoint.UI.Presenters.SearchEditorDialog.IView Factory.IViewsFactory.CreateSearchEditorDialogView() => null;

        LogJoint.UI.Presenters.SearchesManagerDialog.IView Factory.IViewsFactory.CreateSearchesManagerDialogView() => null;

        LogJoint.UI.Presenters.FilterDialog.IView Factory.IViewsFactory.CreateSearchFilterDialogView(LogJoint.UI.Presenters.SearchEditorDialog.IDialogView parentView) => FilterDialog;

        LogJoint.UI.Presenters.SearchPanel.IView Factory.IViewsFactory.CreateSearchPanelView() => SearchPanel;

        LogJoint.UI.Presenters.SearchResult.IView Factory.IViewsFactory.CreateSearchResultView() => SearchResult;

        LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateSequenceDiagramView() => null;

        LogJoint.UI.Presenters.SharingDialog.IView Factory.IViewsFactory.CreateSharingDialogView() => null;

        LogJoint.UI.Presenters.SourcePropertiesWindow.IView Factory.IViewsFactory.CreateSourcePropertiesWindowView() => SourcePropertiesWindow;

        LogJoint.UI.Presenters.SourcesList.IView Factory.IViewsFactory.CreateSourcesListView() => SourcesListViewProxy;

        LogJoint.UI.Presenters.SourcesManager.IView Factory.IViewsFactory.CreateSourcesManagerView() => SourcesManagerViewProxy;

        LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateStateInspectorView() => PostprocesssingStateInspectorViewProxy;

        LogJoint.UI.Presenters.StatusReports.IView Factory.IViewsFactory.CreateStatusReportsView() => StatusReport;

        LogJoint.UI.Presenters.ThreadsList.IView Factory.IViewsFactory.CreateThreadsListView() => null;

        LogJoint.UI.Presenters.TimelinePanel.IView Factory.IViewsFactory.CreateTimelinePanelView() => TimelinePanel;

        LogJoint.UI.Presenters.Timeline.IView Factory.IViewsFactory.CreateTimelineView() => Timeline;

        LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateTimelineView() => PostprocesssingTimelineViewProxy;

        LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.IView LogJoint.UI.Presenters.Postprocessing.Factory.IViewsFactory.CreateTimeSeriesView() => null;

        LogJoint.UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.IView Factory.IViewsFactory.CreateWindowsEventsLogFormatView() => null;
    }
    class SystemThemeDetector : LogJoint.UI.Presenters.ISystemThemeDetector
    {
        ColorThemeMode LogJoint.UI.Presenters.ISystemThemeDetector.Mode => ColorThemeMode.Light;
    }
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
            bool IWebContentCacheConfig.IsCachingForcedForHost(string hostName) => true;
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
                    resolve("System.Runtime.dll"),
                    resolve("System.Private.CoreLib.dll"),
                    resolve("netstandard.dll"),
                    resolve("logjoint.model.dll"),
                    resolve("logjoint.model.sdk.dll")
                ));
            }

            IReadOnlyList<MetadataReference> IMetadataReferencesProvider.GetMetadataReferences() => references;
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
                        PersistenceFileSystem = new LogJoint.Wasm.PersistenceFileSystem(
                            (IJSInProcessRuntime)serviceProvider.GetService<IJSRuntime>(),
                            serviceProvider.GetService<JsInterop>().IndexedDB, "userData"),
                        ContentCacheFileSystem = new LogJoint.Wasm.PersistenceFileSystem(
                            (IJSInProcessRuntime)serviceProvider.GetService<IJSRuntime>(),
                            serviceProvider.GetService<JsInterop>().IndexedDB, "contentCache"),
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
            builder.Services.AddSingleton<ViewProxies>(serviceProvider =>
            {
                return new ViewProxies();
            });
            builder.Services.AddSingleton<LogJoint.UI.Presenters.PresentationObjects>(serviceProvider =>
            {
                var model = serviceProvider.GetService<ModelObjects>();
                var jsRuntime = serviceProvider.GetService<IJSRuntime>();
                var viewProxies = serviceProvider.GetService<ViewProxies>();

                var fileDialogs = new FileDialogs(serviceProvider.GetService<JsInterop>());

                var shellOpen = new ShellOpen();

                var presentationObjects = LogJoint.UI.Presenters.Factory.Create(
                    model,
                    new Clipboard(jsRuntime),
                    shellOpen,
                    /*alertPopup=*/null,
                    fileDialogs,
                    /*prompt=*/null,
                    /*aboutConfig=*/null,
                    /*dragDropHandler=*/null,
                    new SystemThemeDetector(),
                    viewProxies
                );

                shellOpen.SetFileEditor(presentationObjects.FileEditor);

                return presentationObjects;
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
                var presentation = wasmHost.Services.GetService<LogJoint.UI.Presenters.PresentationObjects>();
                model.PluginsManager.LoadPlugins(new Extensibility.Application(
                    model.ExpensibilityEntryPoint,
                    presentation.ExpensibilityEntryPoint), string.Join(',', pluginsDirsList), false);
            }

            jsInterop.ChromeExtension.OnOpen += async (sender, evt) =>
            {
                Console.WriteLine("Opening blob id: '{0}', displayName: '{1}'", evt.Id, evt.DisplayName);
                var model = wasmHost.Services.GetService<ModelObjects>();
                using var stream = new MemoryStream();
                using (var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true))
                    writer.Write(evt.LogText);
                stream.Position = 0;
                await model.ExpensibilityEntryPoint.WebContentCache.SetValue(new Uri(evt.Url), stream);
                var task = model.LogSourcesPreprocessings.Preprocess(
                    new[] { model.PreprocessingStepsFactory.CreateLocationTypeDetectionStep(
                        new LogJoint.Preprocessing.PreprocessingStepParams(evt.Url, displayName: evt.DisplayName)) },
                    "Processing file"
                );
                await task;
            };

            await wasmHost.RunAsync();
        }
    }
}

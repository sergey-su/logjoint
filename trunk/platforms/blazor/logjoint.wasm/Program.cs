using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Reflection;
using LogJoint.Wasm.UI;
using System.IO;
using System.IO.Compression;

namespace LogJoint.Wasm
{
    public class Program
    {
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
                BlazorWebContentConfig webContentConfig = new();
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

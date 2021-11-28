using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using LogJoint.Wasm.UI;

namespace LogJoint.Wasm
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            // var fieldsProcessorMetadataReferencesProvider = new MetadataReferencesProvider();

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
                        UserCodeAssemblyProvider = null, // TODO: pass one
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

            WebAssemblyHost wasmHost = builder.Build();

            var jsInterop = wasmHost.Services.GetService<JsInterop>();
            await jsInterop.Init();

            var jsRuntime = wasmHost.Services.GetService<IJSRuntime>();
            // await fieldsProcessorMetadataReferencesProvider.Init(jsRuntime);

            Extensibility.BlazorPluginLoader.LoadPlugins(wasmHost);
            ChromeExtensionIntegration.Init(jsInterop, wasmHost);

            await wasmHost.RunAsync();
        }
    }
}

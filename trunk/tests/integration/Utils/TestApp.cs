﻿using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
    class Mocks : IMocks
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

    class ViewModelObjects
    {
        public UI.Presenters.MainForm.IViewModel MainForm;
        public UI.Presenters.PreprocessingUserInteractions.IViewModel PreprocessingUserInteractions;
        public UI.Presenters.MessagePropertiesDialog.IDialogViewModel MessagePropertiesDialog;
        public UI.Presenters.SourcePropertiesWindow.IViewModel SourcePropertiesWindow;
    };

    public class TestAppConfig
    {
        public int LogViewerViewSize = 20;
        public string LocalPluginsList;
    };

    class TestAppInstance : IContext, IRegistry
    {
        private bool disposed;
        private TraceListener traceListener;
        private readonly Dictionary<Type, object> registry = new Dictionary<Type, object>();
        private readonly IUtils utils;

        public TestAppInstance()
        {
            utils = new TestAppExtensions.UtilsImpl(this);
        }

        public ISynchronizationContext SynchronizationContext { get; private set; }
        public ModelObjects ModelObjects { get; private set; }
        public UI.Presenters.PresentationObjects PresentationObjects { get; private set; }
        public ViewModelObjects ViewModel { get; private set; }
        public Mocks Mocks { get; private set; }
        public ISamples Samples { get; private set; }

        public IModel Model => ModelObjects.ExpensibilityEntryPoint; // logjoint API entry point prop
        public UI.Presenters.IPresentation Presentation => PresentationObjects.ExpensibilityEntryPoint; // logjoint API entry point prop

        /// <summary>
        /// Temporary folder where this instance of application stores its state.
        /// </summary>
        public string AppDataDirectory { get; private set; }
        /// <summary>
        /// Temporary folder where a test can put custom format definitions.
        /// </summary>
        public string TestFormatDirectory { get; private set; }

        IModel IContext.Model => ModelObjects.ExpensibilityEntryPoint;
        UI.Presenters.IPresentation IContext.Presentation => PresentationObjects.ExpensibilityEntryPoint;
        IMocks IContext.Mocks => Mocks;
        IRegistry IContext.Registry => this;
        ISamples IContext.Samples => Samples;
        IUtils IContext.Utils => utils;
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

            InitializeMocks(mocks, viewModel);

            var appDataDir = Path.Combine(Path.GetTempPath(),
                $"logjoint.int.test.workdir.{DateTime.Now:yyyy'-'MM'-'dd'T'HH'-'mm'-'ss'.'fff}");
            var testFormatsDir = Path.Combine(appDataDir, "TestFormats");

            Directory.CreateDirectory(appDataDir);
            Directory.CreateDirectory(testFormatsDir);
            var traceListener = new TraceListener(Path.Combine(appDataDir, "test-debug.log") + ";logical-thread=1");

            ISynchronizationContext serialSynchronizationContext = new SerialSynchronizationContext();

            var app = await serialSynchronizationContext.Invoke(() =>
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
                        TraceListeners = new[] { traceListener },
                        DisableLogjointInstancesCounting = true,
                        AdditionalFormatDirectories = new[] { testFormatsDir },
                        UserCodeAssemblyProvider = new CompilingUserCodeAssemblyProvider(new DefaultMetadataReferencesProvider()),
                    },
                    serialSynchronizationContext,
                    (_1) => mocks.CredentialsCache,
                    (_1, _2, _3) => mocks.WebBrowserDownloader,
                    Substitute.For<Drawing.IMatrixFactory>(), // todo: won't work for SequenceDiagram presenter tests
                    RegularExpressions.FCLRegexFactory.Instance
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

                var loadedMessagesLogViewer = Substitute.For<UI.Presenters.LogViewer.IView>();
                loadedMessagesLogViewer.DisplayLinesPerPage.Returns(config.LogViewerViewSize);
                presentationObjects.ViewModels.LoadedMessages.LogViewer.SetView(loadedMessagesLogViewer);

                var searchResultLogViewer = Substitute.For<UI.Presenters.LogViewer.IView>();
                searchResultLogViewer.DisplayLinesPerPage.Returns(config.LogViewerViewSize);
                presentationObjects.ViewModels.SearchResult.LogViewer.SetView(searchResultLogViewer);

                return new TestAppInstance
                {
                    SynchronizationContext = serialSynchronizationContext,
                    Mocks = mocks,
                    ModelObjects = modelObjects,
                    PresentationObjects = presentationObjects,
                    ViewModel = viewModel,
                    Samples = new SamplesUtils(),
                    traceListener = traceListener,
                    AppDataDirectory = appDataDir,
                    TestFormatDirectory = testFormatsDir,
                };
            });

            if (config.LocalPluginsList != null)
            {
                app.ModelObjects.PluginsManager.LoadPlugins(app, config.LocalPluginsList, preferTestPluginEntryPoints: true);
            }

            return app;
        }

        private static void InitializeMocks(Mocks mocks, ViewModelObjects viewModel)
        {
            mocks.Views.CreateMainFormView().SetViewModel(
                Arg.Do<UI.Presenters.MainForm.IViewModel>(x => viewModel.MainForm = x));

            mocks.Views.CreatePreprocessingView().SetViewModel(
                Arg.Do<UI.Presenters.PreprocessingUserInteractions.IViewModel>(x => viewModel.PreprocessingUserInteractions = x));

            mocks.Views.CreateMessagePropertiesDialogView().CreateDialog(
                Arg.Do<UI.Presenters.MessagePropertiesDialog.IDialogViewModel>(x => viewModel.MessagePropertiesDialog = x));

            mocks.Views.CreateSourcePropertiesWindowView().SetViewModel(
                Arg.Do<UI.Presenters.SourcePropertiesWindow.IViewModel>(x => viewModel.SourcePropertiesWindow = x));
        }

        public async Task Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            var tcs = new TaskCompletionSource<int>();
            await this.ModelObjects.SynchronizationContext.Invoke(() =>
            {
                var mainFormView = Mocks.Views.CreateMainFormView();
                mainFormView.When(x => x.ForceClose()).Do(x => tcs.SetResult(0));
                ViewModel.MainForm.OnClosing();
            });
            await tcs.Task;
            traceListener.Flush();
        }
    };
}

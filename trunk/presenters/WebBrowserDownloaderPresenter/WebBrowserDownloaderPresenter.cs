using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using LogJoint.WebViewTools;
using System.Runtime.InteropServices;

namespace LogJoint.UI.Presenters.WebViewTools
{
    public class Presenter : IPresenter, IViewModel, IWebViewTools
    {
        #region readonly thread-safe objects, no thread sync required to access or invoke
        readonly object syncRoot = new object();
        readonly ISynchronizationContext uiInvokeSynchronization;
        readonly Persistence.IWebContentCache cache;
        readonly LJTraceSource tracer;
        #endregion

        #region data accessed from UI thread only
        readonly IView downloaderForm;
        #endregion

        #region fields that are accessed from UI thread and from random IWebBrowserDownloader's user thread. access synced through syncRoot.
        readonly Queue<PendingTask> tasks = new Queue<PendingTask>();
        PendingTask currentTask;
        BrowserState browserState;
        Timer timer;
        #endregion


        public Presenter(
            IView view,
            ISynchronizationContext uiInvokeSynchronization,
            Persistence.IWebContentCache cache,
            IShutdown shutdown,
            ITraceSourceFactory traceSourceFactory
        )
        {
            this.downloaderForm = view;
            this.uiInvokeSynchronization = uiInvokeSynchronization;
            this.tracer = traceSourceFactory.CreateTraceSource("BrowserDownloader", "web.dl");
            this.cache = cache;

            shutdown.Cleanup += Shutdown;

            downloaderForm.SetViewModel(this);
        }

        async Task<Stream> IWebViewTools.Download(DownloadParams downloadParams)
        {
            if (downloadParams.CacheMode == CacheMode.AllowCacheReading || downloadParams.CacheMode == CacheMode.DownloadFromCacheOnly)
            {
                var cachedValue = await cache.GetValue(downloadParams.Location);
                if (cachedValue != null)
                {
                    tracer.Info("found in cache content for location='{0}'", downloadParams.Location);
                    return cachedValue;
                }
                if (downloadParams.CacheMode == CacheMode.DownloadFromCacheOnly)
                {
                    return null;
                }
            }
            var task = new DownloadTask
            {
                location = downloadParams.Location,
                expectedMimeType = downloadParams.ExpectedMimeType,
                cancellation = downloadParams.Cancellation,
                progress = downloadParams.Progress,
                isLoginUrl = downloadParams.IsLoginUrl,
                stream = new MemoryStream(),
                promise = new TaskCompletionSource<Stream>(),
            };
            lock (syncRoot)
            {
                tracer.Info("new download task {0} added for location='{1}'", task, task.location);
                tasks.Enqueue(task);
            }
            TryTakeNewTask();
            var stream = await task.promise.Task;
            if (stream != null)
            {
                bool setCache = true;
                if (downloadParams.AllowCacheWriting != null)
                {
                    stream.Position = 0;
                    setCache = downloadParams.AllowCacheWriting(stream);
                }
                if (setCache)
                {
                    stream.Position = 0;
                    await cache.SetValue(downloadParams.Location, stream);
                }
                stream.Position = 0;
            }
            return stream;
        }

        async Task<UploadFormResult> IWebViewTools.UploadForm(UploadFormParams uploadFormParams)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new NotImplementedException("Not implemented on Windows");
            }

            var task = new UploadFormTask
            {
                location = uploadFormParams.Location,
                formLocation = uploadFormParams.FormUri,
                cancellation = uploadFormParams.Cancellation,
                promise = new TaskCompletionSource<IReadOnlyList<KeyValuePair<string, string>>>(),
            };
            lock (syncRoot)
            {
                tracer.Info("new form upload task {0} added for location='{1}'", task, task.location);
                tasks.Enqueue(task);
            }
            TryTakeNewTask();
            var values = await task.promise.Task;
            return new UploadFormResult
            {
                Values = values
            };
        }

        #region View events

        bool IViewModel.OnStartDownload(Uri uri)
        {
            lock (syncRoot)
            {
                bool shouldContinue = IsCurrentTaskValid();
                tracer.Info("OnStartDownload. will continue? {0}", shouldContinue ? "yes" : "no");
                if (!shouldContinue)
                    return false;
                if (browserState == BrowserState.Showing)
                    SetBroswerState(BrowserState.Busy);
                uiInvokeSynchronization.Post(() => downloaderForm.Visible = false);
                return true;
            }
        }

        bool IViewModel.OnProgress(int currentValue, int totalSize, string statusText)
        {
            lock (syncRoot)
            {
                bool shouldContinue = IsCurrentTaskValid();
                tracer.Info("OnProgress {1}/{2} {3}. will continue? {0}", shouldContinue ? "yes" : "no", currentValue, totalSize, statusText);
                if (!shouldContinue)
                    return false;
                if (browserState == BrowserState.Showing)
                    SetBroswerState(BrowserState.Busy);
                if (totalSize > 0 && currentValue <= totalSize)
                    SetProgress((double)currentValue / (double)totalSize);
                return true;
            }
        }

        WebViewAction IViewModel.CurrentAction
        {
            get
            {
                lock (syncRoot)
                {
                    return currentTask?.ToAction();
                }
            }
        }

        bool IViewModel.OnDataAvailable(byte[] buffer, int bytesAvailable)
        {
            lock (syncRoot)
            {
                bool shouldContinue = IsCurrentTaskValid();
                tracer.Info("OnDataAvailable {1}. will continue? {0}", shouldContinue ? "yes" : "no", bytesAvailable);
                if (!shouldContinue || !(currentTask is DownloadTask downloadTask))
                    return false;
                if (browserState == BrowserState.Showing)
                    SetBroswerState(BrowserState.Busy);
                downloadTask.stream.Write(buffer, 0, bytesAvailable);
                return true;
            }
        }

        void IViewModel.OnDownloadCompleted(bool success, string statusText)
        {
            lock (syncRoot)
            {
                tracer.Info("OnDownloadCompleted {0}. statusText={1}. current task={2}", success ? "successfully" : "with failure", statusText, currentTask);
                if (currentTask is DownloadTask downloadTask)
                {
                    if (success)
                    {
                        downloadTask.stream.Position = 0;
                        downloadTask.promise.SetResult(downloadTask.stream);
                    }
                    else
                    {
                        downloadTask.promise.SetException(new Exception(statusText ?? "download failed"));
                    }
                }
                currentTask?.Dispose();
                currentTask = null;
            }
            ResetBrowser();
            TryTakeNewTask();
        }

        void IViewModel.OnAborted()
        {
            lock (syncRoot)
            {
                tracer.Info("OnAborted. current task={0}", currentTask);
                if (currentTask != null)
                {
                    currentTask.Reject(new TaskCanceledException());
                    currentTask.Dispose();
                    currentTask = null;
                }
            }
            ResetBrowser();
            TryTakeNewTask();
        }

        void IViewModel.OnBrowserNavigated(Uri url)
        {
            tracer.Info("OnBrowserNavigated {0}", url);
            bool setTimer = false;
            bool clearTimer = false;
            lock (syncRoot)
            {
                if (currentTask is DownloadTask downloadTask)
                {
                    if (downloadTask.isLoginUrl != null && downloadTask.isLoginUrl(url))
                    {
                        setTimer = browserState == BrowserState.Busy;
                        if (setTimer)
                            SetBroswerState(BrowserState.Showing);
                    }
                    else if (browserState == BrowserState.Showing && currentTask.location.Host == url.Host)
                    {
                        clearTimer = true;
                        SetBroswerState(BrowserState.Busy);
                    }
                }
                else if (currentTask is UploadFormTask)
                {
                    setTimer = browserState == BrowserState.Busy;
                    if (setTimer)
                        SetBroswerState(BrowserState.Showing);
                }
                if (setTimer || clearTimer)
                {
                    timer?.Dispose();
                    timer = null;
                }
                if (setTimer)
                {
                    timer = new Timer(_ => OnTimer(),
                        null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
                }
                if (clearTimer)
                {
                    uiInvokeSynchronization.Post(() => downloaderForm.Visible = false);
                }
            }
        }

        void IViewModel.OnFormSubmitted(IReadOnlyList<KeyValuePair<string, string>> values)
        {
            lock (syncRoot)
            {
                tracer.Info("OnFormSubmitted. current task={0}", currentTask);
                if (currentTask is UploadFormTask uploadFormTask)
                {
                    uploadFormTask.promise.SetResult(values);
                    currentTask.Dispose();
                    currentTask = null;
                }
                else
                {
                    return;
                }
            }
            ResetBrowser();
            TryTakeNewTask();
        }

        #endregion

        #region Implementation

        void OnTimer()
        {
            tracer.Info("OnTimer");
            lock (syncRoot)
            {
                if (browserState == BrowserState.Showing)
                {
                    uiInvokeSynchronization.Post(() => downloaderForm.Visible = true);
                }
            }
        }

        bool IsCurrentTaskValid()
        {
            return currentTask != null && !currentTask.cancellation.IsCancellationRequested;
        }

        void ResetBrowser()
        {
            uiInvokeSynchronization.Post(() =>
            {
                downloaderForm.Visible = false;
                downloaderForm.Navigate(new Uri("about:blank"));
            });
            lock (syncRoot)
            {
                timer?.Dispose();
                timer = null;
                SetBroswerState(BrowserState.Ready);
            }
            tracer.Info("browser reset");
        }

        bool TryTakeNewTask()
        {
            Uri navigateTo;
            lock (syncRoot)
            {
                if (currentTask != null || browserState != BrowserState.Ready)
                    return false;
                for (; tasks.Count > 0 && currentTask == null;)
                {
                    var task = tasks.Dequeue();
                    if (!CompletePromiseIfCancellationRequested(task))
                        currentTask = task;
                }
                if (currentTask == null)
                    return false;
                tracer.Info("taking new task {0}", currentTask);
                currentTask.cancellationRegistration = currentTask.cancellation.Register(
                    OnTaskCancelled, useSynchronizationContext: false);
                currentTask.progressSink = currentTask.progress?.CreateProgressSink();
                navigateTo = currentTask.location;
                SetBroswerState(BrowserState.Busy);
            }
            uiInvokeSynchronization.Post(() => downloaderForm.Navigate(navigateTo));
            return true;
        }

        void OnTaskCancelled()
        {
            lock (syncRoot)
            {
                tracer.Info("task cancelled callback received");
                var cpy = tasks.ToArray();
                tasks.Clear();
                foreach (var task in cpy)
                {
                    if (!CompletePromiseIfCancellationRequested(task))
                        tasks.Enqueue(task);
                }
                if (currentTask != null)
                {
                    if (CompletePromiseIfCancellationRequested(currentTask))
                        currentTask = null;
                }
            }
        }

        bool CompletePromiseIfCancellationRequested(PendingTask task)
        {
            if (!task.cancellation.IsCancellationRequested)
                return false;
            tracer.Info("completing task {0} with TaskCanceledException as its cancellation was requested", task);
            task.Reject(new TaskCanceledException());
            task.Dispose();
            return true;
        }

        void SetBroswerState(BrowserState value)
        {
            tracer.Info("browser state -> {0}", value);
            browserState = value;
        }

        void SetProgress(double value)
        {
            lock (syncRoot)
            {
                if (currentTask != null && currentTask.progressSink != null)
                {
                    currentTask.progressSink.SetValue(value);
                }
            }
        }

        void Shutdown(object sender, EventArgs e)
        {
            lock (syncRoot)
            {
                while (tasks.Count > 0)
                {
                    var t = tasks.Dequeue();
                    tracer.Info("cancelling pending task {0}", t);
                    t.Reject(new TaskCanceledException());
                    t.Dispose();
                }
            }
        }


        #endregion


        #region Helper types

        abstract class PendingTask
        {
            public Uri location;
            public CancellationToken cancellation;
            public CancellationTokenRegistration? cancellationRegistration;
            public Progress.IProgressAggregator progress;
            public Progress.IProgressEventsSink progressSink;

            public virtual void Dispose()
            {
                if (cancellationRegistration.HasValue)
                    cancellationRegistration.Value.Dispose();
                progressSink?.Dispose();
            }

            public abstract WebViewAction ToAction();
            public abstract void Reject(Exception exception);

            public override string ToString()
            {
                return string.Format("{0:x}", this.GetHashCode());
            }
        };

        class DownloadTask : PendingTask
        {
            public string expectedMimeType;
            public MemoryStream stream;
            public TaskCompletionSource<Stream> promise;
            public Predicate<Uri> isLoginUrl;

            public override WebViewAction ToAction()
            {
                return new WebViewAction
                {
                    Type = WebViewActionType.Download,
                    Uri = location,
                    MimeType = expectedMimeType,
                };
            }

            public override void Reject(Exception exception)
            {
                promise.TrySetException(exception);
            }
        };

        class UploadFormTask : PendingTask
        {
            public Uri formLocation;
            public TaskCompletionSource<IReadOnlyList<KeyValuePair<string, string>>> promise;

            public override WebViewAction ToAction()
            {
                return new WebViewAction
                {
                    Type = WebViewActionType.UploadForm,
                    Uri = formLocation,
                };
            }

            public override void Reject(Exception exception)
            {
                promise.TrySetException(exception);
            }
        };

        enum BrowserState
        {
            Ready,
            Busy,
            Showing
        };

        #endregion
    };
};

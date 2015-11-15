using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace LogJoint.UI.Presenters.WebBrowserDownloader
{
	public class Presenter : IPresenter, IViewEvents
	{
		#region readonly thread-safe objects, no thread sync required to access or invoke
		readonly object syncRoot = new object();
		readonly IInvokeSynchronization uiInvokeSynchronization;
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
		Task downloaderFormNavigationTask;
		#endregion


		public Presenter(
			IView view,
			IInvokeSynchronization uiInvokeSynchronization,
			Persistence.IWebContentCache cache
		)
		{
			this.downloaderForm = view;
			this.uiInvokeSynchronization = uiInvokeSynchronization;
			this.tracer = new LJTraceSource("BrowserDownloader", "web.dl");
			this.cache = cache;

			downloaderForm.SetEventsHandler(this);
		}

		#region IPresenter

		async Task<Stream> IPresenter.Download(Uri uri, CancellationToken cancellation)
		{
			var cachedValue = cache.GetValue(uri);
			if (cachedValue != null)
				return cachedValue;
			var task = new PendingTask()
			{
				location = uri,
				cancellation = cancellation,
				stream = new MemoryStream(),
				promise = new TaskCompletionSource<Stream>()
			};
			lock (syncRoot)
			{
				tracer.Info("new task {0} added for location='{1}'", task, task.location);
				tasks.Enqueue(task);
			}
			TryTakeNewTask();
			var stream = await task.promise.Task;
			if (stream != null)
			{
				await cache.SetValue(uri, stream);
				stream.Position = 0;
			}
			return stream;
		}


		#endregion

		#region View events

		bool IViewEvents.OnStartDownload(Uri uri)
		{
			bool shouldContinue = ShouldContinueDownloading();
			tracer.Info("OnStartDownload. will continue? {0}", shouldContinue ? "yes" : "no");
			if (!shouldContinue)
				return false;
			if (browserState == BrowserState.Showing)
				SetBroswerState(BrowserState.Busy);
			downloaderForm.Visible = false;
			return true;
		}

		bool IViewEvents.OnProgress(int currentValue, int totalSize, string statusText)
		{
			bool shouldContinue = ShouldContinueDownloading();
			tracer.Info("OnProgress {1}/{2} {3}. will continue? {0}", shouldContinue ? "yes" : "no", currentValue, totalSize, statusText);
			if (!shouldContinue)
				return false;
			if (browserState == BrowserState.Showing)
				SetBroswerState(BrowserState.Busy);
			return true;
		}

		void IViewEvents.OnDecideOnMIMEType(string mimeType, ref bool download)
		{
			download = mimeType == "application/json"; // todo: do not hard-code expected mime type to download
		}

		bool IViewEvents.OnDataAvailable(byte[] buffer, int bytesAvailable)
		{
			lock (syncRoot)
			{
				bool shouldContinue = ShouldContinueDownloading();
				tracer.Info("OnDataAvailable {1}. will continue? {0}", shouldContinue ? "yes" : "no", bytesAvailable);
				if (!shouldContinue)
					return false;
				if (browserState == BrowserState.Showing)
					SetBroswerState(BrowserState.Busy);
				currentTask.stream.Write(buffer, 0, bytesAvailable);
				return true;
			}
		}

		void IViewEvents.OnDownloadCompleted(bool success, string statusText)
		{
			lock (syncRoot)
			{
				tracer.Info("OnDownloadCompleted {0}. statusText={1}. current task={2}", success ? "successfully" : "with failure", statusText, currentTask);
				if (currentTask != null)
				{
					if (success)
					{
						currentTask.stream.Position = 0;
						currentTask.promise.SetResult(currentTask.stream);
					}
					else
					{
						currentTask.promise.SetException(new Exception(statusText ?? "download failed"));
					}
					currentTask.Dispose();
					currentTask = null;
				}
			}
			ResetBrowser();
			TryTakeNewTask();
		}

		void IViewEvents.OnAborted()
		{
			lock (syncRoot)
			{
				tracer.Info("OnAborted. current task={0}", currentTask);
				if (currentTask != null)
				{
					currentTask.promise.SetException(new TaskCanceledException());
					currentTask.Dispose();
					currentTask = null;
				}
			}
			ResetBrowser();
			TryTakeNewTask();
		}

		void IViewEvents.OnBrowserNavigated(Uri url)
		{
			tracer.Info("OnBrowserNavigated {0}", url);
			if (url.ToString().ToLower().Contains("adfs"))
			{
				bool setTimer;
				lock (syncRoot)
				{
					setTimer = browserState == BrowserState.Busy;
					if (setTimer)
						SetBroswerState(BrowserState.Showing);
				}
				if (setTimer)
				{
					downloaderForm.SetTimer(TimeSpan.FromSeconds(5));
				}
			}
		}

		void IViewEvents.OnTimer()
		{
			tracer.Info("OnTimer");
			if (browserState == BrowserState.Showing)
				downloaderForm.Visible = true;
		}

		#endregion

		#region Implementation

		bool ShouldContinueDownloading()
		{
			lock (syncRoot)
			{
				return currentTask != null && !currentTask.cancellation.IsCancellationRequested;
			}
		}

		void ResetBrowser()
		{
			downloaderForm.Visible = false;
			downloaderForm.Navigate(new Uri("about:blank"));
			downloaderForm.SetTimer(null);
			lock (syncRoot)
			{
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
				for (; tasks.Count > 0 && currentTask == null; )
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
				navigateTo = currentTask.location;
				SetBroswerState(BrowserState.Busy);
			}
			downloaderFormNavigationTask = uiInvokeSynchronization.Invoke(() => downloaderForm.Navigate(navigateTo));
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
			task.promise.SetException(new TaskCanceledException());
			task.Dispose();
			return true;
		}

		void SetBroswerState(BrowserState value)
		{
			tracer.Info("browser state -> {0}", value);
			browserState = value;
		}


		#endregion


		#region Helper types

		class PendingTask
		{
			public Uri location;
			public CancellationToken cancellation;
			public MemoryStream stream;
			public TaskCompletionSource<Stream> promise;
			public CancellationTokenRegistration? cancellationRegistration;

			public void Dispose()
			{
				if (cancellationRegistration.HasValue)
					cancellationRegistration.Value.Dispose();
			}

			public override string ToString()
			{
				return string.Format("{0:x}", this.GetHashCode());
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
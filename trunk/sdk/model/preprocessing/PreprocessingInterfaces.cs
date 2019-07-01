using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.MRU;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;

namespace LogJoint.Preprocessing
{
	public interface ILogSourcesPreprocessingManager
	{
		IEnumerable<ILogSourcePreprocessing> Items { get; }
		Task<YieldedProvider[]> Preprocess(IEnumerable<IPreprocessingStep> steps, string preprocessingDisplayName, PreprocessingOptions options = PreprocessingOptions.None);
		Task<YieldedProvider[]> Preprocess(IRecentlyUsedEntity recentLogEntry, PreprocessingOptions options = PreprocessingOptions.None);
		bool ConnectionRequiresDownloadPreprocessing(IConnectionParams connectParams);
		string ExtractContentsContainerNameFromConnectionParams(IConnectionParams connectParams);
		string ExtractCopyablePathFromConnectionParams(IConnectionParams connectParams);
		string ExtractUserBrowsableFileLocationFromConnectionParams(IConnectionParams connectParams);
		IConnectionParams AppendStep(IConnectionParams connectParams, string stepName, string stepArgument = null);

		/// <summary>
		/// Raised when new preprocessing object added to LogSourcesPreprocessingManager.
		/// That usually happens when one calls Preprocess().
		/// </summary>
		event EventHandler<LogSourcePreprocessingEventArg> PreprocessingAdded;
		/// <summary>
		/// Raised when preprocessing object gets disposed and deleted from LogSourcesPreprocessingManager.
		/// Preprocessing object deletes itself automatically when it finishes. 
		/// This event is called through IInvokeSynchronization passed to 
		/// LogSourcesPreprocessingManager's constructor.
		/// </summary>
		event EventHandler<LogSourcePreprocessingEventArg> PreprocessingDisposed;
		/// <summary>
		/// Raised when properties of one of ILogSourcePreprocessing objects changed. 
		/// Note: This event is raised in worker thread.
		/// That's for optimization purposes: PreprocessingChangedAsync can be raised very often and we didn't 
		/// want invocation queue to be spammed.
		/// </summary>
		event EventHandler<LogSourcePreprocessingEventArg> PreprocessingChangedAsync;
		/// <summary>
		/// Raised when preprocessing has resulted to a new log source
		/// </summary>
		event EventHandler<YieldedProvider> ProviderYielded;
		/// <summary>
		/// Preprocessing finished and yielded no logs
		/// </summary>
		event EventHandler<LogSourcePreprocessingWillYieldEventArg> PreprocessingWillYieldProviders;
		event EventHandler<LogSourcePreprocessingFailedEventArg> PreprocessingYieldFailed;
	};

	public struct YieldedProvider
	{
		public ILogProviderFactory Factory;
		public IConnectionParams ConnectionParams;
		public string DisplayName;
		public bool IsHiddenLog;

		public YieldedProvider(ILogProviderFactory factory, IConnectionParams connectionParams, string displayName, bool isHiddenLog)
		{
			Factory = factory;
			ConnectionParams = connectionParams;
			DisplayName = displayName;
			IsHiddenLog = isHiddenLog;
		}
	};

	public class LogSourcePreprocessingEventArg : EventArgs
	{
		public ILogSourcePreprocessing LogSourcePreprocessing { get; private set; }

		public LogSourcePreprocessingEventArg(ILogSourcePreprocessing lsp)
		{
			this.LogSourcePreprocessing = lsp;
		}
	};

	public class LogSourcePreprocessingFailedEventArg : LogSourcePreprocessingEventArg
	{
		public IReadOnlyList<YieldedProvider> FailedProviders { get; private set; }

		public LogSourcePreprocessingFailedEventArg(ILogSourcePreprocessing lsp, IReadOnlyList<YieldedProvider> failedProviders) : base(lsp)
		{
			FailedProviders = failedProviders;
		}
	};

	public class LogSourcePreprocessingWillYieldEventArg : LogSourcePreprocessingEventArg
	{
		private readonly bool[] disallowed;
		private readonly List<Task> postponeTasks;

		public IReadOnlyList<YieldedProvider> Providers { get; private set; }
		public void SetIsAllowed(int providerIdx, bool value) => disallowed[providerIdx] = !value;
		public bool IsAllowed(int providerIdx) => !disallowed[providerIdx];

		public void PostponeUntilCompleted(Task task)
		{
			postponeTasks.Add(task);
		}

		public LogSourcePreprocessingWillYieldEventArg(
			ILogSourcePreprocessing lsp,
			IReadOnlyList<YieldedProvider> providers,
			List<Task> postponeTasks
		) : base(lsp)
		{
			this.Providers = providers;
			this.disallowed = new bool[providers.Count];
			this.postponeTasks = postponeTasks;
		}
	};

	public interface ILogSourcePreprocessing
	{
		string DisplayName { get; }
		string CurrentStepDescription { get; }
		Exception Failure { get; }
		bool IsDisposed { get; }
		PreprocessingOptions Flags { get; }
		Task Dispose();
	};

	public interface ICredentialsCache
	{
		NetworkCredential QueryCredentials(Uri site, string authType);
		void InvalidateCredentialsCache(Uri site, string authType);
	}

	/// <summary>
	/// A callback interface for a preprocessing step.
	/// This callback object is valid only during the execution of IPreprocessingStep's methods.
	/// </summary>
	public interface IPreprocessingStepCallback
	{
		void YieldLogProvider(YieldedProvider provider);
		void YieldChildPreprocessing(IRecentlyUsedEntity log, bool makeHiddenLog);
		void YieldNextStep(IPreprocessingStep step);
		Task<PreprocessingStepParams> ReplayHistory(ImmutableArray<PreprocessingHistoryItem> history);
		/// <summary>
		/// await on the returned Awaitable to schedule the rest of your IPreprocessingStep's method
		/// for execution in the thread-pool. All subsequent await-able calls will also be done in
		/// the default (threadpool-based) synchronization context.
		/// </summary>
		ConfiguredTaskAwaitable BecomeLongRunning();
		/// <summary>
		/// Use this cancellation token to check whether your long preprocessing step should be interrupted.
		/// </summary>
		CancellationToken Cancellation { get; }
		ITempFilesManager TempFilesManager { get; }
		ITempFilesCleanupList TempFilesCleanupList { get; }
		IFormatAutodetect FormatAutodetect { get; }
		/// <summary>
		/// Trace source shared by all preprocessing steps spawned by their root preprocessing task.
		/// </summary>
		LJTraceSource Trace { get; }
		/// <summary>
		/// Updates user-visible description of your running preprocessing step.
		/// </summary>
		void SetStepDescription(string desc);
		ISharedValueLease<T> GetOrAddSharedValue<T>(string key, Func<T> valueFactory) where T : IDisposable;
		void SetOption(PreprocessingOptions opt, bool value);
		ILogSourcePreprocessing Owner { get; }
	};

	public interface IPreprocessingStep
	{
		Task Execute(IPreprocessingStepCallback callback);
		Task<PreprocessingStepParams> ExecuteLoadedStep(IPreprocessingStepCallback callback);
	};

	public interface IGetPreprocessingStep: IPreprocessingStep
	{
		string GetContentsContainerName(string param);
		string GetContentsUrl(string param);
	};

	public interface IUnpackPreprocessingStep: IPreprocessingStep
	{
	};

	public interface IDownloadPreprocessingStep: IPreprocessingStep
	{
	};


	public class PreprocessingStepParams
	{
		/// <summary>
		/// Location where actual input data can be found by a preprocessing step.
		/// The nature of the location depends on the preprocessing step type.
		/// Most steps require local file location. Downloading step requires a url.
		/// </summary>
		public string Location { get; private set; }
		public string FullPath { get; private set; }
		public ImmutableList<PreprocessingHistoryItem> PreprocessingHistory { get; private set; }
		public string DisplayName { get; private set; }
		public string Argument { get; private set; }

		internal const string DefaultStepName = "get";

		public PreprocessingStepParams(
			string location,
			string fullPath,
			ImmutableList<PreprocessingHistoryItem> history = null,
			string displayName = null,
			string argument = null
		)
		{
			PreprocessingHistory = history ?? ImmutableList<PreprocessingHistoryItem>.Empty;
			Location = location;
			FullPath = fullPath;
			DisplayName = displayName;
			Argument = argument;
		}

		public PreprocessingStepParams(string originalSource)
		{
			PreprocessingHistory = ImmutableList.Create(new PreprocessingHistoryItem(DefaultStepName, originalSource));
			Location = originalSource;
			FullPath = originalSource;
		}
	};

	public class PreprocessingHistoryItem
	{
		public string StepName { get; private set; }
		public string Argument { get; private set; }

		public PreprocessingHistoryItem(string name, string argument = null)
		{
			StepName = !string.IsNullOrEmpty(name) ? name : throw new ArgumentException(nameof(name));
			Argument = !string.IsNullOrEmpty(argument) ? argument : null;
		}

		public static bool TryParse(string str, out PreprocessingHistoryItem ret)
		{
			str = str.Trim();
			if (str.Length != 0)
			{
				int idx = str.IndexOf(' ');
				if (idx == -1)
					ret = new PreprocessingHistoryItem(str);
				else
					ret = new PreprocessingHistoryItem(str.Substring(0, idx), str.Substring(idx + 1));
				return true;
			}
			ret = null;
			return false;
		}

		public override string ToString()
		{
			return Argument != null ? $"{StepName} {Argument}" : $"{StepName}";
		}
	};

	/// <summary>
	/// When preprocessing step fails with this exception 
	/// it will not be reported to telemetry.
	/// Expected errors reported to user as well as any other failures.
	/// </summary>
	public class ExpectedErrorException : AggregateException
	{
		public ExpectedErrorException(Exception inner): base(new [] {inner})
		{
		}
	};

	public interface ISharedValueLease<out T> : IDisposable where T : IDisposable
	{
		T Value { get; }
		bool IsValueCreator { get; }
		void KeepAlive(TimeSpan ttl);
	};

	public interface IPreprocessingStepsFactory
	{
		IPreprocessingStep CreateFormatDetectionStep(PreprocessingStepParams p);
		IPreprocessingStep CreateDownloadingStep(PreprocessingStepParams p);
		IPreprocessingStep CreateUnpackingStep(PreprocessingStepParams p);
		IPreprocessingStep CreateURLTypeDetectionStep(PreprocessingStepParams p);
		IPreprocessingStep CreateOpenWorkspaceStep(PreprocessingStepParams p);
		IPreprocessingStep CreateLocationTypeDetectionStep(PreprocessingStepParams p);
		IPreprocessingStep CreateGunzippingStep(PreprocessingStepParams sourceFile);
		IPreprocessingStep CreateTimeAnomalyFixingStep(PreprocessingStepParams p);
		IPreprocessingStep CreateUntarStep(PreprocessingStepParams p);
	};

	public interface IPreprocessingManagerExtension
	{
		IPreprocessingStep DetectFormat(PreprocessingStepParams param, IStreamHeader header);
		IPreprocessingStep CreateStepByName(string stepName, PreprocessingStepParams stepParams);
		IPreprocessingStep TryParseLaunchUri(Uri url);
		Task FinalizePreprocessing(IPreprocessingStepCallback callback);
	};

	public interface IPreprocessingManagerExtensionsRegistry
	{
		IEnumerable<IPreprocessingManagerExtension> Items { get; }
		void Register(IPreprocessingManagerExtension detector);
		void AddLogDownloaderRule(Uri uri, LogDownloaderRule rule);
	};

	/// <summary>
	/// Provides access to first bytes of the stream.
	/// Used to detect file format by looking at the header.
	/// </summary>
	public interface IStreamHeader
	{
		/// <summary>
		/// First 1024 bytes of the stream.
		/// Array is smaller is the stream is smaller than 1024.
		/// </summary>
		byte[] Header { get; }
	};

	[Flags]
	public enum PreprocessingOptions
	{
		None = 0,
		Silent = 1,
		HighlightNewPreprocessing = 2,
		MakeLogHidden = 4,
	};

	public class LogDownloaderRule
	{
		public bool UseWebBrowserDownloader { get; private set; }
		public string ExpectedMimeType { get; private set; }
		public IReadOnlyList<string> LoginUrls { get; private set; }

		public static LogDownloaderRule CreateBrowserDownloaderRule(IEnumerable<string> loginUrls, string expectedMimeType = null)
		{
			var result = new LogDownloaderRule(true, loginUrls, expectedMimeType);
			if (result.LoginUrls.Count == 0)
				throw new ArgumentException("At least one login URL has to be provider", nameof(loginUrls));
			return result;
		}

		public static LogDownloaderRule CreatePlainHttpDownloaderRule()
		{
			return plainHttpDownloaderRule;
		}

		internal LogDownloaderRule(bool useWebBrowserDownloader, IEnumerable<string> loginUrls, string expectedMimeType)
		{
			UseWebBrowserDownloader = useWebBrowserDownloader;
			LoginUrls = ImmutableArray.CreateRange(loginUrls);
			ExpectedMimeType = expectedMimeType;
		}

		private static readonly LogDownloaderRule plainHttpDownloaderRule = new LogDownloaderRule(false, Enumerable.Empty<string>(), null);
	};
}

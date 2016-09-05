using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.MRU;
using System.Runtime.CompilerServices;

namespace LogJoint.Preprocessing
{
	public interface ILogSourcesPreprocessingManager
	{
		void SetUserRequestsHandler(IPreprocessingUserRequests userRequests);
		IEnumerable<ILogSourcePreprocessing> Items { get; }
		Task<YieldedProvider[]> Preprocess(IEnumerable<IPreprocessingStep> steps, string preprocessingDisplayName, PreprocessingOptions options = PreprocessingOptions.None);
		Task<YieldedProvider[]> Preprocess(RecentLogEntry recentLogEntry, bool makeHiddenLog);
		bool ConnectionRequiresDownloadPreprocessing(IConnectionParams connectParams);
		string ExtractContentsContainerNameFromConnectionParams(IConnectionParams connectParams);
		string ExtractCopyablePathFromConnectionParams(IConnectionParams connectParams);
		string ExtractUserBrowsableFileLocationFromConnectionParams(IConnectionParams connectParams);
		IConnectionParams AppendReorderingStep(IConnectionParams connectParams, ILogProviderFactory sourceFormatFactory);

		/// <summary>
		/// Raised when new preprocessing object added to LogSourcesPreprocessingManager.
		/// That usually happens when one calls Preprocess().
		/// </summary>
		event EventHandler<LogSourcePreprocessingEventArg> PreprocessingAdded;
		/// <summary>
		/// Raised when preprocessing object gets disposed and deleted from LogSourcesPreprocessingManager.
		/// Preprocessing object deletes itself automatically when it finishes. 
		/// This event is called throught IInvokeSynchronization passed to 
		/// LogSourcesPreprocessingManager's constructor.
		/// </summary>
		event EventHandler<LogSourcePreprocessingEventArg> PreprocessingDisposed;
		/// <summary>
		/// Raised when properties of one of ILogSourcePreprocessing objects changed. 
		/// Note: This event is raised in worker thread.
		/// That's for optimization purposes: PreprocessingChangedAsync can be raised very often and we we din't 
		/// want invocation queue to be spammed.
		/// </summary>
		event EventHandler<LogSourcePreprocessingEventArg> PreprocessingChangedAsync;
		/// <summary>
		/// Raised when preprocessing has resulted to a new log source
		/// </summary>
		event EventHandler<YieldedProvider> ProviderYielded;
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

	public interface ILogSourcePreprocessing
	{
		string CurrentStepDescription { get; }
		Exception Failure { get; }
		bool IsDisposed { get; }
		PreprocessingOptions Flags { get; }
		Task Dispose();
	};

	public interface IPreprocessingUserRequests
	{
		bool[] SelectItems(string prompt, string[] items);
		void NotifyUserAboutIneffectivePreprocessing(string notificationSource);
		void NotifyUserAboutPreprocessingFailure(string notificationSource, string message);
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
		void YieldChildPreprocessing(RecentLogEntry log, bool makeHiddenLog);
		void YieldNextStep(IPreprocessingStep step);
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
		/// Updates user-visible descritpion of your running preprocessing step.
		/// </summary>
		void SetStepDescription(string desc);
		ISharedValueLease<T> GetOrAddSharedValue<T>(string key, Func<T> valueFactory) where T : IDisposable;
	};

	public interface IPreprocessingStep
	{
		Task Execute(IPreprocessingStepCallback callback);
		Task<PreprocessingStepParams> ExecuteLoadedStep(IPreprocessingStepCallback callback, string param);
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
		public readonly string Uri;
		public readonly string FullPath;
		public readonly string[] PreprocessingSteps;
		public readonly string DisplayName;
		public const string DefaultStepName = "get";

		public PreprocessingStepParams(string uri, string fullPath, IEnumerable<string> steps = null, string displayName = null)
		{
			PreprocessingSteps = (steps ?? Enumerable.Empty<string>()).ToArray();
			Uri = uri;
			FullPath = fullPath;
			DisplayName = displayName;
		}
		public PreprocessingStepParams(string originalSource)
		{
			PreprocessingSteps = new string[] { string.Format("{0} {1}", DefaultStepName, originalSource) };
			Uri = originalSource;
			FullPath = originalSource;
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
	};

	public interface IPreprocessingManagerExtension
	{
		IPreprocessingStep DetectFormat(PreprocessingStepParams param, IStreamHeader header);
		IPreprocessingStep CreateStepByName(string stepName, PreprocessingStepParams stepParams);
		IPreprocessingStep TryParseLaunchUri(Uri url);
	};

	public interface IPreprocessingManagerExtensionsRegistry
	{
		IEnumerable<IPreprocessingManagerExtension> Items { get; }
		void Register(IPreprocessingManagerExtension detector);
	};

	public interface IStreamHeader
	{
		byte[] Header { get; }
	};

	[Flags]
	public enum PreprocessingOptions
	{
		None = 0,
		SkipLogsSelectionDialog = 1,
		HighlightNewPreprocessing = 2,
	};
}

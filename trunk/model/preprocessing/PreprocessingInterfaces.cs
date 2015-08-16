using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.MRU;

namespace LogJoint.Preprocessing
{
	public interface ILogSourcesPreprocessingManager
	{
		void SetUserRequestsHandler(IPreprocessingUserRequests userRequests);
		IEnumerable<ILogSourcePreprocessing> Items { get; }
		Task Preprocess(IEnumerable<IPreprocessingStep> steps, string preprocessingDisplayName, PreprocessingOptions options = PreprocessingOptions.None);
		Task Preprocess(RecentLogEntry recentLogEntry, bool makeHiddenLog);
		bool ConnectionRequiresDownloadPreprocessing(IConnectionParams connectParams);

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
	};

	public interface ILogSourcePreprocessing : IDisposable
	{
		string CurrentStepDescription { get; }
		Exception Failure { get; }
		bool IsDisposed { get; }
	};

	public interface IPreprocessingUserRequests
	{
		NetworkCredential QueryCredentials(Uri site, string authType);
		void InvalidateCredentialsCache(Uri site, string authType);
		bool[] SelectItems(string prompt, string[] items);
		void NotifyUserAboutIneffectivePreprocessing(string notificationSource);
		void NotifyUserAboutPreprocessingFailure(string notificationSource, string message);
	};

	public interface IPreprocessingStepCallback
	{
		void YieldLogProvider(ILogProviderFactory providerFactory, IConnectionParams providerConnectionParams, string displayName, bool makeHiddenLog);
		void YieldChildPreprocessing(RecentLogEntry recentLogEntry, bool makeHiddenLog);
		void BecomeLongRunning();
		CancellationToken Cancellation { get; }
		ITempFilesManager TempFilesManager { get; }
		IFormatAutodetect FormatAutodetect { get; }
		IPreprocessingUserRequests UserRequests { get; }
		LJTraceSource Trace { get; }
		void SetStepDescription(string desc);
		ISharedValueLease<T> GetOrAddSharedValue<T>(string key, Func<T> valueFactory) where T : IDisposable;
	};

	public interface IPreprocessingStep
	{
		IEnumerable<IPreprocessingStep> Execute(IPreprocessingStepCallback callback);
		PreprocessingStepParams ExecuteLoadedStep(IPreprocessingStepCallback callback, string param);
	};

	public class PreprocessingStepParams
	{
		public readonly string Uri;
		public readonly string FullPath;
		public readonly string[] PreprocessingSteps;
		public const string DefaultStepName = "get";

		public PreprocessingStepParams(string uri, string fullPath, IEnumerable<string> steps = null)
		{
			PreprocessingSteps = (steps ?? Enumerable.Empty<string>()).ToArray();
			Uri = uri;
			FullPath = fullPath;
		}
		public PreprocessingStepParams(string originalSource)
		{
			PreprocessingSteps = new string[] { string.Format("{0} {1}", DefaultStepName, originalSource) };
			Uri = originalSource;
			FullPath = originalSource;
		}
	};

	public interface ISharedValueLease<out T> : IDisposable where T : IDisposable
	{
		T Value { get; }
		bool IsValueCreator { get; }
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
	};

	public interface IPreprocessingManagerExtension
	{
		IPreprocessingStep DetectFormat(PreprocessingStepParams param, IStreamHeader header);
		IPreprocessingStep CreateStepByName(string stepName, PreprocessingStepParams stepParams);
		bool IsDownloadingStep(string stepName);
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
		SkipLogsSelectionDialog = 1
	};
}

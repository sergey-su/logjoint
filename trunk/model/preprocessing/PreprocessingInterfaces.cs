using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{
	public interface ILogSourcePreprocessing : IDisposable
	{
		string CurrentStepDescription { get; }
		Exception Failure { get; }
		bool IsDisposed { get; }
	};

	public interface IPreprocessingUserRequests
	{
		NetworkCredential QueryCredentials(Uri site, string authType);
		void InvalidCredentials(Uri site, string authType);
		bool[] SelectItems(string prompt, string[] items);
	};

	public interface IPreprocessingStepCallback
	{
		void YieldLogProvider(ILogProviderFactory providerFactory, IConnectionParams providerConnectionParams, string displayName);
		void BecomeLongRunning();
		bool IsCancellationRequested { get; }
		WaitHandle CancellationEvent { get; }
		ITempFilesManager TempFilesManager { get; }
		IFormatAutodetect FormatAutodetect { get; }
		IPreprocessingUserRequests UserRequests { get; }
		LJTraceSource Trace { get; }
		void SetStepDescription(string desc);
	};

	public interface IPreprocessingStep
	{
		IEnumerable<IPreprocessingStep> Execute(IPreprocessingStepCallback callback);
	};

	internal class PreprocessingStepParams
	{
		public readonly string Uri;
		public readonly string FullPath;
		public readonly string[] PreprocessingSteps;

		public PreprocessingStepParams(string uri, string fullPath, IEnumerable<string> steps = null)
		{
			PreprocessingSteps = (steps ?? Enumerable.Empty<string>()).ToArray();
			Uri = uri;
			FullPath = fullPath;
		}
		public PreprocessingStepParams(string originalSource)
		{
			PreprocessingSteps = new string[] {string.Format("get {0}", originalSource)};
			Uri = originalSource;
			FullPath = originalSource;
		}
	};

	public static class Utils
	{
		public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, TSource value)
		{
			return Enumerable.Concat(first, Enumerable.Repeat(value, 1));
		}
		internal static void DumpPreprocessingParamsToConnectionParams(PreprocessingStepParams prepParams, IConnectionParams connectParams)
		{
			int stepIdx = 0;
			foreach (var step in prepParams.PreprocessingSteps)
			{
				connectParams[string.Format("{0}{1}", ConnectionParamsUtils.PreprocessingStepParamPrefix, stepIdx)] = step;
				++stepIdx;
			}
			connectParams[ConnectionParamsUtils.IdentityConnectionParam] = prepParams.FullPath;
		}
	};
}

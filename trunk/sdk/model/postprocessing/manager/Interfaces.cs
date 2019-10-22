using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
	/// <summary>
	/// For each log source of supported type manages its postprocessing results.
	/// Postprocessing results include the status of last postprocessor's run,
	/// the postprocessor's output, postprocessing error if any.
	/// </summary>
	public interface IManager
	{
		void RegisterLogType(LogSourceMetadata meta); // todo: rename Register
		void Register(ILogPartTokenFactory logPartFactory);
		void Register(Correlation.ISameNodeDetectionTokenFactory factory);
		event EventHandler Changed; // todo: remove
		IEnumerable<LogSourcePostprocessorOutput> LogSourcePostprocessorsOutputs { get; } // todo: return immutable
		IEnumerable<ILogSource> KnownLogSources { get; } // todo: return immutable
		IEnumerable<LogSourceMetadata> KnownLogTypes { get; } // todo: return immutable
		Task<bool> RunPostprocessor(
			KeyValuePair<ILogSourcePostprocessor, ILogSource>[] forLogSources,
			object customData = null
		);
	};

	/// <summary>
	/// Contains postprocessing-related meta information for a given log source type.
	/// </summary>
	public class LogSourceMetadata
	{
		public ILogSourcePostprocessor[] SupportedPostprocessors { get; private set; }
		public ILogProviderFactory LogProviderFactory { get; private set; }

		public LogSourceMetadata(ILogProviderFactory logProviderFactory, params ILogSourcePostprocessor[] supportedPostprocessors)
		{
			this.LogProviderFactory = logProviderFactory;
			this.SupportedPostprocessors = supportedPostprocessors;
		}
	};

	/// <summary>
	/// A log can be postprocessed by different kinds of postprocessors.
	/// This class contains meta information about postprocessor type.
	/// </summary>
	public interface ILogSourcePostprocessor
	{
		PostprocessorKind Kind { get; }
		Task<IPostprocessorRunSummary> Run(LogSourcePostprocessorInput[] forLogs);
	};

	/// <summary>
	/// Result of a log postprocessor
	/// </summary>
	public struct LogSourcePostprocessorOutput
	{
		public ILogSource LogSource;
		public LogSourceMetadata LogSourceMeta;
		public ILogSourcePostprocessor PostprocessorMetadata;
		public enum Status
		{
			NeverRun,
			InProgress,
			Loading,
			Finished,
			Failed,
			Outdated,
		};
		public Status OutputStatus;
		public IPostprocessorRunSummary LastRunSummary;
		public object OutputData;
		public double? Progress;
	};

	public struct LogSourcePostprocessorInput
	{
		public ILogSource LogSource;
		public string LogFileName;
		public string OutputFileName;
		public CancellationToken CancellationToken;
		public Action<double> ProgressHandler;
		public Progress.IProgressAggregator ProgressAggregator;
		public ICodepathTracker TemplatesTracker;
		public string InputContentsEtag;
		public object CustomData;
	};

	public struct LogSourcePostprocessorDeserializationParams
	{
		public XmlReader Reader;
		public ILogSource LogSource;
		public CancellationToken Cancellation;
	};

	public enum PostprocessorKind
	{
		StateInspector,
		Timeline,
		SequenceDiagram,
		Correlator,
		TimeSeries,
	};

	public interface IPostprocessorOutputETag
	{
		string ETag { get; }
 	};
 
}

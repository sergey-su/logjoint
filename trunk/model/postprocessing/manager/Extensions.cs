using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogJoint.Analytics;

namespace LogJoint.Postprocessing
{
	public static class PostprocessorsManagerExtensions
	{
		public static Dictionary<ILogSource, LogSourceNames> GetSourcesSequenceDiagramNames(
			this ILogSourceNamesProvider logSourceNamesProvider,
			IEnumerable<ILogSource> sources,
			Dictionary<ILogSource, LogSourceNames> suggestedNames = null)
		{
			var dict = new Dictionary<ILogSource, LogSourceNames>();
			using (var logSourceNamesGenerator = logSourceNamesProvider.CreateNamesGenerator())
			{
				int unknownLogCounter = 0;
				foreach (var src in sources)
				{
					LogSourceNames name = null;

					var annotation = src.Annotation;
					if (!string.IsNullOrEmpty(annotation))
					{
						name = new LogSourceNames()
						{
							RoleInstanceName = annotation
						};
					}

					if (name == null && suggestedNames != null)
					{
						suggestedNames.TryGetValue(src, out name);
					}

					if (name == null)
					{
						name = logSourceNamesGenerator.Generate(src);
					}

					if (name == null)
					{
						name = new LogSourceNames()
						{
							RoleInstanceName = new string((char)((int)('A') + unknownLogCounter++), 1)
						};
					}

					dict[src] = name;
				}
			}
			return dict;
		}

		public static string GetLogFileNameHint(this ILogProvider provider)
		{
			var saveAs = provider as ISaveAs;
			if (saveAs == null || !saveAs.IsSavableAs)
				return null;
			return saveAs.SuggestedFileName;
		}

		public static string GetLogFileNameHint(this LogSourcePostprocessorInput input)
		{
			return GetLogFileNameHint(input.LogSource.Provider);
		}

		public static LogSourcePostprocessorInput AttachProgressHandler(this LogSourcePostprocessorInput input,
			Progress.IProgressAggregator progressAggregator, List<Progress.IProgressEventsSink> progressSinks)
		{
			var progressSink = progressAggregator.CreateProgressSink();
			progressSinks.Add(progressSink);
			input.ProgressHandler += progressSink.SetValue;
			input.ProgressAggregator = progressAggregator;
			return input;
		}

		public static LogSourcePostprocessorInput SetTemplatesTracker(this LogSourcePostprocessorInput input,
			ICodepathTracker value)
		{
			input.TemplatesTracker = value;
			return input;
		}
	
		public static LogSourcePostprocessorOutput[] GetPostprocessorOutputsByPostprocessorId(this IPostprocessorsManager postprocessorsManager, string postprocessorId)
		{
			return postprocessorsManager
				.LogSourcePostprocessorsOutputs
				.Where(output => output.PostprocessorMetadata.TypeID == postprocessorId)
				.ToArray();
		}

		public static IEnumerable<LogSourcePostprocessorOutput> GetAutoPostprocessingCapableOutputs(this IPostprocessorsManager postprocessorsManager)
		{
			Predicate<string> isRelevantPostprocessor = (id) =>
			{
				return
					id == PostprocessorIds.StateInspector
					|| id == PostprocessorIds.Timeline
					|| id == PostprocessorIds.SequenceDiagram
					|| id == PostprocessorIds.TimeSeries
					|| id == PostprocessorIds.Correlator;
			};

			Predicate<LogSourcePostprocessorOutput.Status> isStatusOk = (value) =>
			{
				return
					value == LogSourcePostprocessorOutput.Status.NeverRun
					|| value == LogSourcePostprocessorOutput.Status.Failed
					|| value == LogSourcePostprocessorOutput.Status.Outdated;
			};

			return
				postprocessorsManager
				.LogSourcePostprocessorsOutputs
				.Where(output => isRelevantPostprocessor(output.PostprocessorMetadata.TypeID) && isStatusOk(output.OutputStatus));
		}
	};
}

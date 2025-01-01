﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogJoint.Postprocessing.Correlation;

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

        public static LogSourcePostprocessorState[] GetPostprocessorOutputsByPostprocessorId(this IReadOnlyList<LogSourcePostprocessorState> outputs, PostprocessorKind postprocessorKind)
        {
            return outputs
                .Where(output => output.Postprocessor.Kind == postprocessorKind)
                .ToArray();
        }

        public static IEnumerable<LogSourcePostprocessorState> GetAutoPostprocessingCapableOutputs(this IReadOnlyList<LogSourcePostprocessorState> outputs)
        {
            bool isRelevantPostprocessor(PostprocessorKind id)
            {
                return
                       id == PostprocessorKind.StateInspector
                    || id == PostprocessorKind.Timeline
                    || id == PostprocessorKind.SequenceDiagram
                    || id == PostprocessorKind.TimeSeries;
            }

            bool isStatusOk(LogSourcePostprocessorState output)
            {
                var status = output.OutputStatus;
                return
                       status == LogSourcePostprocessorState.Status.NeverRun
                    || status == LogSourcePostprocessorState.Status.Failed
                    || status == LogSourcePostprocessorState.Status.Outdated;
            }

            return
                outputs
                .Where(output => isRelevantPostprocessor(output.Postprocessor.Kind) && isStatusOk(output));
        }

        internal static string MakePostprocessorOutputFileName(this ILogSourcePostprocessor pp)
        {
            return string.Format("postproc-{0}.xml", pp.Kind.ToString().ToLower());
        }

        internal static bool IsOutputOutdated(this ILogSource logSource, object outputData)
        {
            var logSourceEtag = logSource.Provider.Stats.ContentsEtag;
            if (logSourceEtag != null)
            {
                var etagAttr = (outputData as IPostprocessorOutputETag)?.ETag;
                if (etagAttr != null)
                {
                    if (logSourceEtag.Value.ToString() != etagAttr)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    };
}

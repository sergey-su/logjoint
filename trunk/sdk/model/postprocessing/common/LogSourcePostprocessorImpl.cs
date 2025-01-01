using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LogJoint.Postprocessing
{
    public class LogSourcePostprocessor : ILogSourcePostprocessor
    {
        readonly PostprocessorKind kind;
        readonly Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> run;

        public LogSourcePostprocessor(
            PostprocessorKind kind,
            Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> run
        )
        {
            this.kind = kind;
            this.run = run;
        }

        public LogSourcePostprocessor(
            PostprocessorKind kind,
            Func<LogSourcePostprocessorInput, Task> run
        ) : this(kind, MakeRunAdapter(run))
        {
        }

        public LogSourcePostprocessor(
            PostprocessorKind kind,
            Func<LogSourcePostprocessorInput, Task<IPostprocessorRunSummary>> run
        ) : this(kind, MakeRunAdapter(run))
        {
        }

        PostprocessorKind ILogSourcePostprocessor.Kind => kind;

        Task<IPostprocessorRunSummary> ILogSourcePostprocessor.Run(LogSourcePostprocessorInput[] forLogs)
        {
            return run(forLogs);
        }

        static Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> MakeRunAdapter(Func<LogSourcePostprocessorInput, Task> postprocessor)
        {
            Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> helper = async (inputs) =>
            {
                await Task.WhenAll(inputs.Select(i => postprocessor(i)));
                return (IPostprocessorRunSummary)null;
            };
            return helper;
        }

        static Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> MakeRunAdapter(Func<LogSourcePostprocessorInput, Task<IPostprocessorRunSummary>> postprocessor)
        {
            Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> helper = async (inputs) =>
            {
                var tasks = await Task.WhenAll(inputs.Select(i => postprocessor(i)));
                return new AggregatedRunSummary(
                    inputs.Zip(tasks, (input, task) => new { input.LogSource, task }).ToDictionary(x => x.LogSource, x => x.task)
                );
            };
            return helper;
        }
    };
}

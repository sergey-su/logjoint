using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
    public interface IOutputDataDeserializer
    {
        object Deserialize(PostprocessorKind kind, LogSourcePostprocessorDeserializationParams p);
    };

    public interface IManagerInternal : IManager
    {
        IReadOnlyList<LogSourcePostprocessorState> LogSourcePostprocessors { get; }
        Task RunPostprocessors(
            IReadOnlyList<LogSourcePostprocessorState> postprocessors,
            object customData = null
        );

        event EventHandler Changed; // todo: remove
    };

    /// <summary>
    /// Represents the state of a particular postprocessor applied to a particular log source.
    /// </summary>
    public class LogSourcePostprocessorState
    {
        public ILogSource LogSource { get; internal set; }
        public ILogSourcePostprocessor Postprocessor { get; internal set; }
        public enum Status
        {
            NeverRun,
            InProgress,
            Loading,
            Finished,
            Failed,
            Outdated,
        };
        public Status OutputStatus { get; internal set; }
        public IPostprocessorRunSummary LastRunSummary { get; internal set; }
        public object OutputData { get; internal set; }
        public double? Progress { get; internal set; }

        public override string ToString()
        {
            return $"{Postprocessor.Kind} for {LogSource.Provider.Factory}, status={OutputStatus}";
        }
    };
}


using System;

namespace LogJoint.Postprocessing
{
    public class Model : IModel
    {
        readonly ITextLogParser textLogParser = new TextLogParser();

        public Model(
            IManager postprocessorsManager,
            TimeSeries.ITimeSeriesTypesAccess timeSeriesTypes,
            StateInspector.IModel stateInspector,
            Timeline.IModel timeline,
            SequenceDiagram.IModel sequenceDiagram,
            TimeSeries.IModel timeSeries,
            Correlation.IModel correlation
        )
        {
            Manager = postprocessorsManager;
            TimeSeriesTypes = timeSeriesTypes;
            StateInspector = stateInspector;
            Timeline = timeline;
            SequenceDiagram = sequenceDiagram;
            TimeSeries = timeSeries;
            Correlation = correlation;
        }

        public IManager Manager { get; private set; }

        public TimeSeries.ITimeSeriesTypesAccess TimeSeriesTypes { get; private set; }

        public StateInspector.IModel StateInspector { get; private set; }

        public Timeline.IModel Timeline { get; private set; }

        public SequenceDiagram.IModel SequenceDiagram { get; private set; }

        public TimeSeries.IModel TimeSeries { get; private set; }

        public Correlation.IModel Correlation { get; private set; }

        IPrefixMatcher IModel.CreatePrefixMatcher() => new PrefixMatcher();

        ITextLogParser IModel.TextLogParser => textLogParser;

        IPostprocessorRunSummaryBuilder IModel.CreatePostprocessorRunSummaryBuilder() => new PostprocessorRunSummary();
    }
}

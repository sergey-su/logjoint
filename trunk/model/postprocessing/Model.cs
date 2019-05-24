using LogJoint.Analytics;

namespace LogJoint.Postprocessing
{
	public class Model : IModel
	{
		public Model(
			IPostprocessorsManager postprocessorsManager,
			Analytics.TimeSeries.ITimeSeriesTypesAccess timeSeriesTypes,
			StateInspector.IModel stateInspector,
			Timeline.IModel timeline,
			SequenceDiagram.IModel sequenceDiagram,
			TimeSeries.IModel timeSeries
		)
		{
			PostprocessorsManager = postprocessorsManager;
			TimeSeriesTypes = timeSeriesTypes;
			StateInspector = stateInspector;
			Timeline = timeline;
			SequenceDiagram = sequenceDiagram;
			TimeSeries = timeSeries;
		}

		public IPostprocessorsManager PostprocessorsManager { get; private set; }

		public Analytics.TimeSeries.ITimeSeriesTypesAccess TimeSeriesTypes { get; private set; }

		public StateInspector.IModel StateInspector { get; private set; }

		public Timeline.IModel Timeline { get; private set; }

		public SequenceDiagram.IModel SequenceDiagram { get; private set; }

		public TimeSeries.IModel TimeSeries { get; private set; }

		Analytics.Correlation.ICorrelator IModel.CreateCorrelator()
		{
			return new Analytics.Correlation.Correlator(
				new Analytics.Messaging.Analisys.InternodeMessagesDetector(),
				Analytics.Correlation.SolverFactory.Create());
		}

		IPrefixMatcher IModel.CreatePrefixMatcher()
		{
			return new PrefixMatcher();
		}
	}
}

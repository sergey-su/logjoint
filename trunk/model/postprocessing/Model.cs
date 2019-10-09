
using System;

namespace LogJoint.Postprocessing
{
	public class Model : IModel
	{
		readonly ITextLogParser textLogParser = new TextLogParser();
		readonly Func<Correlation.Solver.ISolver> solverFactory;

		public Model(
			IManager postprocessorsManager,
			TimeSeries.ITimeSeriesTypesAccess timeSeriesTypes,
			StateInspector.IModel stateInspector,
			Timeline.IModel timeline,
			SequenceDiagram.IModel sequenceDiagram,
			TimeSeries.IModel timeSeries,
			Func<Correlation.Solver.ISolver> solverFactory
		)
		{
			Manager = postprocessorsManager;
			TimeSeriesTypes = timeSeriesTypes;
			StateInspector = stateInspector;
			Timeline = timeline;
			SequenceDiagram = sequenceDiagram;
			TimeSeries = timeSeries;
			this.solverFactory = solverFactory;
		}

		public IManager Manager { get; private set; }

		public TimeSeries.ITimeSeriesTypesAccess TimeSeriesTypes { get; private set; }

		public StateInspector.IModel StateInspector { get; private set; }

		public Timeline.IModel Timeline { get; private set; }

		public SequenceDiagram.IModel SequenceDiagram { get; private set; }

		public TimeSeries.IModel TimeSeries { get; private set; }

		Correlation.ICorrelator IModel.CreateCorrelator()
		{
			return new Correlation.Correlator(
				new Messaging.Analisys.InternodeMessagesDetector(),
				solverFactory());
		}

		IPrefixMatcher IModel.CreatePrefixMatcher() => new PrefixMatcher();

		ITextLogParser IModel.TextLogParser => textLogParser;
	}
}

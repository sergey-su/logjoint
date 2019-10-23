
namespace LogJoint.Postprocessing
{
	class OutputDataDeserializer: IOutputDataDeserializer
	{
		readonly TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess;
		readonly ILogPartTokenFactories logPartTokenFactories;
		readonly Correlation.ISameNodeDetectionTokenFactories nodeDetectionTokenFactories;

		public OutputDataDeserializer(TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess,
			ILogPartTokenFactories logPartTokenFactories, Correlation.ISameNodeDetectionTokenFactories nodeDetectionTokenFactories)
		{
			this.timeSeriesTypesAccess = timeSeriesTypesAccess;
			this.logPartTokenFactories = logPartTokenFactories;
			this.nodeDetectionTokenFactories = nodeDetectionTokenFactories;
		}

		public object Deserialize(PostprocessorKind kind, LogSourcePostprocessorDeserializationParams p)
		{
			switch (kind)
			{
				case PostprocessorKind.StateInspector:
					return new StateInspector.StateInspectorOutput(p, logPartTokenFactories);
				case PostprocessorKind.Timeline:
					return new Timeline.TimelinePostprocessorOutput(p, logPartTokenFactories);
				case PostprocessorKind.SequenceDiagram:
					return new SequenceDiagram.SequenceDiagramPostprocessorOutput(p, null);
				case PostprocessorKind.TimeSeries:
					return new TimeSeries.TimeSeriesPostprocessorOutput(p, null, timeSeriesTypesAccess);
				case PostprocessorKind.Correlator:
					return new Correlation.PostprocessorOutput(p, logPartTokenFactories, nodeDetectionTokenFactories);
				default:
					return null;
			}
		}
	};
}

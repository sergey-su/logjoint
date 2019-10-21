
namespace LogJoint.Postprocessing
{
	class OutputDataDeserializer: IOutputDataDeserializer
	{
		readonly TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess;
		readonly ILogPartTokenFactories logPartTokenFactories;

		public OutputDataDeserializer(TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess, ILogPartTokenFactories logPartTokenFactories)
		{
			this.timeSeriesTypesAccess = timeSeriesTypesAccess;
			this.logPartTokenFactories = logPartTokenFactories;
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
					return Correlation.CorrelatorPostprocessorOutput.Parse(p.Reader);
				default:
					return null;
			}
		}
	};
}

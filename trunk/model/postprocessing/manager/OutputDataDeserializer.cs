
namespace LogJoint.Postprocessing
{
	public class OutputDataDeserializer: IOutputDataDeserializer
	{
		readonly TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess;

		public OutputDataDeserializer(TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			this.timeSeriesTypesAccess = timeSeriesTypesAccess;
		}

		public object Deserialize(PostprocessorKind kind, LogSourcePostprocessorDeserializationParams p)
		{
			switch (kind)
			{
				case PostprocessorKind.StateInspector:
					return new StateInspector.StateInspectorOutput(p);
				case PostprocessorKind.Timeline:
					return new Timeline.TimelinePostprocessorOutput(p, null);
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

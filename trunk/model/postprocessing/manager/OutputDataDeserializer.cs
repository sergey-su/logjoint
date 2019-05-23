using LogJoint.Analytics.TimeSeries;

namespace LogJoint.Postprocessing
{
	public class OutputDataDeserializer: IOutputDataDeserializer
	{
		readonly ITimeSeriesTypesAccess timeSeriesTypesAccess;

		public OutputDataDeserializer(ITimeSeriesTypesAccess timeSeriesTypesAccess)
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
					return Correlator.CorrelatorPostprocessorOutput.Parse(p.Reader);
				default:
					return null;
			}
		}
	};
}

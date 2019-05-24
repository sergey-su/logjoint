namespace LogJoint.Postprocessing
{
	public interface IModel
	{
		IPostprocessorsManager PostprocessorsManager { get; } // todo: rename to managet
		Analytics.IPrefixMatcher CreatePrefixMatcher();
		Analytics.Correlation.ICorrelator CreateCorrelator(); // todo: have Correlation ns
		StateInspector.IModel StateInspector { get; }
		Timeline.IModel Timeline { get; }
		SequenceDiagram.IModel SequenceDiagram { get; }
		TimeSeries.IModel TimeSeries { get; }
	};
}

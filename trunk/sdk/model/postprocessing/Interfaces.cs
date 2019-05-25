namespace LogJoint.Postprocessing
{
	public interface IModel
	{
		IPostprocessorsManager Manager { get; }
		IPrefixMatcher CreatePrefixMatcher();
		ITextLogParser TextLogParser { get; }
		Correlation.ICorrelator CreateCorrelator();
		StateInspector.IModel StateInspector { get; }
		Timeline.IModel Timeline { get; }
		SequenceDiagram.IModel SequenceDiagram { get; }
		TimeSeries.IModel TimeSeries { get; }
	};
}

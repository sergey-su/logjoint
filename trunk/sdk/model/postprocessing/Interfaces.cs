namespace LogJoint.Postprocessing
{
	public interface IModel
	{
		IManager Manager { get; }
		IPrefixMatcher CreatePrefixMatcher();
		ITextLogParser TextLogParser { get; }
		Correlation.ICorrelator CreateCorrelator(); // todo: remove
		StateInspector.IModel StateInspector { get; }
		Timeline.IModel Timeline { get; }
		SequenceDiagram.IModel SequenceDiagram { get; }
		TimeSeries.IModel TimeSeries { get; }
		Correlation.IModel Correlation { get; }
	};
}

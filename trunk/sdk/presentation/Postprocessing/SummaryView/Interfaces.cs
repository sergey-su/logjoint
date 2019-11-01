namespace LogJoint.UI.Presenters.Postprocessing.SummaryView
{
	public interface IPresenter
	{
		ActionState StateInspector { get; }
		ActionState Timeline { get; }
		ActionState SequenceDiagram { get; }
		ActionState TimeSeries { get; }
	};

	public class ActionState
	{
		public bool Enabled { get; internal set; }
	};
}

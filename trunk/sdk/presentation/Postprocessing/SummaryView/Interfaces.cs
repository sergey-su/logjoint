using System;

namespace LogJoint.UI.Presenters.Postprocessing.SummaryView
{
	public interface IPresenter
	{
		ActionState StateInspector { get; }
		ActionState Timeline { get; }
		ActionState SequenceDiagram { get; }
		ActionState TimeSeries { get; }
		ActionState Correlation { get; }
	};

	public class ActionState
	{
		public bool Enabled { get; internal set; }
		public Action Run { get; internal set; }
		public Action Show { get; internal set; }
	};
}

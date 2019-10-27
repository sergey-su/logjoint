namespace LogJoint.UI.Presenters.Postprocessing
{
	public interface IPostprocessorVisualizerPresenter
	{
		/// <summary>
		/// Shows the view that displays the visualizer.
		/// </summary>
		void Show();
	};

	public interface IPresentation
	{
		StateInspectorVisualizer.IPresenter StateInspector { get; }
		TimelineVisualizer.IPresenter Timeline { get; }
		SequenceDiagramVisualizer.IPresenter SequenceDiagram { get; }
		TimeSeriesVisualizer.IPresenter TimeSeries { get; }
		SummaryView.IPresenter SummaryView { get; }
	};
}

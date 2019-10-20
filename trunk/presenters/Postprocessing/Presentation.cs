namespace LogJoint.UI.Presenters.Postprocessing
{
	public class Presentation: IPresentation
	{
		public Presentation(IFactory factory)
		{
			StateInspector = new StateInspectorVisualizer.StateInspectorPresenterLazyProxy(factory);
			Timeline = new TimelineVisualizer.TimelineVisualizerPresenterLazyProxy(factory);
			SequenceDiagram = new SequenceDiagramVisualizer.SequenceDiagramVisualizerPresenterLazyProxy(factory);
			TimeSeries = new TimeSeriesVisualizer.TimeSeriesVisualizerPresenterLazyProxy(factory);
		}

		public StateInspectorVisualizer.IPresenter StateInspector { get; private set; }
		public TimelineVisualizer.IPresenter Timeline { get; private set; }
		public SequenceDiagramVisualizer.IPresenter SequenceDiagram { get; private set; }
		public TimeSeriesVisualizer.IPresenter TimeSeries { get; private set; }
	}
}

using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System;

namespace LogJoint.UI.Postprocessing
{
	public class PostprocessorOutputFormFactory : PostprocessorOutputFormFactoryBase
	{
		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.StateInspectorVisualizer.IView> CreateStateInspectorViewObjects()
		{
			var wnd = new StateInspector.StateInspectorWindowController();
			return Tuple.Create((IPostprocessorOutputForm)wnd, (Presenters.Postprocessing.StateInspectorVisualizer.IView)wnd);
		}
		
		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.TimelineVisualizer.IView> CreateTimelineViewObjects()
		{
			var wnd = new TimelineVisualizer.TimelineWindowController ();
			return Tuple.Create((IPostprocessorOutputForm)wnd, (Presenters.Postprocessing.TimelineVisualizer.IView)wnd);
		}
		
		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.SequenceDiagramVisualizer.IView> CreateSequenceDiagramViewObjects()
		{
			var wnd = new SequenceDiagramVisualizer.SequenceDiagramWindowController ();
			return Tuple.Create((IPostprocessorOutputForm)wnd, (Presenters.Postprocessing.SequenceDiagramVisualizer.IView)wnd);
		}

		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.TimeSeriesVisualizer.IView> CreateTimeSeriesViewObjects()
		{
			var wnd = new TimeSeriesVisualizer.TimeSeriesWindowController ();
			return Tuple.Create((IPostprocessorOutputForm)wnd, (Presenters.Postprocessing.TimeSeriesVisualizer.IView)wnd);
		}
	}
}

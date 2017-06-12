using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System;

namespace LogJoint.UI.Postprocessing
{
	public class PostprocessorOutputFormFactory : PostprocessorOutputFormFactoryBase
	{
		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.StateInspectorVisualizer.IView> CreateStateInspectorViewObjects()
		{
			var impl = new StateInspector.StateInspectorForm();
			app.View.RegisterToolForm(impl);
			return Tuple.Create((IPostprocessorOutputForm)impl, (Presenters.Postprocessing.StateInspectorVisualizer.IView)impl);
		}

		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.TimelineVisualizer.IView> CreateTimelineViewObjects()
		{
			var impl = new TimelineVisualizer.TimelineForm();
			app.View.RegisterToolForm(impl);
			return Tuple.Create((IPostprocessorOutputForm)impl, impl.TimelineVisualizerView);
		}

		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.SequenceDiagramVisualizer.IView> CreateSequenceDiagramViewObjects()
		{
			var impl = new SequenceDiagramVisualizer.SequenceDiagramForm();
			app.View.RegisterToolForm(impl);
			return Tuple.Create((IPostprocessorOutputForm)impl, impl.SequenceDiagramVisualizerView);
		}

		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.TimeSeriesVisualizer.IView> CreateTimeSeriesViewObjects()
		{
			var impl = new TimeSeriesVisualizer.TimeSeriesForm();
			app.View.RegisterToolForm(impl);
			return Tuple.Create((IPostprocessorOutputForm)impl, impl.TimeSeriesVisualizerView);
		}
	}
}

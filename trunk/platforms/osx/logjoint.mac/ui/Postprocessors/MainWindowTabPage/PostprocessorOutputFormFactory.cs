using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System;

namespace LogJoint.UI.Postprocessing
{
	public class PostprocessorOutputFormFactory : PostprocessorOutputFormFactoryBase
	{
		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.StateInspectorVisualizer.IView> CreateStateInspectorViewObjects()
		{
			var wnd = new Postprocessing.StateInspector.StateInspectorWindowController();
			return Tuple.Create((IPostprocessorOutputForm)wnd, (Presenters.Postprocessing.StateInspectorVisualizer.IView)wnd);
		}
		
		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.TimelineVisualizer.IView> CreateTimelineViewObjects()
		{
			// todo
			return null;
		}
		
		protected override Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.SequenceDiagramVisualizer.IView> CreateSequenceDiagramViewObjects()
		{
			// todo
			return null;
		}
	}
}

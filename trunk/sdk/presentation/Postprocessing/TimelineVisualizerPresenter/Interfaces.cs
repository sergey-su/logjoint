using System;

namespace LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer
{
	public interface IPresenter: IPostprocessorVisualizerPresenter
	{
		void Navigate(TimeSpan t1, TimeSpan t2);
	};
}

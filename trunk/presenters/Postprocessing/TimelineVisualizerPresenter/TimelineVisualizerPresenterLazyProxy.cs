using System;

namespace LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer
{
	public class TimelineVisualizerPresenterLazyProxy: IPresenter
	{
		readonly IFactory factory;

		public TimelineVisualizerPresenterLazyProxy(IFactory factory) 
		{
			this.factory = factory;
		}

		void IPresenter.Navigate(TimeSpan t1, TimeSpan t2)
		{
			factory.GetTimelineVisualizer(true).Navigate(t1, t2);
		}

		void IPostprocessorVisualizerPresenter.Show()
		{
			factory.GetTimelineVisualizer(true).Show();
		}
	}
}

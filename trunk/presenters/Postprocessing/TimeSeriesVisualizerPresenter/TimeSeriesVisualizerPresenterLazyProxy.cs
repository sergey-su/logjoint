using System;

namespace LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer
{
	public class TimeSeriesVisualizerPresenterLazyProxy: IPresenter
	{
		readonly IFactory factory;

		public TimeSeriesVisualizerPresenterLazyProxy(IFactory factory)
		{
			this.factory = factory;
		}

		bool IPresenter.ConfigNodeExists(Predicate<ITreeNodeData> predicate) => GetOrCreate().ConfigNodeExists(predicate);
		void IPresenter.OpenConfigDialog() => GetOrCreate().OpenConfigDialog();
		bool IPresenter.SelectConfigNode(Predicate<ITreeNodeData> predicate) => GetOrCreate().SelectConfigNode(predicate);
		void IPostprocessorVisualizerPresenter.Show() => GetOrCreate().Show();

		IPresenter GetOrCreate() => factory.GetTimeSeriesVisualizer(true);
	}
}

using System;
using LogJoint.Postprocessing;

namespace LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer
{
	public class StateInspectorPresenterLazyProxy: IPresenter
	{
		readonly IFactory factory;

		public StateInspectorPresenterLazyProxy(IFactory factory)
		{
			this.factory = factory;

			factory.StateInspectorCreated += (cs, ce) =>
			{
				GetOrCreate().OnMenu += (s, e) => OnMenu?.Invoke(s, e);
				GetOrCreate().OnNodeCreated += (s, e) => OnNodeCreated?.Invoke(s, e);
			};
		}

		public event EventHandler<MenuData> OnMenu;
		public event EventHandler<NodeCreatedEventArgs> OnNodeCreated;

		IVisualizerNode IPresenter.SelectedObject => Get()?.SelectedObject;
		IEnumerableAsync<IVisualizerNode> IPresenter.Roots => GetOrCreate().Roots;
		void IPostprocessorVisualizerPresenter.Show() => GetOrCreate().Show();

		IPresenter Get() => factory.GetStateInspectorVisualizer(false);
		IPresenter GetOrCreate() => factory.GetStateInspectorVisualizer(true);
	}
}

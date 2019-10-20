
namespace LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer
{
	public class SequenceDiagramVisualizerPresenterLazyProxy: IPresenter
	{
		readonly IFactory factory;

		public SequenceDiagramVisualizerPresenterLazyProxy(IFactory factory)
		{
			this.factory = factory;
		}

		void IPostprocessorVisualizerPresenter.Show() => factory.GetSequenceDiagramVisualizer(true).Show();
	}
}

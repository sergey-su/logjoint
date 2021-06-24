using LogJoint.UI.Presenters.Timeline;

namespace LogJoint.Wasm.UI
{
	public class TimelineViewProxy : IView
	{
		IViewModel viewModel;
		IView component;

		public TimelineViewProxy()
		{
		}

		public void SetComponent(IView component)
		{
			this.component = component;
			component?.SetViewModel(viewModel);
		}

		PresentationMetrics IView.GetPresentationMetrics() => component?.GetPresentationMetrics() ?? new PresentationMetrics();

		void IView.InterruptDrag() => component?.InterruptDrag();

		void IView.ResetToolTipPoint(int x, int y) => component?.ResetToolTipPoint(x, y);

		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
		}

		void IView.TryBeginDrag(int x, int y) => component.TryBeginDrag(x, y);

		void IView.UpdateDragViewPositionDuringAnimation(int y, bool topView) => component.UpdateDragViewPositionDuringAnimation(y, topView);
	}
}

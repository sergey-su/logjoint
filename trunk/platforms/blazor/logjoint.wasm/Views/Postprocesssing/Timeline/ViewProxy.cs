using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;

namespace LogJoint.Wasm.UI.Postprocesssing.Timeline
{
	public class ViewProxy : IView
	{
		IViewModel viewModel;
		IView component;

		public void SetComponent(IView component)
		{
			this.component = component;
			component?.SetViewModel(viewModel);
		}

		LogJoint.UI.Presenters.QuickSearchTextBox.IView IView.QuickSearchTextBox => null;
		LogJoint.UI.Presenters.TagsList.IView IView.TagsListView => null;
		LogJoint.UI.Presenters.ToastNotificationPresenter.IView IView.ToastNotificationsView => null;
		RulerMetrics IView.VisibleRangeRulerMetrics => component?.VisibleRangeRulerMetrics;
		RulerMetrics IView.AvailableRangeRulerMetrics => component?.AvailableRangeRulerMetrics;

		void IView.Show() => component?.Show();
		HitTestResult IView.HitTest(object hitTestToken) => component != null ? component.HitTest(hitTestToken) : new HitTestResult();
		void IView.EnsureActivityVisible(int activityIndex) => component?.EnsureActivityVisible(activityIndex);
		void IView.ReceiveInputFocus() => component?.ReceiveInputFocus();


		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
			component?.SetViewModel(value);
		}
	}
}

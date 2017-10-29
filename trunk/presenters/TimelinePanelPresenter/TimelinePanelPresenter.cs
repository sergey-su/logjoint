
namespace LogJoint.UI.Presenters.TimelinePanel
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly Timeline.IPresenter timelinePresenter;

		public Presenter(
			IView view,
			Timeline.IPresenter timelinePresenter)
		{
			this.timelinePresenter = timelinePresenter;

			timelinePresenter.Updated += (s, e) => UpdateView();

			view.SetPresenter(this);
			view.SetEnabled(false);
		}


		void IViewEvents.OnZoomToolButtonClicked(int delta)
		{
			timelinePresenter.Zoom(delta);
		}

		void IViewEvents.OnZoomToViewAllToolButtonClicked()
		{
			timelinePresenter.ZoomToViewAll();
		}

		void IViewEvents.OnScrollToolButtonClicked(int delta)
		{
			timelinePresenter.Scroll(delta);
		}

		void UpdateView()
		{
			view.SetEnabled(!timelinePresenter.IsEmpty);
		}
	};
};
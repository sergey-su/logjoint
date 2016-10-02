namespace LogJoint.UI.Presenters.TimelinePanel
{
	public interface IPresenter
	{
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
		void SetEnabled(bool value);
	};


	public interface IViewEvents
	{
		void OnZoomToolButtonClicked(int delta);
		void OnZoomToViewAllToolButtonClicked();
		void OnScrollToolButtonClicked(int delta);
	};
};
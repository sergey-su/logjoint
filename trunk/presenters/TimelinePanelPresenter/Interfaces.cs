namespace LogJoint.UI.Presenters.TimelinePanel
{
	public interface IPresenter
	{
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
	};


	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		bool IsVisible { get; }
		bool IsEnabled { get; }
		double? Size { get; }
		string HideButtonTooltip { get; }
		string ShowButtonTooltip { get; }
		string ResizerTooltip { get; }

		void OnResize(double size);
		void OnHideButtonClicked();
		void OnShowButtonClicked();
		void OnZoomToolButtonClicked(int delta);
		void OnZoomToViewAllToolButtonClicked();
		void OnScrollToolButtonClicked(int delta);
	};
};
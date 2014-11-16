using System;

namespace LogJoint.UI.Presenters.SourcePropertiesWindow
{
	public interface IPresenter
	{
		void UpdateOpenWindow();
		void ShowWindow(ILogSource forSource);
	};

	// todo: move presentation logic to presenter
	public interface IView
	{
		IWindow _CreateWindow(ILogSource forSource, IPresentersFacade navHandler);
	};

	public interface IWindow
	{
		void ShowDialog();
		void _UpdateView();
	};

	public interface IPresenterEvents
	{
	};
};
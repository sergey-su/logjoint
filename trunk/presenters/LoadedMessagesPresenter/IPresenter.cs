using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public interface IPresenter
	{
		Presenters.LogViewer.IPresenter LogViewerPresenter { get; }
		void Focus();
		event EventHandler OnResizingStarted;
		event EventHandler<ResizingEventArgs> OnResizing;
		event EventHandler OnResizingFinished;
	};

	public interface IViewEvents
	{
		void OnToggleRawView();
		void OnColoringButtonClicked(Settings.Appearance.ColoringMode mode);
		void OnToggleBookmark();
		void OnResizingStarted();
		void OnResizingFinished();
		void OnResizing(int delta);
	};
};
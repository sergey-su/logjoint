using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public interface IPresenter
	{
		Presenters.LogViewer.IPresenter LogViewerPresenter { get; }
		void ToggleRawView();
		void ColoringButtonClicked(Settings.Appearance.ColoringMode mode);
		void ToggleBookmark();
		void Focus();
	};
};
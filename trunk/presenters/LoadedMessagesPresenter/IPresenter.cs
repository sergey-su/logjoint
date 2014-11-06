using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public interface IPresenter
	{
		Presenters.LogViewer.Presenter LogViewerPresenter { get; } // todo: introduce and use LogViewer.Presenter's interface
		bool RawViewAllowed { get; set; }
		void ToggleRawView();
		void ColoringButtonClicked(LogViewer.ColoringMode mode);
		void ToggleBookmark();
		void Focus();
	};
};
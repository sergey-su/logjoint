using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public interface IView
	{
		void SetPresenter(IPresenter presenter);
		Presenters.LogViewer.IView MessagesView { get; }
		void SetRawViewButtonState(bool visible, bool checked_);
		void SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked);
		void Focus();
	};
};
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.QuickSearchTextBox
{
	public interface IPresenter
	{
		event EventHandler SearchNow;
		event EventHandler RealtimeSearch;
		event EventHandler Cancelled;

		string Text { get; }
		void Focus(char initialSearchChar);
		void Focus(string initialSearchString);
		void Reset();
	};

	public interface IView
	{
		void SetPresenter(IViewEvents viewEvents);

		string Text { get; set; }
		void SelectEnd();
		void ReceiveInputFocus();
		void ResetQuickSearchTimer(int due);
	};

	public interface IViewEvents
	{
		void OnEscapePressed();
		void OnEnterPressed();
		void OnTextChanged();
		void OnQuickSearchTimerTriggered();
	};
};
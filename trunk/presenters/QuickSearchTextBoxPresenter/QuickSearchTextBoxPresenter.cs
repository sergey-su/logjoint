using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.QuickSearchTextBox
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(IView view)
		{
			this.view = view;

			view.SetPresenter(this);
		}

		public event EventHandler SearchNow;
		public event EventHandler RealtimeSearch;
		public event EventHandler Cancelled;

		string IPresenter.Text
		{
			get { return view.Text; }
		}

		void IPresenter.Focus(char initialSearchChar)
		{
			((IPresenter)this).Focus(new string(initialSearchChar, 1));
		}

		void IPresenter.Focus(string initialSearchString)
		{
			if (initialSearchString != null)
				view.Text = initialSearchString;
			view.SelectEnd();
			view.ReceiveInputFocus();
		}

		void IPresenter.Reset()
		{
			if (view.Text != "")
			{
				CancelInternal();
			}
		}

		void IViewEvents.OnEscapePressed()
		{
			CancelInternal();
		}

		void IViewEvents.OnEnterPressed()
		{
			this.realtimeSearchCachedText = view.Text;
			SearchNow?.Invoke(this, EventArgs.Empty);
		}

		void IViewEvents.OnQuickSearchTimerTriggered()
		{
			if (view.Text != realtimeSearchCachedText)
			{
				realtimeSearchCachedText = view.Text;
				RealtimeSearch?.Invoke(this, EventArgs.Empty);
			}
		}

		void IViewEvents.OnTextChanged()
		{
			view.ResetQuickSearchTimer(500);
		}


		private void CancelInternal()
		{
			view.Text = "";
			this.realtimeSearchCachedText = "";
			Cancelled?.Invoke(this, EventArgs.Empty);
		}

		readonly IView view;

		string realtimeSearchCachedText;
	};
};
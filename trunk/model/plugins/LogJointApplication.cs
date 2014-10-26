using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace LogJoint
{
	class LogJointApplication: ILogJointApplication
	{
		public LogJointApplication(Model model, UI.MainForm mainForm, 
			UI.Presenters.LogViewer.Presenter messagesPresenter,
			UI.Presenters.FiltersListBox.IPresenter filtersPresenter)
		{
			this.model = model;
			this.mainForm = mainForm;
			this.messagesPresenter = messagesPresenter;
			this.filtersPresenter = filtersPresenter;
		}

		public void FireFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		public void FireSourcesChanged()
		{
			if (SourcesChanged != null)
				SourcesChanged(this, EventArgs.Empty);
		}

		#region ILogJointApplication Members

		public Model Model
		{
			get { return model; }
		}

		public void RegisterToolForm(Form f)
		{
			mainForm.AddOwnedForm(f);
		}

		public void ShowFilter(Filter f)
		{
			mainForm.menuTabControl.SelectedTab = mainForm.filtersTabPage;
			filtersPresenter.SelectFilter(f);
		}

		public MessageBase FocusedMessage
		{
			get { return messagesPresenter.FocusedMessage; }
		}

		public IMessagesCollection LoadedMessagesCollection
		{
			get { return messagesPresenter.LoadedMessages; }
		}

		public void SelectMessageAt(IBookmark bmk, Predicate<MessageBase> messageMatcherWhenNoHashIsSpecified)
		{
			mainForm.NavigateToBookmark(bmk, messageMatcherWhenNoHashIsSpecified, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
		}

		public event EventHandler FocusedMessageChanged;
		public event EventHandler SourcesChanged;

		#endregion

		Model model;
		UI.MainForm mainForm;
		UI.Presenters.LogViewer.Presenter messagesPresenter;
		UI.Presenters.FiltersListBox.IPresenter filtersPresenter;
	}
}

using LogJoint.UI;
using LogJoint.UI.Presenters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace LogJoint
{
	class LogJointApplication: ILogJointApplication
	{
		public LogJointApplication(IModel model,
			UI.MainForm mainForm, 
			UI.Presenters.LogViewer.Presenter messagesPresenter,
			UI.Presenters.FiltersListBox.IPresenter filtersPresenter,
			UI.Presenters.LogViewer.Presenter viewerPresenter,
			UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter,
			UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter)
		{
			this.model = model;
			this.mainForm = mainForm;
			this.messagesPresenter = messagesPresenter;
			this.filtersPresenter = filtersPresenter;
			this.viewerPresenter = viewerPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;

			sourcesManagerPresenter.OnViewUpdated += (s, e) =>
			{
				this.FireSourcesChanged();
			};
			viewerPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				this.FireFocusedMessageChanged();
			};
		}

		#region ILogJointApplication Members

		public IModel Model
		{
			get { return model; }
		}

		public void RegisterToolForm(Form f)
		{
			mainForm.AddOwnedForm(f);
		}

		public void ShowFilter(IFilter f)
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
			bookmarksManagerPresenter.NavigateToBookmark(bmk, messageMatcherWhenNoHashIsSpecified, 
				BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
		}

		public event EventHandler FocusedMessageChanged;
		public event EventHandler SourcesChanged;

		#endregion

		void FireFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		void FireSourcesChanged()
		{
			if (SourcesChanged != null)
				SourcesChanged(this, EventArgs.Empty);
		}

		IModel model;
		UI.MainForm mainForm;
		UI.Presenters.LogViewer.Presenter messagesPresenter;
		UI.Presenters.FiltersListBox.IPresenter filtersPresenter;
		UI.Presenters.LogViewer.Presenter viewerPresenter;
		UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter;
	}
}

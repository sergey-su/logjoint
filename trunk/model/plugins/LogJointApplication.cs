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
			UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter,
			UI.Presenters.FiltersListBox.IPresenter filtersPresenter,
			UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter,
			UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter,
			UI.Presenters.IPresentersFacade presentersFacade,
			IInvokeSynchronization uiInvokeSynchronization)
		{
			this.model = model;
			this.mainForm = mainForm;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.messagesPresenter = loadedMessagesPresenter.LogViewerPresenter;
			this.filtersPresenter = filtersPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;
			this.presentersFacade = presentersFacade;
			this.uiInvokeSynchronization = uiInvokeSynchronization;

			sourcesManagerPresenter.OnViewUpdated += (s, e) =>
			{
				this.FireSourcesChanged();
			};
			messagesPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				this.FireFocusedMessageChanged();
			};
		}

		#region ILogJointApplication Members

		public IModel Model
		{
			get { return model; }
		}

		public IInvokeSynchronization UIInvokeSynchronization
		{
			get { return uiInvokeSynchronization; }
		}

		public void RegisterToolForm(Form f)
		{
			mainForm.AddOwnedForm(f);
		}

		public IMessage FocusedMessage
		{
			get { return messagesPresenter.FocusedMessage; }
		}

		public IMessagesCollection LoadedMessagesCollection
		{
			get { return messagesPresenter.LoadedMessages; }
		}

		public void SelectMessageAt(IBookmark bmk, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified)
		{
			bookmarksManagerPresenter.NavigateToBookmark(bmk, messageMatcherWhenNoHashIsSpecified, 
				BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
		}

		public UI.Presenters.LoadedMessages.IPresenter LoadedMessagesPresenter { get { return loadedMessagesPresenter; } }

		public UI.Presenters.IPresentersFacade PresentersFacade { get { return presentersFacade; } }

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
		UI.Presenters.LogViewer.IPresenter messagesPresenter;
		UI.Presenters.FiltersListBox.IPresenter filtersPresenter;
		UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter;
		UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter;
		UI.Presenters.IPresentersFacade presentersFacade;
		IInvokeSynchronization uiInvokeSynchronization;
	}
}

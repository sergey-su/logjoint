using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters
{
	public class Facade: IPresentersFacade
	{
		MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter;
		ThreadsList.IPresenter threadsListPresenter;
		SourcesList.IPresenter sourcesListPresenter;
		BookmarksManager.IPresenter bookmarksManagerPresenter;
		MainForm.IPresenter mainFormPresenter;

		public void Init(
			MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter,
			ThreadsList.IPresenter threadsListPresenter,
			SourcesList.IPresenter sourcesListPresenter,
			BookmarksManager.IPresenter bookmarksManagerPresenter,
			MainForm.IPresenter mainFormPresenter)
		{
			this.messagePropertiesDialogPresenter = messagePropertiesDialogPresenter;
			this.threadsListPresenter = threadsListPresenter;
			this.sourcesListPresenter = sourcesListPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;
			this.mainFormPresenter = mainFormPresenter;
		}

		void IPresentersFacade.ShowMessageProperties()
		{
			messagePropertiesDialogPresenter.ShowDialog();
		}

		bool IPresentersFacade.ShowMessage(
			IBookmark bmk, 
			BookmarkNavigationOptions options,
			Predicate<IMessage> messageMatcherWhenNoHashIsSpecified)
		{
			return bookmarksManagerPresenter.NavigateToBookmark(bmk, messageMatcherWhenNoHashIsSpecified, options);
		}

		void IPresentersFacade.ExecuteThreadPropertiesDialog(IThread thread)
		{
			mainFormPresenter.ExecuteThreadPropertiesDialog(thread);
		}

		void IPresentersFacade.ShowThread(IThread thread)
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.Threads);
			threadsListPresenter.Select(thread);
		}

		void IPresentersFacade.ShowLogSource(ILogSource source)
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.Sources);
			sourcesListPresenter.SelectSource(source);
		}

		void IPresentersFacade.ShowFiltersView()
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.DisplayFilteringRules);
		}

		void IPresentersFacade.SaveLogSourceAs(ILogSource logSource)
		{
			sourcesListPresenter.SaveLogSourceAs(logSource);
		}
	};
};
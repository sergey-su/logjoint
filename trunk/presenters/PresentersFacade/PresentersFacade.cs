using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters
{
	public class Facade: IUINavigationHandler
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

		void IUINavigationHandler.ShowMessageProperties()
		{
			messagePropertiesDialogPresenter.ShowDialog();
		}

		bool IUINavigationHandler.ShowLine(IBookmark bmk, BookmarkNavigationOptions options)
		{
			return bookmarksManagerPresenter.NavigateToBookmark(bmk, null, options);
		}

		void IUINavigationHandler.ExecuteThreadPropertiesDialog(IThread thread)
		{
			mainFormPresenter.ExecuteThreadPropertiesDialog(thread);
		}

		void IUINavigationHandler.ShowThread(IThread thread)
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.Threads);
			threadsListPresenter.Select(thread);
		}

		void IUINavigationHandler.ShowLogSource(ILogSource source)
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.Sources);
			sourcesListPresenter.SelectSource(source);
		}

		void IUINavigationHandler.ShowFiltersView()
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.DisplayFilteringRules);
		}

		void IUINavigationHandler.SaveLogSourceAs(ILogSource logSource)
		{
			sourcesListPresenter.SaveLogSourceAs(logSource);
		}
	};
};
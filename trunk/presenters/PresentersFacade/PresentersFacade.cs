using System;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
	public class Facade: IPresentersFacade
	{
		MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter;
		ThreadsList.IPresenter threadsListPresenter;
		SourcesList.IPresenter sourcesListPresenter;
		BookmarksManager.IPresenter bookmarksManagerPresenter;
		MainForm.IPresenter mainFormPresenter;
		About.IPresenter aboutDialogPresenter;
		Options.Dialog.IPresenter optionsDialogPresenter;
		HistoryDialog.IPresenter historyDialogPresenter;

		public void Init(
			MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter,
			ThreadsList.IPresenter threadsListPresenter,
			SourcesList.IPresenter sourcesListPresenter,
			BookmarksManager.IPresenter bookmarksManagerPresenter,
			MainForm.IPresenter mainFormPresenter,
			About.IPresenter aboutDialogPresenter,
			Options.Dialog.IPresenter optionsDialogPresenter,
			HistoryDialog.IPresenter historyDialogPresenter
		)
		{
			this.messagePropertiesDialogPresenter = messagePropertiesDialogPresenter;
			this.threadsListPresenter = threadsListPresenter;
			this.sourcesListPresenter = sourcesListPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;
			this.mainFormPresenter = mainFormPresenter;
			this.optionsDialogPresenter = optionsDialogPresenter;
			this.historyDialogPresenter = historyDialogPresenter;
			this.aboutDialogPresenter = aboutDialogPresenter;
		}

		void IPresentersFacade.ShowMessageProperties()
		{
			messagePropertiesDialogPresenter.ShowDialog();
		}

		Task<bool> IPresentersFacade.ShowMessage(
			IBookmark bmk, 
			BookmarkNavigationOptions options)
		{
			return bookmarksManagerPresenter.NavigateToBookmark(bmk, options);
		}

		void IPresentersFacade.ExecuteThreadPropertiesDialog(IThread thread)
		{
			mainFormPresenter.ExecuteThreadPropertiesDialog(thread);
		}

		bool IPresentersFacade.CanShowThreads => threadsListPresenter != null;

		void IPresentersFacade.ShowThread(IThread thread)
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.Threads);
			if (threadsListPresenter != null)
				threadsListPresenter.Select(thread);
		}

		void IPresentersFacade.ShowLogSource(ILogSource source)
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.Sources);
			sourcesListPresenter.SelectSource(source);
		}

		void IPresentersFacade.SaveLogSourceAs(ILogSource logSource)
		{
			sourcesListPresenter.SaveLogSourceAs(logSource);
		}

		void IPresentersFacade.ShowPreprocessing(Preprocessing.ILogSourcePreprocessing preproc)
		{
			mainFormPresenter.ActivateTab(MainForm.TabIDs.Sources);
			sourcesListPresenter.SelectPreprocessing(preproc);
		}

		void IPresentersFacade.ShowAboutDialog()
		{
			aboutDialogPresenter.Show();
		}

		void IPresentersFacade.ShowOptionsDialog()
		{
			optionsDialogPresenter.ShowDialog();
		}

		void IPresentersFacade.ShowHistoryDialog()
		{
			historyDialogPresenter.ShowDialog();
		}
	};
};
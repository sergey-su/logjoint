using System;
using System.Threading.Tasks;
using static LogJoint.Workspaces.WorkspaceDTO;

namespace LogJoint.UI.Presenters
{
    public class Facade : IPresentersFacade
    {
        MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter;
        ThreadsList.IPresenter threadsListPresenter;
        SourcesList.IPresenter sourcesListPresenter;
        BookmarksManager.IPresenter bookmarksManagerPresenter;
        MainForm.IPresenter mainFormPresenter;
        About.IPresenter aboutDialogPresenter;
        Options.Dialog.IPresenter optionsDialogPresenter;
        HistoryDialog.IPresenter historyDialogPresenter;
        ShortcutsDialog.IPresenter shortcutsDialog;
        FiltersManager.IPresenter hightlightingPresenter;

        public void Init(
            MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter,
            ThreadsList.IPresenter threadsListPresenter,
            SourcesList.IPresenter sourcesListPresenter,
            BookmarksManager.IPresenter bookmarksManagerPresenter,
            MainForm.IPresenter mainFormPresenter,
            About.IPresenter aboutDialogPresenter,
            Options.Dialog.IPresenter optionsDialogPresenter,
            HistoryDialog.IPresenter historyDialogPresenter,
            ShortcutsDialog.IPresenter shortcutsDialog,
            FiltersManager.IPresenter hightlightingPresenter
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
            this.shortcutsDialog = shortcutsDialog;
            this.hightlightingPresenter = hightlightingPresenter;
        }

        void IPresentersFacade.ShowMessageProperties()
        {
            messagePropertiesDialogPresenter.Show();
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

        void IPresentersFacade.ShowSettings()
        {
            mainFormPresenter.ActivateTab(MainForm.TabIDs.Settings);
        }

        Task IPresentersFacade.SaveLogSourceAs(ILogSource logSource)
        {
            return sourcesListPresenter.SaveLogSourceAs(logSource);
        }

        void IPresentersFacade.ShowPreprocessing(LogJoint.Preprocessing.ILogSourcePreprocessing preproc)
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

        void IPresentersFacade.ShowKeyboardShortcuts()
        {
            shortcutsDialog.ShowDialog();
        }

        void IPresentersFacade.ShowHighlightingDialogForSelectedText()
        {
            hightlightingPresenter?.OpenDialogForSelectedText();
        }
    };
};
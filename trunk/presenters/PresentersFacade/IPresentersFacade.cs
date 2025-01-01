using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
    [Flags]
    public enum BookmarkNavigationOptions
    {
        Default = 0,
        EnablePopups = 1,
        GenericStringsSet = 2,
        BookmarksStringsSet = 4,
        SearchResultStringsSet = 8,
        NoLinksInPopups = 16
    };

    /// <summary>
    /// A facade interface that aggregates main functions of few other presenters.
    /// </summary>
    public interface IPresentersFacade
    {
        Task<bool> ShowMessage(IBookmark bmk, BookmarkNavigationOptions options = BookmarkNavigationOptions.Default);
        bool CanShowThreads { get; }
        void ShowThread(IThread thread);
        void ShowLogSource(ILogSource source);
        void ShowMessageProperties();
        Task SaveLogSourceAs(ILogSource logSource);
        void ExecuteThreadPropertiesDialog(IThread thread);
        void ShowPreprocessing(LogJoint.Preprocessing.ILogSourcePreprocessing preproc);
        void ShowAboutDialog();
        void ShowOptionsDialog();
        void ShowHistoryDialog();
        void ShowSettings();
    };
};
using LogJoint.Drawing;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.MessagePropertiesDialog
{
    public interface IView
    {
        IDialog CreateDialog(IDialogViewModel host);
    };

    public interface IDialog
    {
        void Show();
        bool IsDisposed { get; }
    };

    public interface IDialogViewModel
    {
        IChangeNotification ChangeNotification { get; }
        InlineSearch.IViewModel InlineSearch { get; }
        DialogData Data { get; }

        void OnNextClicked(bool highlightedChecked);
        void OnPrevClicked(bool highlightedChecked);

        void OnBookmarkActionClicked();

        void OnThreadLinkClicked();

        void OnSourceLinkClicked();

        void OnContentViewModeChange(int value);

        void OnClosed();

        void OnSearchShortcutPressed(string selection);
    };

    public enum TextSegmentType
    {
        Plain,
        PrimarySearchResult,
        SecondarySearchResult,
        Annotation,
    };

    public record TextSegment(TextSegmentType Type, StringSlice Value);

    public class DialogData
    {
        public string TimeValue;

        public bool ThreadLinkEnabled;
        public string ThreadLinkValue;
        public Color? ThreadLinkBkColor;

        public bool SourceLinkEnabled;
        public string SourceLinkValue;
        public Color? SourceLinkBkColor;

        public string BookmarkedStatusText;
        public string BookmarkActionLinkText;
        public bool BookmarkActionLinkEnabled;

        public string SeverityValue;

        public IReadOnlyList<string> ContentViewModes;
        public int? ContentViewModeIndex;
        public IReadOnlyList<TextSegment> TextSegments;
        public object CustomView;

        public bool HighlightedCheckboxEnabled;

        public bool MessageLinkEnabled;
        public string MessageLinkValue;
    };
};
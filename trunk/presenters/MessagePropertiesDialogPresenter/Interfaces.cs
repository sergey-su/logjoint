using LogJoint.Drawing;

namespace LogJoint.UI.Presenters.MessagePropertiesDialog
{
	public interface IPresenter
	{
		void ShowDialog();
	};


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
		DialogData Data { get; }

		void OnNextClicked(bool highlightedChecked);
		void OnPrevClicked(bool highlightedChecked);

		void OnBookmarkActionClicked();

		void OnThreadLinkClicked();

		void OnSourceLinkClicked();
	};

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

		public string TextValue;

		public bool HighlightedCheckboxEnabled;
	};
};
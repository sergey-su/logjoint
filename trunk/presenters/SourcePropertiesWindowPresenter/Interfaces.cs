using System;

namespace LogJoint.UI.Presenters.SourcePropertiesWindow
{
	public interface IPresenter
	{
		void UpdateOpenWindow();
		void ShowWindow(ILogSource forSource);
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents viewEvents);
		IWindow CreateWindow();
		uint DefaultControlForeColor { get; }
		void ShowErrorPopup(string caption, string text);
	};

	public interface IWindow
	{
		void ShowDialog();
		void Close();
		void WriteControl(ControlFlag flags, string value);
		string ReadControl(ControlFlag flags);
		void ShowColorSelector(ModelColor[] options);
	};

	[Flags]
	public enum ControlFlag
	{
		Value = 1024,
		Visibility = 2048,
		Checked = 4096,
		BackColor = 8192,
		ForeColor = 16384,
		Enabled = 32768,

		ControlIdMask = 0xff,
		NameEditbox = 1,
		FormatTextBox,
		VisibleCheckBox,
		ColorPanel,
		StateDetailsLink,
		StateLabel,
		LoadedMessagesTextBox,
		LoadedMessagesWarningIcon,
		LoadedMessagesWarningLinkLabel,
		TrackChangesLabel,
		SuspendResumeTrackingLink,
		FirstMessageLinkLabel,
		LastMessageLinkLabel,
		SaveAsButton,
		AnnotationTextBox,
		TimeOffsetTextBox
	};

	public interface IViewEvents
	{
		void OnVisibleCheckBoxClicked();
		void OnSuspendResumeTrackingLinkClicked();
		void OnStateDetailsLinkClicked();
		void OnBookmarkLinkClicked(ControlFlag controlId);
		void OnSaveAsButtonClicked();
		void OnClosingDialog();
		void OnLoadedMessagesWarningIconClicked();
		void OnChangeColorLinkClicked();
		void OnColorSelected(ModelColor color);
	};
};
using System.Threading.Tasks;
using LogJoint.Drawing;

namespace LogJoint.UI.Presenters.SourcePropertiesWindow
{
	public interface IPresenter
	{
		void ShowWindow(ILogSource forSource);
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
		IWindow CreateWindow();
	};

	public interface IWindow
	{
		Task ShowModalDialog();
		void Close();
		void ShowColorSelector(Color[] options);
	};

	public struct ControlState
	{
		public bool Disabled;
		public bool Hidden;
		public Color? BackColor;
		public Color? ForeColor;
		public string Text;
		public bool? Checked;
		public string Tooltip;
	};

	public interface IViewState
	{
		ControlState NameEditbox { get; }
		ControlState FormatTextBox { get; }
		ControlState VisibleCheckBox { get; }
		ControlState ColorPanel { get; }
		ControlState StateDetailsLink { get; }
		ControlState StateLabel { get; }
		ControlState LoadedMessagesTextBox { get; }
		ControlState LoadedMessagesWarningIcon { get; }
		ControlState LoadedMessagesWarningLinkLabel { get; }
		ControlState TrackChangesLabel { get; }
		ControlState SuspendResumeTrackingLink { get; }
		ControlState FirstMessageLinkLabel { get; }
		ControlState LastMessageLinkLabel { get; }
		ControlState SaveAsButton { get; }
		ControlState AnnotationTextBox { get; }
		ControlState TimeOffsetTextBox { get; }
		ControlState CopyPathButton { get; }
		ControlState OpenContainingFolderButton { get; }
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }

		IViewState ViewState { get; }

		void OnVisibleCheckBoxChange(bool value);
		void OnSuspendResumeTrackingLinkClicked();
		void OnStateDetailsLinkClicked();
		void OnFirstKnownMessageLinkClicked();
		void OnLastKnownMessageLinkClicked();
		void OnSaveAsButtonClicked();
		void OnClosingDialog();
		void OnLoadedMessagesWarningIconClicked();
		void OnChangeColorLinkClicked();
		void OnColorSelected(Color color);
		void OnCopyButtonClicked();
		void OnOpenContainingFolderButtonClicked();
		void OnChangeAnnotation(string value);
		void OnChangeChangeTimeOffset(string value);
	};
};

using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.StatusReports
{
	public interface IReport : IDisposable
	{
		void SetCancellationHandler(Action handler);
		void ShowStatusPopup(string caption, string text, bool autoHide);
		void ShowStatusPopup(string caption, IEnumerable<MessagePart> parts, bool autoHide);
		void ShowStatusText(string text, bool autoHide);
	};

	public interface IPresenter
	{
		IReport CreateNewStatusReport();
		void CancelActiveStatus();
	};

	public class MessagePart
	{
		public readonly string Text;
		public MessagePart(string text) { Text = text; }
	};

	public class MessageLink : MessagePart
	{
		public readonly Action Click;
		public MessageLink(string text, Action click) : base(text) { Click = click; }
	};

	public class PopupData
	{
		public string Caption { get; internal set; }
		public IEnumerable<MessagePart> Parts { get; internal set; }
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		string StatusText { get; }
		bool CancelLongRunningControlVisibile { get; }
		PopupData PopupData { get; }
		void OnCancelLongRunningProcessButtonClicked();
	};
};
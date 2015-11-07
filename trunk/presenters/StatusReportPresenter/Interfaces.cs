using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.StatusReports
{
	public interface IReport : IDisposable
	{
		void ShowStatusPopup(string caption, string text, bool autoHide);
		void ShowStatusPopup(string caption, IEnumerable<MessagePart> parts, bool autoHide);
		void ShowStatusText(string text, bool autoHide);
	};

	public interface IPresenter
	{
		IReport CreateNewStatusReport();
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

	public interface IView
	{
		void SetStatusText(string value);
		void HidePopup();
		void ShowPopup(string caption, IEnumerable<MessagePart> parts);
	};
};
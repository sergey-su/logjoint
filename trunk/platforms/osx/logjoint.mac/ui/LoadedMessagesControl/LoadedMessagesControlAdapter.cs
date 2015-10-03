using System;
using MonoMac.Foundation;
using LogJoint.UI.Presenters.LoadedMessages;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public class LoadedMessagesControlAdapter: NSObject, IView
	{
		LogViewerControlAdapter logViewerControlAdapter;
		IPresenter viewEvents;

		[Export("logViewerPlaceholder")]
		NSView logViewerPlaceholder { get; set;}

		[Export("view")]
		public LoadedMessagesControl View { get; set;}

		public LoadedMessagesControlAdapter()
		{
			NSBundle.LoadNib ("LoadedMessagesControl", this);
			logViewerControlAdapter = new LogViewerControlAdapter();
			logViewerControlAdapter.View.MoveToPlaceholder(logViewerPlaceholder);
		}

		#region IView implementation

		void IView.SetPresenter(IPresenter presenter)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetRawViewButtonState(bool visible, bool checked_)
		{
		}

		void IView.SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked)
		{
		}

		void IView.Focus()
		{
			logViewerControlAdapter.View.BecomeFirstResponder();
		}

		LogJoint.UI.Presenters.LogViewer.IView IView.MessagesView
		{
			get
			{
				return logViewerControlAdapter;
			}
		}

		#endregion
	}
}


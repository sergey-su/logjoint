using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.TestDialog;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public partial class TestFormatDialogController : NSWindowController, IView
	{
		IViewEvents events;
		LogViewerControlAdapter logViewerControlAdapter;

		public TestFormatDialogController () : base ("TestFormatDialog")
		{
			Window.EnsureCreated();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			logViewerControlAdapter = new LogViewerControlAdapter();
			logViewerControlAdapter.View.MoveToPlaceholder(logViewerPlaceholder);
		}

		partial void OnCloseClicked (Foundation.NSObject sender)
		{
			events.OnCloseButtonClicked();
		}

		void IView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IView.Close ()
		{
			NSApplication.SharedApplication.AbortModal();
			base.Close();
		}

		Presenters.LogViewer.IView IView.LogViewer => logViewerControlAdapter;

		void IView.SetData (string message, TestOutcome testOutcome)
		{
			statusLabel.StringValue = message;
			if (testOutcome == TestOutcome.Success)
			{
				iconLabel.StringValue =  "✔";
				iconLabel.TextColor = Color.FromArgb(255, 53, 204, 75).ToNSColor();
			}
			else if (testOutcome == TestOutcome.Failure)
			{
				iconLabel.StringValue =  "✘";
				iconLabel.TextColor = Color.Red.ToNSColor();
			}
			else
			{
				iconLabel.StringValue =  "";
			}
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.events = eventsHandler;
		}
	}
}

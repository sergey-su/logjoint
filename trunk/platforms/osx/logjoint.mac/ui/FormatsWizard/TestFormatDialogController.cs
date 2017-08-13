using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public partial class TestFormatDialogController : NSWindowController, ITestDialogView
	{
		ITestDialogViewEvents events;
		LogViewerControlAdapter logViewerControlAdapter;

		public TestFormatDialogController (ITestDialogViewEvents events) : base ("TestFormatDialog")
		{
			this.events = events;
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

		void ITestDialogView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void ITestDialogView.Close ()
		{
			NSApplication.SharedApplication.AbortModal();
			base.Close();
		}

		Presenters.LogViewer.IView ITestDialogView.LogViewer => logViewerControlAdapter;

		void ITestDialogView.SetData (string message, TestOutcome testOutcome)
		{
			statusLabel.StringValue = message;
			if (testOutcome == TestOutcome.Success)
			{
				iconLabel.StringValue =  "✔";
				iconLabel.TextColor = System.Drawing.Color.Green.ToNSColor();
			}
			else if (testOutcome == TestOutcome.Failure)
			{
				iconLabel.StringValue =  "✘";
				iconLabel.TextColor = System.Drawing.Color.Red.ToNSColor();
			}
			else
			{
				iconLabel.StringValue =  "";
			}
		}
	}
}

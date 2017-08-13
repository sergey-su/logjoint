using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;

namespace LogJoint.UI
{
	public partial class EditSampleLogDialogController : NSWindowController, IEditSampleDialogView
	{
		IEditSampleDialogViewEvents events;

		public EditSampleLogDialogController (IEditSampleDialogViewEvents events) : base ("EditSampleLogDialog")
		{
			this.events = events;
			Window.EnsureCreated();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			textView.Font = NSFont.FromFontName("Courier", 11);
		}

		partial void OnCancelClicked (Foundation.NSObject sender)
		{
			events.OnCloseButtonClicked(accepted: false);
		}

		partial void OnLoadFileClicked (Foundation.NSObject sender)
		{
			events.OnLoadSampleButtonClicked();
		}

		partial void OnOkClicked (Foundation.NSObject sender)
		{
			events.OnCloseButtonClicked(accepted: true);
		}

		void IEditSampleDialogView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IEditSampleDialogView.Close ()
		{
			this.Close();
			NSApplication.SharedApplication.StopModal();
		}

		string IEditSampleDialogView.SampleLogTextBoxValue 
		{
			get => textView.Value;
			set => textView.Value = value;
		}
	}
}

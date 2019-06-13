using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.EditSampleDialog;

namespace LogJoint.UI
{
	public partial class EditSampleLogDialogController : NSWindowController, IView
	{
		IViewEvents events;

		public EditSampleLogDialogController () : base ("EditSampleLogDialog")
		{
			Window.EnsureCreated();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			textView.AutomaticQuoteSubstitutionEnabled = false;
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

		void IView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IView.Close ()
		{
			this.Close();
			NSApplication.SharedApplication.StopModal();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.events = eventsHandler;
		}

		string IView.SampleLogTextBoxValue 
		{
			get => textView.Value;
			set => textView.Value = value;
		}
	}
}

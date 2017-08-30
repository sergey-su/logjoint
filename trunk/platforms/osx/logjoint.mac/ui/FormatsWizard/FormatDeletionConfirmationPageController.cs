using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.FormatDeleteConfirmPage;


namespace LogJoint.UI
{
	public partial class FormatDeletionConfirmationPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;


		// Called when created from unmanaged code
		public FormatDeletionConfirmationPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public FormatDeletionConfirmationPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public FormatDeletionConfirmationPageController () : base ("FormatDeletionConfirmationPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.Update (
			string messageLabelText, 
			string descriptionTextBoxValue, 
			string fileNameTextBoxValue, 
			string dateTextBoxValue
		)
		{
			View.GetType();
			messageLabel.StringValue = messageLabelText;
			descriptionTextBox.StringValue = descriptionTextBoxValue;
			fileNameTextBox.StringValue = fileNameTextBoxValue;
			dateTextBox.StringValue = dateTextBoxValue;
		}

		//strongly typed view accessor
		public new FormatDeletionConfirmationPage View {
			get {
				return (FormatDeletionConfirmationPage)base.View;
			}
		}
	}
}

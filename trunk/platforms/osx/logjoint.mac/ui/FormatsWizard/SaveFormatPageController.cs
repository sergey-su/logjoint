using System;
using Foundation;

using LogJoint.UI.Presenters.FormatsWizard.SaveFormatPage;

namespace LogJoint.UI
{
	public partial class SaveFormatPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;

		// Called when created from unmanaged code
		public SaveFormatPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public SaveFormatPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public SaveFormatPageController () : base ("SaveFormatPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		public new SaveFormatPage View {
			get {
				return (SaveFormatPage)base.View;
			}
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			View.EnsureCreated();
		}

		partial void OnFileNameBasisTextBoxChanged (Foundation.NSObject sender)
		{
			eventsHandler.OnFileNameBasisTextBoxChanged();
		}

		string IView.FileNameBasisTextBoxValue
		{ 
			get => fileNameBasisTextBox.StringValue;
			set => fileNameBasisTextBox.StringValue = value;
		}

		string IView.FileNameTextBoxValue
		{ 
			get => fileNameTextBox.StringValue;
			set => fileNameTextBox.StringValue = value;
		}
	}
}

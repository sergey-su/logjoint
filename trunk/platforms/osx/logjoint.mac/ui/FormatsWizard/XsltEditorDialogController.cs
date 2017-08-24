using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.XsltEditorDialog;

namespace LogJoint.UI
{
	public partial class XsltEditorDialogController : NSWindowController, IView
	{
		IViewEvents events;

		public XsltEditorDialogController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public XsltEditorDialogController (NSCoder coder) : base (coder)
		{
		}

		public XsltEditorDialogController () : base ("XsltEditorDialog")
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			codeTextView.Font = NSFont.FromFontName("Courier", 11);
			helpLink.LinkClicked = (sender, e) => events.OnHelpLinkClicked();
		}

		void IView.SetEventsHandler (IViewEvents events)
		{
			this.events = events;
			Window.EnsureCreated();
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

		void IView.InitStaticControls (string titleValue, string helpLinkValue)
		{
			titleLabel.StringValue = titleValue;
			helpLink.StringValue = helpLinkValue;
		}

		string IView.CodeTextBoxValue 
		{ 
			get => codeTextView.Value;
			set => codeTextView.Value = value;
		}

		partial void OnCancelClicked (Foundation.NSObject sender)
		{
			events.OnCancelClicked();
		}

		partial void OnOkClicked (Foundation.NSObject sender)
		{
			events.OnOkClicked();
		}

		partial void OnTestClicked (Foundation.NSObject sender)
		{
			events.OnTestButtonClicked();
		}
	}
}

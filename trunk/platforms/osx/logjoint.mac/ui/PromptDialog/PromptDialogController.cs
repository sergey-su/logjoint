using System;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters;

namespace LogJoint.UI
{
	public partial class PromptDialogController : NSWindowController, IPromptDialog
	{
		string dialogResult;
	
		#region Constructors

		// Called when created from unmanaged code
		public PromptDialogController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public PromptDialogController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public PromptDialogController () : base ("PromptDialog")
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		string IPromptDialog.ExecuteDialog(string caption, string prompt, string defaultValue)
		{
			Window.Title = caption ?? "Prompt";
			promptLabel.StringValue = prompt ?? "";
			contentTextField.Value = defaultValue ?? "";
			dialogResult = null;
			NSApplication.SharedApplication.RunModalForWindow(Window);
			return dialogResult;
		}

		new PromptDialog Window {
			get {
				return (PromptDialog)base.Window;
			}
		}

		partial void OnAcceptClicked (Foundation.NSObject sender)
		{
			EndDialog(true);
		}

		partial void OnCancelClicked (Foundation.NSObject sender)
		{
			EndDialog(false);
		}

		void EndDialog(bool acceptNewValue)
		{
			if (acceptNewValue)
				dialogResult = contentTextField.Value;
			NSApplication.SharedApplication.AbortModal();
			Close();
		}
	}
}

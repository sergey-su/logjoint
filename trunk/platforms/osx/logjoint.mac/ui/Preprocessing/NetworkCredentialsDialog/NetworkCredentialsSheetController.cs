using System;
using Foundation;
using AppKit;
using System.Net;

namespace LogJoint.UI
{
	public class NetworkCredentialsDialogController: NSObject
	{
		string title;
		bool noUserName;
		bool confirmed;

		public static NetworkCredential ShowSheet(NSWindow inWindow, string title, bool noUserName) 
		{
			var dlg = new NetworkCredentialsDialogController(title, noUserName);
			NSApplication.SharedApplication.BeginSheet (dlg.Window, inWindow);
			NSApplication.SharedApplication.RunModalForWindow(dlg.Window);
			if (!dlg.confirmed)
				return null;
			return new NetworkCredential(dlg.userNameTextField.StringValue, dlg.passwordTextField.StringValue);
		}

		[Export("window")]
		public NetworkCredentialsSheet Window { get; set;}

		[Outlet]
		public NSTextField captionLabel { get; set; }

		[Outlet]
		public NSSecureTextField passwordTextField { get; set; }

		[Outlet]
		public NSTextField userNameTextField { get; set; }

	
		[Export ("OnCancelled:")]
		public void OnCancelled(NSObject sender)
		{
			confirmed = false;
			CloseSheet();
		}

		[Export ("OnConfirmed:")]
		public void OnConfirmed(NSObject sender)
		{
			confirmed = true;
			CloseSheet();
		}

		NetworkCredentialsDialogController(string title, bool noUserName)
		{
			this.title = title;
			this.noUserName = noUserName;
			NSBundle.LoadNib ("NetworkCredentialsSheet", this);
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			captionLabel.StringValue = title; 
			userNameTextField.Enabled = !noUserName;
			userNameTextField.StringValue = noUserName ? "N/A" : "";
		}
			
		void CloseSheet() 
		{
			NSApplication.SharedApplication.EndSheet (Window);
			Window.Close();
			NSApplication.SharedApplication.StopModal();
		}
	}
}


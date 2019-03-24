
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.About;

namespace LogJoint.UI
{
	public partial class AboutDialogAdapter : NSWindowController, IView
	{
		IViewEvents eventsHandler;

		#region Constructors

		// Called when created from unmanaged code
		public AboutDialogAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public AboutDialogAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public AboutDialogAdapter()
			: base("AboutDialog")
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public new AboutDialog Window
		{
			get
			{
				return (AboutDialog)base.Window;
			}
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.Show(
			string text, 
			string feedbackText,
			string feedbackLink,
			string shareText, 
			string shareTextWin, string winInstallerLink, 
			string shareTextMac, string macInstallerLink)
		{
			var wnd = Window;
			textField.StringValue = text;

			shareLabel.Hidden = shareText == null;
			shareLabel.StringValue = shareText ?? "";

			macInstallerText.Hidden = macInstallerLink == null;
			macInstallerLinkField.Hidden = macInstallerLink == null;
			copyMacLinkButton.Hidden = macInstallerLink == null;
			macInstallerText.StringValue = shareTextMac ?? "";
			macInstallerLinkField.StringValue = macInstallerLink ?? "";

			winInstallerText.Hidden = winInstallerLink == null;
			winInstallerLinkLabel.Hidden = winInstallerLink == null;
			copyWinLinkButton.Hidden = winInstallerLink == null;
			winInstallerText.StringValue = shareTextWin ?? "";
			winInstallerLinkLabel.StringValue = winInstallerLink ?? "";

			feedbackLebel.Hidden = feedbackLink == null;
			feedbackLinkLabel.Hidden = feedbackLink == null;
			feedbackLebel.StringValue = feedbackText ?? "";
			feedbackLinkLabel.StringValue = feedbackLink ?? "";

			this.ShowWindow(this);
		}

		void IView.SetAutoUpdateControlsState (
			bool featureEnabled, bool checkNowEnabled,
			string status, string details
		)
		{
			var wnd = Window;
			updatesCaptionLabel.Hidden = !featureEnabled;
			updatesStatusLabel.Hidden = !featureEnabled;
			updateNowButton.Hidden = !featureEnabled;

			updatesStatusLabel.StringValue = status ?? "";
			updateNowButton.ToolTip = details ?? "";

			updateNowButton.Enabled = checkNowEnabled;
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			Window.DidResignKey += (s, e) =>
			{
				this.Close();
			};
			feedbackLinkLabel.LinkClicked = (s, e) =>
			{
				eventsHandler.OnFeedbackLinkClicked();	
			};
		}

		partial void OnCopyMacInstallerClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnCopyMacInstallerLink();
		}

		partial void OnCopyWinInstallerClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnCopyWinInstallerLink();
		}

		partial void OnUpdateNowClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnUpdateNowClicked();
		}
	}
}


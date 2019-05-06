
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.MessagePropertiesDialog;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public partial class MessagePropertiesDialogAdapter : AppKit.NSWindowController, IDialog
	{
		private readonly IChangeNotification changeNotification;
		private readonly IDialogViewModel viewModel;
		private readonly ISubscription subscription;

		public MessagePropertiesDialogAdapter(IChangeNotification changeNotification, IDialogViewModel viewModel)
			: base("MessagePropertiesDialog")
		{
			this.changeNotification = changeNotification;
			this.viewModel = viewModel;

			NSColor resolveColor (Color? cl) =>
				cl.HasValue ? cl.Value.ToNSColor() : null;

			var update = Updaters.Create (() => viewModel.Data, viewData =>
			{
				timestampLabel.StringValue = viewData.TimeValue;
				threadLabel.StringValue = viewData.ThreadLinkValue;
				threadLabel.BackgroundColor =
					viewData.ThreadLinkEnabled ? resolveColor (viewData.ThreadLinkBkColor) : null;
				threadLabel.IsEnabled = viewData.ThreadLinkEnabled;
				sourceLabel.StringValue = viewData.SourceLinkValue;
				sourceLabel.IsEnabled = viewData.SourceLinkEnabled;
				sourceLabel.BackgroundColor = resolveColor (viewData.SourceLinkBkColor);
				bookmarkStatusLabel.StringValue = viewData.BookmarkedStatusText;
				bookmarkActionLabel.StringValue = viewData.BookmarkActionLinkText;
				bookmarkActionLabel.IsEnabled = viewData.BookmarkActionLinkEnabled;
				severityLabel.StringValue = viewData.SeverityValue;
				textView.Value = viewData.TextValue;
				hlCheckbox.Enabled = viewData.HighlightedCheckboxEnabled;
			});

			subscription = changeNotification.CreateSubscription (update, initiallyActive: false);
		}

		public new MessagePropertiesDialog Window
		{
			get { return (MessagePropertiesDialog)base.Window; }
		}

		bool IDialog.IsDisposed => false;

		void IDialog.Show ()
		{
			Window.MakeKeyAndOrderFront (null);
			subscription.Active = true;
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			Window.WillClose += (s, e) => subscription.Active = false;
			threadLabel.LinkClicked = (s, e) => viewModel.OnThreadLinkClicked ();
			threadLabel.SingleLine = true;
			sourceLabel.LinkClicked = (s, e) => viewModel.OnSourceLinkClicked ();
			sourceLabel.SingleLine = true;
			bookmarkActionLabel.LinkClicked = (s, e) => viewModel.OnBookmarkActionClicked ();
		}

		partial void onNextMessageCliecked (Foundation.NSObject sender)
		{
			viewModel.OnNextClicked (hlCheckbox.State == NSCellStateValue.On);
		}

		partial void onPrevMessageClicked (Foundation.NSObject sender)
		{
			viewModel.OnPrevClicked (hlCheckbox.State == NSCellStateValue.On);
		}

		partial void onCloseClicked (Foundation.NSObject sender)
		{
			Window.Close ();
		}
	}

	public class MessagePropertiesDialogView: IView
	{
		private readonly IChangeNotification changeNotification;

		public MessagePropertiesDialogView (IChangeNotification changeNotification)
		{
			this.changeNotification = changeNotification;
		}

		IDialog IView.CreateDialog (IDialogViewModel viewModel)
		{
			return new MessagePropertiesDialogAdapter (changeNotification, viewModel);
		}
	};
}


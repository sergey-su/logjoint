
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

		public MessagePropertiesDialogAdapter(IDialogViewModel viewModel)
			: base("MessagePropertiesDialog")
		{
			this.changeNotification = viewModel.ChangeNotification;
			this.viewModel = viewModel;

			NSColor resolveColor (Color? cl) =>
				cl.HasValue ? cl.Value.ToNSColor() : null;

			var update = Updaters.Create (() => viewModel.Data, (viewData, prevData) =>
			{
				this.Window.EnsureCreated ();

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
				var prevCustomView = prevData?.CustomView as NSView;
				if (viewData.CustomView is NSView customView) {
					textContentScrollView.Hidden = true;
					if (prevCustomView != customView) {
						prevCustomView?.RemoveFromSuperview();
						customView.MoveToPlaceholder (messageContentContainerView);
						customView.Hidden = false;
					}
				} else {
					textContentScrollView.Hidden = false;
					textView.Value = viewData.TextValue ?? "";
					prevCustomView?.RemoveFromSuperview ();
				}
				hlCheckbox.Enabled = viewData.HighlightedCheckboxEnabled;
				contentModeSegmentedControl.SegmentCount = viewData.ContentViewModes.Count;
				foreach (var i in viewData.ContentViewModes.Select ((lbl, idx) => (lbl, idx)))
					contentModeSegmentedControl.SetLabel (i.lbl, i.idx);
				if (viewData.ContentViewModeIndex != null)
					contentModeSegmentedControl.SelectedSegment = viewData.ContentViewModeIndex.Value; 
			});

			changeNotification.CreateSubscription (update);
		}

		public new MessagePropertiesDialog Window
		{
			get { return (MessagePropertiesDialog)base.Window; }
		}

		bool IDialog.IsDisposed => false;

		void IDialog.Show ()
		{
			Window.MakeKeyAndOrderFront (null);
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			Window.owner = this;
			Window.WillClose += (s, e) => viewModel.OnClosed();
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

		partial void onViewModeChanged (Foundation.NSObject sender)
		{
			viewModel.OnContentViewModeChange ((int)contentModeSegmentedControl.SelectedSegment);
		}

		public void OnCancelOp ()
		{
			Window.Close ();
		}
	}

	public class MessagePropertiesDialogView: IView
	{
		IDialog IView.CreateDialog (IDialogViewModel viewModel)
		{
			return new MessagePropertiesDialogAdapter (viewModel);
		}
	};
}


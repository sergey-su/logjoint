
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.SourcePropertiesWindow;
using LogJoint.Drawing;
using ObjCRuntime;

namespace LogJoint.UI
{
	public partial class SourcePropertiesDialogAdapter : AppKit.NSWindowController, IWindow
	{
		readonly IViewModel viewModel;
		NSEvent changeColorNSEvent;

		public SourcePropertiesDialogAdapter(IViewModel viewModel)
			: base("SourcePropertiesDialog")
		{
			this.viewModel = viewModel;

			viewModel.ChangeNotification.CreateSubscription (
				Updaters.Create (() => viewModel.ViewState, UpdateView)
			);
		}

		void IWindow.ShowDialog()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IWindow.ShowColorSelector(Color[] options)
		{
			if (changeColorNSEvent == null)
				return;
			var menu = new NSMenu();
			foreach (var opt in options)
			{
				var item = new NSMenuItem();
				var imgSize = new CoreGraphics.CGSize (50, 12);
				var img = new NSImage (imgSize);
				img.LockFocus ();
				using (var path = NSBezierPath.FromRect (
					new CoreGraphics.CGRect (0, 0, imgSize.Width, imgSize.Height)))
				using (var cl = opt.ToNSColor()) {
					cl.SetFill ();
					path.Fill ();
				}
				img.UnlockFocus ();
				item.Image = img;
				item.Title = "";
				item.Action = new Selector("OnColorMenuItemClicked:");
				item.Target = this;
				item.Tag = unchecked ((int)opt.ToUnsignedArgb());
				menu.AddItem(item);
			}
			NSMenu.PopUpContextMenu(menu, changeColorNSEvent, changeColorLinkLabel);
		}

		void UpdateView (IViewState viewState)
		{
			bool updateControl(ControlState state, NSControl control)
			{
				var txt = state.Text ?? "";
				bool result = control.StringValue != txt;
				if (result)
					control.StringValue = txt;
				control.Hidden = state.Hidden;
				control.Enabled = !state.Disabled;
				control.ToolTip = state.Tooltip ?? "";
				return result;
			}

			void updateTextField(ControlState state, NSTextField control)
			{
				if (updateControl (state, control)) {
					if (control.CurrentEditor != null)
						control.CurrentEditor.SelectedRange = new NSRange ();
				}
				control.BackgroundColor = state.BackColor != null ? state.BackColor.Value.ToNSColor() : NSColor.TextBackground;
			}

			void updateLinkLabel (ControlState state, NSLinkLabel control)
			{
				control.StringValue = state.Text ?? "";
				control.Hidden = state.Hidden;
				control.IsEnabled = !state.Disabled;
				control.TextColor = state.ForeColor != null ? state.ForeColor.Value.ToNSColor () : NSColor.LinkColor;
			}

			void updateButton (ControlState state, NSButton control)
			{
				updateControl (state, control);
				if (state.Checked != null)
					control.State = state.Checked == true ? NSCellStateValue.On : NSCellStateValue.Off;
			}

			updateTextField (viewState.NameEditbox, nameTextField);
			updateTextField (viewState.FormatTextBox, formatTextField);
			updateButton (viewState.VisibleCheckBox, visibleCheckbox);
			updateTextField (viewState.ColorPanel, colorPanel);
			updateLinkLabel (viewState.StateDetailsLink, stateDetailsLink);
			updateTextField (viewState.StateLabel, stateLabel);
			updateTextField (viewState.LoadedMessagesTextBox, loadedMessagesLabel);
			updateButton (viewState.LoadedMessagesWarningIcon, loadedMessagesWarningIcon);
			updateLinkLabel (viewState.LoadedMessagesWarningLinkLabel, loadedMessagesWarningLinkLabel);
			updateTextField (viewState.TrackChangesLabel, trackChangesLabel);
			updateLinkLabel (viewState.SuspendResumeTrackingLink, suspendResumeTrackingLinkLabel);
			updateLinkLabel (viewState.FirstMessageLinkLabel, firstMessageLinkLabel);
			updateLinkLabel (viewState.LastMessageLinkLabel, lastMessageLinkLabel);
			updateButton (viewState.SaveAsButton, saveAsButton);
			updateTextField (viewState.AnnotationTextBox, annotationEditBox);
			updateTextField (viewState.TimeOffsetTextBox, timeShiftTextField);
			updateButton (viewState.CopyPathButton, copyPathButton);
			updateButton (viewState.OpenContainingFolderButton, openContainingFolderButton);
		}

		[Export("OnColorMenuItemClicked:")]
		public void OnColorMenuItemClicked(NSMenuItem sender)
		{
			viewModel.OnColorSelected(new Color(unchecked ((uint) sender.Tag)));
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			copyPathButton.Image.Template = true;

			Window.WillClose += (object sender, EventArgs e) =>
			{
				viewModel.OnClosingDialog();
				NSApplication.SharedApplication.AbortModal();
			};

			firstMessageLinkLabel.LinkClicked += (s, e) => viewModel.OnFirstKnownMessageLinkClicked();
			lastMessageLinkLabel.LinkClicked += (s, e) => viewModel.OnLastKnownMessageLinkClicked();
			suspendResumeTrackingLinkLabel.LinkClicked += (s, e) => viewModel.OnSuspendResumeTrackingLinkClicked ();
			changeColorLinkLabel.StringValue = "change";
			changeColorLinkLabel.LinkClicked += (s, e) =>
			{
				changeColorNSEvent = e.NativeEvent;
				viewModel.OnChangeColorLinkClicked();
			};
			loadedMessagesWarningLinkLabel.LinkClicked += (s, e) => viewModel.OnLoadedMessagesWarningIconClicked ();
			annotationEditBox.Changed += (s, e) => viewModel.OnChangeAnnotation (annotationEditBox.StringValue);
			timeShiftTextField.Changed += (s, e) => viewModel.OnChangeChangeTimeOffset (timeShiftTextField.StringValue);
		}

		partial void OnCloseButtonClicked (Foundation.NSObject sender)
		{
			Window.Close();
		}

		partial void OnVisibleCheckboxClicked (Foundation.NSObject sender)
		{
			viewModel.OnVisibleCheckBoxChange(visibleCheckbox.State == NSCellStateValue.On);
		}

		partial void OnSaveButtonClicked (Foundation.NSObject sender)
		{
			viewModel.OnSaveAsButtonClicked();
		}

		partial void OnCopyButtonClicked (Foundation.NSObject sender)
		{
			viewModel.OnCopyButtonClicked();
		}

		partial void OnOpenContainingFolderButtonClicked (NSObject sender)
		{
			viewModel.OnOpenContainingFolderButtonClicked();
		}

		new SourcePropertiesDialog Window
		{
			get { return (SourcePropertiesDialog)base.Window; }
		}
	}

	public class SourcePropertiesDialogView: IView
	{
		IViewModel viewModel;

		void IView.SetViewModel(IViewModel viewEvents)
		{
			this.viewModel = viewEvents;
		}

		IWindow IView.CreateWindow()
		{
			var wnd = new SourcePropertiesDialogAdapter(viewModel);
			wnd.Window.GetHashCode(); // force loading from nib
			return wnd;
		}
	};
}


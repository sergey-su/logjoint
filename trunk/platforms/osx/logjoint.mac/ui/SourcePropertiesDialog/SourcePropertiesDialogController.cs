
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.SourcePropertiesWindow;
using LogJoint.Drawing;
using MonoMac.ObjCRuntime;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public partial class SourcePropertiesDialogAdapter : MonoMac.AppKit.NSWindowController, IWindow
	{
		readonly IViewEvents viewEvents;
		readonly Dictionary<ControlFlag, NSView> controls = new Dictionary<ControlFlag, NSView>();
		NSEvent changeColorNSEvent;

		#region Constructors

		// Called when created from unmanaged code
		public SourcePropertiesDialogAdapter(IntPtr handle)
			: base(handle)
		{
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public SourcePropertiesDialogAdapter(NSCoder coder)
			: base(coder)
		{
		}
		
		// Call to load from the XIB/NIB file
		public SourcePropertiesDialogAdapter(IViewEvents viewEvents)
			: base("SourcePropertiesDialog")
		{
			this.viewEvents = viewEvents;
		}
		
		#endregion

		void IWindow.ShowDialog()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IWindow.WriteControl(ControlFlag flags, string value)
		{
			NSView view;
			if (!controls.TryGetValue(flags & ControlFlag.ControlIdMask, out view))
				return;
			NSControl ctrl;
			NSButton btn;
			NSLinkLabel ll;
			NSTextField txt;
			if ((flags & ControlFlag.Value) != 0)
			{
				if ((ctrl = view as NSControl) != null)
				{
					ctrl.StringValue = value;
					if ((txt = view as NSTextField) != null && txt.CurrentEditor != null)
						txt.CurrentEditor.SelectedRange = new NSRange();
				}
				else if ((ll = view as NSLinkLabel) != null)
				{
					ll.StringValue = value;
				}
			}
			else if ((flags & ControlFlag.Checked) != 0)
			{
				if ((btn = view as NSButton) != null)
					btn.State = value != null ? NSCellStateValue.On : NSCellStateValue.Off;
			}
			else if ((flags & ControlFlag.Visibility) != 0)
			{
				view.Hidden = value == null;
			}
			else if ((flags & ControlFlag.BackColor) != 0)
			{
				if ((txt = view as NSTextField) != null)
				{
					txt.BackgroundColor = new ModelColor(uint.Parse(value)).ToColor().ToNSColor();
				}
			}
			else if ((flags & ControlFlag.ForeColor) != 0)
			{
				if ((ll = view as NSLinkLabel) != null)
				{
					ll.TextColor = new ModelColor(uint.Parse(value)).ToColor().ToNSColor();
				}
			}
			else if ((flags & ControlFlag.Enabled) != 0)
			{
				if ((ctrl = view as NSControl) != null)
				{
					ctrl.Enabled = value != null;
				}
				else if ((ll = view as NSLinkLabel) != null)
				{
					ll.IsEnabled = value != null;
				}
			}
		}

		string IWindow.ReadControl(ControlFlag flags)
		{
			NSView view;
			if (!controls.TryGetValue(flags & ControlFlag.ControlIdMask, out view))
				return null;
			if ((flags & ControlFlag.Value) != 0)
				return view is NSControl ? (view as NSControl).StringValue : null;
			else if ((flags & ControlFlag.Checked) != 0)
				return view is NSButton && (view as NSButton).State == NSCellStateValue.On ? "" : null;
			else
				return null;
		}

		void IWindow.ShowColorSelector(ModelColor[] options)
		{
			if (changeColorNSEvent == null)
				return;
			var menu = new NSMenu();
			foreach (var opt in options)
			{
				var item = new NSMenuItem();
				var dict = new NSMutableDictionary();
				dict.SetValueForKey(opt.ToColor().ToNSColor(), NSAttributedString.BackgroundColorAttributeName);
				var attrStr = new NSAttributedString("                  ", dict);
				item.AttributedTitle = attrStr;
				item.Action = new Selector("OnColorMenuItemClicked:");
				item.Target = this;
				item.Tag = unchecked ((int)opt.Argb);
				menu.AddItem(item);
			}
			NSMenu.PopUpContextMenu(menu, changeColorNSEvent, changeColorLinkLabel);
		}

		[Export("OnColorMenuItemClicked:")]
		public void OnColorMenuItemClicked(NSMenuItem sender)
		{
			viewEvents.OnColorSelected(new ModelColor(unchecked ((uint) sender.Tag)));
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			controls[ControlFlag.NameEditbox] = nameTextField;
			controls[ControlFlag.FormatTextBox] = formatTextField;
			controls[ControlFlag.VisibleCheckBox] = visibleCheckbox;
			controls[ControlFlag.ColorPanel] = colorPanel;
			controls[ControlFlag.StateDetailsLink] = stateDetailsLink;
			controls[ControlFlag.StateLabel] = stateLabel;
			controls[ControlFlag.LoadedMessagesTextBox] = loadedMessagesLabel;
			controls[ControlFlag.LoadedMessagesWarningIcon] = loadedMessagesWarningIcon;
			controls[ControlFlag.LoadedMessagesWarningLinkLabel] = loadedMessagesWarningLinkLabel;
			controls[ControlFlag.TrackChangesLabel] = trackChangesLabel;
			controls[ControlFlag.SuspendResumeTrackingLink] = suspendResumeTrackingLinkLabel;
			controls[ControlFlag.FirstMessageLinkLabel] = firstMessageLinkLabel;
			controls[ControlFlag.LastMessageLinkLabel] = lastMessageLinkLabel;
			controls[ControlFlag.SaveAsButton] = saveAsButton;
			controls[ControlFlag.AnnotationTextBox] = annotationEditBox;
			controls[ControlFlag.TimeOffsetTextBox] = timeShiftTextField;
			controls[ControlFlag.CopyPathButton] = copyPathButton;

			Window.WillClose += (object sender, EventArgs e) =>
			{
				viewEvents.OnClosingDialog();
				NSApplication.SharedApplication.AbortModal();
			};

			firstMessageLinkLabel.LinkClicked += (object sender, NSLinkLabel.LinkClickEventArgs e) =>
			{
				viewEvents.OnBookmarkLinkClicked(ControlFlag.FirstMessageLinkLabel);
			};
			lastMessageLinkLabel.LinkClicked += (object sender, NSLinkLabel.LinkClickEventArgs e) =>
			{
				viewEvents.OnBookmarkLinkClicked(ControlFlag.LastMessageLinkLabel);
			};
			suspendResumeTrackingLinkLabel.LinkClicked += (object sender, NSLinkLabel.LinkClickEventArgs e) =>
			{
				viewEvents.OnSuspendResumeTrackingLinkClicked();
			};
			changeColorLinkLabel.StringValue = "change";
			changeColorLinkLabel.LinkClicked += (object sender, NSLinkLabel.LinkClickEventArgs e) =>
			{
				changeColorNSEvent = e.NativeEvent;
				viewEvents.OnChangeColorLinkClicked();
			};

			copyPathButton.ToolTip = "copy log source path";
		}
			
		partial void OnCloseButtonClicked (MonoMac.Foundation.NSObject sender)
		{
			Window.Close();
		}

		partial void OnVisibleCheckboxClicked (MonoMac.Foundation.NSObject sender)
		{
			viewEvents.OnVisibleCheckBoxClicked();
		}

		partial void OnSaveButtonClicked (MonoMac.Foundation.NSObject sender)
		{
			viewEvents.OnSaveAsButtonClicked();
		}

		partial void OnCopyButtonClicked (MonoMac.Foundation.NSObject sender)
		{
			viewEvents.OnCopyButtonClicked();
		}

		//strongly typed window accessor
		new SourcePropertiesDialog Window
		{
			get { return (SourcePropertiesDialog)base.Window; }
		}
	}

	public class SourcePropertiesDialogView: IView
	{
		IViewEvents viewEvents;

		void IView.SetEventsHandler(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		IWindow IView.CreateWindow()
		{
			var wnd = new SourcePropertiesDialogAdapter(viewEvents);
			wnd.Window.GetHashCode(); // force loading from nib
			return wnd;
		}

		uint IView.DefaultControlForeColor
		{
			get { unchecked {
				return (uint)NSColor.Text.ToColor().ToArgb();
			} }
		}
	};
}


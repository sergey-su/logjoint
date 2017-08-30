using System;
using System.Linq;
using System.Threading.Tasks;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.NLogGenerationLogPage;

namespace LogJoint.UI
{
	public partial class NLogGenerationLogPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;
		readonly UIUtils.SimpleTableDataSource<MessagesListItem> messagesDataSource = new UIUtils.SimpleTableDataSource<MessagesListItem>();
		NSImage warnIcon, errIcon;
		string lastLayoutTextboxValue = "";

		// Called when created from unmanaged code
		public NLogGenerationLogPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public NLogGenerationLogPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public NLogGenerationLogPageController () : base ("NLogGenerationLogPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				errIcon?.Dispose();
				warnIcon?.Dispose();
			}
			base.Dispose (disposing);
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			messagesTable.DataSource = messagesDataSource;
			messagesTable.Delegate = new MessagesDelegate() { owner = this };
			errIcon = NSImage.ImageNamed("ErrorLogSeverity.png");
			warnIcon = NSImage.ImageNamed("WarnLogSeverity.png");
			templateTextBox.Formatter = new UIUtils.ReadonlyFormatter();
		}

		NSImage GetIcon (IconType iconType)
		{
			return
				iconType == IconType.WarningIcon ? warnIcon :
				iconType == IconType.ErrorIcon ? errIcon :
				null;
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			View.EnsureCreated();
		}

		void IView.Update (
			string layoutTextboxValue, 
			string headerLabelValue, 
			IconType headerIcon, 
			MessagesListItem [] messagesList)
		{
			this.headerLabel.StringValue = headerLabelValue;
			this.templateTextBox.StringValue = layoutTextboxValue;
			this.lastLayoutTextboxValue = layoutTextboxValue;
			this.messagesDataSource.Items.Clear ();
			this.messagesDataSource.Items.AddRange (messagesList);
			this.messagesTable.ReloadData ();
			this.headerIcon.Image = GetIcon (headerIcon);
		}

		void IView.SelectLayoutTextRange (int idx, int len)
		{
			templateTextBox.StringValue = lastLayoutTextboxValue; // restore the value is user modified it.
			// templateTextBox is editable to allow selection modification via CurrentEditor object.
			View.Window.MakeFirstResponder(templateTextBox);
			if (templateTextBox.CurrentEditor != null)
				templateTextBox.CurrentEditor.SelectedRange = new NSRange(idx, len);
		}

		class MessagesDelegate: NSTableViewDelegate
		{
			public NLogGenerationLogPageController owner;
			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				var item = owner.messagesDataSource.Items[(int)row];
				if (tableColumn == owner.textColumn)
				{
					var lbl = new NSLinkLabel();
					lbl.StringValue = item.Text;
					lbl.Links = item.Links.Select(l => new NSLinkLabel.Link(l.Item1, l.Item2, l.Item3)).ToList();
					lbl.LinkClicked = async (sender, e) => 
					{
						// await below is to "re-post" click handling to the end of UI messages queue.
						// Without this workaround currently handled mouse click 
						// de-facto fails SelectLayoutTextRange() that is a part of this link's behavior.
						// The failure is that templateTextBox fails to become first responder
						// which in turn makes it impossible to change TextBox's SelectedRange.
						await Task.Yield(); 
						(e.Link.Tag as Action)?.Invoke();
					};
					return lbl;
				}
				else if (tableColumn == owner.iconColumn)
				{
					return new NSImageView()
					{
						Image = owner.GetIcon(item.Icon),
					};
				}
				return null;
			}

			public override bool SelectionShouldChange (NSTableView tableView)
			{
				return false;
			}
		};
	}
}

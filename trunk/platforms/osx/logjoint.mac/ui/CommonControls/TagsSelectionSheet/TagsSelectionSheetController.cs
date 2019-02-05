using System;
using System.Linq;
using Foundation;
using AppKit;
using System.Collections.Generic;
using LogJoint.UI;

namespace LogJoint.UI
{
	public class TagsSelectionSheetController: NSObject
	{
		List<NSButton> views = new List<NSButton>();
		bool confirmed;

		[Outlet]
		NSTableView table { get; set; }

		[Outlet]
		NSLinkLabel linkLabel { get; set; }

		[Export("window")]
		public TagsSelectionSheet Window { get; set;}

		public TagsSelectionSheetController ()
		{
			NSBundle.LoadNib ("TagsSelectionSheet", this);
		}

		public static HashSet<string> Run(Dictionary<string, bool> tags, NSWindow parentWindow, string focusedTag)
		{
			var dlg = new TagsSelectionSheetController ();
			dlg.Window.GetHashCode();
			int focusedRow = -1;
			foreach (var t in tags) {
				var b = new NSButton () {
					Title = t.Key,
					State = t.Value ? NSCellStateValue.On : NSCellStateValue.Off
				};
				b.SetButtonType (NSButtonType.Switch);
				if (focusedTag == t.Key)
					focusedRow = dlg.views.Count;
				dlg.views.Add (b);
			}
			dlg.table.Delegate = new Delegate () { owner = dlg };
			dlg.table.DataSource = new DataSource () { owner = dlg };
			if (focusedRow >= 0) {
				dlg.table.SelectRow (focusedRow, byExtendingSelection: false);
				dlg.table.ScrollRowToVisible (focusedRow);
			}
			dlg.linkLabel.StringValue = "select: all   none";
			dlg.linkLabel.Links = new [] {
				new NSLinkLabel.Link(8, 3, ""),
				new NSLinkLabel.Link(14, 4, null),
			};
			dlg.linkLabel.LinkClicked = (s, e) =>
				dlg.views.ForEach(b => b.State = e.Link.Tag != null ? NSCellStateValue.On : NSCellStateValue.Off);
			NSApplication.SharedApplication.BeginSheet (dlg.Window, parentWindow);
			NSApplication.SharedApplication.RunModalForWindow (dlg.Window);
			if (!dlg.confirmed)
				return null;
			return new HashSet<string> (
				dlg.views.Where (b => b.State == NSCellStateValue.On).Select (b => b.Title));;
		}

		[Action ("OnCancelled:")]
		void OnCancelled (NSObject sender)
		{
			NSApplication.SharedApplication.EndSheet (Window);
			Window.Close();
			NSApplication.SharedApplication.AbortModal ();
		}

		[Action ("OnConfirmed:")]
		void OnConfirmed (NSObject sender)
		{
			confirmed = true;
			NSApplication.SharedApplication.EndSheet (Window);
			Window.Close();
			NSApplication.SharedApplication.StopModal ();
		}

		class DataSource: NSTableViewDataSource
		{
			public TagsSelectionSheetController owner;

			public override nint GetRowCount (NSTableView tableView) { return owner.views.Count; }
		};

		class Delegate: NSTableViewDelegate
		{
			public TagsSelectionSheetController owner;

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				return owner.views [(int)row];
			}
		};
	}
}


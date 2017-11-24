using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Foundation;
using LogJoint.UI.Presenters.SourcesList;
using LJD = LogJoint.Drawing;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public class SourcesListControlAdapter: NSOutlineViewDelegate, IView
	{
		internal IViewEvents viewEvents;
		SourcesListOutlineDataSource dataSource = new SourcesListOutlineDataSource();
		bool updating;
		LJD.Image currentSourceImage;

		[Export("view")]
		public SourcesListControl View { get; set;}
		[Outlet]
		NSOutlineView outlineView { get; set; }
		[Outlet]
		NSTableColumn sourceCheckedColumn { get; set; }
		[Outlet]
		NSTableColumn sourceDescriptionColumn { get; set; }
		[Outlet]
		NSTableColumn currentSourceColumn { get; set; }

		public SourcesListControlAdapter()
		{
			NSBundle.LoadNib ("SourcesListControl", this);

			outlineView.DataSource = dataSource;
			currentSourceImage = new LJD.Image(NSImage.ImageNamed("FocusedMsgSlave.png"));
			View.owner = this;
			outlineView.Menu = new NSMenu { Delegate = new MenuDelegate { owner = this } };
		}
			
		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.BeginUpdate()
		{
			updating = true;
		}

		void IView.EndUpdate()
		{
			updating = false;
			DoFullUpdate();
		}

		IEnumerable<IViewItem> IView.Items { get { return GetItems(); } }
		
		void IView.Remove(IViewItem item)
		{
			var li = (SourcesListItem)item;
			li.updater = null;
			li.viewEvents = null;
			(li.parent?.items ?? dataSource.Items).Remove(li);
			if (!updating)
				DoFullUpdate();
		}

		IViewItem IView.AddItem(object datum, IViewItem parent)
		{
			var item = new SourcesListItem()
			{
				datum = datum,
				updater = UpdateItem,
				viewEvents = viewEvents,
				parent = parent as SourcesListItem,
			};
			if (item.parent != null)
				item.parent.items.Add(item);
			else
				dataSource.Items.Add(item);
			UpdateItem(item);
			return item;
		}

		void IView.SetTopItem(IViewItem item)
		{
			var li = (SourcesListItem)item;
			if (li.parent != null && !outlineView.IsItemExpanded(li.parent))
				outlineView.ExpandItem(li.parent);
			var row = outlineView.RowForItem(li);
			if (row != -1)
				outlineView.ScrollRowToVisible(row);
		}

		void IView.InvalidateFocusedMessageArea()
		{
			for (var i = 0; i < outlineView.RowCount; ++i)
			{
				var cell = outlineView.GetView(1, i, false);
				if (cell != null)
					cell.NeedsDisplay = true;
			}
		}

		public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item) 
		{
			var sourceItem = item as SourcesListItem;

			if (tableColumn == sourceCheckedColumn)
			{
				if (sourceItem.isChecked == null)
					return null;

				var cellIdentifier = "checked_cell";
				var view = (NSButton)outlineView.MakeView(cellIdentifier, this);

				if (view == null)
				{
					view = new NSButton();
					view.Identifier = cellIdentifier;
					view.SetButtonType(NSButtonType.Switch);
					view.BezelStyle = 0;
					view.ImagePosition = NSCellImagePosition.ImageOnly;
					view.Action = new ObjCRuntime.Selector("ItemChecked:");
				}

				view.Target = sourceItem;
				view.State = sourceItem.isChecked.GetValueOrDefault(false) ? 
					NSCellStateValue.On : NSCellStateValue.Off;
				return view;
			}
			else if (tableColumn == sourceDescriptionColumn)
			{
				var cellIdentifier = "description_cell";
				var view = (NSLinkLabel)outlineView.MakeView(cellIdentifier, this);

				if (view == null)
				{
					view = NSLinkLabel.CreateLabel();
					view.Identifier = cellIdentifier;
					view.LinkClicked = (sender, e) => 
					{
						if (e.NativeEvent.ClickCount == 2)
							viewEvents.OnEnterKeyPressed();
					}; 
				}

				view.BackgroundColor = sourceItem.color != null ?
					sourceItem.color.Value.ToColor().ToNSColor() : NSColor.Clear;
				view.StringValue = sourceItem.text;
				view.Menu = outlineView.Menu;
				return view;
			}
			else if (tableColumn == currentSourceColumn)
			{
				var cellIdentifier = "current_source_mark_cell";
				var view = (NSCustomizableView)outlineView.MakeView(cellIdentifier, this);

				if (view == null)
				{
					view = new NSCustomizableView();
					view.Identifier = cellIdentifier;
				}

				view.OnPaint = (ditryRect) =>
				{
					var focusedItem = viewEvents.OnFocusedMessageSourcePainting() as SourcesListItem;
					if (focusedItem == null)
						return;
					if (!(focusedItem == sourceItem || focusedItem.parent == sourceItem))
						return;
					using (var g = new LJD.Graphics())
					{
						var s = currentSourceImage.GetSize(height: 9);
						var r = view.Bounds;
						r = new CoreGraphics.CGRect((r.Left + r.Right - s.Width) / 2, 
							(r.Top + r.Bottom - s.Height) / 2, s.Width, s.Height);
						g.DrawImage (currentSourceImage, r.ToRectangleF ());
					}
				};
				return view;
			}


			return null;
		}

		class MenuDelegate: NSMenuDelegate
		{
			public SourcesListControlAdapter owner;

			public override void MenuWillOpen (NSMenu menu)
			{
				var e = owner.viewEvents;
				menu.RemoveAllItems();
				var visibleItems = MenuItem.None;
				var checkedItems = MenuItem.None;
				e.OnMenuItemOpening(true, out visibleItems, out checkedItems);
				Action<MenuItem, string, Action> addItem = (item, str, handler) =>
				{
					if ((item & visibleItems) != 0)
						menu.AddItem(new NSMenuItem(str, (sender, evt) => handler()));
				};
				addItem(MenuItem.SaveLogAs, "Save as...", e.OnSaveLogAsMenuItemClicked);
				addItem(MenuItem.SourceProprties, "Properties...", e.OnSourceProprtiesMenuItemClicked);
				addItem(MenuItem.OpenContainingFolder, "Open containing folder", e.OnOpenContainingFolderMenuItemClicked);
				addItem(MenuItem.ShowOnlyThisLog, "Show only this log", e.OnShowOnlyThisLogClicked);
				addItem(MenuItem.ShowAllLogs, "Show all logs", e.OnShowAllLogsClicked);
				addItem(MenuItem.CloseOthers, "Close others", e.OnCloseOthersClicked);
				addItem(MenuItem.CopyErrorMessage, "Copy error message", e.OnCopyErrorMessageCliecked);
				addItem(MenuItem.SaveMergedFilteredLog, "Save joint log...", e.OnSaveMergedFilteredLogMenuItemClicked);
			}

			public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
			{
			}
		};

		public override void SelectionDidChange(NSNotification notification)
		{
			foreach (var x in GetItems().ZipWithIndex())
				x.Value.isSelected = outlineView.IsRowSelected(x.Key);
			viewEvents.OnSelectionChanged();
		}

		void UpdateItem(SourcesListItem item)
		{
			if (!updating)
			{
				outlineView.ReloadItem(item);
				if (item.isSelected)
					outlineView.SelectRow(outlineView.RowForItem(item), true);
				else
					outlineView.DeselectRow(outlineView.RowForItem(item));
			}
		}

		void DoFullUpdate()
		{
			outlineView.ReloadData();
			outlineView.SelectRows(NSIndexSet.FromArray(
				GetItems()
				.ZipWithIndex()
				.Where(x => x.Value.isSelected)
				.Select(x => x.Key)
				.ToArray()
			), byExtendingSelection: false);
		}
		
		IEnumerable<SourcesListItem> GetItems() {
			foreach (var i in dataSource.Items) {
				yield return i;
				foreach (var j in i.items)
					yield return j;
			}
		}

		internal void ToggleSelectedSource() 
		{
			foreach (var i in GetItems())
			{
				if (i.isSelected && i.isChecked.HasValue)
				{
					i.isChecked = !i.isChecked.Value;
					outlineView.ReloadItem(i);
					viewEvents.OnItemChecked(i);
				}
			}
		}
	}
}
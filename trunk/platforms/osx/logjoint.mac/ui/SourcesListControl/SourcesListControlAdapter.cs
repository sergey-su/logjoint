using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.AppKit;
using MonoMac.Foundation;
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
			// todo
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

		string IView.ShowSaveLogDialog(string suggestedLogFileName)
		{
			var dlg = new NSSavePanel ();
			dlg.Title = "Save";
			dlg.NameFieldStringValue = suggestedLogFileName;
			if (dlg.RunModal () == 1) {
				return dlg.Url.Path.ToString();
			}
			return null;
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
					view.Action = new MonoMac.ObjCRuntime.Selector("ItemChecked:");
				}

				view.Target = sourceItem;
				view.State = sourceItem.isChecked.GetValueOrDefault(false) ? 
					NSCellStateValue.On : NSCellStateValue.Off;
				return view;
			}
			else if (tableColumn == sourceDescriptionColumn)
			{
				var cellIdentifier = "description_cell";
				var view = (NSTextField)outlineView.MakeView(cellIdentifier, this);

				if (view == null)
				{
					view = new NSTextField();
					view.Identifier = cellIdentifier;
					view.Bordered = false;
					view.Selectable = false;
					view.Editable = false;
					view.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;
				}

				view.BackgroundColor = sourceItem.color != null ?
					sourceItem.color.Value.ToColor().ToNSColor() : NSColor.Clear;
				view.StringValue = sourceItem.text;
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
						r = new System.Drawing.RectangleF((r.Left + r.Right - s.Width) / 2, 
							(r.Top + r.Bottom - s.Height) / 2, s.Width, s.Height);
						g.DrawImage(currentSourceImage, r);
					}
				};
				return view;
			}


			return null;
		}

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
	}
}
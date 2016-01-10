using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using LogJoint.UI.Presenters.BookmarksList;
using LogJoint.Settings;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public partial class BookmarksListControlAdapter : NSViewController, IView
	{
		readonly DataSource dataSource = new DataSource();
		IViewEvents viewEvents;
		IPresentationDataAccess presentationDataAccess;
		bool isUpdating;

		#region Constructors

		// Called when created from unmanaged code
		public BookmarksListControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public BookmarksListControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public BookmarksListControlAdapter()
			: base("BookmarksListControl", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			View.Initialize(this);
			tableView.DataSource = dataSource;
			tableView.Delegate = new Delegate() { owner = this };
		}

		public new BookmarksListControl View
		{
			get { return (BookmarksListControl)base.View; }
		}

		internal IViewEvents ViewEvents
		{
			get { return viewEvents; }
		}

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
			this.presentationDataAccess = (IPresentationDataAccess)viewEvents;
		}

		void IView.UpdateItems(IEnumerable<ViewItem> viewItems, ViewUpdateFlags flags)
		{
			isUpdating = true;
			var items = dataSource.items;
			items.Clear();
			items.AddRange(viewItems.Select((d, i) => new Item(this, d, i)));
			tableView.ReloadData();
			tableView.SelectRows(
				NSIndexSet.FromArray(items.Where(i => i.Data.IsSelected).Select(i => i.Index).ToArray()),
				byExtendingSelection: false
			);
			UpdateTimeDeltasColumn();
			isUpdating = false;
		}

		void IView.RefreshFocusedMessageMark()
		{
			InvalidateTable();
		}

		void IView.Invalidate()
		{
			InvalidateTable();
		}

		LogJoint.IBookmark IView.SelectedBookmark
		{
			get
			{
				return GetBookmark(GetItem(tableView.SelectedRow));
			}
		}

		IEnumerable<LogJoint.IBookmark> IView.SelectedBookmarks
		{
			get
			{
				return 
					dataSource.items
					.Where(i => tableView.IsRowSelected((int)i.Index))
					.Select(i => GetBookmark(i))
					.Where(b => b != null);
			}
		}


		void UpdateTimeDeltasColumn()
		{
			float w = 0;
			for (int i = 0; i < dataSource.items.Count; ++i)
				w = Math.Max(w, timeDeltaColumn.DataCellForRow(i).CellSize.Width);
			timeDeltaColumn.MinWidth = w;
			timeDeltaColumn.Width = w;
		}

		Item GetItem(int row)
		{
			return row >= 0 && row < dataSource.items.Count ? dataSource.items[row] : null;
		}

		IBookmark GetBookmark(Item item)
		{
			return item != null ? item.Data.Bookmark : null;
		}

		void OnItemClicked(Item item, NSEvent evt)
		{
			if (evt.ClickCount == 1)
				viewEvents.OnBookmarkLeftClicked(item.Data.Bookmark);
			else if (evt.ClickCount == 2)
				viewEvents.OnViewDoubleClicked();
		}

		void InvalidateTable()
		{
			for (int ridx = 0; ridx < tableView.RowCount; ++ridx)
			{
				var v = tableView.GetRowView(ridx, false);
				if (v != null)
					v.NeedsDisplay = true;
			}
		}

		class Item: NSObject
		{
			readonly ViewItem data;
			readonly int index;

			public Item(BookmarksListControlAdapter owner, ViewItem data, int index)
			{
				this.data = data;
				this.index = index;
			}

			public ViewItem Data
			{
				get { return data; }
			}

			public int Index
			{
				get { return index; }
			}
		};

		class DataSource: NSTableViewDataSource
		{
			public List<Item> items = new List<Item>();

			public override int GetRowCount (NSTableView tableView)
			{
				return items.Count;
			}
		};

		class Delegate: NSTableViewDelegate
		{
			private const string timeDeltaCellId = "TimeDeltaCell";
			private const string textCellId = "TextCell";
			public BookmarksListControlAdapter owner;

			public override bool ShouldSelectRow (NSTableView tableView, int row)
			{
				return true;
			}

			public override NSTableRowView CoreGetRowView(NSTableView tableView, int row)
			{
				return new TableRowView() { owner = owner, row = row };
			}

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, int row)
			{
				// todo: represent item.Data.IsEnabled

				var item = owner.dataSource.items[row];
				if (tableColumn == owner.timeDeltaColumn)
				{
					var view = (NSTextField )tableView.MakeView(timeDeltaCellId, this);
					if (view == null)
					{
						view = new NSTextField()
						{
							Identifier = timeDeltaCellId,
							BackgroundColor = NSColor.Clear,
							Bordered = false,
							Selectable = false,
							Editable = false,
						};
					}

					view.StringValue = item.Data.Delta;

					return view;
				}
				else if (tableColumn == owner.currentPositionIndicatorColumn)
				{
					return null;
				}
				else if (tableColumn == owner.textColumn)
				{
					var view = (NSLinkLabel)tableView.MakeView(textCellId, this);
					if (view == null)
						view = new NSLinkLabel();

					view.StringValue = item.Data.Bookmark.DisplayName;
					view.LinkClicked = (s, e) => owner.OnItemClicked(item, e.NativeEvent);

					return view;
				}
				return null;
			}
				
			public override void SelectionDidChange(NSNotification notification)
			{
				if (!owner.isUpdating)
					owner.viewEvents.OnSelectionChanged();
			}
		};

		class TableRowView: NSTableRowView
		{
			public BookmarksListControlAdapter owner;
			public int row;
			static NSColor selectedBkColor = NSColor.FromDeviceRgba(.77f, .80f, .90f, 1f);

			public override NSBackgroundStyle InteriorBackgroundStyle
			{
				get
				{
					return NSBackgroundStyle.Light; // this makes cells believe they need to have black (dark) text when selected
				}
			}

			public override void DrawBackground(RectangleF dirtyRect)
			{
				base.DrawBackground(dirtyRect);

				if (row < 0 || row >= owner.dataSource.items.Count)
					return;
				var bmk = owner.dataSource.items[row].Data.Bookmark;

				ModelColor? cl = null;

				switch (owner.presentationDataAccess.Coloring)
				{
					case Appearance.ColoringMode.None:
						return;
					case Appearance.ColoringMode.Sources:
						var ls = bmk.GetSafeLogSource();
						if (ls != null)
							cl = ls.Color;
						break;
					case Appearance.ColoringMode.Threads:
						var t = bmk.GetSafeThread();
						if (t != null)
							cl = t.ThreadColor;						
						break;
				}

				if (cl != null)
				{
					cl.Value.ToColor().ToNSColor().SetFill();
					NSBezierPath.FillRect(dirtyRect);
				}
				DrawFocusedMessage();
			}

			public override void DrawSelection(RectangleF dirtyRect)
			{
				selectedBkColor.SetFill();
				NSBezierPath.FillRect(dirtyRect);
				DrawFocusedMessage();
			}

			void DrawFocusedMessage()
			{
				Tuple<int, int> focused;
				owner.viewEvents.OnFocusedMessagePositionRequired(out focused);
				if (focused != null)
				{
					var frame = this.Frame;
					float y;
					float itemH = frame.Height;
					SizeF markSize = UIUtils.FocusedItemMarkFrame.Size;
					if (focused.Item1 != focused.Item2)
						y = itemH * focused.Item1 + itemH / 2;
					else
						y = itemH * focused.Item1;
					if (Math.Abs(y) < .01f)
						y = markSize.Height / 2;
					y -= frame.Y;
					using (var g = new LogJoint.Drawing.Graphics())
						UIUtils.DrawFocusedItemMark(g, 
							owner.tableView.GetCellFrame(1, row).Left, y);
				}
			}
		};
	}
}


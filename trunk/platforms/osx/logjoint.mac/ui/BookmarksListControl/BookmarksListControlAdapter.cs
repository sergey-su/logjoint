using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using LogJoint.UI.Presenters.BookmarksList;

namespace LogJoint.UI
{
	public partial class BookmarksListControlAdapter : NSViewController, IView
	{
		readonly DataSource dataSource = new DataSource();
		NSFont font;
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

			font = NSFont.SystemFontOfSize(NSFont.SystemFontSize);
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

		void IView.UpdateItems(IEnumerable<ViewItem> viewItems)
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
			// todo
		}

		void IView.SetClipboard(string text)
		{
			NSPasteboard.GeneralPasteboard.ClearContents();
			NSPasteboard.GeneralPasteboard.SetStringForType(text, NSPasteboard.NSStringType);
		}

		void IView.Invalidate()
		{
			tableView.NeedsDisplay = true;
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

		class Item: NSObject
		{
			readonly BookmarksListControlAdapter owner;
			readonly ViewItem data;
			readonly int index;
			NSMutableAttributedString attrString;

			public Item(BookmarksListControlAdapter owner, ViewItem data, int index)
			{
				this.owner = owner;
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

			public NSMutableAttributedString TextAttributedString
			{
				get
				{
					if (attrString != null)
						return attrString;
					attrString = new NSMutableAttributedString(data.Bookmark.DisplayName);
					var range = new NSRange(0, attrString.Length);
					attrString.BeginEditing();
					attrString.AddAttribute(NSAttributedString.ForegroundColorAttributeName, NSColor.Blue, range);
					var NSUnderlineStyleSingle = 1;
					attrString.AddAttribute(NSAttributedString.UnderlineStyleAttributeName, new NSNumber(NSUnderlineStyleSingle), range);	
					var para = new NSMutableParagraphStyle();
					para.Alignment = NSTextAlignment.Left;
					para.LineBreakMode = NSLineBreakMode.TruncatingTail;
					attrString.AddAttribute(NSAttributedString.ParagraphStyleAttributeName, para, range);
					attrString.AddAttribute(NSAttributedString.FontAttributeName, owner.font, range);
					attrString.EndEditing();
					return attrString;
				}
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
				return new TableRowView() { owner = owner };
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
					var view = (LinkLabel)tableView.MakeView(textCellId, this);
					if (view == null)
						view = new LinkLabel();

					view.Text = item.TextAttributedString;
					view.Click = e => owner.OnItemClicked(item, e);

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

		class LinkLabel: NSView
		{
			public NSMutableAttributedString Text;
			public Action<NSEvent> Click;

			public override void ResetCursorRects()
			{
				AddCursorRect(Bounds, NSCursor.PointingHandCursor);
			}

			public override void DrawRect(RectangleF dirtyRect)
			{
				base.DrawRect(dirtyRect);
				Text.DrawString(Bounds);
			}

			public override void MouseDown(NSEvent theEvent)
			{
				base.MouseDown(theEvent);
				Click(theEvent);
			}
		};

		class TableRowView: NSTableRowView
		{
			public BookmarksListControlAdapter owner;
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
				// todo: draw bookmark background (thread or source color)
				base.DrawBackground(dirtyRect);

				var r = owner.tableView.RectForColumn(1);

				NSColor.Orange.SetFill();
				//NSBezierPath.FillRect(r);
			}

			public override void DrawSelection(RectangleF dirtyRect)
			{
				selectedBkColor.SetFill();
				NSBezierPath.FillRect(dirtyRect);
			}
		};
	}
}


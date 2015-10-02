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
		IViewEvents viewEvents;

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

			tableView.DataSource = dataSource;
			tableView.Delegate = new Delegate() { owner = this };
		}

		public new BookmarksListControl View
		{
			get { return (BookmarksListControl)base.View; }
		}


		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.UpdateItems(IEnumerable<ViewItem> items)
		{
			dataSource.items.Clear();
			dataSource.items.AddRange(items.Select(i => new Item(i)));
			tableView.ReloadData();
		}

		void IView.RefreshFocusedMessageMark()
		{
			// todo
		}

		void IView.SetClipboard(string text)
		{
			// todo
		}

		void IView.Invalidate()
		{
			// todo
		}

		LogJoint.IBookmark IView.SelectedBookmark
		{
			get
			{
				return null;
			}
		}

		IEnumerable<LogJoint.IBookmark> IView.SelectedBookmarks
		{
			get
			{
				// todo
				return Enumerable.Empty<LogJoint.IBookmark>();
			}
		}



		class Item
		{
			readonly ViewItem data;
			NSMutableAttributedString attrString;

			public Item(ViewItem data)
			{
				this.data = data;
			}

			public ViewItem Data
			{
				get { return data; }
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
					attrString.AddAttribute(NSAttributedString.LinkAttributeName, new NSString("#"), range);
					attrString.AddAttribute(NSAttributedString.ForegroundColorAttributeName, NSColor.Blue, range);
					var NSUnderlineStyleSingle = 1;
					attrString.AddAttribute(NSAttributedString.UnderlineStyleAttributeName, new NSNumber(NSUnderlineStyleSingle), range);
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
				var item = owner.dataSource.items[row];
				if (tableColumn == owner.timeDeltaColumn)
				{
					NSTextField view = (NSTextField)tableView.MakeView(timeDeltaCellId, this);
					if (view == null)
						view = MakeField(timeDeltaCellId, isLink: false);

					view.StringValue = item.Data.Delta;

					return view;
				}
				else if (tableColumn == owner.currentPositionIndicatorColumn)
				{
					return null;
				}
				else if (tableColumn == owner.textColumn)
				{
					NSTextField view = (NSTextField)tableView.MakeView(textCellId, this);
					if (view == null)
						view = MakeField(textCellId, isLink: true);
					
					view.AttributedStringValue = item.TextAttributedString;				

					return view;
				}
				return null;
			}

			static NSTextField MakeField(string id, bool isLink)
			{
				return new TextField(isLink)
				{
					Identifier = id,
					BackgroundColor = NSColor.Clear,
					Bordered = false,
					Selectable = false,
					Editable = false
				};
			}
		};

		class TextField: NSTextField
		{
			readonly bool isLink;

			public TextField(bool isLink)
			{
				this.isLink = isLink;
			}

			public override void ResetCursorRects()
			{
				if (isLink)
					AddCursorRect(Bounds, NSCursor.PointingHandCursor);
				else
					base.ResetCursorRects();
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


using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.UI
{
	public partial class SearchSuggestionsListController : AppKit.NSViewController
	{
		internal QuickSearchTextBoxAdapter owner;
		readonly DataSource dataSource = new DataSource();

		#region Constructors

		// Called when created from unmanaged code
		public SearchSuggestionsListController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public SearchSuggestionsListController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public SearchSuggestionsListController () : base ("SearchSuggestionsList", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		//strongly typed view accessor
		public new SearchSuggestionsList View {
			get {
				return (SearchSuggestionsList)base.View;
			}
		}

		public NSOutlineView ListView => list;

		public void SetListItems(IReadOnlyList<ISuggestionsListItem> items)
		{
			dataSource.Items = items.Select(
				(i, idx) => new Item { PresenationItem = i, Index = idx }).ToList();
			list.ReloadData();
			list.AutoSizeColumn(1);
		}

		public void SetListSelectedItem(int index)
		{
			list.SelectRows(
				NSIndexSet.FromArray (index >= 0 ? new [] {index} : new int[0]),
				byExtendingSelection: false
			);
			if (index >= 0)
			{
				list.ScrollRowToVisible(index);
			}
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			list.DataSource = dataSource;
			list.Delegate = new Delegate() { owner = this };
		}

		class Item: NSObject
		{
			public int Index;
			public ISuggestionsListItem PresenationItem;
		};

		class DataSource: NSOutlineViewDataSource
		{
			public List<Item> Items = new List<Item>();

			public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
			{
				return Items.Count;
			}

			public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
			{
				return Items [(int)childIndex];
			}

			public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
			{
				return false;
			}			
		};

		class Delegate: NSOutlineViewDelegate
		{
			public SearchSuggestionsListController owner;

			public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item) 
			{
				var sourceItem = item as Item;

				if (tableColumn == owner.displayNameColumn)
				{
					var cellIdentifier = "text_cell";
					var view = (NSLinkLabel)outlineView.MakeView(cellIdentifier, this);

					if (view == null)
					{
						view = new NSLinkLabel();
						view.Identifier = cellIdentifier;
						view.UnderlineLinks = false;
					}

					view.LinkClicked = (sender, e) => 
					{
						e.SuppressDefault = true;
						owner.owner.viewModel.OnSuggestionClicked(sourceItem.Index);
					};
					view.StringValue = sourceItem.PresenationItem.Text;
					if (sourceItem.PresenationItem.IsSelectable)
					{
						view.LinksColor = NSColor.ControlText;
						view.Cursor = NSCursor.PointingHandCursor;
						view.FontSize = NSFont.SystemFontSize;
					}
					else
					{
						view.LinksColor = NSColor.Gray;
						view.Cursor = NSCursor.ArrowCursor;
						view.FontSize = NSFont.SmallSystemFontSize;
					}
					return view;
				}
				else if (tableColumn == owner.linkColumn)
				{
					if (sourceItem.PresenationItem.LinkText == null)
						return null;
					
					var cellIdentifier = "link_cell";
					var view = (NSLinkLabel)outlineView.MakeView(cellIdentifier, this);

					if (view == null)
					{
						view = new NSLinkLabel();
						view.Identifier = cellIdentifier;
					}

					view.LinkClicked = (sender, e) => 
					{
						e.SuppressDefault = true;
						owner.owner.viewModel.OnSuggestionLinkClicked(sourceItem.Index);
					};
					view.StringValue = sourceItem.PresenationItem.LinkText;
					return view;
				}

				return null;
			}
		};
	}
}

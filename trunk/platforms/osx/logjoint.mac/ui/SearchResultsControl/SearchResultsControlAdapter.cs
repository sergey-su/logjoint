
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.SearchResult;
using MonoMac.ObjCRuntime;

namespace LogJoint.UI
{
	public partial class SearchResultsControlAdapter : NSViewController, IView
	{
		LogViewerControlAdapter logViewerControlAdapter;
		IViewEvents viewEvents;
		readonly DataSource dataSource = new DataSource();

		#region Constructors

		// Called when created from unmanaged code
		public SearchResultsControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public SearchResultsControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public SearchResultsControlAdapter()
			: base("SearchResultsControl", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed view accessor
		public new SearchResultsControl View
		{
			get
			{
				return (SearchResultsControl)base.View;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			logViewerControlAdapter = new LogViewerControlAdapter();
			logViewerControlAdapter.View.MoveToPlaceholder(this.logViewerPlaceholder);

			selectCurrentTimeButton.ToolTip = "Find current time in search results";

			tableView.DataSource = dataSource;
			tableView.Delegate = new Delegate() { owner = this };
		}


		void IView.SetEventsHandler(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.FocusMessagesView()
		{
			logViewerControlAdapter.View.BecomeFirstResponder();
		}

		Presenters.LogViewer.IView IView.MessagesView
		{
			get { return logViewerControlAdapter; }
		}

		bool IView.IsMessagesViewFocused
		{
			get
			{
				return logViewerControlAdapter.isFocused;
			}
		}

		void IView.UpdateItems(IList<ViewItem> items)
		{
			dataSource.items.Clear();
			dataSource.items.AddRange(items.Select(d => new Item(this, d)));
			tableView.ReloadData();
		}

		void IView.UpdateItem(ViewItem item)
		{
			// todo
		}

		void IView.UpdateExpandedState(bool isExpandable, bool isExpanded)
		{
			dropdownView.BorderType = isExpanded ? NSBorderType.BezelBorder : NSBorderType.NoBorder;
			dropdownHeightConstraint.Constant = isExpanded ? 100 : 1;
		}

		partial void OnCloseSearchResultsButtonClicked (NSObject sender)
		{
			closeSearchResultsButton.State = NSCellStateValue.Off;
			viewEvents.OnCloseSearchResultsButtonClicked();
		}

		partial void OnSelectCurrentTimeClicked (NSObject sender)
		{
			viewEvents.OnFindCurrentTimeButtonClicked();
		}

		partial void OnDropdownButtonClicked (NSObject sender)
		{
			viewEvents.OnExpandSearchesListClicked();
		}

		class DataSource: NSTableViewDataSource
		{
			public List<Item> items = new List<Item>();

			public override int GetRowCount (NSTableView tableView)
			{
				return items.Count;
			}
		};

		class Item: NSObject
		{
			readonly SearchResultsControlAdapter owner;
			readonly ViewItem data;

			public Item(SearchResultsControlAdapter owner, ViewItem data)
			{
				this.owner = owner;
				this.data = data;
			}

			public ViewItem Data
			{
				get { return data; }
			}

			[Export("OnPinButtonClicked:")]
			void OnPinButtonClicked(NSButton sender)
			{
				owner.viewEvents.OnPinCheckboxClicked(data);
			}

			[Export("OnVisiblitityButtonClicked:")]
			void OnVisiblitityButtonClicked(NSButton sender)
			{
				owner.viewEvents.OnVisibilityCheckboxClicked(data);
			}
		};

		class Delegate: NSTableViewDelegate
		{
			private const string visiblityCellId = "visiblityCell";
			private const string pinCellId = "pinCell";
			private const string textCellId = "textCell";
			public SearchResultsControlAdapter owner;
			private NSImage pinImage;

			static NSButton MakeImageButton(string id, NSImage img)
			{
				var view = new NSButton()
				{
					Identifier = id,
					BezelStyle = NSBezelStyle.SmallSquare,
				};
				view.SetButtonType(NSButtonType.MomentaryPushIn);
				view.ImagePosition = NSCellImagePosition.ImageOnly;
				return view;
			}

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, int row)
			{
				var item = owner.dataSource.items[row];
				if (tableColumn == owner.visiblityColumn)
				{
					var view = (NSButton)tableView.MakeView(visiblityCellId, this);
					if (view == null)
					{
						view = new NSButton()
						{
							Identifier = visiblityCellId,
							Action = new Selector("OnVisiblitityButtonClicked:"),
							Title = "",
							ToolTip = "Show or hide result of this search", // todo: pass texts from presenter
						};
						view.SetButtonType(NSButtonType.Switch);
					}
					view.Target = item;
					view.State = item.Data.VisiblityControlChecked ? NSCellStateValue.On : NSCellStateValue.Off;
					return view;
				}
				else if (tableColumn == owner.pinColumn)
				{
					var view = (NSButton)tableView.MakeView(pinCellId, this);
					if (view == null)
					{
						view = new NSButton()
						{
							Identifier = pinCellId,
							BezelStyle = NSBezelStyle.RoundRect,
							ImagePosition = NSCellImagePosition.ImageOnly,
							Action = new Selector("OnPinButtonClicked:"),
							ToolTip = "Pin search result to prevent it from eviction by new searches"
						};
						view.SetButtonType(NSButtonType.OnOff);
						if (pinImage == null)
							pinImage = NSImage.ImageNamed("Pin.png");
						view.Image = pinImage;
						view.Cell.ImageScale = NSImageScale.ProportionallyDown;
						view.SetFrameSize(new System.Drawing.SizeF(24, 24));
					}
					view.Target = item;
					view.State = item.Data.PinControlChecked ? NSCellStateValue.On : NSCellStateValue.Off;
					return view;
				}
				else if (tableColumn == owner.textColumn)
				{
					var view = (NSTextField)tableView.MakeView(textCellId, this);
					if (view == null)
						view = new NSTextField()
						{
							Identifier = textCellId,
							BackgroundColor = NSColor.Clear,
							Bordered = false,
							Selectable = false,
							Editable = false,
						};
					view.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;

					view.StringValue = item.Data.Text;
					// view.TextColor =  todo: paint warnings yellow-ish

					return view;
				}
				return null;
			}
		};
	}
}


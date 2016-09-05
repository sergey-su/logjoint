
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.SearchResult;
using MonoMac.ObjCRuntime;
using System.Drawing;

namespace LogJoint.UI
{
	public partial class SearchResultsControlAdapter : NSViewController, IView
	{
		LogViewerControlAdapter logViewerControlAdapter;
		internal IViewEvents viewEvents;
		readonly DataSource dataSource = new DataSource();
		internal bool? dropdownExpanded;

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
			((SearchResultsDropdownTable)tableView).owner = this;
			((SearchResultsScrollView)dropdownScrollView).owner = this;

			dropdownContainerView.CanBeFirstResponder = true;
			dropdownContainerView.OnPaint += dirtyRect =>
			{
				if (!dropdownExpanded.GetValueOrDefault())
					return;
				NSColor.Control.SetFill();
				NSBezierPath.FillRect(dirtyRect);
				NSColor.ControlShadow.SetStroke();
				NSBezierPath.StrokeRect(dirtyRect);
			};
			dropdownContainerView.OnResignFirstResponder = () => viewEvents.OnDropdownContainerLostFocus();;
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

		void IView.UpdateExpandedState(bool isExpandable, bool isExpanded, string a, string b)
		{
			dropdownButton.Enabled = isExpandable;

			bool needsDropdownUpdate = 
			    dropdownExpanded == null // view is in initial unitialized state
			 || dropdownExpanded.Value != isExpanded; // or differs from requested
			if (!needsDropdownUpdate)
				return;
			dropdownExpanded = isExpanded;
			dropdownButton.State = isExpanded ? NSCellStateValue.On : NSCellStateValue.Off;
			dropdownContainerView.NeedsDisplay = true;
			dropdownHeightConstraint.Constant = isExpanded ? 100 : 1;
			if (isExpanded)
			{
				tableView.Window.MakeFirstResponder(tableView);
			}
			if (!isExpanded)
			{
				dropdownClipView.ScrollToPoint(new PointF(0, 0));
			}
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
							ToolTip = item.Data.VisiblityControlHint
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
							ToolTip = item.Data.PinControlHint
						};
						view.SetButtonType(NSButtonType.OnOff);
						if (pinImage == null)
							pinImage = NSImage.ImageNamed("Pin.png");
						view.Image = pinImage;
						view.Cell.ImageScale = NSImageScale.ProportionallyDown;
					}
					view.Target = item;
					view.State = item.Data.PinControlChecked ? NSCellStateValue.On : NSCellStateValue.Off;
					return view;
				}
				else if (tableColumn == owner.textColumn)
				{
					var view = (NSProgressTextField)tableView.MakeView(textCellId, this);
					if (view == null)
					{
						view = new NSProgressTextField()
						{
							Identifier = textCellId,
							BackgroundColor = NSColor.Clear,
							Bordered = false,
							Selectable = false,
							Editable = false,
						};
						view.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;
					}

					view.StringValue = item.Data.Text;
					view.TextColor = item.Data.IsWarningText ? 
						NSColor.Red : NSColor.ControlText;
					view.ProgressValue = item.Data.ProgressVisible ? 
						item.Data.ProgressValue : new double?();

					return view;
				}
				return null;
			}
		};
	}

	[Register ("SearchResultsDropdownTable")]
	class SearchResultsDropdownTable: NSTableView
	{
		internal SearchResultsControlAdapter owner;

		public SearchResultsDropdownTable (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SearchResultsDropdownTable (NSCoder coder) : base (coder)
		{
		}

		public override bool ResignFirstResponder()
		{
			if (owner != null && owner.viewEvents != null)
				owner.viewEvents.OnDropdownContainerLostFocus();
			return base.ResignFirstResponder();
		}

		[Export ("cancelOperation:")]
		void OnCancelOp (NSObject theEvent)
		{
			if (owner != null && owner.viewEvents != null)
				owner.viewEvents.OnDropdownEscape();
		}
	}

	[Register ("SearchResultsScrollView")]
	class SearchResultsScrollView: NSScrollView
	{
		internal SearchResultsControlAdapter owner;

		public SearchResultsScrollView (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SearchResultsScrollView (NSCoder coder) : base (coder)
		{
		}

		public override void ScrollWheel(NSEvent theEvent)
		{
			if (!owner.dropdownExpanded.GetValueOrDefault(false))
				return;
			base.ScrollWheel(theEvent);
		}
	}

	class NSProgressTextField: NSTextField
	{
		double? progressValue;

		public NSProgressTextField(): base()
		{
		}

		public NSProgressTextField (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public NSProgressTextField (NSCoder coder) : base (coder)
		{
		}


		public double? ProgressValue
		{
			get 
			{ 
				return progressValue; 
			}
			set 
			{
				progressValue = value;
				NeedsDisplay= true;
			}
		}

		public override void DrawRect(RectangleF dirtyRect)
		{
			if (progressValue != null)
			{
				var r = Bounds;
				r.Inflate(-0.5f, -0.5f);

				var cl = NSColor.FromDeviceRgba(0f, 0.3f, 1f, 0.20f);
				cl.SetStroke();
				NSBezierPath.StrokeRect(r);

				cl.SetFill();
				r.Width = (int)(r.Width * progressValue.Value);
				NSBezierPath.FillRect(r);
			}
			base.DrawRect(dirtyRect);
		}
	};
}



using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.SearchResult;
using ObjCRuntime;
using CoreGraphics;
using LogJoint.Drawing;
using LogJoint.UI.Presenters;

namespace LogJoint.UI
{
	public partial class SearchResultsControlAdapter : NSViewController, IView
	{
		LogViewerControlAdapter logViewerControlAdapter;
		internal IViewModel viewModel;
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
			selectCurrentTimeButton.Image.Template = true;

			tableView.DataSource = dataSource;
			tableView.Delegate = new Delegate() { owner = this };
			((SearchResultsDropdownTable)tableView).owner = this;
			((SearchResultsScrollView)dropdownScrollView).owner = this;

			dropdownContainerView.CanBeFirstResponder = true;
			dropdownContainerView.OnPaint += dirtyRect =>
			{
				if (!dropdownExpanded.GetValueOrDefault())
					return;
				NSColor.WindowBackground.SetFill();
				NSBezierPath.FillRect(dirtyRect.ToCGRect ());
				NSColor.WindowFrame.SetStroke();
				NSBezierPath.StrokeRect(dropdownContainerView.Bounds);
			};
			dropdownContainerView.OnResignFirstResponder = () => viewModel.OnDropdownContainerLostFocus();;
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			var updateItems = Updaters.Create (
				() => viewModel.Items,
				items => {
					dataSource.items.Clear ();
					dataSource.items.AddRange(items.Select(d => new Item(this, d)));
					tableView.ReloadData ();
				}
			);

			var updateSearchInProgressIndicator = Updaters.Create (
				() => viewModel.IsCombinedProgressIndicatorVisible,
				value => {
					progressIndicator.Hidden = !value;
					if (value)
						progressIndicator.StartAnimation (null);
					else
						progressIndicator.StopAnimation (null);
				}
			);

			viewModel.ChangeNotification.CreateSubscription (() => {
				updateItems ();
				updateSearchInProgressIndicator ();
			});
		}

		Presenters.LogViewer.IView IView.MessagesView
		{
			get { return logViewerControlAdapter; }
		}

		void IView.UpdateExpandedState(bool isExpandable, bool isExpanded, 
			int preferredListHeightInRows, string expandButtonHint, string unexpandButtonHint)
		{
			dropdownButton.Enabled = isExpandable;

			bool needsDropdownUpdate = 
			    dropdownExpanded == null // view is in initial unitialized state
			 || dropdownExpanded.Value != isExpanded; // or differs from requested
			if (!needsDropdownUpdate)
				return;
			dropdownExpanded = isExpanded;
			dropdownButton.State = isExpanded ? NSCellStateValue.On : NSCellStateValue.Off;
			dropdownButton.ToolTip = isExpanded ? unexpandButtonHint : expandButtonHint;
			dropdownContainerView.NeedsDisplay = true;
			dropdownHeightConstraint.Constant = isExpanded ? 23 * preferredListHeightInRows : 1;
			if (isExpanded)
			{
				tableView.Window.MakeFirstResponder(tableView);
			}
			if (!isExpanded)
			{
				dropdownClipView.ScrollToPoint(new CGPoint(0, 0));
			}
			dropdownScrollView.HasVerticalScroller = isExpanded;
			logViewerControlAdapter.EnableCursor = !isExpanded;
		}

		partial void OnCloseSearchResultsButtonClicked (NSObject sender)
		{
			closeSearchResultsButton.State = NSCellStateValue.Off;
			viewModel.OnCloseSearchResultsButtonClicked();
		}

		partial void OnSelectCurrentTimeClicked (NSObject sender)
		{
			viewModel.OnFindCurrentTimeButtonClicked();
		}

		partial void OnDropdownButtonClicked (NSObject sender)
		{
			viewModel.OnExpandSearchesListClicked();
		}

		class DataSource: NSTableViewDataSource
		{
			public List<Item> items = new List<Item>();

			public override nint GetRowCount (NSTableView tableView)
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
				owner.viewModel.OnPinCheckboxClicked(data);
			}

			[Export("OnVisiblitityButtonClicked:")]
			void OnVisiblitityButtonClicked(NSButton sender)
			{
				owner.viewModel.OnVisibilityCheckboxClicked(data);
			}
		};

		class Delegate: NSTableViewDelegate
		{
			private const string visiblityCellId = "visiblityCell";
			private const string pinCellId = "pinCell";
			private const string textCellId = "textCell";
			public SearchResultsControlAdapter owner;
			private NSImage pinImage, pinnedImage;

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

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				var item = owner.dataSource.items[(int)row];
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
						view.Cell.ImageScale = NSImageScale.ProportionallyDown;
					}
					view.Target = item;
					view.State = item.Data.PinControlChecked ? NSCellStateValue.On : NSCellStateValue.Off;
					view.Image = item.Data.PinControlChecked ?
						(pinnedImage ?? (pinnedImage = UIUtils.GetNamedTemplateImage ("Pinned.png"))) :
						(pinImage ?? (pinImage = UIUtils.GetNamedTemplateImage ("Pin.png")));
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
						view.Clicked = (s, e) => owner.viewModel.OnDropdownTextClicked();
					}

					view.StringValue = item.Data.Text;
					view.TextColor = item.Data.IsWarningText ? 
						NSColor.Red : NSColor.ControlText;
					view.ColorTheme = owner.viewModel.ColorTheme;
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
			if (owner != null && owner.viewModel != null)
				owner.viewModel.OnDropdownContainerLostFocus();
			return base.ResignFirstResponder();
		}

		[Export ("cancelOperation:")]
		void OnCancelOp (NSObject theEvent)
		{
			if (owner != null && owner.viewModel != null)
				owner.viewModel.OnDropdownEscape();
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

		public ColorThemeMode ColorTheme;

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

		public EventHandler Clicked;

		public override void DrawRect(CGRect dirtyRect)
		{
			if (progressValue != null)
			{
				var r = Bounds;
				r.Inflate(-0.5f, -0.5f);

				var cl = ColorTheme == ColorThemeMode.Light ?
					NSColor.FromDeviceRgba(0f, 0.3f, 1f, 0.20f) :
					NSColor.FromDeviceRgba (0.1f, 0.3f, 1f, 0.80f);
				cl.SetStroke();
				NSBezierPath.StrokeRect(r);

				cl.SetFill();
				r.Width = (int)(r.Width * progressValue.Value);
				NSBezierPath.FillRect(r);
			}
			base.DrawRect(dirtyRect);
		}

		public override void MouseDown(NSEvent theEvent)
		{
			Clicked?.Invoke (this, EventArgs.Empty);
			base.MouseDown(theEvent);
		}
	};
}


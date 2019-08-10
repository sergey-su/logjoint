using System;
using System.Linq;
using AppKit;
using Foundation;
using LogJoint.UI.Presenters.SourcesList;
using LJD = LogJoint.Drawing;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public class SourcesListControlAdapter : NSOutlineViewDelegate, IView
	{
		internal IViewModel viewModel;
		Reactive.INSOutlineViewController outlineViewController;
		LJD.Image currentSourceImage;
		IViewItem requestedTopItem;

		[Export ("view")]
		public SourcesListControl View { get; set; }
		[Outlet]
		NSOutlineView outlineView { get; set; }
		[Outlet]
		NSTableColumn sourceCheckedColumn { get; set; }
		[Outlet]
		NSTableColumn sourceDescriptionColumn { get; set; }
		[Outlet]
		NSTableColumn currentSourceColumn { get; set; }

		public SourcesListControlAdapter ()
		{
			NSBundle.LoadNib ("SourcesListControl", this);

			currentSourceImage = new LJD.Image (NSImage.ImageNamed ("FocusedMsgSlave.png"));
			View.owner = this;
			outlineView.Menu = new NSMenu { Delegate = new MenuDelegate { owner = this } };
		}

		public void Init (Mac.IReactive reactive)
		{
			View.EnsureCreated ();
			outlineViewController = reactive.CreateOutlineViewController (outlineView);

			outlineViewController.OnView = GetView;
		}

		void IView.SetViewModel (IViewModel viewModel)
		{
			this.viewModel = viewModel;

			outlineViewController.OnExpand = n => viewModel.OnItemExpand (n as IViewItem);
			outlineViewController.OnCollapse = n => viewModel.OnItemCollapse (n as IViewItem);
			outlineViewController.OnSelect = items => viewModel.OnSelectionChange (items.OfType<IViewItem> ().ToArray ());

			var updateTree = Updaters.Create (
				() => viewModel.RootItem,
				outlineViewController.Update
			);

			var updateFocusedMessageArea = Updaters.Create (
				() => viewModel.FocusedMessageItem,
				_ => {
					for (var i = 0; i < outlineView.RowCount; ++i) {
						var cell = outlineView.GetView (1, i, false);
						if (cell != null)
							cell.NeedsDisplay = true;
					}
				}
			);

			void setTopItem()
			{
				if (requestedTopItem != null) {
					outlineViewController.ScrollToVisible (requestedTopItem);
					requestedTopItem = null;
				}
			}

			viewModel.ChangeNotification.CreateSubscription (() => {
				updateTree ();
				updateFocusedMessageArea ();
				setTopItem ();
			});
		}

		void IView.SetTopItem (IViewItem item)
		{
			requestedTopItem = item;
		}

		public override NSTableRowView RowViewForItem (NSOutlineView outlineView, NSObject item)
		{
			return new NSCustomTableRowView { InvalidateSubviewsOnSelectionChange = true };
		}

		class CheckTarget : NSObject
		{
			public IViewItem item;
			public IViewModel viewModel;

			[Export ("ItemChecked:")]
			public void ItemChecked (NSObject sender)
			{
				bool isChecked = ((NSButton) sender).State == NSCellStateValue.On;
				viewModel.OnItemCheck(item, isChecked);
			}
		}

		NSView GetView (NSTableColumn tableColumn, Presenters.Reactive.ITreeNode item)
		{
			var sourceItem = item as IViewItem;

			if (tableColumn == sourceCheckedColumn) {
				if (sourceItem.Checked == null)
					return null;

				var cellIdentifier = "checked_cell";
				var view = (NSButton)outlineView.MakeView (cellIdentifier, this);

				if (view == null) {
					view = new NSButton ();
					view.Identifier = cellIdentifier;
					view.SetButtonType (NSButtonType.Switch);
					view.BezelStyle = 0;
					view.ImagePosition = NSCellImagePosition.ImageOnly;
					view.Action = new ObjCRuntime.Selector ("ItemChecked:");
				}

				view.Target = new CheckTarget {
					item = sourceItem,
					viewModel = viewModel
				};
				view.State = sourceItem.Checked.GetValueOrDefault (false) ?
					NSCellStateValue.On : NSCellStateValue.Off;
				return view;
			} else if (tableColumn == sourceDescriptionColumn) {
				var cellIdentifier = "description_cell";
				var view = (NSLinkLabel)outlineView.MakeView (cellIdentifier, this);

				if (view == null) {
					view = NSLinkLabel.CreateLabel ();
					view.Identifier = cellIdentifier;
					view.LinkClicked = (sender, e) => {
						if (e.NativeEvent.ClickCount == 2)
							viewModel.OnEnterKeyPressed ();
					};
				}

				view.BackgroundColor = sourceItem.Color.isFailureColor ? sourceItem.Color.value.ToNSColor () : NSColor.Clear;
				view.StringValue = sourceItem.ToString();
				view.Menu = outlineView.Menu;
				return view;
			} else if (tableColumn == currentSourceColumn) {
				var cellIdentifier = "current_source_mark_cell";
				var view = (NSCustomizableView)outlineView.MakeView (cellIdentifier, this);

				if (view == null) {
					view = new NSCustomizableView ();
					view.Identifier = cellIdentifier;
				}

				view.OnPaint = (ditryRect) => {
					var focusedItem = viewModel.FocusedMessageItem;
					if (focusedItem == null)
						return;
					if (!(focusedItem == sourceItem || focusedItem.Parent == sourceItem))
						return;
					using (var g = new LJD.Graphics ()) {
						var s = currentSourceImage.GetSize (height: 9);
						var r = view.Bounds;
						r = new CoreGraphics.CGRect ((r.Left + r.Right - s.Width) / 2,
							(r.Top + r.Bottom - s.Height) / 2, s.Width, s.Height);
						g.DrawImage (currentSourceImage, r.ToRectangleF ());
					}
				};
				return view;
			}


			return null;
		}

		class MenuDelegate : NSMenuDelegate
		{
			public SourcesListControlAdapter owner;

			public override void MenuWillOpen (NSMenu menu)
			{
				var e = owner.viewModel;
				menu.RemoveAllItems ();
				var (visibleItems, checkedItems) = e.OnMenuItemOpening (true);
				Action<MenuItem, string, Action> addItem = (item, str, handler) => {
					if ((item & visibleItems) != 0)
						menu.AddItem (new NSMenuItem (str, (sender, evt) => handler ()));
				};
				addItem (MenuItem.SaveLogAs, "Save as...", e.OnSaveLogAsMenuItemClicked);
				addItem (MenuItem.SourceProperties, "Properties...", e.OnSourceProprtiesMenuItemClicked);
				addItem (MenuItem.OpenContainingFolder, "Open containing folder", e.OnOpenContainingFolderMenuItemClicked);
				addItem (MenuItem.ShowOnlyThisLog, "Show only this log", e.OnShowOnlyThisLogClicked);
				addItem (MenuItem.ShowAllLogs, "Show all logs", e.OnShowAllLogsClicked);
				addItem (MenuItem.CloseOthers, "Close others", e.OnCloseOthersClicked);
				addItem (MenuItem.CopyErrorMessage, "Copy error message", e.OnCopyErrorMessageClicked);
				addItem (MenuItem.SaveMergedFilteredLog, "Save joint log...", e.OnSaveMergedFilteredLogMenuItemClicked);
			}

			public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
			{
			}
		};
	}
}
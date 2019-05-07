﻿using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.FiltersListBox;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public partial class FiltersListController : AppKit.NSViewController, IView
	{
		internal IViewEvents eventsHandler;
		DataSource dataSource = new DataSource();
		bool updating;

		// Called when created from unmanaged code
		public FiltersListController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public FiltersListController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public FiltersListController () : base ("FiltersList", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		void IView.SetPresenter (IViewEvents presenter)
		{
			this.eventsHandler = presenter;
		}

		void IView.BeginUpdate ()
		{
			updating = true;
		}

		void IView.EndUpdate ()
		{
			updating = false;
			DoFullUpdate();
		}

		IViewItem IView.CreateItem (IFilter filter, string key)
		{
			var item = new Item()
			{
				update = UpdateItem,
				events = eventsHandler,
				filter = filter,
				key = key
			};
			return item;
		}

		IViewItem IView.GetItem (int index)
		{
			return dataSource.Items[index];
		}

		void IView.RemoveAt (int index)
		{
			((IView)this).Remove(dataSource.Items[index]);
		}

		void IView.Remove (IViewItem item)
		{
			var imp = (Item)item;
			imp.update = null;
			imp.events = null;
			dataSource.Items.Remove(imp);
			if (!updating)
				DoFullUpdate();
		}

		void IView.Insert (int index, IViewItem item)
		{
			var imp = (Item)item;
			dataSource.Items.Insert(index, imp);
			UpdateItem(imp);
		}

		int IView.GetItemIndexByKey (string key)
		{
			return dataSource.Items.IndexOf(i => i.key == key).GetValueOrDefault(-1);
		}

		void IView.SetEnabled (bool value)
		{
			// hard to disable nicely on mac. keeping enabled does not harm.
			//listView.Enabled = value;
		}

		int IView.Count => dataSource.Items.Count;

		IEnumerable<IViewItem> IView.SelectedItems => dataSource.Items.Where(i => i.isSelected);

		public new FiltersList View =>  (FiltersList)base.View;

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			View.owner = this;
			listView.DataSource = dataSource;
			listView.Delegate = new Delegate() { owner = this };
		}

		void UpdateItem(Item item)
		{
			if (!updating)
			{
				listView.ReloadItem(item);
				if (item.isSelected)
					listView.SelectRow(listView.RowForItem(item), true);
				else
					listView.DeselectRow(listView.RowForItem(item));
			}
		}

		void DoFullUpdate()
		{
			listView.ReloadData();
			listView.SelectRows(NSIndexSet.FromArray(
				dataSource.Items
				.ZipWithIndex()
				.Where(x => x.Value.isSelected)
				.Select(x => x.Key)
				.ToArray()
			), byExtendingSelection: false);
		}

		class Item : NSObject, IViewItem
		{
			public IFilter filter;
			public string key;
			public Action<Item> update;
			public IViewEvents events;
			public string text;
			public bool? isChecked;
			public bool isSelected;
			public Color? color;
			public ViewItemImageType image;
			public string checkboxTooltip;
			public string actionTooltip;

			IFilter IViewItem.Filter => filter;

			string IViewItem.Text { get => text; set { text = value; update?.Invoke(this); } }
			bool? IViewItem.Checked { get => isChecked; set { isChecked = value; update?.Invoke(this); } }
			bool IViewItem.Selected { get => isSelected; set { isSelected = value; update?.Invoke(this); } }
			Color? IViewItem.Color { get => color; set { color = value; update?.Invoke(this); } }
			string IViewItem.CheckboxTooltip { get => checkboxTooltip; set { checkboxTooltip = value; update?.Invoke(this); } }
			string IViewItem.ActionTooltip { get => actionTooltip; set { actionTooltip = value; update?.Invoke(this); } }

			void IViewItem.SetImageType (ViewItemImageType imageType)
			{
				this.image = imageType;
				update?.Invoke(this);
			}

			[Export("ItemChecked:")]
			public void ItemChecked(NSObject sender)
			{
				isChecked = ((NSButton)sender).State == NSCellStateValue.On;
				events?.OnItemChecked(this);
			}
		};

		class DataSource: NSOutlineViewDataSource
		{
			public List<Item> Items = new List<Item>();

			public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
			{
				return item == null ? Items.Count : 0;
			}

			public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
			{
				return item == null ? Items [(int)childIndex] : null;
			}

			public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
			{
				return false;
			}
		}

		public class Delegate: NSOutlineViewDelegate
		{
			public FiltersListController owner;

			public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject obj) 
			{
				var item = obj as Item;
				if (tableColumn == owner.checkboxColumn)
				{
					if (item.isChecked == null)
						return null;
					var view = new NSButton();
					view.SetButtonType(NSButtonType.Switch);
					view.BezelStyle = 0;
					view.ImagePosition = NSCellImagePosition.ImageOnly;
					view.Action = new ObjCRuntime.Selector("ItemChecked:");
					view.Target = item;
					view.State = item.isChecked == true ? NSCellStateValue.On : NSCellStateValue.Off;
					view.ToolTip = item.checkboxTooltip ?? "";
					return view;
				}
				if (tableColumn == owner.textColumn)
				{
					var view = NSLinkLabel.CreateLabel();
					view.LinkClicked = (sender, e) => 
					{
						if (e.NativeEvent.ClickCount == 2)
							owner.eventsHandler.OnDoubleClicked();
					};
					view.Menu = new NSMenu
					{
						Delegate = new ContextMenuDelegate()
						{
							owner = owner	
						}
					};
					view.StringValue = item.text;
					return view;
				}
				if (tableColumn == owner.imageColumn)
				{
					if (item.image == ViewItemImageType.None)
						return null;
					var view = new NSTextField();
					view.Bordered = false;
					view.Selectable = false;
					view.Editable = false;
					view.BackgroundColor = NSColor.Clear;
					view.ToolTip = item.actionTooltip ?? "";
					if (item.image == ViewItemImageType.Include)
					{
						view.TextColor = NSColor.Black;
						view.StringValue = "✓";
						view.BackgroundColor = item.color != null ? 
							item.color.Value.ToNSColor() : NSColor.Clear;
					}
					else
					{
						view.TextColor = NSColor.Red;
						view.StringValue = "✘";
					}
					return view;
				}
				return null;
			}

			public override void SelectionDidChange(NSNotification notification)
			{
				foreach (var x in owner.dataSource.Items.ZipWithIndex())
					x.Value.isSelected = owner.listView.IsRowSelected(x.Key);
				owner.eventsHandler.OnSelectionChanged();
			}
		}

		class ContextMenuDelegate : NSMenuDelegate
		{
			public FiltersListController owner;

			public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
			{
			}

			public override void MenuWillOpen (NSMenu menu)
			{
				menu.RemoveAllItems();
				ContextMenuItem enabledItems;
				ContextMenuItem checkedItems;
				owner.eventsHandler.OnContextMenuOpening(out enabledItems, out checkedItems);
				foreach (var item in new []
				{
					new { i = ContextMenuItem.FilterEnabled, t = "Rule enabled", a = (Action)owner.eventsHandler.OnFilterEnabledMenuItemClicked },
					new { i = ContextMenuItem.MoveUp, t = "Move up", a = (Action)owner.eventsHandler.OnMoveUpMenuItemClicked },
					new { i = ContextMenuItem.MoveDown, t = "Move down", a = (Action)owner.eventsHandler.OnMoveDownMenuItemClicked },
					new { i = ContextMenuItem.Properties, t = "Properties", a = (Action)owner.eventsHandler.OnPropertiesMenuItemClicked },
				})
				{
					if ((enabledItems & item.i) == 0)
						continue;
					menu.AddItem(new NSMenuItem(item.t, (sender, e) => item.a())
					{
						State = (checkedItems & item.i) != 0 ? NSCellStateValue.On : NSCellStateValue.Off
					});
				}
			}
		};
	}
}

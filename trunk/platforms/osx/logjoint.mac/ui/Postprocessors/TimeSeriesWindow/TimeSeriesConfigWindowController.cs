﻿using System;
﻿using System.Linq;

using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using System.Collections.Generic;
using CoreGraphics;
using LogJoint.Drawing;
using System.Drawing;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesConfigWindowController : 
		NSWindowController,
		IConfigDialogView
	{
		IConfigDialogEventsHandler evts;
		Drawing.Resources resources;
		const string DataProp = "Data";
		OutlineDataSource dataSource = new OutlineDataSource();
		Dictionary<uint, NSMenuItem> colorItems = new Dictionary<uint, NSMenuItem>();
		Dictionary<MarkerType, NSMenuItem> markerItems = new Dictionary<MarkerType, NSMenuItem>();

		public TimeSeriesConfigWindowController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public TimeSeriesConfigWindowController (NSCoder coder) : base (coder)
		{
		}

		public TimeSeriesConfigWindowController (IConfigDialogEventsHandler evts, Drawing.Resources resources) : 
			base ("TimeSeriesConfigWindow")
		{
			this.evts = evts;
			this.resources = resources;
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			Window.owner = this;

			collapseAllLinkLabel.StringValue = "collapse all";
			collapseAllLinkLabel.LinkClicked = (s, e) =>
			{
				foreach (var item in dataSource.Items)
					treeView.CollapseItem(item, collapseChildren: true);
			};

			uncheckAllLinkLabel.StringValue = "uncheck all";
			uncheckAllLinkLabel.LinkClicked = (s, e) =>
			{
				var checkedNodes = dataSource.Items
				    .SelectMany(i => i.Traverse())
				    .Where(n => n.Data.Checkable)
	        	    .Where(n => evts.IsNodeChecked(n.Data))
				    .ToArray();
				evts.OnNodesChecked(checkedNodes.Select(n => n.Data).ToArray(), false);					
				foreach (var item in checkedNodes)
					treeView.ReloadItem(item, reloadChildren: false);
			};

			
			treeView.Delegate = new TreeDelegate() { owner = this };
			treeView.DataSource = dataSource;
		}

		NSMenuItem CreateGraphicsMenuItem(Action<Graphics, RectangleF> makeImage, Action click)
		{
			var item = new NSMenuItem("", (sender, e) => click());

			var h = 15;
			CGSize size = new CGSize(h * 4, h);
			using (CGColorSpace colorSpace = CGColorSpace.CreateDeviceRGB())
			using (CGBitmapContext context = new CGBitmapContext(IntPtr.Zero, 
	             (int)size.Width, (int)size.Height, 
	             8, (int)size.Width * 4, colorSpace, 
	             CGImageAlphaInfo.PremultipliedFirst
			))
			using (var g = new Graphics(context))
			{
				makeImage(g, new RectangleF(0, 0, (float)size.Width, (float)size.Height));
				item.Image = new NSImage(context.ToImage(), size);
			}
			return item;
		}

		public new TimeSeriesConfigWindow Window 
		{
			get { return (TimeSeriesConfigWindow)base.Window; }
		}

		void EnsureLoaded()
		{
			Window.GetHashCode();
		}

		void IConfigDialogView.AddRootNode (TreeNodeData n)
		{
			EnsureLoaded();
			var item = new TreeItem(null, n, evts);
			dataSource.Items.Add(item);
			treeView.ReloadData();
		}

		void IConfigDialogView.RemoveRootNode (TreeNodeData n)
		{
			EnsureLoaded();
			var item = dataSource.Items.FirstOrDefault(i => i.Data == n);
			if (item != null)
			{
				dataSource.Items.Remove(item);
				treeView.ReloadData();
			}
		}

		IEnumerable<TreeNodeData> IConfigDialogView.GetRoots ()
		{
			return dataSource.Items.Select(i => i.Data);
		}

		void IConfigDialogView.UpdateNodePropertiesControls (NodeProperties props)
		{
			nodeDescriptionTextView.Value = props?.Caption ?? "";

			if (colorPopup.Menu.Count == 0 && props != null)
			{
				foreach (var c in props.Palette)
				{
					colorPopup.Menu.AddItem(colorItems[c.Argb] = CreateGraphicsMenuItem((g, r) => 
					{
						using (var b = new Brush(c.ToColor()))
							g.FillRectangle(b, r);
					}, () => evts.OnColorChanged(c)));
				}
			}
			if (markerPopup.Menu.Count == 0 && props != null)
			{
				foreach (var m in Enum.GetValues(typeof(MarkerType)))
				{
					markerPopup.Menu.AddItem(markerItems[(MarkerType)m] = CreateGraphicsMenuItem((g, r) => 
					{
						Drawing.DrawLegendSample(g, resources, 
							new ModelColor(0xff, 0, 0, 0),(MarkerType)m, r);
					}, () => evts.OnMarkerChanged((MarkerType)m)));
				}
			}

			if ((colorPopup.Enabled = (props != null && props.Color != null)) == true)
				colorPopup.SelectItem(colorItems[props.Color.Value.Argb]);
			else
				colorPopup.SelectItem((NSMenuItem)null);
			if ((markerPopup.Enabled = (props != null && props.Marker != null)) == true)
				markerPopup.SelectItem(markerItems[props.Marker.Value]);
			else
				markerPopup.SelectItem((NSMenuItem)null);
		}

		void IConfigDialogView.Activate ()
		{
			Window.MakeKeyAndOrderFront(null);
		}

		bool IConfigDialogView.Visible
		{
			get => Window.IsVisible;
			set
			{
				if (value)
					Window.MakeKeyAndOrderFront (null);
				else
					Window.Close();
			}
		}

		TreeNodeData IConfigDialogView.SelectedNode
		{ 
			get 
			{
				if (treeView.SelectedRow >= 0)
					return (treeView.ItemAtRow(treeView.SelectedRow) as TreeItem)?.Data;
				return null;
			}
			set
			{
				var item = dataSource.Items.SelectMany(i => i.Traverse()).FirstOrDefault(i => i.Data == value);
				if (item != null)
					UIUtils.SelectAndScrollInView(treeView, new [] { item }, i => i.Parent);
			}
		}

		public class TreeItem: NSObject
		{
			public readonly TreeItem Parent;
			public readonly TreeNodeData Data;
			public readonly List<TreeItem> Items;
			public readonly IConfigDialogEventsHandler EventsHandler;

			public TreeItem(TreeItem parent, TreeNodeData data, IConfigDialogEventsHandler evts)
			{
				this.Parent = parent;
				this.Data = data;
				this.Items = data.Children.Select(c => new TreeItem(this, c, evts)).ToList();
				this.EventsHandler = evts;
			}

			[Export("ItemChecked:")]
			public void ItemChecked(NSObject sender)
			{
				bool isChecked = ((NSButton)sender).State == NSCellStateValue.On;
				if (EventsHandler != null)
					EventsHandler.OnNodesChecked(new [] { Data }, isChecked);
			}

			public IEnumerable<TreeItem> Traverse()
			{
				return Items.SelectMany(i => i.Traverse()).Union(new [] {this});
			}
		}

		public class OutlineDataSource: NSOutlineViewDataSource
		{
			public List<TreeItem> Items = new List<TreeItem>();

			public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
			{
				if (item == null)
					return Items.Count;
				else
					return ((item as TreeItem)?.Items?.Count).GetValueOrDefault();
			}

			public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
			{
				if (item == null)
					return Items [(int)childIndex];
				else
					return (item as TreeItem)?.Items?.ElementAtOrDefault((int)childIndex);
			}

			public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
			{
				return (item as TreeItem)?.Items?.Count > 0;
			}
		}

		public class TreeDelegate: NSOutlineViewDelegate
		{
			internal TimeSeriesConfigWindowController owner;

			public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item) 
			{
				var treeItem = item as TreeItem;


				if (tableColumn == owner.nodeColumn)
				{
					var cellIdentifier = "text_cell";
					var view = (NSTextField)outlineView.MakeView(cellIdentifier, this);
					if (view == null)
					{
						view = new NSTextField();
						view.Identifier = cellIdentifier;
						view.Bordered = false;
						view.Selectable = false;
						view.Editable = false;
						view.Cell.LineBreakMode = NSLineBreakMode.TruncatingMiddle;
						view.BackgroundColor = NSColor.Clear;
					}
					var caption = treeItem.Data.Caption ?? "";
					if (treeItem.Data.Counter != null)
						view.StringValue = string.Format("{0} ({1})", caption, treeItem.Data.Counter);
					else
						view.StringValue = caption;
					return view;
				}
				else if (tableColumn == owner.checkedColumn)
				{
					if (!treeItem.Data.Checkable)
						return null;
					
					var cellIdentifier = "check_cell";
					var view = (NSButton)outlineView.MakeView(cellIdentifier, this);

					if (view == null)
					{
						view = new NSButton();
						view.Identifier = cellIdentifier;
						view.SetButtonType(NSButtonType.Switch);
						view.BezelStyle = 0;
						view.ImagePosition = NSCellImagePosition.ImageOnly;
						view.Action = new ObjCRuntime.Selector("ItemChecked:");
					}

					view.Target = item;
					view.State = owner.evts.IsNodeChecked(treeItem.Data) ? NSCellStateValue.On : NSCellStateValue.Off;

					return view;					
				}

				return null;
			}

			public override void SelectionDidChange (NSNotification notification)
			{
				owner.evts.OnSelectedNodeChanged();
			}
		};

		internal void OnCancelOp()
		{
			Window.Close();
		}
	}
}

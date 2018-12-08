using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.UI.Presenters.HistoryDialog;
using AppKit;
using Foundation;
using ObjCRuntime;

namespace LogJoint.UI
{
	public partial class HistoryDialogAdapter : NSWindowController, IView
	{
		#region Constructors

		// Called when created from unmanaged code
		public HistoryDialogAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public HistoryDialogAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public HistoryDialogAdapter()
			: base("HistoryDialog")
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
			quickSearchTextBoxAdapter = new QuickSearchTextBoxAdapter();
		}

		#endregion
	

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			quickSearchTextBoxAdapter.View.MoveToPlaceholder(quickSearchTextBoxPlaceholder);
			outlineView.Delegate = new HistoryViewDelegate() { owner = this };
			Window.DefaultButtonCell = openButton.Cell;
			outlineView.SizeLastColumnToFit();
		}

		public new HistoryDialog Window
		{
			get { return (HistoryDialog)base.Window; }
		}

		void IView.SetEventsHandler(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.Update(ViewItem[] items)
		{
			WillChangeValue(DataProp);
			data.RemoveAllObjects();
			foreach (var i in items)
				data.Add(new DataItem(i));
			DidChangeValue(DataProp);

			for (int i = items.Length - 1; i >= 0; --i)
				outlineView.ExpandItem(outlineView.ItemAtRow(i));
		}

		void IView.AboutToShow()
		{
			Window.GetHashCode(); // force nib loading
		}

		void IView.Show()
		{
			InvokeOnMainThread(() => viewEvents.OnDialogShown());
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IView.Hide()
		{
			NSApplication.SharedApplication.AbortModal();
			this.Close();
		}

		void IView.PutInputFocusToItemsList()
		{
			outlineViewContainer.BecomeFirstResponder();
		}

		void IView.EnableOpenButton(bool enable)
		{
			openButton.Enabled = enable;
		}

		LogJoint.UI.Presenters.QuickSearchTextBox.IView IView.QuickSearchTextBox
		{
			get
			{
				return quickSearchTextBoxAdapter;
			}
		}

		ViewItem[] IView.SelectedItems
		{
			get
			{
				return
					Enumerable.Range(0, (int)outlineView.RowCount)
					.Where(i => outlineView.IsRowSelected(i))
					.Select(i => outlineView.ItemAtRow(i))
					.OfType<NSTreeNode>()
					.Select(n => n.RepresentedObject)
					.OfType<DataItem>()
					.Select(i => i.data)
					.ToArray();
			}
			set
			{
				var lookup = new HashSet<ViewItem>(value);
				outlineView.SelectRows(
					NSIndexSet.FromArray((
						from idx in Enumerable.Range(0, (int)outlineView.RowCount)
						let viewNode = outlineView.ItemAtRow(idx) as NSTreeNode
						where viewNode != null
						let dataItem = viewNode.RepresentedObject as DataItem
						where dataItem != null && lookup.Contains(dataItem.data)
						select idx
					).ToArray()),
					false
				);
			}
		}

		[Export(DataProp)]
		NSArray Data 
		{
			get { return data; }
		}

		[Export ("performFindPanelAction:")]
		void OnPerformFindPanelAction (NSObject theEvent)
		{
			viewEvents.OnFindShortcutPressed();
		}

		[Export ("validateMenuItem:")]
		bool OnValidateMenuItem (NSMenuItem item)
		{
			return true;
		}


		partial void OnClearHistoryButtonClicked (NSObject sender)
		{
			viewEvents.OnClearHistoryButtonClicked();
		}

		partial void OnListDoubleClicked (Foundation.NSObject sender)
		{
			viewEvents.OnDoubleClick();
		}

		partial void OnCancelButtonClicked (NSObject sender)
		{
			NSApplication.SharedApplication.AbortModal ();
			this.Close();
		}

		partial void OnOpenButtonClicked (NSObject sender)
		{
			NSApplication.SharedApplication.StopModal ();
			this.Close();
			viewEvents.OnOpenClicked();
		}

		class HistoryViewDelegate: NSOutlineViewDelegate
		{
			public HistoryDialogAdapter owner;

			public override void SelectionDidChange(NSNotification notification)
			{
				owner.viewEvents.OnSelectedItemsChanged();
			}
		};


		[Register("Item")]
		public class DataItem : NSObject
		{
			public ViewItem data;
			public NSMutableArray children = new NSMutableArray();
			const string ChildrenProp = "Children";

			[Export("Text")]
			public string Text
			{
				get { return data.Text; }
			}

			[Export("Annotation")]
			public string Annotation
			{
				get { return data.Annotation; }
			}

			[Export(ChildrenProp)]
			public NSArray Children
			{
				get { return children; }
			}

			[Export("NumberOfChildren")]
			public uint NumberOfChildren
			{
				get { return (uint) children.Count; }
			}

			[Export("IsLeaf")]
			public bool IsLeaf
			{
				get { return data.Type == ViewItemType.Leaf; }
			}

			[Export("IsSelectable")]
			public bool IsSelectable
			{
				get { return data.Type != ViewItemType.Comment; }
			}

			[Export("Color")]
			public NSColor Color
			{
				get { return data.Type == ViewItemType.Comment ? NSColor.FromDeviceRgba(0.7f, 0.7f, 0.7f, 1.0f) : NSColor.Text; }
			}

			public DataItem(ViewItem item)
			{
				this.data = item;
				foreach (var c in item.Children ?? Enumerable.Empty<ViewItem>())
					children.Add(new DataItem(c));
			}
		}

		IViewEvents viewEvents;
		QuickSearchTextBoxAdapter quickSearchTextBoxAdapter;

		private NSMutableArray data = new NSMutableArray();
		const string DataProp = "Data";
	}
}


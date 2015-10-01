
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.HistoryDialog;
using MonoMac.ObjCRuntime;

namespace LogJoint.UI
{
	public partial class HistoryDialogAdapter : MonoMac.AppKit.NSWindowController, IView
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

			outlineView.Delegate = new HistoryViewDelegate() { owner = this };

			quickSearchTextBoxAdapter.View.MoveToPlaceholder(quickSearchTextBoxPlaceholder);

			Window.DefaultButtonCell = openButton.Cell;
		}

		//strongly typed window accessor
		public new HistoryDialog Window
		{
			get
			{
				return (HistoryDialog)base.Window;
			}
		}

		void IView.SetEventsHandler(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.Update(ViewItem[] items)
		{
			WillChangeValue ("Data");
			data.RemoveAllObjects();
			allDataItems.Clear();
			DataItem lastContainer = null;
			var containers = new List<int>();
			int rowIdx = 0;
			foreach (var i in items)
			{
				var itemModel = new DataItem(i);
				if (itemModel.IsLeaf)
				{
					if (lastContainer != null)
						lastContainer.Add(itemModel);
					else
						data.Add(itemModel);
				}
				else
				{
					data.Add(itemModel);
					lastContainer = itemModel;
					containers.Add(rowIdx);
				}
				rowIdx++;
				allDataItems.Add(itemModel);
			}
			DidChangeValue ("Data");

			containers.ForEach(idx => outlineView.ExpandItem(outlineView.ItemAtRow(idx)));
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

		bool IView.ShowClearHistoryConfirmationDialog(string message)
		{
			var alert = new NSAlert () {
				AlertStyle = NSAlertStyle.Warning,
				InformativeText = message,
				MessageText = "Clear history",
			};
			alert.AddButton("Yes");
			alert.AddButton("No");
			alert.AddButton("Cancel");
			var res = alert.RunModal ();

			return res == 1000;
		}

		void IView.ShowOpeningFailurePopup(string message)
		{
			// todo
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
				var nodes = allDataItems.ZipWithIndex().Where(i => outlineView.IsRowSelected(i.Key)).Select(i => i.Value.data).ToArray();
				return nodes;
			}
			set
			{
				var lookup = new HashSet<ViewItem>(value);
				outlineView.SelectRows(NSIndexSet.FromArray(allDataItems.ZipWithIndex().Where(i => lookup.Contains(i.Value.data)).Select(i => i.Key).ToArray()), false);
			}
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

		[Export("Data")]
		public NSArray Data 
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


		partial void OnListDoubleClicked (MonoMac.Foundation.NSObject sender)
		{
			viewEvents.OnDoubleClick();
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

			[Export("Children")]
			public NSArray Children
			{
				get { return children; }
			}

			[Export("NumberOfChildren")]
			public uint NumberOfChildren
			{
				get { return children.Count; }
			}

			[Export("IsLeaf")]
			public bool IsLeaf
			{
				get { return data.Type != ViewItemType.HistoryComment; }
			}

			[Export("IsSelectable")]
			public bool IsSelectable
			{
				get { return data.Type != ViewItemType.HistoryComment; }
			}

			[Export("Color")]
			public NSColor Color
			{
				get { return data.Type == ViewItemType.HistoryComment ? NSColor.Gray : NSColor.Black; }
			}


			public DataItem(ViewItem item)
			{
				this.data = item;
			}

			public void Add(DataItem i)
			{
				WillChangeValue ("Children");
				children.Add(i);
				DidChangeValue ("Children");
			}
		}

		IViewEvents viewEvents;
		QuickSearchTextBoxAdapter quickSearchTextBoxAdapter;

		private NSMutableArray data = new NSMutableArray();
		List<DataItem> allDataItems = new List<DataItem>();
	}
		
}


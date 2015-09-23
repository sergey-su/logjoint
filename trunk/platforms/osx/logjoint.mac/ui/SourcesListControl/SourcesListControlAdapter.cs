using System;
using System.Linq;
using MonoMac.AppKit;
using MonoMac.Foundation;
using LogJoint.UI.Presenters.SourcesList;

namespace LogJoint.UI
{
	public class SourcesListControlAdapter: NSOutlineViewDelegate, IView
	{
		[Export("view")]
		public SourcesListControl View { get; set;}
		[Outlet]
		NSOutlineView outlineView { get; set; }
		[Outlet]
		NSTableColumn sourceCheckedColumn { get; set; }
		[Outlet]
		NSTableColumn sourceDescriptionColumn { get; set; }


		IViewEvents viewEvents;
		SourcesListOutlineDataSource dataSource = new SourcesListOutlineDataSource();
		bool updating;


		public SourcesListControlAdapter()
		{
			NSBundle.LoadNib ("SourcesListControl", this);

			outlineView.DataSource = dataSource;
		}
			
		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.BeginUpdate()
		{
			updating = true;
		}

		void IView.EndUpdate()
		{
			updating = false;
			DoFullUpdate();
		}

		IViewItem IView.CreateItem(string key, ILogSource logSource, LogJoint.Preprocessing.ILogSourcePreprocessing logSourcePreprocessing)
		{
			return new SourcesListItem()
				{
					key = key,
					logSource = logSource,
					logSourcePreprocessing = logSourcePreprocessing
				};
		}

		IViewItem IView.GetItem(int idx)
		{
			return dataSource.Items[idx];
		}

		void IView.RemoveAt(int idx)
		{
			dataSource.Items[idx].updater = null;
			dataSource.Items.RemoveAt(idx);
			if (!updating)
				DoFullUpdate();
		}

		int IView.IndexOfKey(string key)
		{
			return dataSource.Items.IndexOf(i => i.key == key).GetValueOrDefault(-1);
		}

		void IView.Add(IViewItem itemIntf)
		{
			var item = (SourcesListItem)itemIntf; 
			dataSource.Items.Add(item);
			item.updater = UpdateItem;
			UpdateItem(item);
		}

		void IView.SetTopItem(IViewItem item)
		{
			// todo
		}

		void IView.InvalidateFocusedMessageArea()
		{
			// todo
		}

		string IView.ShowSaveLogDialog(string suggestedLogFileName)
		{
			return null;
		}

		void IView.ShowSaveLogError(string msg)
		{
		}

		int IView.ItemsCount
		{
			get { return dataSource.Items.Count; }
		}

		public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item) 
		{
			var sourceItem = item as SourcesListItem;

			if (tableColumn == sourceCheckedColumn)
			{
				var cellIdentifier = "checked_cell";
				var view = (NSButton)outlineView.MakeView (cellIdentifier, this);

				if (view == null)
				{
					view = new NSButton ();
					view.Identifier = cellIdentifier;
					view.SetButtonType(NSButtonType.Switch);
					view.BezelStyle = 0;
					view.ImagePosition = NSCellImagePosition.ImageOnly;
				}

				view.State = sourceItem.isChecked.GetValueOrDefault(false) ? 
					NSCellStateValue.On : NSCellStateValue.Off;
				return view;
			}
			else if (tableColumn == sourceDescriptionColumn)
			{
				var cellIdentifier = "description_cell";
				var view = (NSTextField)outlineView.MakeView (cellIdentifier, this);

				if (view == null)
				{
					view = new NSTextField ();
					view.Identifier = cellIdentifier;
					view.BackgroundColor = NSColor.Clear;
					view.Bordered = false;
					view.Selectable = false;
					view.Editable = false;
				}

				view.StringValue = sourceItem.text;
				return view;
			}


			return null;
		}

		public override void SelectionDidChange(NSNotification notification)
		{
			foreach (var x in dataSource.Items.ZipWithIndex())
				x.Value.isSelected = outlineView.IsRowSelected(x.Key);
			viewEvents.OnSelectionChanged();
		}

		void UpdateItem(SourcesListItem item)
		{
			if (!updating)
			{
				outlineView.ReloadItem(item);
				if (item.isSelected)
					outlineView.SelectRow(outlineView.RowForItem(item), true);
				else
					outlineView.DeselectRow(outlineView.RowForItem(item));
			}
		}

		void DoFullUpdate()
		{
			outlineView.ReloadData();
			outlineView.SelectRows(NSIndexSet.FromArray(
				dataSource.Items
				.ZipWithIndex()
				.Where(x => x.Value.isSelected)
				.Select(x => x.Key)
				.ToArray()
			), byExtendingSelection: false);
		}
	}
}
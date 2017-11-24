
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using ObjCRuntime;

namespace LogJoint.UI
{
	public partial class FilesSelectionDialogController : AppKit.NSWindowController
	{
		readonly DataSource dataSource = new DataSource();
		readonly string prompt;

		#region Constructors

		// Called when created from unmanaged code
		public FilesSelectionDialogController(IntPtr handle)
			: base(handle)
		{
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public FilesSelectionDialogController(NSCoder coder)
			: base(coder)
		{
		}
		
		// Call to load from the XIB/NIB file
		public FilesSelectionDialogController(string prompt)
			: base("FilesSelectionDialog")
		{
			this.prompt = prompt;
		}
		
		#endregion

		public new FilesSelectionDialog Window
		{
			get
			{
				return (FilesSelectionDialog)base.Window;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			Window.owner = this;

			Window.Title = prompt;
			tableView.Delegate = new Delegate() { owner = this };
			tableView.DataSource = dataSource;

			checkAllButton.StringValue = "check all";
			checkAllButton.LinkClicked = (s, e) => SetAllChecked(true);
			uncheckAllButton.StringValue = "uncheck all";
			uncheckAllButton.LinkClicked = (s, e) => SetAllChecked(false);
		}

		public static bool[] Execute(string prompt, string[] choises)
		{
			var dialog = new FilesSelectionDialogController (prompt);
			return dialog.ExecuteInternal(choises);
		}

		bool[] ExecuteInternal(string[] choises)
		{
			Window.ToString(); // force loading nib
			dataSource.Items.Clear();
			dataSource.Items.AddRange(choises.Select((i, idx) => new Item() 
			{ 
				Idx = idx, 
				Data = i, 
				IsSelected = true,
				Table = tableView
			}));
			tableView.ReloadData();
			if (choises.Length > 0)
				tableView.SelectRow(0, false);
			NSApplication.SharedApplication.RunModalForWindow (Window);
			return dataSource.Items.Select(i => i.IsSelected).ToArray();
		}

		partial void OnCancelButtonClicked (NSObject sender)
		{
			dataSource.Items.ForEach(i => { i.IsSelected = false; });
			NSApplication.SharedApplication.AbortModal ();
			this.Close();
		}

		partial void OnOpenButtonClicked (NSObject sender)
		{
			NSApplication.SharedApplication.StopModal ();
			this.Close();
		}

		public void ToggleSelected()
		{
			var updated = new List<nuint>();
			for (int i = 0; i < dataSource.Items.Count; ++i)
			{
				if (tableView.IsRowSelected(i))
				{
					updated.Add((nuint)i);
					dataSource.Items[i].IsSelected = !dataSource.Items[i].IsSelected;
				}
			};
			tableView.ReloadData(NSIndexSet.FromArray(updated.ToArray()), NSIndexSet.FromIndex(0));
		}

		void SetAllChecked(bool value)
		{
			dataSource.Items.ForEach(i => { i.IsSelected = value; });
			tableView.ReloadData();
		}

		class Delegate: NSTableViewDelegate
		{
			public FilesSelectionDialogController owner;

			public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				var item = owner.dataSource.Items[(int)row];

				NSButton view = (NSButton)tableView.MakeView ("cb", this);
				if (view == null) 
				{
					view = new NSButton()
					{
						Identifier = "cb",
						Bordered = false,
						Action = new Selector("OnChecked:")
					};
					view.SetButtonType(NSButtonType.Switch);
				}

				view.Target = item;
				view.Title = item.Data;
				view.State = item.IsSelected ? NSCellStateValue.On : NSCellStateValue.Off;

				return view;
			}
		};

		class DataSource: NSTableViewDataSource
		{
			public List<Item> Items = new List<Item>();

			public override nint GetRowCount(NSTableView tableView)
			{
				return Items.Count;
			}
		};

		class Item: NSObject
		{
			public int Idx;
			public string Data;
			public bool IsSelected;
			public NSTableView Table;

			[Export("OnChecked:")]
			void OnChecked(NSButton sender)
			{
				IsSelected = sender.State == NSCellStateValue.On;
				Table.SelectRow(Idx, byExtendingSelection: false);
			}
		};
	}
}


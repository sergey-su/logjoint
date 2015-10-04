
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace LogJoint.UI
{
	public partial class FilesSelectionDialogController : MonoMac.AppKit.NSWindowController
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

			Window.Title = prompt;
			tableView.Delegate = new Delegate() { owner = this };
			tableView.DataSource = dataSource;
		}

		public static bool[] Execute(string prompt, string[] choises)
		{
			var dialog = new FilesSelectionDialogController (prompt);
			return dialog.ExecuteInternal(choises);
		}

		bool[] ExecuteInternal(string[] choises)
		{
			dataSource.Items.Clear();
			dataSource.Items.AddRange(choises.Select(i => new Item() { Data = i, IsSelected = true }));
			Window.ToString(); // force loading nib
			tableView.ReloadData();
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


		class Delegate: NSTableViewDelegate
		{
			public FilesSelectionDialogController owner;

			public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, int row)
			{
				var item = owner.dataSource.Items[row];

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

			public override int GetRowCount(NSTableView tableView)
			{
				return Items.Count;
			}
		};

		class Item: NSObject
		{
			public string Data;
			public bool IsSelected;

			[Export("OnChecked:")]
			void OnChecked(NSButton sender)
			{
				IsSelected = sender.State == NSCellStateValue.On;
			}
		};
	}
}


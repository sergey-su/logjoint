
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.NewLogSourceDialog;

namespace LogJoint.UI
{
	public partial class NewLogSourceDialogController : AppKit.NSWindowController, IDialogView
	{
		IDialogViewEvents eventsHandler;
		List<IViewListItem> items = new List<IViewListItem>();

		#region Constructors

		// Called when created from unmanaged code
		public NewLogSourceDialogController(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public NewLogSourceDialogController(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public NewLogSourceDialogController(IDialogViewEvents eventsHandler)
			: base("NewLogSourceDialog")
		{
			this.eventsHandler = eventsHandler;
			this.Window.GetHashCode();
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			formatsListTable.DataSource = new DataSource() { owner = this };
			formatsListTable.Delegate = new Delegate() { owner = this };
		}

		//strongly typed window accessor
		public new NewLogSourceDialog Window
		{
			get { return (NewLogSourceDialog)base.Window; }
		}
			
		void IDialogView.ShowModal()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IDialogView.EndModal()
		{
			NSApplication.SharedApplication.AbortModal();
			this.Close();
		}

		void IDialogView.SetList(IViewListItem[] items, int selectedIndex)
		{
			this.items.Clear();
			this.items.AddRange(items);
			formatsListTable.ReloadData();
			if (selectedIndex < items.Length)
				formatsListTable.SelectRow(selectedIndex, false);
		}

		IViewListItem IDialogView.GetItem(int idx)
		{
			return items[idx];
		}

		void IDialogView.DetachPageView(object view)
		{
			NSView nsView = view as NSView;
			if (nsView == null)
				return;
			nsView.Hidden = true;
		}

		void IDialogView.AttachPageView(object view)
		{
			NSView nsView = view as NSView;
			if (nsView == null)
				return;
			nsView.MoveToPlaceholder((NSView)formatOptionsPagePlaceholder.ContentView);
			nsView.Hidden = false;
		}

		void IDialogView.SetFormatControls(string nameLabelValue, string descriptionLabelValue)
		{
			formatNameLabel.StringValue = nameLabelValue;
			formatDescriptionLabel.StringValue = descriptionLabelValue;
		}

		int IDialogView.SelectedIndex
		{
			get
			{
				return Enumerable.Range(0, items.Count).Where(i => formatsListTable.IsRowSelected(i)).FirstOrDefault(-1);
			}
		}

		partial void OnCancelPressed (Foundation.NSObject sender)
		{
			eventsHandler.OnCancelButtonClicked();
		}

		partial void OnOKPressed (Foundation.NSObject sender)
		{
			eventsHandler.OnOKButtonClicked();
		}

		partial void OnManageClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnManageFormatsButtonClicked();
		}

		class Delegate: NSTableViewDelegate
		{
			public NewLogSourceDialogController owner;

			public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				return new NSTextField()
				{
					Bordered = false,
					Editable = false,
					Selectable = false,
					BackgroundColor = NSColor.Clear,
					StringValue = owner.items[(int)row].ToString()
				};
			}

			public override void SelectionDidChange(NSNotification notification)
			{
				owner.eventsHandler.OnSelectedIndexChanged();
			}
		};

		class DataSource: NSTableViewDataSource
		{
			public NewLogSourceDialogController owner;

			public override nint GetRowCount(NSTableView tableView)
			{
				return owner.items.Count;
			}
		};
	}
}


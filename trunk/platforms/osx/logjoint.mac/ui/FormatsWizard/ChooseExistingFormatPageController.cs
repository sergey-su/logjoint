using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.ChooseExistingFormatPage;

namespace LogJoint.UI
{
	public partial class ChooseExistingFormatPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;
		DataSource dataSource = new DataSource();

		// Called when created from unmanaged code
		public ChooseExistingFormatPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public ChooseExistingFormatPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public ChooseExistingFormatPageController () : base ("ChooseExistingFormatPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		//strongly typed view accessor
		public new ChooseExistingFormatPage View {
			get {
				return (ChooseExistingFormatPage)base.View;
			}
		}

		partial void OnRadioButtonSelected (Foundation.NSObject sender)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			formatsTable.DataSource = dataSource;
			formatsTable.Delegate = new Delegate() { owner = this };
			((NSExtendedButton)deleteButton).OnDblClicked += (sender, e) => eventsHandler.OnControlDblClicked();
			((NSExtendedButton)changeButton).OnDblClicked += (sender, e) => eventsHandler.OnControlDblClicked();
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetFormatsListBoxItems (string [] items)
		{
			View.EnsureCreated();
			dataSource.items = items;
			formatsTable.ReloadData();
		}

		ControlId IView.SelectedOption 
		{
			get
			{
				if (deleteButton.State == NSCellStateValue.On)
					return ControlId.Delete;
				if (changeButton.State == NSCellStateValue.On)
					return ControlId.Change;
				return ControlId.None;
			}
		}

		int IView.SelectedFormatsListBoxItem => (int)(formatsTable?.SelectedRow).GetValueOrDefault(-1);

		class DataSource: NSTableViewDataSource
		{
			public string[] items = new string[0];

			public override nint GetRowCount (NSTableView tableView) => items.Length;
		};

		class Delegate: NSTableViewDelegate
		{
			public ChooseExistingFormatPageController owner;

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				var ret = NSLinkLabel.CreateLabel();
				ret.StringValue = owner.dataSource.items[(int)row];
				ret.LinkClicked = (sender, e) => 
				{
					if (e.NativeEvent.ClickCount >= 2)
						owner.eventsHandler.OnControlDblClicked();
				};
				return ret;
			}
		};
	}
}

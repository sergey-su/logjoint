using System;
using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.ImportNLogPage;

namespace LogJoint.UI
{
	public partial class ImportNLogPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;
		readonly UIUtils.SimpleTableDataSource<string> patternsDataSource = new UIUtils.SimpleTableDataSource<string>();

		// Called when created from unmanaged code
		public ImportNLogPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public ImportNLogPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public ImportNLogPageController () : base ("ImportNLogPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			patternsTable.DataSource = patternsDataSource;
			patternsTable.Delegate = new PatternsDelegate() { owner = this };
		}

		partial void OnOpenFileClicked(NSObject sender)
		{
			eventsHandler.OnOpenConfigButtonClicked();
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetAvailablePatternsListBoxItems (string [] values)
		{
			patternsDataSource.Items.Clear();
			patternsDataSource.Items.AddRange(values);
			patternsTable.ReloadData();
		}

		string IView.PatternTextBoxValue 
		{ 
			get => patternTextBox.StringValue;
			set => patternTextBox.StringValue = value;
		}
		string IView.ConfigFileTextBoxValue
		{ 
			get => configFileTextBox.StringValue;
			set => configFileTextBox.StringValue = value;
		}

		class PatternsDelegate: NSTableViewDelegate
		{
			public ImportNLogPageController owner;
			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				var lnk = NSLinkLabel.CreateLabel(owner.patternsDataSource.Items[(int)row]);
				lnk.LinkClicked = (sender, e) => 
				{
					if (e.NativeEvent.ClickCount == 2)
						owner.eventsHandler.OnSelectedAvailablePatternDoubleClicked();
				};
				return lnk;
			}
			public override void SelectionDidChange (NSNotification notification)
			{
				owner.eventsHandler.OnSelectedAvailablePatternChanged(
					owner.patternsTable.GetSelectedIndices().FirstOrDefault(-1));
			}
		};
	}
}

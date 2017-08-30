using System;
using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.ImportLog4NetPage;

namespace LogJoint.UI
{
	public partial class ImportLog4NetPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;
		readonly UIUtils.SimpleTableDataSource<string> patternsDataSource = new UIUtils.SimpleTableDataSource<string>();

		// Called when created from unmanaged code
		public ImportLog4NetPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public ImportLog4NetPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public ImportLog4NetPageController () : base ("ImportLog4NetPage", NSBundle.MainBundle)
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

		void IView.SetAvailablePatternsListItems (string [] value)
		{
			patternsDataSource.Items.Clear();
			patternsDataSource.Items.AddRange(value);
			patternsTable.ReloadData();
		}

		void IView.SetConfigFileTextBoxValue (string value)
		{
			configFileTextField.StringValue = value;
		}

		string IView.PatternTextBoxValue 
		{ 
			get => patternTextField.StringValue;
			set => patternTextField.StringValue = value;
		}

		class PatternsDelegate: NSTableViewDelegate
		{
			public ImportLog4NetPageController owner;
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

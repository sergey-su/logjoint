using System;
using System.Linq;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.FormatAdditionalOptionsPage;

namespace LogJoint.UI
{
	public partial class FormatAdditionalOptionsPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;
		NSLabeledStepperController bufferSizeStepper;
		UIUtils.SimpleTableDataSource<string> dataSource = new UIUtils.SimpleTableDataSource<string>();

		// Called when created from unmanaged code
		public FormatAdditionalOptionsPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public FormatAdditionalOptionsPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public FormatAdditionalOptionsPageController () : base ("FormatAdditionalOptionsPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		public new FormatAdditionalOptionsPage View {
			get {
				return (FormatAdditionalOptionsPage)base.View;
			}
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			bufferSizeStepper = new NSLabeledStepperController();
			bufferSizeStepper.View.MoveToPlaceholder(bufferSizeStepperPlaceholder);
			extensionsTable.DataSource = dataSource;
			extensionsTable.Delegate = new Delegate() { owner = this };
			newExtensionTextBox.Changed += (sender, e) => eventsHandler.OnExtensionTextBoxChanged();
		}

		partial void OnAddButtonClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnAddExtensionClicked();
		}

		partial void OnEnableBufferCheckBoxClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnEnableDejitterCheckBoxClicked();
		}

		partial void OnExtensionTextBoxChanged (Foundation.NSObject sender)
		{
		}

		partial void OnRemoveButtonClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnDelExtensionClicked();
		}


		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			View.EnsureCreated();
		}

		void IView.SetPatternsListBoxItems (string [] value)
		{
			dataSource.Items.Clear();
			dataSource.Items.AddRange(value);
			extensionsTable.ReloadData();
		}

		int [] IView.GetPatternsListBoxSelection ()
		{
			return extensionsTable.GetSelectedIndices().ToArray();
		}

		void IView.SetEncodingComboBoxItems (string [] items)
		{
			encodingCombobox.RemoveAllItems();
			encodingCombobox.AddItems(items);
			int idx = 0;
			foreach (var i in encodingCombobox.Items())
				i.Tag = idx++;
		}

		void IView.EnableControls (bool addExtensionButton, bool removeExtensionButton)
		{
			this.addExtensionButton.Enabled = addExtensionButton;
			this.removeExtensionButton.Enabled = removeExtensionButton;
		}

		int IView.EncodingComboBoxSelection 
		{ 
			get => (int)encodingCombobox.SelectedItem.Tag;
			set => encodingCombobox.SelectItem(value);
		}
		bool IView.EnableDejitterCheckBoxChecked 
		{ 
			get => enableBufferCheckbox.GetBoolValue();
			set => enableBufferCheckbox.SetBoolValue(value);
		}
		string IView.ExtensionTextBoxValue 
		{ 
			get => newExtensionTextBox.StringValue;
			set => newExtensionTextBox.StringValue = value;
		}

		Presenters.LabeledStepperPresenter.IView IView.BufferStepperView => bufferSizeStepper;

		class Delegate: NSTableViewDelegate
		{
			public FormatAdditionalOptionsPageController owner;

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row) 
			{
				return NSLinkLabel.CreateLabel(owner.dataSource.Items[(int)row]);
			}

			public override void SelectionDidChange (NSNotification notification)
			{
				owner.eventsHandler.OnExtensionsListBoxSelectionChanged();
			}
		};
	}
}

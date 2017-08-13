using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;
using System.Text;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class FieldsMappingDialogController : NSWindowController, IFieldsMappingDialogView
	{
		readonly IFieldsMappingDialogViewEvents events;
		readonly FieldsDataSource fieldsDataSource = new FieldsDataSource();

		public FieldsMappingDialogController (IFieldsMappingDialogViewEvents events) : base ("FieldsMappingDialog")
		{
			this.events = events;
			Window.EnsureCreated();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			availableInputFieldsContainer.LinkClicked = (sender, e) => (e.Link.Tag as Action)?.Invoke();
			helpLinkLabel.StringValue = "Help";
			helpLinkLabel.LinkClicked = (sender, e) => events.OnHelpLinkClicked();
			fieldsTable.DataSource = fieldsDataSource;
			fieldsTable.Delegate = new FieldsDelegate() { owner = this };
			nameComboBox.Changed += (sender, e) => events.OnNameComboBoxTextChanged();
			codeTextBox.Font = NSFont.FromFontName("Courier", 11);
			codeTextBox.TextDidChange += (sender, e) => events.OnCodeTextBoxChanged();
			fieldsLinksHScroller.ScrollerStyle = NSScrollerStyle.Legacy;
		}

		NSView GetControl(FieldsMappingDialogControlId ctrl)
		{
			switch (ctrl)
			{
			case FieldsMappingDialogControlId.RemoveFieldButton: return removeFieldButton;
			case FieldsMappingDialogControlId.NameComboBox: return nameComboBox;
			case FieldsMappingDialogControlId.CodeTypeComboBox: return codeTypeComboxBox;
			case FieldsMappingDialogControlId.CodeTextBox: return codeTextBox;
			case FieldsMappingDialogControlId.AvailableInputFieldsContainer: return availableInputFieldsContainer;
			default: return null;
			}
		}

		partial void OnAddFieldClicked (Foundation.NSObject sender)
		{
			events.OnAddFieldButtonClicked();
		}

		partial void OnCancelClicked (Foundation.NSObject sender)
		{
			events.OnCancelClicked();
		}

		partial void OnOkClicked (Foundation.NSObject sender)
		{
			events.OnOkClicked();
		}

		partial void OnRemoveFieldClicked (Foundation.NSObject sender)
		{
			events.OnRemoveFieldButtonClicked();
		}

		partial void OnCodeTypeChanged(NSObject sender)
		{
			events.OnCodeTypeSelectedIndexChanged();
		}

		partial void OnTestClicked (Foundation.NSObject sender)
		{
			events.OnTestClicked((NSEvent.CurrentModifierFlags & NSEventModifierMask.CommandKeyMask) != 0);
		}

		void IFieldsMappingDialogView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IFieldsMappingDialogView.Close ()
		{
			NSApplication.SharedApplication.StopModal();
			base.Close();
		}

		void IFieldsMappingDialogView.AddFieldsListBoxItem (string text)
		{
			fieldsDataSource.items.Add(text);
			fieldsTable.ReloadData();
		}

		void IFieldsMappingDialogView.RemoveFieldsListBoxItem (int idx)
		{
			fieldsDataSource.items.RemoveAt(idx);
			fieldsTable.ReloadData();
		}

		void IFieldsMappingDialogView.ChangeFieldsListBoxItem (int idx, string value)
		{
			fieldsDataSource.items[idx]= value;
			fieldsTable.ReloadData();
		}

		void IFieldsMappingDialogView.ModifyControl (FieldsMappingDialogControlId id, string text, bool? enabled)
		{
			var obj = GetControl(id);
			if (enabled != null)
				if (obj is NSControl)
					((NSControl)obj).Enabled = enabled.Value;
				else if (obj is NSLinkLabel)
					((NSLinkLabel)obj).IsEnabled = enabled.Value;
			if (text != null)
				if (obj is NSTextView)
					((NSTextView)obj).Value = text;
				else if (obj is NSButton)
					((NSButton)obj).StringValue = text;
				else if (obj is NSComboBox)
					((NSComboBox)obj).StringValue = text;
		}

		void IFieldsMappingDialogView.SetControlOptions (FieldsMappingDialogControlId id, string [] options)
		{
			var cb = (NSComboBox)GetControl(id);
			cb.RemoveAll();
			foreach (var opt in options)
				cb.Add(new NSString(opt));
		}

		void IFieldsMappingDialogView.SetAvailableInputFieldsLinks (Tuple<string, Action> [] links)
		{
			var linkBuilder = new StringBuilder();
			var linkRanges = new List<NSLinkLabel.Link>();
			foreach (var l in links)
			{
				if (linkBuilder.Length > 0)
					linkBuilder.Append("  ");
				linkBuilder.Append(l.Item1);
				linkRanges.Add(new NSLinkLabel.Link(
					linkBuilder.Length - l.Item1.Length, l.Item1.Length, l.Item2));
			}
			availableInputFieldsContainer.StringValue = linkBuilder.ToString();
			availableInputFieldsContainer.Links = linkRanges;
		}

		string IFieldsMappingDialogView.ReadControl (FieldsMappingDialogControlId id)
		{
			var obj = GetControl(id);
			if (obj is NSTextView)
				return ((NSTextView)obj).Value;
			else if (obj is NSButton)
				return ((NSButton)obj).StringValue;
			else if (obj is NSComboBox)
				return ((NSComboBox)obj).StringValue;
			return null;
		}

		void IFieldsMappingDialogView.ModifyCodeTextBoxSelection (int start, int len)
		{
			codeTextBox.SetSelectedRange(new NSRange(start, len));
		}

		int IFieldsMappingDialogView.FieldsListBoxSelection 
		{ 
			get => fieldsTable.GetSelectedIndices().FirstOrDefault(-1);
			set => fieldsTable.SelectRow(value, false);
		}
		int IFieldsMappingDialogView.CodeTypeComboBoxSelectedIndex 
		{ 
			get => (int)(codeTypeComboxBox.SelectedItem?.Tag).GetValueOrDefault(-1);
			set => codeTypeComboxBox.SelectItemWithTag(value);
		}
		int IFieldsMappingDialogView.CodeTextBoxSelectionStart
		{
			get => (int)codeTextBox.SelectedRange.Location;
		}

		class FieldsDataSource: NSTableViewDataSource
		{
			public List<string> items = new List<string>();
			public override nint GetRowCount (NSTableView tableView) => items.Count;
		};

		class FieldsDelegate: NSTableViewDelegate
		{
			public FieldsMappingDialogController owner;
			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				return NSLinkLabel.CreateLabel(owner.fieldsDataSource.items[(int)row]);
			}
			public override void SelectionDidChange (NSNotification notification)
			{
				owner.events.OnSelectedFieldChanged();
			}
		};
	}
}

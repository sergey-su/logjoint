using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.EditFieldsMapping;
using System.Text;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class FieldsMappingDialogController : NSWindowController, IView
	{
		IViewEvents events;
		readonly FieldsDataSource fieldsDataSource = new FieldsDataSource();

		public FieldsMappingDialogController () : base ("FieldsMappingDialog")
		{
			Window.EnsureCreated();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			codeTextBox.AutomaticQuoteSubstitutionEnabled = false;
			availableInputFieldsContainer.LinkClicked = (sender, e) => (e.Link.Tag as Action)?.Invoke();
			helpLinkLabel.StringValue = "Help";
			helpLinkLabel.LinkClicked = (sender, e) => events.OnHelpLinkClicked();
			fieldsTable.DataSource = fieldsDataSource;
			fieldsTable.Delegate = new FieldsDelegate() { owner = this };
			nameComboBox.Changed += (sender, e) => events.OnNameComboBoxTextChanged();
			codeTextBox.Font = NSFont.FromFontName("Courier", 11);
			codeTextBox.TextDidChange += (sender, e) => events.OnCodeTextBoxChanged();
			fieldsLinksHScroller.ScrollerStyle = NSScrollerStyle.Legacy;
			foreach (var i in codeTypeComboxBox.Items().ZipWithIndex())
				i.Value.Tag = i.Key;
		}

		NSView GetControl(ControlId ctrl)
		{
			switch (ctrl)
			{
			case ControlId.RemoveFieldButton: return removeFieldButton;
			case ControlId.NameComboBox: return nameComboBox;
			case ControlId.CodeTypeComboBox: return codeTypeComboxBox;
			case ControlId.CodeTextBox: return codeTextBox;
			case ControlId.AvailableInputFieldsContainer: return availableInputFieldsContainer;
			default: return null;
			}
		}

		partial void OnNameComboxBoxChanged (Foundation.NSObject sender)
		{
			events.OnNameComboBoxTextChanged();
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

		void IView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IView.Close ()
		{
			NSApplication.SharedApplication.StopModal();
			base.Close();
		}

		void IView.AddFieldsListBoxItem (string text)
		{
			fieldsDataSource.items.Add(text);
			fieldsTable.ReloadData();
		}

		void IView.RemoveFieldsListBoxItem (int idx)
		{
			fieldsDataSource.items.RemoveAt(idx);
			fieldsTable.ReloadData();
		}

		void IView.ChangeFieldsListBoxItem (int idx, string value)
		{
			fieldsDataSource.items[idx]= value;
			fieldsTable.ReloadData(new NSIndexSet(idx), new NSIndexSet(0));
		}

		void IView.ModifyControl (ControlId id, string text, bool? enabled)
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

		void IView.SetControlOptions (ControlId id, string [] options)
		{
			var cb = (NSComboBox)GetControl(id);
			cb.RemoveAll();
			foreach (var opt in options)
				cb.Add(new NSString(opt));
		}

		void IView.SetAvailableInputFieldsLinks (Tuple<string, Action> [] links)
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

		string IView.ReadControl (ControlId id)
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

		void IView.ModifyCodeTextBoxSelection (int start, int len)
		{
			codeTextBox.SetSelectedRange(new NSRange(start, len));
		}

		void IView.SetEventsHandler (IViewEvents events)
		{
			this.events = events;
		}

		int IView.FieldsListBoxSelection 
		{ 
			get => fieldsTable.GetSelectedIndices().FirstOrDefault(-1);
			set => fieldsTable.SelectRow(value, false);
		}
		int IView.CodeTypeComboBoxSelectedIndex 
		{ 
			get => (int)(codeTypeComboxBox.SelectedItem?.Tag).GetValueOrDefault(-1);
			set => codeTypeComboxBox.SelectItemWithTag(value);
		}
		int IView.CodeTextBoxSelectionStart
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

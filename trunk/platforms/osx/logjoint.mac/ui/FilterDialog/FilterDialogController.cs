using System;

using Foundation;
using AppKit;
using LogJoint.UI.Presenters.FilterDialog;
using System.Collections.Generic;
using System.Linq;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public partial class FilterDialogController : NSWindowController, IView
	{
		NSWindowController parent;
		IViewEvents eventsHandler;
		bool accepted;
		List<KeyValuePair<ScopeItem, bool>> scopeItems;
		NSButton[] severityCheckboxes;

		public FilterDialogController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public FilterDialogController (NSCoder coder) : base (coder)
		{
		}

		public FilterDialogController (NSWindowController parent) : base ("FilterDialog")
		{
			this.parent = parent;
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			nameEditLinkLabel.LinkClicked = (sender, e) => eventsHandler.OnNameEditLinkClicked();
			templateEditBox.Changed += (sender, e) => eventsHandler.OnCriteriaInputChanged();
			severityCheckboxes = new [] { severityCheckbox1, severityCheckbox2, severityCheckbox3 };
		}

		void IView.SetEventsHandler (IViewEvents handler)
		{
			this.eventsHandler = handler;
		}

		void IView.SetData (string title, KeyValuePair<string, ModelColor?>[] actionComboBoxOptions, 
			string [] typesOptions, DialogValues values)
		{
			Window.Title = title;

			SetNameEditProperties(values.NameEditBoxProperties);

			actionComboxBox.RemoveAllItems();
			actionComboxBox.AddItems(actionComboBoxOptions.Select(i => i.Key).ToArray());
			foreach (var i in actionComboxBox.Items().ZipWithIndex())
			{
				var opt = actionComboBoxOptions[i.Key];
				if (opt.Value == null)
					continue;
				var dict = new NSMutableDictionary();
				dict.SetValueForKey(opt.Value.Value.ToColor().ToNSColor(), NSStringAttributeKey.BackgroundColor);
				var attrStr = new NSAttributedString(opt.Key, dict);
				i.Value.AttributedTitle = attrStr;
			}
			actionComboxBox.SelectItem(values.ActionComboBoxValue);

			enabledCheckbox.State = values.EnabledCheckboxValue ? NSCellStateValue.On : NSCellStateValue.Off;

			templateEditBox.StringValue = values.TemplateEditValue;

			matchCaseCheckbox.State = values.MatchCaseCheckboxValue ? NSCellStateValue.On : NSCellStateValue.Off;
			regexCheckbox.State = values.RegExpCheckBoxValue ? NSCellStateValue.On : NSCellStateValue.Off;
			wholeWordCheckbox.State = values.WholeWordCheckboxValue ? NSCellStateValue.On : NSCellStateValue.Off;

			for (int t = 0; t < Math.Min(typesOptions.Length, severityCheckboxes.Length); ++t)
			{
				severityCheckboxes[t].Title = typesOptions[t];
				severityCheckboxes[t].State = values.TypesCheckboxesValues[t] ? 
					NSCellStateValue.On : NSCellStateValue.Off;
			}

			// todo: impl scope
			scopeItems = values.ScopeItems;
		}

		DialogValues IView.GetData ()
		{
			return new DialogValues()
			{
				NameEditBoxProperties = new NameEditBoxProperties()
				{
					Value = nameTextBox.StringValue,
				},
				EnabledCheckboxValue = enabledCheckbox.State == NSCellStateValue.On,
				TemplateEditValue = templateEditBox.StringValue,
				MatchCaseCheckboxValue = matchCaseCheckbox.State == NSCellStateValue.On,
				RegExpCheckBoxValue = regexCheckbox.State == NSCellStateValue.On,
				WholeWordCheckboxValue = wholeWordCheckbox.State == NSCellStateValue.On,
				ActionComboBoxValue = (int)actionComboxBox.IndexOfSelectedItem,
				TypesCheckboxesValues = severityCheckboxes.Select(cb => cb.State == NSCellStateValue.On).ToList(),

				// todo
				ScopeItems = scopeItems,
			};
		}

		void IView.SetScopeItemChecked (int idx, bool checkedValue)
		{
			// todo
		}

		void IView.SetNameEditProperties (NameEditBoxProperties props)
		{
			SetNameEditProperties (props);
		}

		bool IView.ShowDialog ()
		{
			accepted = false;
			InvokeOnMainThread(SetInitialFocus);
			NSApplication.SharedApplication.BeginSheet (Window, parent?.Window);
			NSApplication.SharedApplication.RunModalForWindow (Window);
			return accepted;
		}

		void IView.PutFocusOnNameEdit()
		{
			Window.MakeFirstResponder(nameTextBox);
			nameTextBox.SelectText(this);
		}

		private void SetNameEditProperties (NameEditBoxProperties props)
		{
			nameTextBox.StringValue = props.Value;
			nameEditLinkLabel.StringValue = props.LinkText;
			nameTextBox.Enabled = props.Enabled;
		}

		partial void OnConfirmed (Foundation.NSObject sender)
		{
			accepted = true;
			NSApplication.SharedApplication.EndSheet (Window);
			Window.Close();
			NSApplication.SharedApplication.StopModal ();
		}

		partial void OnCancelled (Foundation.NSObject sender)
		{
			accepted = false;
			NSApplication.SharedApplication.EndSheet (Window);
			Window.Close();
			NSApplication.SharedApplication.AbortModal ();
		}

		partial void OnInputChanged (Foundation.NSObject sender)
		{
			eventsHandler.OnCriteriaInputChanged();
		}

		void SetInitialFocus()
		{
			Window.MakeFirstResponder(templateEditBox);
		}

		public new FilterDialog Window => (FilterDialog)base.Window;
	}
}

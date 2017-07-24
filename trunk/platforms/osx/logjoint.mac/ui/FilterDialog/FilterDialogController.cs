using System;

using Foundation;
using AppKit;
using LogJoint.UI.Presenters.FilterDialog;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class FilterDialogController : NSWindowController, IView
	{
		NSWindowController parent;
		IViewEvents eventsHandler;
		bool accepted;
		List<KeyValuePair<ScopeItem, bool>> scopeItems;

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
		}

		void IView.SetEventsHandler (IViewEvents handler)
		{
			this.eventsHandler = handler;
		}

		void IView.SetData (string title, string [] actionComboBoxOptions, 
			string [] typesOptions, DialogValues values)
		{
			Window.Title = title;

			nameTextBox.StringValue = values.NameEditValue;

			actionComboxBox.RemoveAllItems();
			actionComboxBox.AddItems(actionComboBoxOptions);
			actionComboxBox.SelectItem(values.ActionComboBoxValue);

			enabledCheckbox.State = values.EnabledCheckboxValue ? NSCellStateValue.On : NSCellStateValue.Off;

			templateEditBox.StringValue = values.TemplateEditValue;

			matchCaseCheckbox.State = values.MatchCaseCheckboxValue ? NSCellStateValue.On : NSCellStateValue.Off;
			regexCheckbox.State = values.RegExpCheckBoxValue ? NSCellStateValue.On : NSCellStateValue.Off;
			wholeWordCheckbox.State = values.WholeWordCheckboxValue ? NSCellStateValue.On : NSCellStateValue.Off;

			// todo
			scopeItems = values.ScopeItems;
			//values.TypesCheckboxesValues
		}

		DialogValues IView.GetData ()
		{
			return new DialogValues()
			{
				NameEditValue = nameTextBox.StringValue,
				EnabledCheckboxValue = enabledCheckbox.State == NSCellStateValue.On,
				TemplateEditValue = templateEditBox.StringValue,
				MatchCaseCheckboxValue = matchCaseCheckbox.State == NSCellStateValue.On,
				RegExpCheckBoxValue = regexCheckbox.State == NSCellStateValue.On,
				WholeWordCheckboxValue = wholeWordCheckbox.State == NSCellStateValue.On,
				ActionComboBoxValue = (int)actionComboxBox.IndexOfSelectedItem,

				// todo
				ScopeItems = scopeItems,
				TypesCheckboxesValues = null
			};
		}

		void IView.SetScopeItemChecked (int idx, bool checkedValue)
		{
			// todo
		}

		void IView.SetNameEditValue (string value)
		{
			nameTextBox.StringValue = value;
		}

		bool IView.ShowDialog ()
		{
			accepted = false;
			NSApplication.SharedApplication.BeginSheet (Window, parent?.Window);
			NSApplication.SharedApplication.RunModalForWindow (Window);
			return accepted;
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

		public new FilterDialog Window => (FilterDialog)base.Window;
	}
}

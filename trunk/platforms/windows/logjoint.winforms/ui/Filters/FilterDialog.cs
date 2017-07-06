using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Threading;
using LogJoint.UI.Presenters.FilterDialog;

namespace LogJoint.UI
{
	public partial class FilterDialog : Form
	{
		public IViewEvents eventsHandler;

		public FilterDialog()
		{
			InitializeComponent();
		}
		
		private void threadsCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			var i = threadsCheckedListBox.Items[e.Index] as FilterDialogView.ScopeItemWrap;
			if (i == null)
				return;
			eventsHandler.OnScopeItemChecked(i.item, threadsCheckedListBox.GetItemChecked(e.Index));
		}

		private void messagesTypesCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			SynchronizationContext.Current.Post(state => eventsHandler.OnCriteriaInputChanged(), null);
		}

		private void FilterDialog_Shown(object sender, EventArgs e)
		{
			templateTextBox.Focus();
		}

		private void criteriaInputChanged(object sender, EventArgs e)
		{
			eventsHandler.OnCriteriaInputChanged();
		}
	}

	public class FilterDialogView : Presenters.FilterDialog.IView
	{
		Lazy<FilterDialog> dialog = new Lazy<FilterDialog>();
		IViewEvents eventsHandler;

		public FilterDialogView()
		{
		}

		void IView.SetEventsHandler(IViewEvents handler)
		{
			this.eventsHandler = handler;
		}

		void IView.SetData(
			string title,
			string[] actionComboBoxOptions,
			string[] typesOptions,
			DialogValues values
		)
		{
			var d = dialog.Value;
			d.eventsHandler = eventsHandler;
			d.Text = title;
			d.nameTextBox.Text = values.NameEditValue;
			d.enabledCheckBox.Checked = values.EnabledCheckboxValue;
			d.templateTextBox.Text = values.TemplateEditValue;
			d.matchCaseCheckbox.Checked = values.MatchCaseCheckboxValue;
			d.regExpCheckBox.Checked = values.RegExpCheckBoxValue;
			d.wholeWordCheckbox.Checked = values.WholeWordCheckboxValue;
			d.actionComboBox.Items.Clear();
			d.actionComboBox.Items.AddRange(actionComboBoxOptions);
			d.actionComboBox.SelectedIndex = values.ActionComboBoxValue;
			d.threadsCheckedListBox.Items.Clear();
			foreach (var i in values.ScopeItems)
				d.threadsCheckedListBox.Items.Add(new ScopeItemWrap() { item = i.Key }, i.Value);
			d.messagesTypesCheckedListBox.Items.Clear();
			for (var i = 0; i < typesOptions.Length; ++i)
				d.messagesTypesCheckedListBox.Items.Add(typesOptions[i], values.TypesCheckboxesValues[i]);
		}

		DialogValues IView.GetData()
		{
			var d = dialog.Value;
			return new DialogValues()
			{
				NameEditValue = d.nameTextBox.Text,
				EnabledCheckboxValue = d.enabledCheckBox.Checked,
				TemplateEditValue = d.templateTextBox.Text,
				MatchCaseCheckboxValue = d.matchCaseCheckbox.Checked,
				RegExpCheckBoxValue = d.regExpCheckBox.Checked,
				WholeWordCheckboxValue = d.wholeWordCheckbox.Checked,
				ActionComboBoxValue = d.actionComboBox.SelectedIndex,
				ScopeItems = d.threadsCheckedListBox.Items.OfType<ScopeItemWrap>().Select(
					(i, idx) => new KeyValuePair<ScopeItem, bool>(i.item, d.threadsCheckedListBox.GetItemChecked(idx))).ToList(),
				TypesCheckboxesValues = Enumerable.Range(0, d.messagesTypesCheckedListBox.Items.Count).Select(
					idx => d.messagesTypesCheckedListBox.GetItemChecked(idx)).ToList()
			};
		}

		void IView.SetNameEditValue(string value)
		{
			dialog.Value.nameTextBox.Text = value;
		}

		void IView.SetScopeItemChecked(int idx, bool checkedValue)
		{
			dialog.Value.threadsCheckedListBox.SetItemChecked(idx, checkedValue);
		}

		bool IView.ShowDialog()
		{
			return dialog.Value.ShowDialog() == DialogResult.OK;
		}

		public class ScopeItemWrap
		{
			public ScopeItem item;
			public static readonly int TabSize = 4;

			public override string ToString()
			{
				return new string(' ', item.Indent * TabSize) + item.ToString();
			}
		}
	};
}
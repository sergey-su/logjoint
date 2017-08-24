using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using LogJoint.UI.Presenters.FormatsWizard.EditFieldsMapping;

namespace LogJoint.UI
{
	public partial class FieldsMappingForm : Form, IView
	{
		IViewEvents eventsHandler;

		public FieldsMappingForm()
		{
			InitializeComponent();
			InitTabStops();
		}

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendTabStopsMessage(HandleRef hWnd, int msg,
			int wParam, [In, MarshalAs(UnmanagedType.LPArray)] uint[] stops);

		void InitTabStops()
		{
			int EM_SETTABSTOPS = 0x00CB;
			SendTabStopsMessage(new HandleRef(codeTextBox, codeTextBox.Handle), EM_SETTABSTOPS, 1, new uint[] { 16 });
		}


		private void addFieldButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnAddFieldButtonClicked();
		}

		private void fieldsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			eventsHandler.OnSelectedFieldChanged();
		}

		private void removeFieldButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnRemoveFieldButtonClicked();
		}

		private void nameComboBox_TextUpdate(object sender, EventArgs e)
		{
			eventsHandler.OnNameComboBoxTextChanged();
		}

		private void codeTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			eventsHandler.OnCodeTypeSelectedIndexChanged();
		}

		private void codeTextBox_TextChanged(object sender, EventArgs e)
		{
			eventsHandler.OnCodeTextBoxChanged();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnOkClicked();
		}
		
		private void testButton_Click(object sender, EventArgs evt)
		{
			eventsHandler.OnTestClicked((Control.ModifierKeys & Keys.Control) != 0);
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnHelpLinkClicked();
		}

		Control GetControl(ControlId ctrl)
		{
			switch (ctrl)
			{
				case ControlId.RemoveFieldButton: return removeFieldButton;
				case ControlId.NameComboBox: return nameComboBox;
				case ControlId.CodeTypeComboBox: return codeTypeComboBox;
				case ControlId.CodeTextBox: return codeTextBox;
				case ControlId.AvailableInputFieldsContainer: return availableInputFieldsPanel;
				default: return null;
			}
		}

		void IView.Show()
		{
			ShowDialog();
		}

		void IView.Close()
		{
			base.Close();
		}

		void IView.AddFieldsListBoxItem(string text)
		{
			fieldsListBox.Items.Add(text);
		}

		void IView.RemoveFieldsListBoxItem(int idx)
		{
			fieldsListBox.Items.RemoveAt(idx);
		}

		void IView.ChangeFieldsListBoxItem(int idx, string value)
		{
			fieldsListBox.Items[idx] = value;
		}

		void IView.ModifyControl(ControlId id, string text, bool? enabled)
		{
			var ctrl = GetControl(id);
			if (text != null)
				ctrl.Text = text;
			if (enabled != null)
				ctrl.Enabled = enabled.Value;
		}

		void IView.SetControlOptions(ControlId id, string[] options)
		{
			var cb = GetControl(id) as ComboBox;
			if (cb == null)
				return;
			cb.Items.Clear();
			cb.Items.AddRange(options);
		}

		void IView.SetAvailableInputFieldsLinks(Tuple<string, Action>[] links)
		{
			availableInputFieldsPanel.Controls.Clear();
			foreach (var f in links)
			{
				LinkLabel l = new LinkLabel();
				l.Text = f.Item1;
				l.AutoSize = true;
				l.Click += (s, e) => f.Item2();
				availableInputFieldsPanel.Controls.Add(l);
			}
		}

		string IView.ReadControl(ControlId id)
		{
			return GetControl(id)?.Text;
		}

		void IView.ModifyCodeTextBoxSelection(int start, int len)
		{
			codeTextBox.SelectionStart = start;
			codeTextBox.SelectionLength = len;
			codeTextBox.Focus();
		}

		void IView.SetEventsHandler(IViewEvents events)
		{
			this.eventsHandler = events;
		}

		int IView.FieldsListBoxSelection
		{
			get { return fieldsListBox.SelectedIndex; }
			set { fieldsListBox.SelectedIndex = value; }
		}

		int IView.CodeTypeComboBoxSelectedIndex
		{
			get { return codeTypeComboBox.SelectedIndex; }
			set { codeTypeComboBox.SelectedIndex = value; }
		}

		int IView.CodeTextBoxSelectionStart => codeTextBox.SelectionStart;
	}
}
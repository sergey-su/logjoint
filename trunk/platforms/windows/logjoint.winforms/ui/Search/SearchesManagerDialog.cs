using LogJoint.UI.Presenters.SearchesManagerDialog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class SearchesManagerDialog : Form, IDialogView
	{
		readonly IDialogViewEvents eventsHandler;
		readonly Dictionary<ViewControl, Control> controls;

		public SearchesManagerDialog(IDialogViewEvents eventsHandler)
		{
			InitializeComponent();
			this.eventsHandler = eventsHandler;
			this.controls = new Dictionary<ViewControl, Control>()
			{
				{ ViewControl.AddButton, addButton },
				{ ViewControl.DeleteButton, deleteButton },
				{ ViewControl.EditButton, editButton },
				{ ViewControl.Export, exportButton },
				{ ViewControl.Import, importButton },
			};
		}

		ViewItem[] IDialogView.SelectedItems
		{
			get
			{
				return listView.SelectedItems.OfType<ListViewItem>().Select(i => i.Tag).OfType<ViewItem>().ToArray();
			}
			set
			{
				var lookup = value.ToLookup(i => i);
				listView.SelectedIndices.Clear();
				foreach (ListViewItem i in listView.Items)
					if (lookup.Contains(i.Tag as ViewItem))
						listView.SelectedIndices.Add(i.Index);
			}
		}

		void IDialogView.CloseModal()
		{
			DialogResult = DialogResult.OK;
		}

		void IDialogView.EnableControl(ViewControl id, bool value)
		{
			Control ctrl;
			if (controls.TryGetValue(id, out ctrl))
				ctrl.Enabled = value;
		}

		void IDialogView.OpenModal()
		{
			ShowDialog();
		}

		void IDialogView.SetItems(ViewItem[] items)
		{
			listView.Items.Clear();
			listView.Items.AddRange(items.Select(i => new ListViewItem(new[] { i.Caption })
			{
				Tag = i
			}).ToArray());
		}

		void IDialogView.SetCloseButtonText(string text)
		{
			closeButton.Text = text;
		}

		private void addButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnAddClicked();
		}

		private void deleteButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnDeleteClicked();
		}

		private void editButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnEditClicked();
		}

		private void exportButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnExportClicked();
		}

		private void importButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnImportClicked();
		}

		private void closeButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnCloseClicked();
		}

		private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			eventsHandler.OnSelectionChanged();
		}

		private void listView_Layout(object sender, LayoutEventArgs e)
		{
			columnHeader1.Width = listView.ClientSize.Width - 2;
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (Form.ModifierKeys == Keys.None && keyData == Keys.Escape)
			{
				this.Close();
				return true;
			}
			return base.ProcessDialogKey(keyData);
		}
	}

	class SearchesManagerDialogView : IView
	{
		IDialogView IView.CreateDialog(IDialogViewEvents eventsHandler)
		{
			return new SearchesManagerDialog(eventsHandler);
		}
	};
}

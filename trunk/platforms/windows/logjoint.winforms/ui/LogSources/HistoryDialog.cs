using LogJoint.UI.Presenters.HistoryDialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class HistoryDialog : Form, IView
	{
		IViewEvents eventsHandler;
		bool refreshColumnHeaderPosted;

		public HistoryDialog()
		{
			InitializeComponent();
		}

		UI.Presenters.QuickSearchTextBox.IView IView.QuickSearchTextBox
		{
			get { return quickSearchTextBox.InnerTextBox; }
		}

		void IView.SetEventsHandler(IViewEvents presenter)
		{
			this.eventsHandler = presenter;
		}

		void IView.Show()
		{
			ShowDialog();
		}

		ViewItem[] IView.SelectedItems
		{
			get
			{
				return
					listView.SelectedItems.OfType<ListViewItem>()
					.Select(i => i.Tag as ViewItem)
					.Where(i => i != null)
					.ToArray(); 
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

		void IView.AboutToShow()
		{
		}

		void IView.Update(ViewItem[] items)
		{
			listView.BeginUpdate();
			listView.Items.Clear();
			listView.Items.AddRange(items.Select(i =>
			{
				var li = new ListViewItem()
				{
					Text = i.Text,
					ForeColor = i.Type == ViewItemType.HistoryComment ? Color.Gray : Color.Black,
					Tag = i
				};
				li.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = i.Annotation });
				return li;
			}).ToArray());
			listView.EndUpdate();
		}

		void IView.PutInputFocusToItemsList()
		{
			if (listView.CanFocus)
			{
				listView.Focus();
			}
		}

		void IView.EnableOpenButton(bool enable)
		{
			openButton.Enabled = enable;
		}

		bool IView.ShowClearHistoryConfirmationDialog(string message)
		{
			return MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes;
		}

		void IView.ShowOpeningFailurePopup(string message)
		{
			MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		private async void RefreshHeaders()
		{
			if (refreshColumnHeaderPosted)
				return;
			refreshColumnHeaderPosted = true;
			await Task.Yield();
			entryColumnHeader.Width = listView.ClientSize.Width - annotationColumnHeader.Width - 10;
			refreshColumnHeaderPosted = false;
		}


		private void listView_Layout(object sender, LayoutEventArgs e)
		{
			RefreshHeaders();
		}

		private void listView_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
		{
			RefreshHeaders();
		}

		private void openButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnOpenClicked();
		}

		private void listView_DoubleClick(object sender, EventArgs e)
		{
			eventsHandler.OnDoubleClick();
		}

		private void HistoryDialog_Shown(object sender, EventArgs e)
		{
			eventsHandler.OnDialogShown();
		}

		private void HistoryDialog_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.F)
				eventsHandler.OnFindShortcutPressed();
		}

		private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			eventsHandler.OnSelectedItemsChanged();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			eventsHandler.OnClearHistoryButtonClicked();
		}

		private void listView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			e.Cancel = true;
		}
	}
}

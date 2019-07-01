using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.PreprocessingUserInteractions;
using LogJoint.UI.Windows.Reactive;

namespace LogJoint.UI
{
	public partial class FilesSelectionDialog : Form
	{
		IListBoxController listBoxController;

		public FilesSelectionDialog()
		{
			InitializeComponent();
		}

		public static void Open(IViewModel viewModel, IReactive reactive, out FilesSelectionDialog dialog)
		{
			dialog = new FilesSelectionDialog();
			var listBox = dialog.checkedListBox1;
			var listBoxController = reactive.CreateListBoxController(listBox);
			dialog.listBoxController = listBoxController;
			listBoxController.OnSelect = s => viewModel.OnSelect(s.LastOrDefault() as IDialogItem);
			listBoxController.OnUpdateRow = (item, i, oldItem) => listBox.SetItemChecked(i, ((IDialogItem)item).IsChecked);
			listBox.ItemCheck += (s, e) =>
			{
				if (!listBoxController.IsUpdating)
					viewModel.OnCheck((IDialogItem)listBox.Items[e.Index], e.NewValue == CheckState.Checked);
			};
			dialog.Update(viewModel.DialogData);
			viewModel.OnCloseDialog(dialog.ShowDialog() == DialogResult.OK);
		}

		public void Update(DialogViewData viewData)
		{
			this.Text = viewData.Title;
			listBoxController.Update(viewData.Items);
		}
	}
}

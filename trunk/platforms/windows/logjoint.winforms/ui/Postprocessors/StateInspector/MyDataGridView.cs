using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;

namespace System.Windows.Forms
{
	public class MyDataGridView : DataGridView
	{
		IViewModel viewModel;

		public void Init(IViewModel eventsHandler)
		{
			this.viewModel = eventsHandler;
		}

		public override DataObject GetClipboardContent()
		{
			if (SelectedRows.Count != 1)
				return null;
			var row = SelectedRows[0];
			if (row.Cells.Count < 2)
				return null;
			if (viewModel != null)
			{
				// hack: 
				// set clipboard manually and return null to suppress clipboard modification by DataGridView
				viewModel.OnPropertyCellCopyShortcutPressed();
				return null;
			}
			else
			{
				var ret = new DataObject();
				ret.SetData(row.Cells[1].Value.ToString());
				return ret;
			}
		}
	}
}

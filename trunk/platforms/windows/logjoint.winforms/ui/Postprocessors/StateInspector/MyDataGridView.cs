using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;

namespace System.Windows.Forms
{
	public class MyDataGridView : DataGridView
	{
		IViewEvents eventsHandler;

		public void Init(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		public override DataObject GetClipboardContent()
		{
			if (SelectedRows.Count != 1)
				return null;
			var row = SelectedRows[0];
			if (row.Cells.Count < 2)
				return null;
			if (eventsHandler != null)
			{
				// hack: 
				// set clipboard manually and return null to suppress clipboard modification by DataGridView
				eventsHandler.OnCopyShortcutPressed();
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

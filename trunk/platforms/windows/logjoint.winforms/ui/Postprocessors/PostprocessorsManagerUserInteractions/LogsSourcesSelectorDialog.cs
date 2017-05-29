using LogJoint.Postprocessing;
using System.Windows.Forms;

namespace LogJoint.UI.Postprocessing.PostprocessorsManagerUserInteractions
{
	public partial class LogsSourcesSelectorDialog : Form
	{
		public LogsSourcesSelectorDialog()
		{
			InitializeComponent();
		}

		public bool ShowDialog(LogsSourcesSelectorDialogParams dialogParams)
		{
			dataGridView1.DataSource = dialogParams.LogSources;
			if (ShowDialog() != DialogResult.OK)
				return false;
			return true;
		}

		private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Space)
			{
				var c = dataGridView1.CurrentCell;
				if (c != null && c.ColumnIndex == 1)
				{
					var cb = c.OwningRow.Cells[0] as DataGridViewCheckBoxCell;
					if (cb != null && cb.Value is bool)
					{
						cb.Value = !(bool)cb.Value;
						e.SuppressKeyPress = true;
					}
				}
			}
			else if (e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				DialogResult = DialogResult.OK;
			}
			else if (e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				DialogResult = DialogResult.Cancel;
			}
		}
	}
}

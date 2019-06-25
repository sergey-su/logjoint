using LogJoint.UI.Presenters.Postprocessing;
using System;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI.Postprocessing
{
	public partial class ToolForm : Form, IPostprocessorOutputForm
	{
		public ToolForm()
		{
			InitializeComponent();
		}

		void IPostprocessorOutputForm.Show()
		{
			Show();
		}

		public new void Show()
		{
			EnsureFormVisible(this);
			base.Show();
			BringToFront();
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Hide();
				return true;
			}
			return base.ProcessDialogKey(keyData);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				this.Visible = false;
			}
			else
			{
				base.OnFormClosing(e);
			}
		}


		private static void EnsureFormVisible(Form frm)
		{
			if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(frm.Bounds)))
			{
				var newPos = Screen.PrimaryScreen.WorkingArea.Location;
				newPos.Offset(100, 100);
				frm.Location = newPos;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class EVTFactoryUI : UserControl, ILogProviderUI
	{
		readonly WindowsEventLog.Factory factory;
		WindowsEventLog.EventLogIdentity currentIdentity;

		public EVTFactoryUI(WindowsEventLog.Factory factory)
		{
			InitializeComponent();

			this.factory = factory;

			var extFilter = new StringBuilder();
			string[] exts = new string[] { ".evtx", ".evt" };
			foreach (string ext in exts)
				extFilter.AppendFormat("*{0} Files|*{0}|", ext);
			extFilter.Append("All Files (*.*)|*.*");
			openFileDialog.Filter = extFilter.ToString();
		}

		Control ILogProviderUI.UIControl
		{
			get { return this; }
		}

		void ILogProviderUI.Apply(IModel model)
		{
			if (currentIdentity == null)
				return;
			IConnectionParams connectParams = factory.CreateParamsFromIdentity(currentIdentity);
			model.CreateLogSource(factory, connectParams);
			SetCurrentIdentity(null);
		}

		private void openButton1_Click(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				SetCurrentIdentity(WindowsEventLog.EventLogIdentity.FromFileName(openFileDialog.FileName));
			}
		}

		void SetCurrentIdentity(WindowsEventLog.EventLogIdentity id)
		{
			currentIdentity = id;
			logTextBox.Text = (id != null) ? id.ToUserFriendlyString() : "";
		}

		private void openButton2_Click(object sender, EventArgs e)
		{
			using (SelectLogSourceDialog dlg = new SelectLogSourceDialog())
			{
				var logIdentity = dlg.ShowDialog();
				if (logIdentity != null)
				{
					SetCurrentIdentity(logIdentity);
				}
			}
		}
	}
}

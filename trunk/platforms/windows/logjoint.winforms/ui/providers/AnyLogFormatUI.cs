using System;
using System.Windows.Forms;
using System.Linq;
using LogJoint.UI.Presenters.MainForm;

namespace LogJoint.UI
{
	public partial class AnyLogFormatUI : UserControl, ILogProviderUI
	{
		readonly ICommandLineHandler commandLineHandler;


		public AnyLogFormatUI(
			ICommandLineHandler commandLineHandler)
		{
			InitializeComponent();
			this.commandLineHandler = commandLineHandler;
		}
		
		
		private void browseButton_Click(object sender, EventArgs e)
		{
			browseFileDialog.Filter = "*.*|*.*";

			if (browseFileDialog.ShowDialog() == DialogResult.OK)
				filePathTextBox.Text = FileListUtils.MakeFileList(browseFileDialog.FileNames);
		}


		Control ILogProviderUI.UIControl
		{
			get { return this; }
		}

		void ILogProviderUI.Apply(IModel model)
		{
			string tmp = filePathTextBox.Text.Trim();
			if (tmp == "")
				return;
			filePathTextBox.Text = "";

			foreach (string fnameOrUrl in FileListUtils.ParseFileList(tmp))
			{
				commandLineHandler.HandleCommandLineArgs(new [] {fnameOrUrl});
			}
		}
	}
}

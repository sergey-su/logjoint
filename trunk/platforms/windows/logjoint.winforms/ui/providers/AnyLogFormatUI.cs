using System;
using System.Windows.Forms;
using System.Linq;
using LogJoint.UI.Presenters.MainForm;

namespace LogJoint
{
	public partial class AnyLogFormatUI : UserControl, ILogProviderFactoryUI
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

		#region ILogReaderFactoryUI Members

		public object UIControl
		{
			get { return this; }
		}

		public void Apply(IModel model)
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

		#endregion
	}
}

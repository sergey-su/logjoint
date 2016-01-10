using System;
using System.Windows.Forms;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FormatDetection
{
	public partial class AnyLogFormatUI : UserControl, IView
	{
		public AnyLogFormatUI()
		{
			InitializeComponent();
		}
		
		
		private void browseButton_Click(object sender, EventArgs e)
		{
			browseFileDialog.Filter = "*.*|*.*";

			if (browseFileDialog.ShowDialog() == DialogResult.OK)
				filePathTextBox.Text = FileListUtils.MakeFileList(browseFileDialog.FileNames);
		}


		object IView.PageView
		{
			get { return this; }
		}

		string IView.InputValue
		{
			get { return filePathTextBox.Text; }
			set { filePathTextBox.Text = value; }
		}
	}
}

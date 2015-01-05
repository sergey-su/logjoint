using System;
using System.Windows.Forms;
using System.Linq;

namespace LogJoint
{
	public partial class AnyLogFormatUI : UserControl, ILogProviderFactoryUI
	{
		readonly Preprocessing.ILogSourcesPreprocessingManager preprocessingManager;
		readonly Preprocessing.IPreprocessingUserRequests userRequests;


		public AnyLogFormatUI(
			Preprocessing.ILogSourcesPreprocessingManager preprocessingManager,
			Preprocessing.IPreprocessingUserRequests userRequests)
		{
			InitializeComponent();
			this.preprocessingManager = preprocessingManager;
			this.userRequests = userRequests;
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
				if (Uri.IsWellFormedUriString(fnameOrUrl, UriKind.Absolute))
				{
					preprocessingManager.Preprocess(
						Enumerable.Repeat(new Preprocessing.URLTypeDetectionStep(fnameOrUrl), 1),
						userRequests
					);
				}
				else
				{
					preprocessingManager.Preprocess(
						Enumerable.Repeat(new Preprocessing.FormatDetectionStep(fnameOrUrl), 1),
						userRequests
					);
				}
			}
		}

		#endregion
	}
}

using LogJoint.UI.Presenters;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class PromptDialog : Form
	{
		public PromptDialog()
		{
			InitializeComponent();
		}

		public class Presenter : IPromptDialog
		{
			string IPromptDialog.ExecuteDialog(string caption, string prompt, string defaultValue)
			{
				return PromptDialog.Execute(caption, prompt, defaultValue);
			}

			public Task<string> ExecuteDialogAsync(string caption, string prompt, string defaultValue)
			{
				return Task.FromResult(Execute(caption, prompt, defaultValue));
			}
		};

		public static string Execute(string caption, string prompt, string defaultValue)
		{
			using (var dlg = new PromptDialog())
			{
				dlg.textBox1.Text = defaultValue;
				dlg.Text = caption;
				dlg.label1.Text = prompt;
				if (dlg.ShowDialog() != DialogResult.OK)
					return null;
				return dlg.textBox1.Text;
			}
		}
	}
}

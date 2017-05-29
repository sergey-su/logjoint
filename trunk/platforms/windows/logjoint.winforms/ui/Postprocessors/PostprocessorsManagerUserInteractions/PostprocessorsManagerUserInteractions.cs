using LogJoint.Postprocessing;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Postprocessing.PostprocessorsManagerUserInteractions
{
	public class PostprocessorsManagerUserInteractions : IPostprocessorsManagerUserInteractions
	{
		readonly Extensibility.IApplication app;
		readonly IInvokeSynchronization uiInvokeSynchronization;

		public PostprocessorsManagerUserInteractions(Extensibility.IApplication app, IInvokeSynchronization uiInvokeSynchronization)
		{
			this.app = app;
			this.uiInvokeSynchronization = uiInvokeSynchronization;
		}

		async Task<bool> IPostprocessorsManagerUserInteractions.ShowLogsSourcesSelectorDialog(LogsSourcesSelectorDialogParams p, CancellationToken cancellationToken)
		{
			return await uiInvokeSynchronization.Invoke(() =>
			{
				using (var dialog = new LogsSourcesSelectorDialog())
				using (var cancellationRegistration = cancellationToken.Register(dialog.Hide))
				{
					app.View.RegisterToolForm(dialog);
					return dialog.ShowDialog(p);
				}
			});
		}
	}
}

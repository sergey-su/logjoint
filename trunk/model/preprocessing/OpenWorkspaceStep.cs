using LogJoint.Workspaces;
using System;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	class OpenWorkspaceStep : IPreprocessingStep
	{
		readonly IWorkspacesManager workspacesManager;
		readonly PreprocessingStepParams source;
		readonly ISynchronizationContext invoke;

		public OpenWorkspaceStep(PreprocessingStepParams p, IWorkspacesManager workspacesManager, ISynchronizationContext invoke)
		{
			this.workspacesManager = workspacesManager;
			this.source = p;
			this.invoke = invoke;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await callback.BecomeLongRunning();
			callback.SetStepDescription("Opening workspace " + source.FullPath);
			callback.SetOption(PreprocessingOptions.SkipLogsSelectionDialog, true);

			foreach (var entry in await await invoke.Invoke(() => workspacesManager.LoadWorkspace(source.Uri, callback.Cancellation)).WithCancellation(callback.Cancellation))
				callback.YieldChildPreprocessing(entry.Log, entry.IsHiddenLog);
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			throw new NotImplementedException();
		}
	}
}

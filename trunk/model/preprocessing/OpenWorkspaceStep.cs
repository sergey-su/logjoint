using LogJoint.Workspaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	class OpenWorkspaceStep : IPreprocessingStep
	{
		readonly IWorkspacesManager workspacesManager;
		readonly PreprocessingStepParams source;
		readonly IInvokeSynchronization invoke;

		public OpenWorkspaceStep(PreprocessingStepParams p, IWorkspacesManager workspacesManager, IInvokeSynchronization invoke)
		{
			this.workspacesManager = workspacesManager;
			this.source = p;
			this.invoke = invoke;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			callback.SetStepDescription("Opening workspace " + source.FullPath);

			foreach (var entry in await await invoke.Invoke(() => workspacesManager.LoadWorkspace(source.Uri, callback.Cancellation), callback.Cancellation))
				callback.YieldChildPreprocessing(entry.Log, entry.IsHiddenLog);
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			throw new NotImplementedException();
		}
	}
}

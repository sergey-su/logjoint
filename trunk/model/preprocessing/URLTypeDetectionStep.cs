using System;
using System.Collections.Generic;

namespace LogJoint.Preprocessing
{
	public class URLTypeDetectionStep : IPreprocessingStep
	{
		internal URLTypeDetectionStep(
			PreprocessingStepParams srcFile,
			IPreprocessingStepsFactory preprocessingStepsFactory,
			Workspaces.IWorkspacesManager workspacesManager,
			AppLaunch.IAppLaunch appLaunch)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.workspacesManager = workspacesManager;
			this.appLaunch = appLaunch;
		}

		IEnumerable<IPreprocessingStep> IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			if (Uri.IsWellFormedUriString(sourceFile.Uri, UriKind.Absolute))
			{
				var uri = new Uri(sourceFile.Uri);
				string localFilePath;
				AppLaunch.LaunchUriData launchUriData;

				if ((localFilePath = TryDetectLocalFileUri(uri)) != null)
				{
					yield return preprocessingStepsFactory.CreateFormatDetectionStep(
						new PreprocessingStepParams(localFilePath, localFilePath, sourceFile.PreprocessingSteps));
				}
				else if (workspacesManager.IsWorkspaceUri(uri))
				{
					yield return preprocessingStepsFactory.CreateOpenWorkspaceStep(sourceFile);
				}
				else if (appLaunch.TryParseLaunchUri(uri, out launchUriData))
				{
					if (launchUriData.SingleLogUri != null)
						yield return preprocessingStepsFactory.CreateURLTypeDetectionStep(new PreprocessingStepParams(launchUriData.SingleLogUri));
					else if (launchUriData.WorkspaceUri != null)
						yield return preprocessingStepsFactory.CreateOpenWorkspaceStep(new PreprocessingStepParams(launchUriData.WorkspaceUri));
				}
				else
				{
					yield return preprocessingStepsFactory.CreateDownloadingStep(sourceFile);
				}
			}
		}

		PreprocessingStepParams IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			throw new InvalidOperationException();
		}

		static string TryDetectLocalFileUri(Uri uri)
		{
			if (String.Compare(uri.Scheme, "file", ignoreCase: true) != 0)
				return null;
			return uri.LocalPath;
		}

		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly Workspaces.IWorkspacesManager workspacesManager;
		readonly AppLaunch.IAppLaunch appLaunch;
	};
}

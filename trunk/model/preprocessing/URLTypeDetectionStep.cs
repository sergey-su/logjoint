using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class URLTypeDetectionStep : IPreprocessingStep
	{
		internal URLTypeDetectionStep(
			PreprocessingStepParams srcFile,
			IStepsFactory preprocessingStepsFactory,
			Workspaces.IWorkspacesManager workspacesManager,
			AppLaunch.ILaunchUrlParser appLaunch,
			IExtensionsRegistry extensions
		)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.workspacesManager = workspacesManager;
			this.appLaunch = appLaunch;
			this.extensions = extensions;
		}

		Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			if (Uri.IsWellFormedUriString(sourceFile.Location, UriKind.Absolute))
			{
				var uri = new Uri(sourceFile.Location);
				string localFilePath;
				AppLaunch.LaunchUriData launchUriData;
				IPreprocessingStep extensionStep;

				if ((localFilePath = TryDetectLocalFileUri(uri)) != null)
				{
					callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(
						new PreprocessingStepParams(localFilePath, localFilePath, sourceFile.PreprocessingHistory, sourceFile.DisplayName)));
				}
				else if (workspacesManager.IsWorkspaceUri(uri))
				{
					callback.YieldNextStep(preprocessingStepsFactory.CreateOpenWorkspaceStep(sourceFile));
				}
				else if (appLaunch.TryParseLaunchUri(uri, out launchUriData))
				{
					if (launchUriData.SingleLogUri != null)
						callback.YieldNextStep(preprocessingStepsFactory.CreateURLTypeDetectionStep(new PreprocessingStepParams(launchUriData.SingleLogUri)));
					else if (launchUriData.WorkspaceUri != null)
						callback.YieldNextStep(preprocessingStepsFactory.CreateOpenWorkspaceStep(new PreprocessingStepParams(launchUriData.WorkspaceUri)));
				}
				else if (TryExtensions(uri, out extensionStep))
				{
					callback.YieldNextStep(extensionStep);
				}
				else
				{
					callback.YieldNextStep(preprocessingStepsFactory.CreateDownloadingStep(sourceFile));
				}
			}
			return Task.FromResult(0);
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			throw new InvalidOperationException();
		}

		static string TryDetectLocalFileUri(Uri uri)
		{
			if (String.Compare(uri.Scheme, "file", ignoreCase: true) != 0)
				return null;
			return uri.LocalPath;
		}

		bool TryExtensions(Uri uri, out IPreprocessingStep extensionStep)
		{
			foreach (var ext in extensions.Items)
			{
				var step = ext.TryParseLaunchUri(uri);
				if (step != null)
				{
					extensionStep = step;
					return true;
				}
			}
			extensionStep = null;
			return false;
		}

		readonly PreprocessingStepParams sourceFile;
		readonly IStepsFactory preprocessingStepsFactory;
		readonly Workspaces.IWorkspacesManager workspacesManager;
		readonly AppLaunch.ILaunchUrlParser appLaunch;
		readonly IExtensionsRegistry extensions;
	};
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class PreprocessingStepsFactory : IPreprocessingStepsFactory
	{
		readonly Workspaces.IWorkspacesManager workspacesManager;
		readonly AppLaunch.IAppLaunch appLaunch;

		public PreprocessingStepsFactory(Workspaces.IWorkspacesManager workspacesManager, AppLaunch.IAppLaunch appLaunch)
		{
			this.workspacesManager = workspacesManager;
			this.appLaunch = appLaunch;
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateFormatDetectionStep(PreprocessingStepParams p)
		{
			return new FormatDetectionStep(p, this);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateDownloadingStep(PreprocessingStepParams p)
		{
			return new DownloadingStep(p, this);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateUnpackingStep(PreprocessingStepParams p)
		{
			return new UnpackingStep(p, this);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateURLTypeDetectionStep(PreprocessingStepParams p)
		{
			return new URLTypeDetectionStep(p, this, workspacesManager, appLaunch);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateOpenWorkspaceStep(PreprocessingStepParams p)
		{
			return new OpenWorkspaceStep(p);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateLocationTypeDetectionStep(PreprocessingStepParams p)
		{
			return new LocationTypeDetectionStep(p, this);
		}
	}
}

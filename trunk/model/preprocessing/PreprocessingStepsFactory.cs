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
		readonly AppLaunch.ILaunchUrlParser appLaunch;
		readonly IInvokeSynchronization invoke;
		readonly IPreprocessingManagerExtensionsRegistry extentions;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly Persistence.IWebContentCache cache;
		readonly ICredentialsCache credCache;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;

		public PreprocessingStepsFactory(
			Workspaces.IWorkspacesManager workspacesManager, 
			AppLaunch.ILaunchUrlParser appLaunch,
			IInvokeSynchronization invoke,
			IPreprocessingManagerExtensionsRegistry extentions,
			Progress.IProgressAggregator progressAggregator,
			Persistence.IWebContentCache cache,
			ICredentialsCache credCache,
			ILogProviderFactoryRegistry logProviderFactoryRegistry)
		{
			this.workspacesManager = workspacesManager;
			this.appLaunch = appLaunch;
			this.invoke = invoke;
			this.extentions = extentions;
			this.progressAggregator = progressAggregator;
			this.cache = cache;
			this.credCache = credCache;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateFormatDetectionStep(PreprocessingStepParams p)
		{
			return new FormatDetectionStep(p, extentions, this);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateDownloadingStep(PreprocessingStepParams p)
		{
			return new DownloadingStep(p, progressAggregator, cache, this, credCache);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateUnpackingStep(PreprocessingStepParams p)
		{
			return new UnpackingStep(p, progressAggregator, this);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateURLTypeDetectionStep(PreprocessingStepParams p)
		{
			return new URLTypeDetectionStep(p, this, workspacesManager, appLaunch, extentions);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateOpenWorkspaceStep(PreprocessingStepParams p)
		{
			return new OpenWorkspaceStep(p, workspacesManager, invoke);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateLocationTypeDetectionStep(PreprocessingStepParams p)
		{
			return new LocationTypeDetectionStep(p, this);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateGunzippingStep(PreprocessingStepParams p)
		{
			return new GunzippingStep(p, progressAggregator, this);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateTimeAnomalyFixingStep(PreprocessingStepParams p)
		{
			return new TimeAnomalyFixingStep(p, progressAggregator, logProviderFactoryRegistry, this);
		}
	}
}

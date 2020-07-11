using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class PreprocessingStepsFactory : IStepsFactory
	{
		readonly Workspaces.IWorkspacesManager workspacesManager;
		readonly AppLaunch.ILaunchUrlParser appLaunch;
		readonly ISynchronizationContext invoke;
		readonly IExtensionsRegistry extentions;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly Persistence.IWebContentCache cache;
		readonly ICredentialsCache credCache;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
		readonly WebViewTools.IWebViewTools webViewTools;
		readonly ILogsDownloaderConfig logsDownloaderConfig;
		readonly RegularExpressions.IRegexFactory regexFactory;
		readonly LogMedia.IFileSystem fileSystem;

		public PreprocessingStepsFactory(
			Workspaces.IWorkspacesManager workspacesManager, 
			AppLaunch.ILaunchUrlParser appLaunch,
			ISynchronizationContext invoke,
			IExtensionsRegistry extentions,
			Progress.IProgressAggregator progressAggregator,
			Persistence.IWebContentCache cache,
			ICredentialsCache credCache,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			WebViewTools.IWebViewTools webBrowserDownloader,
			ILogsDownloaderConfig logsDownloaderConfig,
			RegularExpressions.IRegexFactory regexFactory,
			LogMedia.IFileSystem fileSystem
		)
		{
			this.workspacesManager = workspacesManager;
			this.appLaunch = appLaunch;
			this.invoke = invoke;
			this.extentions = extentions;
			this.progressAggregator = progressAggregator;
			this.cache = cache;
			this.credCache = credCache;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
			this.webViewTools = webBrowserDownloader;
			this.logsDownloaderConfig = logsDownloaderConfig;
			this.regexFactory = regexFactory;
			this.fileSystem = fileSystem;
		}

		IPreprocessingStep IStepsFactory.CreateFormatDetectionStep(PreprocessingStepParams p)
		{
			return new FormatDetectionStep(p, extentions, this, fileSystem);
		}

		IPreprocessingStep IStepsFactory.CreateDownloadingStep(PreprocessingStepParams p)
		{
			return new DownloadingStep(p, progressAggregator, cache, credCache, webViewTools, logsDownloaderConfig, this);
		}

		IPreprocessingStep IStepsFactory.CreateUnpackingStep(PreprocessingStepParams p)
		{
			return new UnpackingStep(p, progressAggregator, credCache, this);
		}

		IPreprocessingStep IStepsFactory.CreateURLTypeDetectionStep(PreprocessingStepParams p)
		{
			return new URLTypeDetectionStep(p, this, workspacesManager, appLaunch, extentions);
		}

		IPreprocessingStep IStepsFactory.CreateOpenWorkspaceStep(PreprocessingStepParams p)
		{
			return new OpenWorkspaceStep(p, workspacesManager, invoke);
		}

		IPreprocessingStep IStepsFactory.CreateLocationTypeDetectionStep(PreprocessingStepParams p)
		{
			return new LocationTypeDetectionStep(p, this);
		}

		IPreprocessingStep IStepsFactory.CreateGunzippingStep(PreprocessingStepParams p)
		{
			return new GunzippingStep(p, progressAggregator, this);
		}

		IPreprocessingStep IStepsFactory.CreateTimeAnomalyFixingStep(PreprocessingStepParams p)
		{
			return new TimeAnomalyFixingStep(p, progressAggregator, logProviderFactoryRegistry, this, regexFactory, fileSystem);
		}

		IPreprocessingStep IStepsFactory.CreateUntarStep(PreprocessingStepParams p)
		{
			return new UntarStep(p, progressAggregator, this);
		}
	}
}

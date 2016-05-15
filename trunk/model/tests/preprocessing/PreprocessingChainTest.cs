using LogJoint.AppLaunch;
using LogJoint.Preprocessing;
using LogJoint.Workspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace LogJointTests
{
	[TestClass()]
	public class PreprocessingChainTest
	{
		IPreprocessingStepsFactory preprocessingStepsFactory;
		IPreprocessingStepCallback callback;
		IWorkspacesManager workspacesManager;
		ILaunchUrlParser appLaunch;
		IPreprocessingManagerExtensionsRegistry extensions;

		void RunChain(params IPreprocessingStep[] initialSteps)
		{
			var steps = new Queue<IPreprocessingStep>(initialSteps);
			callback.When(x => x.YieldNextStep(Arg.Any<IPreprocessingStep>())).Do(
				callInfo => steps.Enqueue(callInfo.Arg<IPreprocessingStep>()));
			while (steps.Count > 0)
				steps.Dequeue().Execute(callback).Wait();
		}

		[TestInitialize]
		public void Setup()
		{
			workspacesManager = Substitute.For<IWorkspacesManager>();
			appLaunch = Substitute.For<ILaunchUrlParser>();
			preprocessingStepsFactory = Substitute.For<IPreprocessingStepsFactory>();
			extensions = Substitute.For<IPreprocessingManagerExtensionsRegistry>();
			preprocessingStepsFactory.CreateURLTypeDetectionStep(null).ReturnsForAnyArgs(
				callInfo => new URLTypeDetectionStep(
					callInfo.Arg<PreprocessingStepParams>(), preprocessingStepsFactory, workspacesManager, appLaunch, extensions));
			callback = Substitute.For<IPreprocessingStepCallback>();
		}

		[TestMethod()]
		public void LocationTypeDetectionStepDetectsLocalFile()
		{
			RunChain(new LocationTypeDetectionStep(
				new PreprocessingStepParams(@"c:\foo.bar"), preprocessingStepsFactory));

			preprocessingStepsFactory.Received().CreateFormatDetectionStep(
				Arg.Is<PreprocessingStepParams>(p => p.Uri == @"c:\foo.bar"));
		}


		[TestMethod()]
		public void LocationTypeDetectionStepDetectsLogUri()
		{
			RunChain(new LocationTypeDetectionStep(
				new PreprocessingStepParams(@"https://foo.bar/123"), preprocessingStepsFactory));

			preprocessingStepsFactory.Received().CreateDownloadingStep(
				Arg.Is<PreprocessingStepParams>(p => p.Uri == @"https://foo.bar/123"));
		}

		[TestMethod()]
		public void LocationTypeDetectionStepDetectsWorkspaceUri()
		{
			workspacesManager.IsWorkspaceUri(new Uri(@"https://workspaces/123")).Returns(true);

			RunChain(new LocationTypeDetectionStep(
				new PreprocessingStepParams(@"https://workspaces/123"), preprocessingStepsFactory));

			preprocessingStepsFactory.Received().CreateOpenWorkspaceStep(
				Arg.Is<PreprocessingStepParams>(p => p.Uri == @"https://workspaces/123"));
		}

		[TestMethod()]
		public void LocationTypeDetectionStepDetectsLocalFileUri()
		{
			RunChain(new LocationTypeDetectionStep(
				new PreprocessingStepParams(@"file:///M:/foo.log"), preprocessingStepsFactory));

			preprocessingStepsFactory.Received().CreateFormatDetectionStep(
				Arg.Is<PreprocessingStepParams>(p => p.Uri == @"M:\foo.log"));
		}

		[TestMethod()]
		public void LocationTypeDetectionStepDetectsWorkspaceLaunchUri()
		{
			string testUri = @"logjoint:?t=workspace&uri=https://workspaces/123";

			LaunchUriData tmp;
			appLaunch.TryParseLaunchUri(new Uri(testUri), out tmp).Returns(callInfo =>
			{
				callInfo[1] = new LaunchUriData() { WorkspaceUri = "https://workspaces/123" };
				return true;
			});

			RunChain(new LocationTypeDetectionStep(
				new PreprocessingStepParams(testUri), preprocessingStepsFactory));

			preprocessingStepsFactory.Received().CreateOpenWorkspaceStep(
				Arg.Is<PreprocessingStepParams>(p => p.Uri == @"https://workspaces/123"));
		}

		[TestMethod()]
		public void LocationTypeDetectionStepDetectsSingleLogLaunchUri()
		{
			string testUri = @"logjoint:?t=log&uri=https://logz/123";

			LaunchUriData tmp;
			appLaunch.TryParseLaunchUri(new Uri(testUri), out tmp).Returns(callInfo =>
			{
				callInfo[1] = new LaunchUriData() { SingleLogUri = "https://logz/123" };
				return true;
			});

			RunChain(new LocationTypeDetectionStep(
				new PreprocessingStepParams(testUri), preprocessingStepsFactory));

			preprocessingStepsFactory.Received().CreateDownloadingStep(
				Arg.Is<PreprocessingStepParams>(p => p.Uri == @"https://logz/123"));
		}
	}
}

using LogJoint.AppLaunch;
using LogJoint.Preprocessing;
using NSubstitute;
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class PreprocessingChainTest
    {
        IStepsFactory preprocessingStepsFactory;
        IPreprocessingStepCallback callback;
        ILaunchUrlParser appLaunch;
        IExtensionsRegistry extensions;

        void RunChain(params IPreprocessingStep[] initialSteps)
        {
            var steps = new Queue<IPreprocessingStep>(initialSteps);
            callback.When(x => x.YieldNextStep(Arg.Any<IPreprocessingStep>())).Do(
                callInfo => steps.Enqueue(callInfo.Arg<IPreprocessingStep>()));
            while (steps.Count > 0)
                steps.Dequeue().Execute(callback)?.Wait();
        }

        [SetUp]
        public void Setup()
        {
            appLaunch = Substitute.For<ILaunchUrlParser>();
            preprocessingStepsFactory = Substitute.For<IStepsFactory>();
            extensions = Substitute.For<IExtensionsRegistry>();
            preprocessingStepsFactory.CreateURLTypeDetectionStep(null).ReturnsForAnyArgs(
                callInfo => new URLTypeDetectionStep(
                    callInfo.Arg<PreprocessingStepParams>(), preprocessingStepsFactory, appLaunch, extensions));
            callback = Substitute.For<IPreprocessingStepCallback>();
        }

        [Test]
        public void LocationTypeDetectionStepDetectsLocalFile()
        {
            RunChain(new LocationTypeDetectionStep(
                new PreprocessingStepParams(@"c:\foo.bar"), preprocessingStepsFactory));

            preprocessingStepsFactory.Received().CreateFormatDetectionStep(
                Arg.Is<PreprocessingStepParams>(p => p.Location == @"c:\foo.bar"));
        }


        [Test]
        public void LocationTypeDetectionStepDetectsLogUri()
        {
            RunChain(new LocationTypeDetectionStep(
                new PreprocessingStepParams(@"https://foo.bar/123"), preprocessingStepsFactory));

            preprocessingStepsFactory.Received().CreateDownloadingStep(
                Arg.Is<PreprocessingStepParams>(p => p.Location == @"https://foo.bar/123"));
        }

        [Test]
        public void LocationTypeDetectionStepDetectsLocalFileUri()
        {
            RunChain(new LocationTypeDetectionStep(
                new PreprocessingStepParams(@"file:///M:/foo.log"), preprocessingStepsFactory));

            preprocessingStepsFactory.Received().CreateFormatDetectionStep(
                Arg.Is<PreprocessingStepParams>(p => p.Location == @"M:\foo.log"));
        }

        [Test]
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
                Arg.Is<PreprocessingStepParams>(p => p.Location == @"https://logz/123"));
        }
    }
}

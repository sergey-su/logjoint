using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlation;
using System.Xml;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogJoint.LogMedia;

namespace LogJoint.Tests.Postprocessing.PostprocessorsManager
{
    [TestFixture]
    public class PostprocessorsManagerTests
    {
        IManagerInternal manager;

        ILogSourcesManager logSources;
        Telemetry.ITelemetryCollector telemetry;
        ManualSynchronizationContext mockedSyncContext;
        IHeartBeatTimer heartbeat;
        Progress.IProgressAggregator progressAggregator;
        Settings.IGlobalSettingsAccessor settingsAccessor;

        ILogProviderFactory logProviderFac1;
        ILogSource logSource1;
        ILogSourcePostprocessor logSourcePP1;
        Persistence.ISaxXMLStorageSection pp1outputXmlSection;
        IPostprocessorOutputETag pp1PostprocessorOutput;
        IPostprocessorRunSummary pp1RunSummary;
        IOutputDataDeserializer outputDataDeserializer;
        ILogPartTokenFactories logPartTokenFactories;
        ISameNodeDetectionTokenFactories sameNodeDetectionTokenFactories;
        IChangeNotification changeNotification;
        IFileSystem fileSystem;


        [SetUp]
        public void BeforeEach()
        {
            logSources = Substitute.For<ILogSourcesManager>();
            telemetry = Substitute.For<Telemetry.ITelemetryCollector>();
            mockedSyncContext = new ManualSynchronizationContext();
            heartbeat = Substitute.For<IHeartBeatTimer>();
            progressAggregator = Substitute.For<Progress.IProgressAggregator>();
            settingsAccessor = Substitute.For<Settings.IGlobalSettingsAccessor>();
            logSource1 = Substitute.For<ILogSource>();
            logProviderFac1 = Substitute.For<ILogProviderFactory>();
            logSource1.Provider.Factory.Returns(logProviderFac1);
            logSource1.Provider.ConnectionParams.Returns(new ConnectionParams($"{ConnectionParamsKeys.PathConnectionParam}=/log.txt"));
            logSource1.Provider.Stats.Returns(new LogProviderStats()
            {
                ContentsEtag = null
            });
            logSourcePP1 = Substitute.For<ILogSourcePostprocessor>();
            logSourcePP1.Kind.Returns(PostprocessorKind.SequenceDiagram);
            pp1outputXmlSection = Substitute.For<Persistence.ISaxXMLStorageSection>();
            logSource1.LogSourceSpecificStorageEntry.OpenSaxXMLSection("postproc-sequencediagram.xml", Persistence.StorageSectionOpenFlag.ReadOnly).Returns(
                Task.FromResult(pp1outputXmlSection));
            pp1outputXmlSection.Reader.Returns(Substitute.For<XmlReader>());
            pp1PostprocessorOutput = Substitute.For<IPostprocessorOutputETag>();
            outputDataDeserializer = Substitute.For<IOutputDataDeserializer>();
            outputDataDeserializer.Deserialize(PostprocessorKind.SequenceDiagram, Arg.Any<LogSourcePostprocessorDeserializationParams>()).Returns(pp1PostprocessorOutput);
            pp1RunSummary = Substitute.For<IPostprocessorRunSummary>();
            logSourcePP1.Run(null).ReturnsForAnyArgs(Task.FromResult(pp1RunSummary));
            pp1RunSummary.GetLogSpecificSummary(null).ReturnsForAnyArgs((IPostprocessorRunSummary)null);
            logPartTokenFactories = Substitute.For<ILogPartTokenFactories>();
            sameNodeDetectionTokenFactories = Substitute.For<ISameNodeDetectionTokenFactories>();
            changeNotification = Substitute.For<IChangeNotification>();
            fileSystem = Substitute.For<IFileSystem>();

            manager = new LogJoint.Postprocessing.PostprocessorsManager(
                logSources, telemetry, mockedSyncContext, mockedSyncContext, heartbeat, progressAggregator, settingsAccessor, outputDataDeserializer, new TraceSourceFactory(),
                logPartTokenFactories, sameNodeDetectionTokenFactories, changeNotification, fileSystem);

            manager.Register(new LogSourceMetadata(logProviderFac1, logSourcePP1));
        }

        private void EmitTimerUpdate()
        {
            heartbeat.OnTimer += Raise.EventWith(heartbeat, new HeartBeatEventArgs(HeartBeatEventType.NormalUpdate));
        }

        private void EmitLogSource1Addition()
        {
            logSources.Items.Returns(new[] { logSource1 });
            logSources.OnLogSourceAdded += Raise.EventWith(logSources, EventArgs.Empty);
            EmitTimerUpdate();
        }

        [Test]
        public void CanRunPostprocessingIfItWasNotRunBefore()
        {
            pp1outputXmlSection.Reader.Returns((XmlReader)null);
            EmitLogSource1Addition();
            mockedSyncContext.Deplete();

            var exposedOutput = manager.LogSourcePostprocessors.Single();
            Assert.That(exposedOutput.OutputData, Is.Null);
            Assert.That(LogSourcePostprocessorState.Status.NeverRun, Is.EqualTo(exposedOutput.OutputStatus));
            Assert.That(exposedOutput.LastRunSummary, Is.Null);
            Assert.That(new double?(), Is.EqualTo(exposedOutput.Progress));
            Assert.That(logSource1, Is.EqualTo(exposedOutput.LogSource));
            Assert.That(logSourcePP1, Is.EqualTo(exposedOutput.Postprocessor));


            var pp1runResult = new TaskCompletionSource<IPostprocessorRunSummary>();
            logSourcePP1.Run(null).ReturnsForAnyArgs(pp1runResult.Task);

            Task runTask = manager.RunPostprocessors(
                new[] { exposedOutput }, null);
            mockedSyncContext.Deplete();
            Assert.That(runTask.IsCompleted, Is.False);

            exposedOutput = manager.LogSourcePostprocessors.Single();
            Assert.That(null, Is.EqualTo(exposedOutput.OutputData));
            Assert.That(LogSourcePostprocessorState.Status.InProgress, Is.EqualTo(exposedOutput.OutputStatus));
            Assert.That(null, Is.EqualTo(exposedOutput.LastRunSummary));
            Assert.That(new double?(), Is.EqualTo(exposedOutput.Progress));
            Assert.That(logSource1, Is.EqualTo(exposedOutput.LogSource));
            Assert.That(logSourcePP1, Is.EqualTo(exposedOutput.Postprocessor));


            pp1outputXmlSection.Reader.Returns(Substitute.For<XmlReader>());
            pp1runResult.SetResult(pp1RunSummary);
            mockedSyncContext.Deplete();
            Assert.That(runTask.IsCompleted, Is.True);

            exposedOutput = manager.LogSourcePostprocessors.Single();
            Assert.That(LogSourcePostprocessorState.Status.Finished, Is.EqualTo(exposedOutput.OutputStatus));
            Assert.That(pp1PostprocessorOutput, Is.EqualTo(exposedOutput.OutputData));
            Assert.That(pp1RunSummary, Is.EqualTo(exposedOutput.LastRunSummary));
            Assert.That(new double?(), Is.EqualTo(exposedOutput.Progress));
            Assert.That(logSource1, Is.EqualTo(exposedOutput.LogSource));
            Assert.That(logSourcePP1, Is.EqualTo(exposedOutput.Postprocessor));
        }

        [Test]
        public void InitialChangeOfETagShouldNotTriggerPostprocessorOutputReload()
        {
            EmitLogSource1Addition();
            mockedSyncContext.Deplete();

            logSource1.Provider.Stats.Returns(new LogProviderStats() { ContentsEtag = 123 });
            logSources.OnLogSourceStatsChanged += Raise.EventWith(logSource1,
                new LogSourceStatsEventArgs(logSource1.Provider.Stats, null, LogProviderStatsFlag.ContentsEtag));
            mockedSyncContext.Deplete();

            outputDataDeserializer.Received(1).Deserialize(PostprocessorKind.SequenceDiagram, Arg.Any<LogSourcePostprocessorDeserializationParams>());
        }
    }
}

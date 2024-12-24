using System;
using System.Text;
using NSubstitute;
using LogJoint.Telemetry;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;

namespace LogJoint.Tests
{
	[TestFixture]
	public class TelemetryTest
	{
		ITelemetryCollector collector;

		Persistence.IStorageManager storage;
		ITelemetryUploader uploader;
		MultiInstance.IInstancesCounter instancesCounter;
		IShutdown shutdown;
		IMemBufferTraceAccess traceAccess;

		[SetUp]
		public void BeforeEach()
		{
			storage = Substitute.For<Persistence.IStorageManager>();
			uploader = Substitute.For<ITelemetryUploader>();
			instancesCounter = Substitute.For<MultiInstance.IInstancesCounter>();
			shutdown = Substitute.For<IShutdown>();
			traceAccess = Substitute.For<IMemBufferTraceAccess>();

			collector = new TelemetryCollector(
				storage, uploader,
				new SerialSynchronizationContext(),
				instancesCounter, shutdown,
				traceAccess,
				new TraceSourceFactory()
			);

			uploader.IsIssuesReportingConfigured.Returns(true);
		}

		[Test]
		public async Task CanReportIssue()
		{
			traceAccess.When(x => x.ClearMemBufferAndGetCurrentContents(Arg.Any<TextWriter>())).Do(call =>
			{
				var w = call.Arg<TextWriter>();
				w.Write("line 1\n");
				w.Write("line 2\n");
			});
			using (var uploadedStreamContents = new MemoryStream())
			{
				uploader.UploadIssueReport(Arg.Do<Stream>(s =>
				{
					IOUtils.CopyStreamWithProgress(s, uploadedStreamContents, _ => { }, CancellationToken.None);
				}), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Task.FromResult("https://blobs/123"));

				await collector.ReportIssue("something went wrong for me");

				uploadedStreamContents.Position = 0;
				using (var zip = new ZipFile(uploadedStreamContents))
				{
					Assert.That(30, Is.EqualTo(zip.GetEntry("description.txt").Size));
					Assert.That(17, Is.EqualTo(zip.GetEntry("membuffer.log").Size));
				}
			}
		}
	}
}

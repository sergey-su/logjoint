using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Preprocessing;

namespace LogJoint.Tests.Integration
{
	[TestFixture]
	class LogsHistoryTests
	{
		readonly SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;

		[SetUp]
		public async Task BeforeEach()
		{
			app = await TestAppInstance.Create();
		}

		[TearDown]
		public async Task AfterEach()
		{
			await app.Dispose();
		}

		[Test]
		public async Task WhenLogIsOpenALogHistoryEntryIsAdded()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				Assert.AreEqual(0, app.Model.RecentlyUsedLogs.GetMRUListSize());

				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));

				Assert.AreEqual(1, app.Model.RecentlyUsedLogs.GetMRUListSize());

				await app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListener1.xml").ToString());
				Assert.AreEqual(2, app.Model.RecentlyUsedLogs.GetMRUListSize());
			});
		}

		// todo: have UI-driven tests

		[Test]
		public async Task RunPluginTests() // todo: remove
		{
			await PluginTestRunner.Run(@"C:\Users\sergeysu\logjoint\trunk\extensions\chromium\plugin\bin\Debug\netstandard2.0");
		}
	}
}

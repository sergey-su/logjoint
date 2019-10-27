using System.Threading.Tasks;
using NFluent;

namespace LogJoint.Tests.Integration
{
	[IntegrationTestFixture]
	class LogsHistoryTests
	{
		[IntegrationTest]
		public async Task WhenLogIsOpenALogHistoryEntryIsAdded(TestAppInstance app)
		{
			Check.That(app.Model.RecentlyUsedLogs.GetMRUListSize()).IsEqualTo(0);

			await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));

			Check.That(app.Model.RecentlyUsedLogs.GetMRUListSize()).IsEqualTo(1);

			await app.EmulateUrlDragAndDrop(app.Samples.GetSampleAsUri("XmlWriterTraceListener1.xml").ToString());
			Check.That(app.Model.RecentlyUsedLogs.GetMRUListSize()).IsEqualTo(2);
		}

		// todo: have UI-driven tests
	}
}

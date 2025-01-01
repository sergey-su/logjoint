using System.Threading.Tasks;
using NFluent;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class LogsHistoryTests
    {
        [IntegrationTest]
        public static async Task WhenLogIsOpenALogHistoryEntryIsAdded(TestAppInstance app)
        {
            Check.That(app.ModelObjects.RecentlyUsedLogs.MRUList.Count).IsEqualTo(0);

            await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));

            Check.That(app.ModelObjects.RecentlyUsedLogs.MRUList.Count).IsEqualTo(1);

            await app.EmulateUrlDragAndDrop(app.Samples.GetSampleAsUri("XmlWriterTraceListener1.xml"));
            Check.That(app.ModelObjects.RecentlyUsedLogs.MRUList.Count).IsEqualTo(2);
        }

        // todo: have UI-driven tests
    }
}

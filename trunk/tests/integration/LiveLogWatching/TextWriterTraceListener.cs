using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class TextWriterTraceListener
    {
        static void Log(StreamWriter logWriter, string line)
        {
            logWriter.WriteLine(
            $@"SampleApp Start: 0 : {line}
  ProcessId=69745
  ThreadId=1
  DateTime=2019-07-30T10:47:37.9252650Z
"
            );
            logWriter.Flush();
        }

        [IntegrationTest]
        public static async Task LiveLogIsFollowed(TestAppInstance app)
        {
            var testLog = app.ModelObjects.TempFilesManager.GenerateNewName();

            using (var logWriter = new StreamWriter(testLog, append: false))
            {
                Log(logWriter, "initial line");

                await app.EmulateFileDragAndDrop(testLog);

                await app.WaitForLogDisplayed("initial line");

                for (int i = 0; i < 10; ++i)
                {
                    Log(logWriter, $"test {i}");

                    var expectedLog = string.Join("\n",
                        new[] { "initial line" }.Union(
                            Enumerable.Range(0, i + 1).Select(j => $"test {j}")));
                    await app.WaitForLogDisplayed(expectedLog);
                }
            }
        }

        [IntegrationTest]
        public static async Task LiveLogCanBeDeletedAndRecreated(TestAppInstance app)
        {
            var testLog = app.ModelObjects.TempFilesManager.GenerateNewName();

            using (var logWriter = new StreamWriter(testLog, append: false))
            {
                Log(logWriter, "test1");
                Log(logWriter, "test2");
                Log(logWriter, "test3");

                await app.EmulateFileDragAndDrop(testLog);

                await app.WaitForLogDisplayed("test1\ntest2\ntest3");
            }

            File.Delete(testLog);

            await app.WaitForLogDisplayed("");

            using (var logWriter = new StreamWriter(testLog, append: false))
            {
                Log(logWriter, "test4");
                Log(logWriter, "test5");
                Log(logWriter, "test6");

                await app.WaitForLogDisplayed("test4\ntest5\ntest6");
            }
        }

        [IntegrationTest]
        public static async Task LiveLogCanBeRewritten(TestAppInstance app)
        {
            var testLog = app.ModelObjects.TempFilesManager.GenerateNewName();

            using (var stream = new FileStream(testLog,
                FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var logWriter = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true))
                {
                    Log(logWriter, "test1");
                    Log(logWriter, "test2");

                    await app.EmulateFileDragAndDrop(testLog);

                    await app.WaitForLogDisplayed("test1\ntest2");
                }

                stream.SetLength(0);
                stream.Position = 0;

                using (var logWriter = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true))
                {
                    Log(logWriter, "test4");

                    await app.WaitForLogDisplayed("test4");
                }
            }
        }
    }
}

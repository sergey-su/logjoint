using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LogJoint.Tests.Integration
{
	[IntegrationTestFixture]
	class FolderMonitoring
	{
		[IntegrationTest]
		public async Task CanMonitorAFolder(TestAppInstance app)
		{
			await app.AddTestCustomFormat();
			var testFolder = app.ModelObjects.TempFilesManager.GenerateNewName();
			Directory.CreateDirectory(testFolder);

			var format1 = (IFileBasedLogProviderFactory)app.Model.UserDefinedFormatsManager.Items.FirstOrDefault(
				i => i.FormatName == "TextWriterTraceListener");
			var format2 = (IFileBasedLogProviderFactory)app.Model.UserDefinedFormatsManager.Items.FirstOrDefault(
				i => i.FormatName == "Test123");

			File.WriteAllText(Path.Combine(testFolder, "prefix-a-1.log"),
				$@"SampleApp Start: 0 : test001
  ProcessId=69745
  ThreadId=1
  DateTime=2021-04-25T01:01:01.0000000Z
");
			File.WriteAllText(Path.Combine(testFolder, "prefix-a-2.log"),
				$@"SampleApp Start: 0 : test002
  ProcessId=69745
  ThreadId=1
  DateTime=2021-04-25T01:01:02.0000000Z
");

			await app.Model.SourcesManager.Create(format1,
				format1.CreateRotatedLogParams(testFolder, new[] { "prefix-a-*.log" }));

			await app.WaitForLogDisplayed("test001\ntest002");

			// Add 3rd part after monitoring has started
			File.WriteAllText(Path.Combine(testFolder, "prefix-a-3.log"),
				$@"SampleApp Start: 0 : test003
  ProcessId=69745
  ThreadId=1
  DateTime=2021-04-25T01:01:03.0000000Z
");
			await app.WaitForLogDisplayed("test001\ntest002\ntest003");

			// Delete the first part
			File.Delete(Path.Combine(testFolder, "prefix-a-1.log"));
			await app.WaitForLogDisplayed("test002\ntest003");

			// Start monitoring another type of logs with another prefix
			await app.Model.SourcesManager.Create(format2,
				format2.CreateRotatedLogParams(testFolder, new[] { "prefix-b-*.log" }));


			File.WriteAllText(Path.Combine(testFolder, "prefix-b-1.log"),
				"2021/04/26 01:01:10.000 T#1 test101\n");
			await app.WaitForLogDisplayed("test002\ntest003\ntest101");

			File.WriteAllText(Path.Combine(testFolder, "prefix-b-2.log"),
				"2021/04/26 01:01:11.000 T#1 test102\n");
			await app.WaitForLogDisplayed("test002\ntest003\ntest101\ntest102");

			// Start monitoring second log type again with another prefix
			await app.Model.SourcesManager.Create(format2,
				format2.CreateRotatedLogParams(testFolder, new[] { "prefix-c-*.log" }));

			File.WriteAllText(Path.Combine(testFolder, "prefix-c-1.log"),
				"2021/04/26 01:01:10.001 T#1 test201\n");
			await app.WaitForLogDisplayed("test002\ntest003\ntest101\ntest201\ntest102");

			File.WriteAllText(Path.Combine(testFolder, "prefix-c-2.log"),
				"2021/04/26 01:01:11.001 T#1 test202\n");
			await app.WaitForLogDisplayed("test002\ntest003\ntest101\ntest201\ntest102\ntest202");
		}
	}
}
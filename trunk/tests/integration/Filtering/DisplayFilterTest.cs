using NFluent;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Castle.Core.Logging;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class DisplayFilterTest
    {
        [IntegrationTest]
        public static async Task FiltersTextLogs(TestAppInstance app)
        {
            await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("chrome_debug_1.log"));

            app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.OnKeyPressed(UI.Presenters.LogViewer.Key.EndOfDocument);

            await app.WaitFor(() =>
            {
                string log = app.GetDisplayedLog();
                return
                    log.Contains("Stopping WebRTC event log.") &&
                    log.Contains("WebRTC event log successfully stopped.");
            });

            await app.SynchronizationContext.Invoke(() =>
            {
                app.PresentationObjects.MainFormPresenter.ActivateTab(UI.Presenters.MainForm.TabIDs.DisplayFilteringRules);
                app.PresentationObjects.ViewModels.DisplayFiltersManagement.OnAddFilterClicked();
                Check.That(app.PresentationObjects.ViewModels.DisplayFilterDialog.IsVisible).IsTrue();
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnTemplateChange("WebRTC event log successfully stopped.");
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnConfirmed();
            });

            await app.WaitFor(() =>
            {
                string log = app.GetDisplayedLog();
                return
                    log.Contains("Stopping WebRTC event log.") &&
                    !log.Contains("WebRTC event log successfully stopped.");
            });
        }

        [IntegrationTest]
        public static async Task FiltersXmlLogs(TestAppInstance app)
        {
            await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));

            app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.OnKeyPressed(UI.Presenters.LogViewer.Key.EndOfDocument);

            await app.WaitFor(() =>
            {
                string log = app.GetDisplayedLog();
                return
                    log.Contains("Searching for data files") &&
                    log.Contains("No free data file found.");
            });

            await app.SynchronizationContext.Invoke(() =>
            {
                app.PresentationObjects.MainFormPresenter.ActivateTab(UI.Presenters.MainForm.TabIDs.DisplayFilteringRules);
                app.PresentationObjects.ViewModels.DisplayFiltersManagement.OnAddFilterClicked();
                Check.That(app.PresentationObjects.ViewModels.DisplayFilterDialog.IsVisible).IsTrue();
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnTemplateChange("Searching for data files.");
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnConfirmed();
            });

            await app.WaitFor(() =>
            {
                string log = app.GetDisplayedLog();
                return
                    log.Contains("No free data file found.") &&
                    !log.Contains("Searching for data files.");
            });
        }

        [IntegrationTest]
        public static async Task FiltersJsonLogs(TestAppInstance app)
        {
            string TestJsonFormatDefinition = @"
<format>
  <id company='LogJoint' name='JSON Test' />
  <description>Test</description>
  <json>
    <head-re><![CDATA[^{\s*\""time\"":]]></head-re>
    <transform><![CDATA[{
  ""d"": ""#customfunction(logjoint.model,LogJoint.Json.Functions.TO_DATETIME,#valueof($.time),yyyy-MM-dd HH:mm:ss.ffff)"",
  ""m"": ""#valueof($.message)""
}]]></transform>
    <encoding>utf-8</encoding>
  </json>
</format>";
            await File.WriteAllTextAsync(
                Path.Combine(app.TestFormatDirectory, "test-json.format.xml"),
                TestJsonFormatDefinition
            );
            app.ModelObjects.UserDefinedFormatsManager.ReloadFactories();

            Check.That(app.Model.UserDefinedFormatsManager.Items.FirstOrDefault(
                i => i.FormatName == "JSON Test")).IsNotNull();

            var testLogFile = app.ModelObjects.TempFilesManager.CreateEmptyFile();
            File.WriteAllText(testLogFile,
                "{ \"time\": \"2024-11-20 04:40:43.1000\", \"message\": \"Foo\" }\r\n" +
                "{ \"time\": \"2024-11-20 04:40:43.2000\", \"message\": \"Bar\" }\r\n" +
                "{ \"time\": \"2024-11-20 04:40:43.2000\", \"message\": \"Zoo\" }\r\n");

            await app.EmulateFileDragAndDrop(testLogFile);

            await app.WaitFor(() =>
            {
                string log = app.GetDisplayedLog();
                return log.Contains("Foo") && log.Contains("Bar") && log.Contains("Zoo");
            });

            await app.SynchronizationContext.Invoke(() =>
            {
                app.PresentationObjects.MainFormPresenter.ActivateTab(UI.Presenters.MainForm.TabIDs.DisplayFilteringRules);
                app.PresentationObjects.ViewModels.DisplayFiltersManagement.OnAddFilterClicked();
                Check.That(app.PresentationObjects.ViewModels.DisplayFilterDialog.IsVisible).IsTrue();
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnTemplateChange("Bar");
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnConfirmed();
            });

            await Task.Delay(3000);
            string log = app.GetDisplayedLog();

            await app.WaitFor(() =>
            {
                string log = app.GetDisplayedLog();
                return log.Contains("Foo") && !log.Contains("Bar") && log.Contains("Zoo");
            });
        }
    }
}
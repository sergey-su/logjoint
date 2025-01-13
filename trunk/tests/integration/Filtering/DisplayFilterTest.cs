using NFluent;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using NSubstitute;
using System;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class DisplayFilterTest
    {
        [IntegrationTest]
        public static async Task FiltersTextLogs(TestAppInstance app)
        {
            await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("TextWriterTraceListener.converted.log"));
            Check.That(app.ModelObjects.LogSourcesManager.Items.Select(src => src.Provider.Factory.FormatName).First()).IsEqualTo("TextWriterTraceListener");

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
                "{ \"time\": \"2024-11-20 04:40:43.3000\", \"message\": \"Fizz\" }\r\n");

            await app.OpenFileAs(testLogFile, "LogJoint", "JSON Test");

            await app.WaitForLogDisplayed(@"Foo
Bar
Fizz");

            await app.SynchronizationContext.Invoke(() =>
            {
                app.PresentationObjects.MainFormPresenter.ActivateTab(UI.Presenters.MainForm.TabIDs.DisplayFilteringRules);
                app.PresentationObjects.ViewModels.DisplayFiltersManagement.OnAddFilterClicked();
                Check.That(app.PresentationObjects.ViewModels.DisplayFilterDialog.IsVisible).IsTrue();
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnTemplateChange("Bar");
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnConfirmed();
            });

            await app.WaitForLogDisplayed(@"Foo
Fizz");
        }

        [IntegrationTest]
        public static async Task FilteringIsPreservedOnReopen(TestAppInstance app)
        {
            await app.OpenFileAs(await app.Samples.GetSampleAsLocalFile("TextWriterTraceListener.converted.log"),
                "Microsoft", "TextWriterTraceListener");
            await app.WaitFor(() => app.GetDisplayedLog().Length > 0);

            await app.SynchronizationContext.Invoke(() =>
            {
                // Set up an inclusive filter
                app.PresentationObjects.MainFormPresenter.ActivateTab(UI.Presenters.MainForm.TabIDs.DisplayFilteringRules);
                app.PresentationObjects.ViewModels.DisplayFiltersManagement.OnAddFilterClicked();
                Check.That(app.PresentationObjects.ViewModels.DisplayFilterDialog.IsVisible).IsTrue();
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnTemplateChange("SetSink: ssrc:970030813");
                Check.That(
                    app.PresentationObjects.ViewModels.DisplayFilterDialog.Config.ActionComboBoxOptions.Select(a => a.Key).ToArray())
                    .Equals(new string[] { "Hide", "Show" });
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnActionComboBoxValueChange(1);
                app.PresentationObjects.ViewModels.DisplayFilterDialog.OnConfirmed();
            });

            string expectedLog = @"SetSink: ssrc:970030813 (ptr)
SetSink: ssrc:970030813 nullptr
SetSink: ssrc:970030813 (ptr)
SetSink: ssrc:970030813 nullptr
SetSink: ssrc:970030813 (ptr)
SetSink: ssrc:970030813 nullptr
SetSink: ssrc:970030813 (ptr)";

            await app.WaitForLogDisplayed(expectedLog);

            // Close all logs
            app.Mocks.AlertPopup.ShowPopupAsync(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(
                Task.FromResult(UI.Presenters.AlertFlags.Yes));
            app.PresentationObjects.ViewModels.SourcesManager.OnDeleteAllLogSourcesButtonClicked();
            await app.WaitFor(() => app.PresentationObjects.ViewModels.SourcesList.RootItem.Children.Count == 0);

            // Reopen the log
            await app.OpenFileAs(await app.Samples.GetSampleAsLocalFile("TextWriterTraceListener.converted.log"),
                "Microsoft", "TextWriterTraceListener");
            await app.WaitForLogDisplayed(expectedLog);
        }
    }
}
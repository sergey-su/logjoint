using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFluent;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class SearchAllTests
    {
        [IntegrationTest]
        public static async Task FindsAllOccurences(TestAppInstance app)
        {
            await app.OpenFileAs(await app.Samples.GetSampleAsLocalFile("TextWriterTraceListener.converted.log"),
                "Microsoft", "TextWriterTraceListener");

            await app.WaitFor(() =>
            {
                return !app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.IsEmpty;
            });


            app.PresentationObjects.MainFormPresenter.ActivateTab(UI.Presenters.MainForm.TabIDs.Search);
            app.PresentationObjects.ViewModels.SearchPanel.QuickSearchTextBox.OnChangeText("76674c6c636d674962746854");
            app.PresentationObjects.ViewModels.SearchPanel.QuickSearchTextBox.OnKeyDown(UI.Presenters.QuickSearchTextBox.Key.Enter);
            Check.That(app.PresentationObjects.ViewModels.SearchResult.IsSearchResultsVisible).IsTrue();

            await app.WaitFor(() =>
            {
                return app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines.Length == 3;
            });

            List<string> lines = app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines.Select(line => line.TextLineValue).ToList();
            Check.That(lines[0].Contains("Sending STUN ping, id=76674c6c636d674962746854")).IsTrue();
            Check.That(lines[1].Contains("Sent STUN ping, id=76674c6c636d674962746854")).IsTrue();
            Check.That(lines[2].Contains("Received STUN ping response, id=76674c6c636d674962746854")).IsTrue();
        }
    }
}

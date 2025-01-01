using NFluent;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class ViewModeTests
    {
        [BeforeEach]
        public async Task BeforeEach(TestAppInstance app)
        {
            await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
            await app.WaitFor(() => !app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.IsEmpty);

            // todo: emulate UI clicks for search
            var filters = app.ModelObjects.FiltersFactory.CreateFiltersList(FilterAction.Exclude, FiltersListPurpose.Search);
            filters.Insert(0, app.ModelObjects.FiltersFactory.CreateFilter(FilterAction.Include, "", true, new Search.Options { Template = "file" }));
            app.ModelObjects.SearchManager.SubmitSearch(new SearchAllOptions { Filters = filters });
            await app.WaitFor(() => !app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines.IsEmpty && app.ModelObjects.SearchManager.Results[0].Status == SearchResultStatus.Finished);
        }

        [IntegrationTest]
        public void ByDefaultColoringIsThreads(TestAppInstance app)
        {
            var vs = app.PresentationObjects.ViewModels.LoadedMessages.ViewState;
            Check.That(vs.Coloring.Visible).IsTrue();
            Check.That(vs.Coloring.Selected).IsEqualTo(1);
            Check.That(vs.Coloring.Options[1].Text.ToLower().Contains("thread"));

            var vl1 = app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines;
            Check.That(vl1[0].ContextColor).IsNotEqualTo(vl1[2].ContextColor);

            var vl2 = app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines;
            Check.That(vl2[19].ContextColor).IsNotEqualTo(vl2[13].ContextColor);
        }

        [IntegrationTest]
        public void ByDefaultTextModeIsSummaryForXmlWriterTraceFormat(TestAppInstance app)
        {
            var vs = app.PresentationObjects.ViewModels.LoadedMessages.ViewState;
            Check.That(vs.RawViewButton.Visible).IsTrue();
            Check.That(vs.RawViewButton.Checked).IsFalse();

            Check.That(app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines[0].TextLineValue).IsEqualTo(
                "File cannot be open which means that it was handled");

            Check.That(app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines[3].TextLineValue).IsEqualTo(
                "Processing new file: d5021b3c-f9ae-4860-a429-d0f32e2b7403.data");
        }

        [IntegrationTest]
        public void SwitchingColoringChangesItInSearchResults(TestAppInstance app)
        {
            app.PresentationObjects.ViewModels.LoadedMessages.OnColoringButtonClicked(0);

            Check.That(app.PresentationObjects.ViewModels.LoadedMessages.ViewState.Coloring.Selected).IsEqualTo(0);

            var vl1 = app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines;
            Check.That(vl1[0].ContextColor).IsEqualTo(vl1[2].ContextColor);

            var vl2 = app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines;
            Check.That(vl2[19].ContextColor).IsEqualTo(vl2[13].ContextColor);
        }

        [IntegrationTest]
        public void SwitchingRawModeChangesItInSearchResults(TestAppInstance app)
        {
            app.PresentationObjects.ViewModels.LoadedMessages.OnToggleRawView();

            Check.That(app.PresentationObjects.ViewModels.LoadedMessages.ViewState.RawViewButton.Checked).IsTrue();

            Check.That(app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines[0].TextLineValue).IsEqualTo(
                "<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\"><EventID>0</EventID><Type>3</Type><SubType Name=\"Information\">0</SubType><Level>8</Level><TimeCreated SystemTime=\"2011-07-24T10:37:43.7104727Z\" /><Source Name=\"SampleApp\" /><Correlation ActivityID=\"{00000000-0000-0000-0000-000000000000}\" /><Execution ProcessName=\"SampleLoggingApp\" ProcessID=\"1956\" ThreadID=\"6\" /><Channel/><Computer>SERGEYS-PC</Computer></System><ApplicationData>File cannot be open which means that it was handled</ApplicationData></E2ETraceEvent>");

            Check.That(app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines[3].TextLineValue).IsEqualTo(
                "<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\"><EventID>0</EventID><Type>3</Type><SubType Name=\"Start\">0</SubType><Level>255</Level><TimeCreated SystemTime=\"2011-07-24T10:37:41.7633614Z\" /><Source Name=\"SampleApp\" /><Correlation ActivityID=\"{00000000-0000-0000-0000-000000000000}\" /><Execution ProcessName=\"SampleLoggingApp\" ProcessID=\"1956\" ThreadID=\"6\" /><Channel/><Computer>SERGEYS-PC</Computer></System><ApplicationData>Processing new file: d5021b3c-f9ae-4860-a429-d0f32e2b7403.data</ApplicationData></E2ETraceEvent>");
        }
    }
}

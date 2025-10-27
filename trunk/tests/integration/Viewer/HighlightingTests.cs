using NFluent;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class HighlightingTests
    {
        [BeforeEach]
        public async Task BeforeEach(TestAppInstance app)
        {
            await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
            await app.WaitFor(() => !app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.IsEmpty);

            var searchFilters = app.ModelObjects.FiltersFactory.CreateFiltersList(FilterAction.Exclude, FiltersListPurpose.Search);
            searchFilters.Insert(0, app.ModelObjects.FiltersFactory.CreateFilter(FilterAction.Include, "", true, new Search.Options { Template = "file" }, timeRange: null));
            app.ModelObjects.SearchManager.SubmitSearch(new SearchAllOptions { Filters = searchFilters });
            await app.WaitFor(() => app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines.Length >= 20 && app.ModelObjects.SearchManager.Results[0].Status == SearchResultStatus.Finished);

            app.ModelObjects.FiltersManager.HighlightFilters.Insert(0, app.ModelObjects.FiltersFactory.CreateFilter(
                FilterAction.IncludeAndColorize1, "", true, new Search.Options { Template = "data" }, timeRange: null));

            await app.PresentationObjects.LoadedMessagesPresenter.LogViewerPresenter.GoHome();
            await app.PresentationObjects.SearchResultPresenter.LogViewerPresenter.GoHome();
        }

        [IntegrationTest]
        public void HighlightingsInLoadedMessagesAndInSearchResults(TestAppInstance app)
        {
            var vl1 = app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines;
            Check.That(vl1[15].HighlightingFiltersHighlightingRanges.ToList()).ContainsExactly([(8, 12, Drawing.Color.FromArgb(0, 255, 255))]);

            var vl2 = app.PresentationObjects.ViewModels.SearchResult.LogViewer.ViewLines;
            Check.That(vl2[0].HighlightingFiltersHighlightingRanges.ToList()).ContainsExactly([(14, 18, Drawing.Color.FromArgb(0, 255, 255))]);
        }
    }
}

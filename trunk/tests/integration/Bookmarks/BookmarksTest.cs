using System.Linq;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.LogViewer;
using NFluent;
using NSubstitute;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class BookmarksTest
    {
        [IntegrationTest]
        public static async Task BookmarkIsPreservedOnReopen(TestAppInstance app)
        {
            await app.OpenFileAs(await app.Samples.GetSampleAsLocalFile("TextWriterTraceListener.converted.log"),
                "Microsoft", "TextWriterTraceListener");

            app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.OnKeyPressed(UI.Presenters.LogViewer.Key.EndOfDocument);

            string testLogLine = "Stopping WebRTC event log";
            await app.WaitFor(() => app.GetDisplayedLog().Contains(testLogLine));

            // Bookmark the log line
            UI.Presenters.LogViewer.ViewLine findTestViewLine()
                => app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.First(
                    l => l.TextLineValue.Contains(testLogLine));

            app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.OnMessageMouseEvent(
                findTestViewLine(), new ViewLineCharIndex(2), MessageMouseEventFlag.SingleClick, null);
            Check.That(findTestViewLine().CursorCharIndex).IsEqualTo(2);
            app.PresentationObjects.ViewModels.BookmarksManager.OnAddBookmarkButtonClicked();

            // Set bookmark's annotation
            await app.WaitFor(() => app.PresentationObjects.ViewModels.BookmarksList.Items.Count == 1);
            app.PresentationObjects.ViewModels.BookmarksList.OnChangeSelection(
                [app.PresentationObjects.ViewModels.BookmarksList.Items[0]]);
            app.Mocks.PromptDialog.ExecuteDialogAsync(null, null, null).ReturnsForAnyArgs(
                Task.FromResult("That's interesting!!1"));
            app.PresentationObjects.ViewModels.BookmarksManager.OnPropertiesButtonClicked();

            // Close all logs
            app.Mocks.AlertPopup.ShowPopupAsync(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(
                Task.FromResult(UI.Presenters.AlertFlags.Yes));
            app.PresentationObjects.ViewModels.SourcesManager.OnDeleteAllLogSourcesButtonClicked();
            await app.WaitFor(() => app.PresentationObjects.ViewModels.SourcesList.RootItem.Children.Count == 0);
            await app.WaitFor(() => app.GetDisplayedLog().Length == 0);

            // Reopen the log
            await app.OpenFileAs(await app.Samples.GetSampleAsLocalFile("TextWriterTraceListener.converted.log"),
                "Microsoft", "TextWriterTraceListener");
            await app.WaitFor(() => app.GetDisplayedLog().Length > 0);

            // Verify that the restored bookmark works
            await app.WaitFor(() => app.PresentationObjects.ViewModels.BookmarksList.Items.Count == 1);
            app.PresentationObjects.ViewModels.BookmarksList.OnBookmarkLeftClicked(
                app.PresentationObjects.ViewModels.BookmarksList.Items[0]);
            await app.WaitFor(() => app.GetDisplayedLog().Contains(testLogLine));
            Check.That(app.PresentationObjects.ViewModels.BookmarksList.Items[0].Annotation)
                .IsEqualTo("That's interesting!!1");
        }
    }
}
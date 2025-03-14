using NSubstitute;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using LogJoint.UI.Presenters.SourcePropertiesWindow;
using SrcListItem = LogJoint.UI.Presenters.SourcesList.IViewItem;
using NFluent;
using System;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    class SourcePropertiesTests
    {
        TestAppInstance appInstance;
        ISamples samples;
        TaskCompletionSource<int> dialogTask;
        string tempLogFileName;

        [BeforeEach]
        public async Task BeforeEach(TestAppInstance app)
        {
            appInstance = app;
            samples = app.Samples;

            tempLogFileName = Path.Combine(app.AppDataDirectory, "XmlWriterTraceListener1.xml");
            File.Copy(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"), tempLogFileName, overwrite: true);
            await app.EmulateFileDragAndDrop(tempLogFileName);
            await app.WaitFor(() => app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.Length > 0);
            await app.WaitFor(() => app.PresentationObjects.ViewModels.SourcesList.RootItem.Children.Count == 1);
            app.PresentationObjects.ViewModels.SourcesList.OnSelectionChange(new[] {
                (SrcListItem)app.PresentationObjects.ViewModels.SourcesList.RootItem.Children[0]
            });
            await OpenDialog();
        }

        async Task OpenDialog()
        {
            await appInstance.WaitFor(() => appInstance.PresentationObjects.ViewModels.SourcesManager.PropertiesButtonEnabled);
            dialogTask = new TaskCompletionSource<int>();
            appInstance.Mocks.Views.CreateSourcePropertiesWindowView().CreateWindow().ShowModalDialog().Returns(dialogTask.Task);
            appInstance.PresentationObjects.ViewModels.SourcesList.OnSourceProprtiesMenuItemClicked();
            await appInstance.WaitFor(() => DialogState.NameEditbox.Text != null);
        }

        void CloseDialog()
        {
            ViewModel.OnClosingDialog();
            dialogTask.SetResult(0);
            dialogTask = null;
        }

        IViewModel ViewModel => appInstance.ViewModel.SourcePropertiesWindow;
        IViewState DialogState => ViewModel.ViewState;

        void EmulateShowTimeMenu()
        {
            var menuData = appInstance.PresentationObjects.ViewModels.LoadedMessages.LogViewer.OnMenuOpening(null);
            Check.That((menuData.VisibleItems & UI.Presenters.LogViewer.ContextMenuItem.ShowTime) != 0).IsTrue();
            Check.That((menuData.CheckedItems & UI.Presenters.LogViewer.ContextMenuItem.ShowTime) != 0).IsFalse();
            appInstance.PresentationObjects.ViewModels.LoadedMessages.LogViewer.OnMenuItemClicked(
                UI.Presenters.LogViewer.ContextMenuItem.ShowTime, null, true);
        }

        [IntegrationTest]
        public async Task CanToggleSourceVisibitity(TestAppInstance app)
        {
            Check.That(DialogState.VisibleCheckBox.Checked).IsEqualTo(true);
            ViewModel.OnVisibleCheckBoxChange(false);
            await app.WaitFor(() => DialogState.VisibleCheckBox.Checked == false);
            await app.WaitForLogDisplayed("");
            Check.That(((SrcListItem)app.PresentationObjects.ViewModels.SourcesList.RootItem.Children[0]).Checked).IsEqualTo(false);
        }

        [IntegrationTest]
        public void SourceNameIsCorrect(TestAppInstance app)
        {
            Check.That(DialogState.NameEditbox.Text.ToLower()).Contains("xmlwritertracelistener1.xml");
        }

        [IntegrationTest]
        public void SourceFormatIsCorrect(TestAppInstance app)
        {
            Check.That(DialogState.FormatTextBox.Text).IsEqualTo(@"Microsoft\XmlWriterTraceListener");
        }

        [IntegrationTest]
        public void CanCopyLogPath(TestAppInstance app)
        {
            Check.That(DialogState.CopyPathButton.Disabled).IsFalse();
            Check.That(DialogState.CopyPathButton.Hidden).IsFalse();
            ViewModel.OnCopyButtonClicked();
            app.Mocks.ClipboardAccess.Received(1).SetClipboard(Arg.Is<string>(
                s => s.ToLower().Contains("xmlwritertracelistener1.xml")));
        }

        [IntegrationTest]
        public async Task CanChangeLogSourceColor(TestAppInstance app)
        {
            ViewModel.OnChangeColorLinkClicked();
            var menuColors = (Drawing.Color[])
                app.Mocks.Views.CreateSourcePropertiesWindowView().CreateWindow().ReceivedCalls().Last().GetArguments()[0];
            Check.That(menuColors).IsNotNull();
            Check.That(menuColors.Length).IsStrictlyGreaterThan(10);
            var newColor = menuColors[7];
            ViewModel.OnColorSelected(newColor);
            await app.WaitFor(() => DialogState.ColorPanel.BackColor.Value == newColor);
            var sourcesColoringMode = app.PresentationObjects.ViewModels.LoadedMessages.ViewState.Coloring.Options.IndexOf(
                i => i.Text.ToLower().Contains("source"));
            app.PresentationObjects.ViewModels.LoadedMessages.OnColoringButtonClicked(sourcesColoringMode.Value);
            await app.WaitFor(() => app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.ElementAtOrDefault(0).ContextColor == newColor);
        }

        [IntegrationTest]
        public async Task LoadedMessagesCounterIsEventuallyCorrect(TestAppInstance app)
        {
            await app.WaitFor(() => int.Parse(DialogState.LoadedMessagesTextBox.Text) > 0);
        }

        [IntegrationTest]
        public async Task CanChangeAnnotation(TestAppInstance app)
        {
            Check.That(DialogState.AnnotationTextBox.Text).IsEqualTo("");
            Check.That(DialogState.AnnotationTextBox.Disabled).IsFalse();
            Check.That(DialogState.AnnotationTextBox.Hidden).IsFalse();

            ViewModel.OnChangeAnnotation("annotation 123");
            await app.WaitFor(() => DialogState.AnnotationTextBox.Text == "annotation 123");

            CloseDialog();

            await app.WaitFor(() => app.PresentationObjects.ViewModels.SourcesList.RootItem.Children[0].ToString().Contains(
                "annotation 123"));

            await OpenDialog();

            Check.That(DialogState.AnnotationTextBox.Text).IsEqualTo("annotation 123");
            Check.That(DialogState.AnnotationTextBox.Disabled).IsFalse();
            Check.That(DialogState.AnnotationTextBox.Hidden).IsFalse();
        }

        [IntegrationTest]
        public async Task CanChangeTimeShift(TestAppInstance app)
        {
            EmulateShowTimeMenu();
            await app.WaitFor(() => (app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.LastOrDefault().Time ?? "").EndsWith(":37:45.475"));

            Check.That(DialogState.TimeOffsetTextBox.Text).IsEqualTo("00:00:00");
            Check.That(DialogState.TimeOffsetTextBox.Disabled).IsFalse();
            Check.That(DialogState.TimeOffsetTextBox.Hidden).IsFalse();

            ViewModel.OnChangeChangeTimeOffset("-00:00:05");
            await app.WaitFor(() => DialogState.TimeOffsetTextBox.Text == "-00:00:05");

            CloseDialog();

            await app.WaitFor(() => app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.ViewLines.Last().Time.EndsWith(":37:40.475"));

            await OpenDialog();

            Check.That(DialogState.TimeOffsetTextBox.Text).IsEqualTo("-00:00:05");
            Check.That(DialogState.AnnotationTextBox.Disabled).IsFalse();
            Check.That(DialogState.AnnotationTextBox.Hidden).IsFalse();
        }

        [IntegrationTest]
        public void CanNotSaveAsTheSingleLog(TestAppInstance app)
        {
            Check.That(DialogState.SaveAsButton.Disabled).IsTrue();
            Check.That(DialogState.SaveAsButton.Hidden).IsFalse();
        }

        [IntegrationTest]
        public async Task CanSaveAsLogFromZipContainer(TestAppInstance app)
        {
            CloseDialog();
            app.PresentationObjects.AlertPopup.ShowPopupAsync(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(Task.FromResult(UI.Presenters.AlertFlags.Yes));
            app.PresentationObjects.ViewModels.SourcesManager.OnDeleteAllLogSourcesButtonClicked();
            await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1AndImage.zip"));
            await app.WaitFor(() => app.PresentationObjects.ViewModels.SourcesList.RootItem.Children.Count == 1);
            app.PresentationObjects.ViewModels.SourcesList.OnSelectionChange(new[] {
                (SrcListItem)app.PresentationObjects.ViewModels.SourcesList.RootItem.Children[0]
            });
            await OpenDialog();

            Check.That(DialogState.SaveAsButton.Disabled).IsFalse();
            Check.That(DialogState.SaveAsButton.Hidden).IsFalse();

            var savedLog = new TaskCompletionSource<string>();
            app.Mocks.FileDialogs.SaveOrDownloadFile(null, Arg.Any<UI.Presenters.SaveFileDialogParams>()).ReturnsForAnyArgs(async callInfo =>
            {
                var destinationFileName = app.ModelObjects.TempFilesManager.GenerateNewName();
                using (var fs = new FileStream(destinationFileName, FileMode.CreateNew))
                    await callInfo.Arg<Func<Stream, Task>>()(fs);
                savedLog.SetResult(File.ReadAllText(destinationFileName));
            });
            ViewModel.OnSaveAsButtonClicked();

            await app.WaitFor(() => savedLog.Task.IsCompleted);

            var head = "<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\"><EventID>0</EventID><Type>3</Type><SubType Name=\"Start\">0</SubType><Level>255</Level><TimeCreated SystemTime=\"2011-07-24T10:37:25.9854589Z\" /><Source Name=\"SampleApp\" />";
            Check.That(head).IsEqualTo((await savedLog.Task).Substring(0, head.Length));
        }

        [IntegrationTest]
        public void CanOpenContainingFolder(TestAppInstance app)
        {
            Check.That(DialogState.OpenContainingFolderButton.Disabled).IsFalse();
            Check.That(DialogState.OpenContainingFolderButton.Hidden).IsFalse();

            ViewModel.OnOpenContainingFolderButtonClicked();
            app.Mocks.ShellOpen.ReceivedWithAnyArgs().OpenFileBrowser(null);
        }

        [IntegrationTest]
        public async Task CanMaintainLiveLogProperties(TestAppInstance app)
        {
            EmulateShowTimeMenu();

            app.PresentationObjects.ViewModels.LoadedMessages.LogViewer.OnKeyPressed(UI.Presenters.LogViewer.Key.BeginOfDocument);
            await app.WaitFor(() => (DialogState.FirstMessageLinkLabel.Text ?? "").EndsWith(":37:25.985"));

            app.PresentationObjects.ViewModels.LoadedMessages.OnToggleViewTail();
            await app.WaitFor(() => DialogState.LastMessageLinkLabel.Text.EndsWith(":37:45.475"));

            var log = File.ReadAllText(tempLogFileName);
            log += "<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\">" +
"  <System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\">" +
"    <EventID>0</EventID>" +
"    <Type>3</Type>" +
"    <SubType Name=\"Start\">0</SubType>" +
"    <Level>255</Level>" +
"    <TimeCreated SystemTime=\"2011-07-24T10:50:01.1000000Z\" />" +
"    <Source Name=\"SampleApp\" />" +
"    <Correlation ActivityID=\"{00000000-0000-0000-0000-000000000000}\" />" +
"    <Execution ProcessName=\"SampleLoggingApp\" ProcessID=\"1956\" ThreadID=\"1\" />" +
"    <Channel/>" +
"    <Computer>SERGEYS-PC</Computer>" +
"  </System>" +
"  <ApplicationData>new line</ApplicationData>" +
"</E2ETraceEvent>";
            File.WriteAllText(tempLogFileName, log);
            await app.WaitFor(() => DialogState.LastMessageLinkLabel.Text.EndsWith(":50:01.100"));
        }
    }
}

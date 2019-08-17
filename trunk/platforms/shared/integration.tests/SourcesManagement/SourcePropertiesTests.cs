using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LogJoint.UI.Presenters.SourcePropertiesWindow;
using SrcListItem = LogJoint.UI.Presenters.SourcesList.IViewItem;

namespace LogJoint.Tests.Integration
{
	[TestFixture]
	class SourcePropertiesTests
	{
		readonly SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;
		TaskCompletionSource<int> dialogTask;
		string tempLogFileName;

		[SetUp]
		public async Task BeforeEach()
		{
			app = await TestAppInstance.Create();

			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				tempLogFileName = Path.Combine(app.AppDataDirectory, "XmlWriterTraceListener1.xml");
				File.Copy(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"), tempLogFileName, overwrite: true);
				await app.EmulateFileDragAndDrop(tempLogFileName);
				await app.WaitFor(() => app.ViewModel.SourcesList.RootItem.Children.Count == 1);
				app.ViewModel.SourcesList.OnSelectionChange(new[] {
					(SrcListItem)app.ViewModel.SourcesList.RootItem.Children[0]
				});
				await OpenDialog();
			});
		}

		[TearDown]
		public async Task AfterEach()
		{
			await app.Dispose();
		}

		async Task OpenDialog()
		{
			await app.WaitFor(() => app.ViewModel.SourcesManager.PropertiesButtonEnabled);
			dialogTask = new TaskCompletionSource<int>();
			app.Mocks.Views.CreateSourcePropertiesWindowView().CreateWindow().ShowModalDialog().Returns(dialogTask.Task);
			app.ViewModel.SourcesList.OnSourceProprtiesMenuItemClicked();
			await app.WaitFor(() => DialogState.NameEditbox.Text != null);
		}

		void CloseDialog()
		{
			ViewModel.OnClosingDialog();
			dialogTask.SetResult(0);
			dialogTask = null;
		}

		IViewModel ViewModel => app.ViewModel.SourcePropertiesWindow;
		IViewState DialogState => ViewModel.ViewState;

		void EmulateShowTimeMenu()
		{
			var menuData = app.ViewModel.LoadedMessagesLogViewer.OnMenuOpening();
			Assert.IsTrue((menuData.VisibleItems & UI.Presenters.LogViewer.ContextMenuItem.ShowTime) != 0);
			Assert.IsFalse((menuData.CheckedItems & UI.Presenters.LogViewer.ContextMenuItem.ShowTime) != 0);
			app.ViewModel.LoadedMessagesLogViewer.OnMenuItemClicked(UI.Presenters.LogViewer.ContextMenuItem.ShowTime, true);
		}

		[Test]
		public async Task CanToggleSourceVisibitity()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				Assert.IsTrue(DialogState.VisibleCheckBox.Checked);
				ViewModel.OnVisibleCheckBoxChange(false);
				await app.WaitFor(() => DialogState.VisibleCheckBox.Checked == false);
				await app.WaitForLogDisplayed("");
				Assert.IsFalse(((SrcListItem)app.ViewModel.SourcesList.RootItem.Children[0]).Checked);
			});
		}

		[Test]
		public void SourceNameIsCorrect()
		{
			Assert.IsTrue(DialogState.NameEditbox.Text.ToLower().Contains("xmlwritertracelistener1.xml"));
		}

		[Test]
		public void SourceFormatIsCorrect()
		{
			Assert.AreEqual(@"Microsoft\XmlWriterTraceListener", DialogState.FormatTextBox.Text);
		}

		[Test]
		public void CanCopyLogPath()
		{
			Assert.IsFalse(DialogState.CopyPathButton.Disabled);
			Assert.IsFalse(DialogState.CopyPathButton.Hidden);
			ViewModel.OnCopyButtonClicked();
			app.Mocks.ClipboardAccess.Received(1).SetClipboard(Arg.Is<string>(
				s => s.ToLower().Contains("xmlwritertracelistener1.xml")));
		}

		[Test]
		public async Task CanChangeLogSourceColor()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				ViewModel.OnChangeColorLinkClicked();
				var menuColors = (Drawing.Color[])
					app.Mocks.Views.CreateSourcePropertiesWindowView().CreateWindow().ReceivedCalls().Last().GetArguments()[0];
				Assert.IsNotNull(menuColors);
				Assert.Greater(menuColors.Length, 10);
				var newColor = menuColors[7];
				ViewModel.OnColorSelected(newColor);
				await app.WaitFor(() => DialogState.ColorPanel.BackColor.Value == newColor);
				var sourcesColoringMode = app.ViewModel.LoadedMessages.ViewState.Coloring.Options.IndexOf(
					i => i.Text.ToLower().Contains("source"));
				app.ViewModel.LoadedMessages.OnColoringButtonClicked(sourcesColoringMode.Value);
				await app.WaitFor(() => app.ViewModel.LoadedMessagesLogViewer.ViewLines.ElementAtOrDefault(0).ContextColor == newColor);
			});
		}

		[Test]
		public async Task LoadedMessagesCounterIsEventuallyCorrect()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.WaitFor(() => int.Parse(DialogState.LoadedMessagesTextBox.Text) > 0);
			});
		}

		[Test]
		public async Task CanChangeAnnotation()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				Assert.AreEqual("", DialogState.AnnotationTextBox.Text);
				Assert.IsFalse(DialogState.AnnotationTextBox.Disabled);
				Assert.IsFalse(DialogState.AnnotationTextBox.Hidden);

				ViewModel.OnChangeAnnotation("annotation 123");
				await app.WaitFor(() => DialogState.AnnotationTextBox.Text == "annotation 123");

				CloseDialog();

				await app.WaitFor(() => app.ViewModel.SourcesList.RootItem.Children[0].ToString().Contains(
					"annotation 123"));

				await OpenDialog();

				Assert.AreEqual("annotation 123", DialogState.AnnotationTextBox.Text);
				Assert.IsFalse(DialogState.AnnotationTextBox.Disabled);
				Assert.IsFalse(DialogState.AnnotationTextBox.Hidden);
			});
		}

		[Test]
		public async Task CanChangeTimeShift()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				EmulateShowTimeMenu();
				await app.WaitFor(() => (app.ViewModel.LoadedMessagesLogViewer.ViewLines.LastOrDefault().Time ?? "").EndsWith(":37:45.475"));

				Assert.AreEqual("00:00:00", DialogState.TimeOffsetTextBox.Text);
				Assert.IsFalse(DialogState.TimeOffsetTextBox.Disabled);
				Assert.IsFalse(DialogState.TimeOffsetTextBox.Hidden);

				ViewModel.OnChangeChangeTimeOffset("-00:00:05");
				await app.WaitFor(() => DialogState.TimeOffsetTextBox.Text == "-00:00:05");

				CloseDialog();

				await app.WaitFor(() => app.ViewModel.LoadedMessagesLogViewer.ViewLines.Last().Time.EndsWith(":37:40.475"));

				await OpenDialog();

				Assert.AreEqual("-00:00:05", DialogState.TimeOffsetTextBox.Text);
				Assert.IsFalse(DialogState.AnnotationTextBox.Disabled);
				Assert.IsFalse(DialogState.AnnotationTextBox.Hidden);
			});
		}

		[Test]
		public void CanNotSaveAsTheSingleLog()
		{
			Assert.IsTrue(DialogState.SaveAsButton.Disabled);
			Assert.IsFalse(DialogState.SaveAsButton.Hidden);
		}

		[Test]
		public async Task CanSaveAsLogFromZipContainer()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				CloseDialog();
				app.Presentation.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(UI.Presenters.AlertFlags.Yes);
				app.ViewModel.SourcesManager.OnDeleteAllLogSourcesButtonClicked();
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1AndImage.zip"));
				await app.WaitFor(() => app.ViewModel.SourcesList.RootItem.Children.Count == 1);
				app.ViewModel.SourcesList.OnSelectionChange(new[] {
					(SrcListItem)app.ViewModel.SourcesList.RootItem.Children[0]
				});
				await OpenDialog();

				Assert.IsFalse(DialogState.SaveAsButton.Disabled);
				Assert.IsFalse(DialogState.SaveAsButton.Hidden);

				var destinationFileName = app.Model.TempFilesManager.GenerateNewName();
				app.Mocks.FileDialogs.SaveFileDialog(Arg.Any<UI.Presenters.SaveFileDialogParams>()).ReturnsForAnyArgs(destinationFileName);
				ViewModel.OnSaveAsButtonClicked();
				app.Mocks.FileDialogs.Received(1).SaveFileDialog(Arg.Any<UI.Presenters.SaveFileDialogParams>());

				var savedLog = File.ReadAllText(destinationFileName);

				var head = "<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\"><EventID>0</EventID><Type>3</Type><SubType Name=\"Start\">0</SubType><Level>255</Level><TimeCreated SystemTime=\"2011-07-24T10:37:25.9854589Z\" /><Source Name=\"SampleApp\" />";
				Assert.AreEqual(head, savedLog.Substring(0, head.Length));
			});
		}

		[Test]
		public void CanOpenContainingFolder()
		{
			Assert.IsFalse(DialogState.OpenContainingFolderButton.Disabled);
			Assert.IsFalse(DialogState.OpenContainingFolderButton.Hidden);

			ViewModel.OnOpenContainingFolderButtonClicked();
			app.Mocks.ShellOpen.ReceivedWithAnyArgs().OpenFileBrowser(null);
		}

		[Test]
		public async Task CanMaintainLiveLogProperties()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				EmulateShowTimeMenu();

				app.ViewModel.LoadedMessagesLogViewer.OnKeyPressed(UI.Presenters.LogViewer.Key.BeginOfDocument);
				await app.WaitFor(() => (DialogState.FirstMessageLinkLabel.Text ?? "").EndsWith(":37:25.985"));

				app.ViewModel.LoadedMessages.OnToggleViewTail();
				await app.WaitFor(() => DialogState.LastMessageLinkLabel.Text.EndsWith(":37:45.475"));

				var log = File.ReadAllText(tempLogFileName);
				log += "<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\">"+
"  <System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\">"+
"    <EventID>0</EventID>"+
"    <Type>3</Type>"+
"    <SubType Name=\"Start\">0</SubType>"+
"    <Level>255</Level>"+
"    <TimeCreated SystemTime=\"2011-07-24T10:50:01.1000000Z\" />"+
"    <Source Name=\"SampleApp\" />"+
"    <Correlation ActivityID=\"{00000000-0000-0000-0000-000000000000}\" />"+
"    <Execution ProcessName=\"SampleLoggingApp\" ProcessID=\"1956\" ThreadID=\"1\" />"+
"    <Channel/>"+
"    <Computer>SERGEYS-PC</Computer>"+
"  </System>"+
"  <ApplicationData>new line</ApplicationData>"+
"</E2ETraceEvent>";
				File.WriteAllText(tempLogFileName, log);
				await app.WaitFor(() => DialogState.LastMessageLinkLabel.Text.EndsWith(":50:01.100"));
			});
		}
	}
}

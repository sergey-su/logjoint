using NSubstitute;
using NFluent;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using LogJoint.UI.Presenters.SourcesList;

namespace LogJoint.Tests.Integration
{
	[IntegrationTestFixture]
	class ZipContainerInSourcesListTests
	{
		TestAppInstance appInstance;

		[BeforeEach]
		public async Task BeforeEach(TestAppInstance app)
		{
			this.appInstance = app;

			var preprocTask = app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListenerAndTextWriterTraceListener.zip"));

			await app.WaitFor(() => app.ViewModel.PreprocessingUserInteractions.DialogData != null);
			app.ViewModel.PreprocessingUserInteractions.OnCloseDialog(accept: true);

			await preprocTask;

			await app.WaitFor(() => app.ViewModel.SourcesList.RootItem.Children.Count == 1);
		}

		IViewItem ListRoot => appInstance.ViewModel.SourcesList.RootItem;

		[IntegrationTest]
		public void SourceListIsPopulated(TestAppInstance app)
		{
			Check.That(ListRoot.Children.Count).IsEqualTo(1);
			var container = (IViewItem)ListRoot.Children[0];
			Check.That(container.Children.Count).IsEqualTo(2);
			Check.That(container.IsExpanded).IsFalse();
			Check.That(container.Checked).IsEqualTo(true);
			Check.That(container.ToString()).Contains("XmlWriterTraceListenerAndTextWriterTraceListener.zip");
			Check.That(container.ToString()).EndsWith("(2 logs)");
			Check.That(container.Children[0].ToString().ToLower().Contains(@"xmlwritertracelistenerandtextwritertracelistener.zip\xmlwritertracelistener1.xml")).IsTrue();
			Check.That(((IViewItem)container.Children[0]).Checked).IsEqualTo(true);
			Check.That(container.Children[1].ToString().ToLower().Contains(@"xmlwritertracelistenerandtextwritertracelistener.zip\textwritertracelistener.log")).IsTrue();
			Check.That(((IViewItem)container.Children[1]).Checked).IsEqualTo(true);
		}

		async Task ExpandContainer()
		{
			appInstance.ViewModel.SourcesList.OnItemExpand(
				(IViewItem)ListRoot.Children[0]);
			await appInstance.WaitFor(() => ListRoot.Children[0].IsExpanded);
		}

		[IntegrationTest]
		public async Task CanRemoveOneLogFromContainer(TestAppInstance app)
		{
			await ExpandContainer();

			app.ViewModel.SourcesList.OnSelectionChange(
				new[] { (IViewItem)ListRoot.Children[0].Children[0] });
			await app.WaitFor(() => ListRoot.Children[0].Children[0].IsSelected);

			Check.That(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled).IsTrue();
			Check.That(app.ViewModel.SourcesManager.PropertiesButtonEnabled).IsTrue();

			app.PresentationObjects.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(UI.Presenters.AlertFlags.Yes);
			app.ViewModel.SourcesManager.OnDeleteSelectedLogSourcesButtonClicked();

			app.PresentationObjects.AlertPopup.Received(1).ShowPopup("Delete", "Are you sure you want to close 1 log (s)", UI.Presenters.AlertFlags.YesNoCancel);

			await app.WaitFor(() =>
					ListRoot.Children.Count == 1
				&& ListRoot.Children[0].Children.Count == 0
				&& ListRoot.Children[0].IsSelected == false);

			Check.That(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled).IsFalse();
			Check.That(app.ViewModel.SourcesManager.PropertiesButtonEnabled).IsFalse();

			await app.WaitForLogDisplayed(
@"No free data file found. Going sleep.
Searching for data files
No free data file found. Going sleep.
File cannot be open which means that it was handled
Timestamp parsed and ignored
Test frame
"
			);
		}

		[IntegrationTest]
		public async Task CanRemoveBothLogsFromContainer(TestAppInstance app)
		{
			await ExpandContainer();

			app.ViewModel.SourcesList.OnSelectionChange(new[] {
				(IViewItem)ListRoot.Children[0].Children[0],
				(IViewItem)ListRoot.Children[0].Children[1]
			});
			await app.WaitFor(() =>
					ListRoot.Children[0].Children[0].IsSelected
				&& ListRoot.Children[0].Children[1].IsSelected
			);

			Check.That(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled).IsTrue();
			Check.That(app.ViewModel.SourcesManager.PropertiesButtonEnabled).IsFalse();

			app.PresentationObjects.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(UI.Presenters.AlertFlags.Yes);
			app.ViewModel.SourcesManager.OnDeleteSelectedLogSourcesButtonClicked();

			app.PresentationObjects.AlertPopup.Received(1).ShowPopup("Delete", "Are you sure you want to close 2 log (s)", UI.Presenters.AlertFlags.YesNoCancel);

			await app.WaitFor(() => ListRoot.Children.Count == 0);

			Check.That(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled).IsFalse();
			Check.That(app.ViewModel.SourcesManager.PropertiesButtonEnabled).IsFalse();

			await app.WaitForLogDisplayed("");
		}

		[IntegrationTest]
		public async Task CanRemoveContainer(TestAppInstance app)
		{
			app.ViewModel.SourcesList.OnSelectionChange(new[] {
				(IViewItem)ListRoot.Children[0]
			});
			await app.WaitFor(() => ListRoot.Children[0].IsSelected);

			Check.That(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled).IsTrue();
			Check.That(app.ViewModel.SourcesManager.PropertiesButtonEnabled).IsFalse();

			app.PresentationObjects.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(UI.Presenters.AlertFlags.Yes);
			app.ViewModel.SourcesManager.OnDeleteSelectedLogSourcesButtonClicked();

			app.PresentationObjects.AlertPopup.Received(1).ShowPopup("Delete", "Are you sure you want to close 2 log (s)", UI.Presenters.AlertFlags.YesNoCancel);

			await app.WaitFor(() => ListRoot.Children.Count == 0);

			Check.That(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled).IsFalse();
			Check.That(app.ViewModel.SourcesManager.PropertiesButtonEnabled).IsFalse();

			await app.WaitForLogDisplayed("");
		}

		[IntegrationTest]
		public async Task CanHideOneLogInContainer(TestAppInstance app)
		{
			await ExpandContainer();

			app.ViewModel.SourcesList.OnItemCheck(
				(IViewItem)ListRoot.Children[0].Children[0], false);
			await app.WaitFor(() => ((IViewItem)ListRoot.Children[0].Children[0]).Checked == false);

			await app.WaitForLogDisplayed(
@"No free data file found. Going sleep.
Searching for data files
No free data file found. Going sleep.
File cannot be open which means that it was handled
Timestamp parsed and ignored
Test frame
"
			);

			Check.WithCustomMessage("Container must be unchecked").That(((IViewItem)ListRoot.Children[0]).Checked).IsEqualTo(false);
		}

		[IntegrationTest]
		public async Task SanSaveMergedLog(TestAppInstance app)
		{
			var destinationFileName = Path.GetTempFileName();
			try
			{
				var expectedJoinedLog = File.ReadAllBytes(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListenerAndTextWriterTraceListenerJoined.log"));
				app.Mocks.FileDialogs.SaveFileDialog(Arg.Any<UI.Presenters.SaveFileDialogParams>()).ReturnsForAnyArgs(destinationFileName);
				var (visibleItems, checkedItems) = app.ViewModel.SourcesList.OnMenuItemOpening(ctrl: false);
				Check.That((visibleItems & MenuItem.SaveMergedFilteredLog) != 0).IsTrue();
				app.ViewModel.SourcesList.OnSaveMergedFilteredLogMenuItemClicked();
				var match = false;
				for (var iter = 0; iter < 25; ++iter)
				{
					await Task.Delay(100);
					var actualJoinedLog = File.ReadAllBytes(destinationFileName);
					if ((match = actualJoinedLog.SequenceEqual(expectedJoinedLog)) == true)
						break;
				}
				Check.That(match).IsTrue();
			}
			finally
			{
				if (File.Exists(destinationFileName))
					File.Delete(destinationFileName);
			}
		}
	}
}

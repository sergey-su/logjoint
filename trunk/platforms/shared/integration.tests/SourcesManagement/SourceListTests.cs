using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Preprocessing;
using System.IO;
using LogJoint.UI.Presenters.SourcesList;

namespace LogJoint.Tests.Integration
{
	[TestFixture]
	class ZipContainerInSourcesListTests
	{
		readonly SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;

		[SetUp]
		public async Task BeforeEach()
		{
			app = await TestAppInstance.Create();

			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				var preprocTask = app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListenerAndTextWriterTraceListener.zip"));

				await app.WaitFor(() => app.ViewModel.PreprocessingUserInteractions.DialogData != null);
				app.ViewModel.PreprocessingUserInteractions.OnCloseDialog(accept: true);

				await preprocTask;

				await app.WaitFor(() => app.ViewModel.SourcesList.RootItem.Children.Count == 1);
			});
		}

		[TearDown]
		public async Task AfterEach()
		{
			await app.Dispose();
		}

		IViewItem ListRoot => app.ViewModel.SourcesList.RootItem;

		[Test]
		public void SourceListIsPopulated()
		{
			Assert.AreEqual(1, ListRoot.Children.Count);
			var container = (IViewItem)ListRoot.Children[0];
			Assert.AreEqual(2, container.Children.Count);
			Assert.IsFalse(container.IsExpanded);
			Assert.IsTrue(container.Checked);
			Assert.IsTrue(container.ToString().Contains("XmlWriterTraceListenerAndTextWriterTraceListener.zip"));
			Assert.IsTrue(container.ToString().EndsWith("(2 logs)", StringComparison.InvariantCultureIgnoreCase));
			Assert.IsTrue(container.Children[0].ToString().Contains(@"xmlwritertracelistenerandtextwritertracelistener.zip\xmlwritertracelistener1.xml",
				StringComparison.InvariantCultureIgnoreCase));
			Assert.IsTrue(((IViewItem)container.Children[0]).Checked);
			Assert.IsTrue(container.Children[1].ToString().Contains(@"xmlwritertracelistenerandtextwritertracelistener.zip\textwritertracelistener.log",
				StringComparison.InvariantCultureIgnoreCase));
			Assert.IsTrue(((IViewItem)container.Children[1]).Checked);
		}

		async Task ExpandContainer()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				app.ViewModel.SourcesList.OnItemExpand(
					(IViewItem)ListRoot.Children[0]);
				await app.WaitFor(() => ListRoot.Children[0].IsExpanded);
			});
		}

		[Test]
		public async Task CanRemoveOneLogFromContainer()
		{
			await ExpandContainer();
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				app.ViewModel.SourcesList.OnSelectionChange(
					new[] { (IViewItem)ListRoot.Children[0].Children[0] });
				await app.WaitFor(() => ListRoot.Children[0].Children[0].IsSelected);

				Assert.IsTrue(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled);
				Assert.IsTrue(app.ViewModel.SourcesManager.PropertiesButtonEnabled);

				app.Presentation.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(UI.Presenters.AlertFlags.Yes);
				app.ViewModel.SourcesManager.OnDeleteSelectedLogSourcesButtonClicked();

				app.Presentation.AlertPopup.Received(1).ShowPopup("Delete", "Are you sure you want to close 1 log (s)", UI.Presenters.AlertFlags.YesNoCancel);

				await app.WaitFor(() =>
					   ListRoot.Children.Count == 1
					&& ListRoot.Children[0].Children.Count == 0
					&& ListRoot.Children[0].IsSelected == false);

				Assert.IsFalse(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled);
				Assert.IsFalse(app.ViewModel.SourcesManager.PropertiesButtonEnabled);

				await app.WaitForLogDisplayed(
@"No free data file found. Going sleep.
Searching for data files
No free data file found. Going sleep.
File cannot be open which means that it was handled
Timestamp parsed and ignored
Test frame
"
				);
			});
		}

		[Test]
		public async Task CanRemoveBothLogsFromContainer()
		{
			await ExpandContainer();
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				app.ViewModel.SourcesList.OnSelectionChange(new[] {
					(IViewItem)ListRoot.Children[0].Children[0],
					(IViewItem)ListRoot.Children[0].Children[1]
				});
				await app.WaitFor(() =>
					   ListRoot.Children[0].Children[0].IsSelected
					&& ListRoot.Children[0].Children[1].IsSelected
				);

				Assert.IsTrue(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled);
				Assert.IsFalse(app.ViewModel.SourcesManager.PropertiesButtonEnabled);

				app.Presentation.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(UI.Presenters.AlertFlags.Yes);
				app.ViewModel.SourcesManager.OnDeleteSelectedLogSourcesButtonClicked();

				app.Presentation.AlertPopup.Received(1).ShowPopup("Delete", "Are you sure you want to close 2 log (s)", UI.Presenters.AlertFlags.YesNoCancel);

				await app.WaitFor(() => ListRoot.Children.Count == 0);

				Assert.IsFalse(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled);
				Assert.IsFalse(app.ViewModel.SourcesManager.PropertiesButtonEnabled);

				await app.WaitForLogDisplayed("");
			});
		}

		[Test]
		public async Task CanRemoveContainer()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				app.ViewModel.SourcesList.OnSelectionChange(new[] {
					(IViewItem)ListRoot.Children[0]
				});
				await app.WaitFor(() => ListRoot.Children[0].IsSelected);

				Assert.IsTrue(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled);
				Assert.IsFalse(app.ViewModel.SourcesManager.PropertiesButtonEnabled);

				app.Presentation.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(UI.Presenters.AlertFlags.Yes);
				app.ViewModel.SourcesManager.OnDeleteSelectedLogSourcesButtonClicked();

				app.Presentation.AlertPopup.Received(1).ShowPopup("Delete", "Are you sure you want to close 2 log (s)", UI.Presenters.AlertFlags.YesNoCancel);

				await app.WaitFor(() => ListRoot.Children.Count == 0);

				Assert.IsFalse(app.ViewModel.SourcesManager.DeleteSelectedSourcesButtonEnabled);
				Assert.IsFalse(app.ViewModel.SourcesManager.PropertiesButtonEnabled);

				await app.WaitForLogDisplayed("");
			});
		}

		[Test]
		public async Task CanHideOneLogInContainer()
		{
			await ExpandContainer();
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
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

				Assert.IsFalse(((IViewItem)ListRoot.Children[0]).Checked, "Container must be unchecked");
			});
		}
	}
}

using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Preprocessing;

namespace LogJoint.Tests.Integration
{
	[TestFixture]
	class ViewModeTests
	{
		readonly SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;

		[OneTimeSetUp]
		public async Task BeforeEach()
		{
			app = await TestAppInstance.Create();
			await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
			await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);

			// todo: emulate UI clicks for search
			var filters = app.Model.FiltersFactory.CreateFiltersList(FilterAction.Exclude, FiltersListPurpose.Search);
			filters.Insert(0, app.Model.FiltersFactory.CreateFilter(FilterAction.Include, "", true, new Search.Options { Template = "file" }));
			app.Model.SearchManager.SubmitSearch(new SearchAllOptions { Filters = filters });
			await app.WaitFor(() => !app.ViewModel.SearchResultLogViewer.ViewLines.IsEmpty && app.Model.SearchManager.Results[0].Status == SearchResultStatus.Finished);
		}

		[OneTimeTearDown]
		public async Task AfterEach()
		{
			await app.Dispose();
		}

		[Test]
		public async Task ByDefaultColoringIsThreads()
		{
			await app.SynchronizationContext.Invoke(() =>
			{
				var vs = app.ViewModel.LoadedMessages.ViewState;
				Assert.IsTrue(vs.Coloring.Visible);
				Assert.AreEqual(1, vs.Coloring.Selected);
				Assert.IsTrue(vs.Coloring.Options[1].Text.ToLower().Contains("thread"));

				var vl1 = app.ViewModel.LoadedMessagesLogViewer.ViewLines;
				Assert.AreNotEqual(vl1[0].ContextColor, vl1[2].ContextColor);

				var vl2 = app.ViewModel.SearchResultLogViewer.ViewLines;
				Assert.AreNotEqual(vl2[19].ContextColor, vl2[13].ContextColor);
			});
		}

		[Test]
		public async Task ByDefaultTextModeIsSummaryForXmlWriterTraceFormat()
		{
			await app.SynchronizationContext.Invoke(() =>
			{
				var vs = app.ViewModel.LoadedMessages.ViewState;
				Assert.IsTrue(vs.RawViewButton.Visible);
				Assert.IsFalse(vs.RawViewButton.Checked);

				Assert.AreEqual("File cannot be open which means that it was handled",
					app.ViewModel.LoadedMessagesLogViewer.ViewLines[0].TextLineValue);

				Assert.AreEqual("Processing new file: d5021b3c-f9ae-4860-a429-d0f32e2b7403.data",
					app.ViewModel.SearchResultLogViewer.ViewLines[3].TextLineValue);
			});
		}

		[Test]
		public async Task SwitchingColoringChangesItInSearchResults()
		{
			await app.SynchronizationContext.Invoke(() =>
			{
				app.ViewModel.LoadedMessages.OnColoringButtonClicked(0);

				Assert.AreEqual(0, app.ViewModel.LoadedMessages.ViewState.Coloring.Selected);

				var vl1 = app.ViewModel.LoadedMessagesLogViewer.ViewLines;
				Assert.AreEqual(vl1[0].ContextColor, vl1[2].ContextColor);

				var vl2 = app.ViewModel.SearchResultLogViewer.ViewLines;
				Assert.AreEqual(vl2[19].ContextColor, vl2[13].ContextColor);
			});
		}

		[Test]
		public async Task SwitchingRawModeChangesItInSearchResults()
		{
			await app.SynchronizationContext.Invoke(() =>
			{
				app.ViewModel.LoadedMessages.OnToggleRawView();

				Assert.IsTrue(app.ViewModel.LoadedMessages.ViewState.RawViewButton.Checked);

				Assert.AreEqual("<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\"><EventID>0</EventID><Type>3</Type><SubType Name=\"Information\">0</SubType><Level>8</Level><TimeCreated SystemTime=\"2011-07-24T10:37:43.7104727Z\" /><Source Name=\"SampleApp\" /><Correlation ActivityID=\"{00000000-0000-0000-0000-000000000000}\" /><Execution ProcessName=\"SampleLoggingApp\" ProcessID=\"1956\" ThreadID=\"6\" /><Channel/><Computer>SERGEYS-PC</Computer></System><ApplicationData>File cannot be open which means that it was handled</ApplicationData></E2ETraceEvent>",
					app.ViewModel.LoadedMessagesLogViewer.ViewLines[0].TextLineValue);

				Assert.AreEqual("<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\"><EventID>0</EventID><Type>3</Type><SubType Name=\"Start\">0</SubType><Level>255</Level><TimeCreated SystemTime=\"2011-07-24T10:37:41.7633614Z\" /><Source Name=\"SampleApp\" /><Correlation ActivityID=\"{00000000-0000-0000-0000-000000000000}\" /><Execution ProcessName=\"SampleLoggingApp\" ProcessID=\"1956\" ThreadID=\"6\" /><Channel/><Computer>SERGEYS-PC</Computer></System><ApplicationData>Processing new file: d5021b3c-f9ae-4860-a429-d0f32e2b7403.data</ApplicationData></E2ETraceEvent>",
					app.ViewModel.SearchResultLogViewer.ViewLines[3].TextLineValue);
			});
		}
	}
}

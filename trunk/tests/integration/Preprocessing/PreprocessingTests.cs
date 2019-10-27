using NSubstitute;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NFluent;

namespace LogJoint.Tests.Integration
{
	[IntegrationTestFixture]
	class PreprocessingTests
	{
		static bool IsXmlWriterTraceListenerLogIsLoaded(TestAppInstance app)
		{
			var viewLines = app.ViewModel.LoadedMessagesLogViewer.ViewLines;
			return !viewLines.IsEmpty && viewLines[0].TextLineValue == "File cannot be open which means that it was handled";
		}

		[IntegrationTest]
		public async Task CanLoadAndDetectFormatOfLocalLog(TestAppInstance app)
		{
			await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
			await app.WaitFor(() => IsXmlWriterTraceListenerLogIsLoaded(app));
		}

		[IntegrationTest]
		public async Task CanDownloadAndDetectFormatOfLogFromTheWeb(TestAppInstance app)
		{
			await app.EmulateUrlDragAndDrop(app.Samples.GetSampleAsUri("XmlWriterTraceListener1.xml").ToString());
			await app.WaitFor(() => IsXmlWriterTraceListenerLogIsLoaded(app));
		}

		[IntegrationTest]
		public async Task CanDownloadZipExtractAndFindKnownLogFormatInArchive(TestAppInstance app)
		{
			await app.EmulateUrlDragAndDrop(app.Samples.GetSampleAsUri("XmlWriterTraceListener1AndImage.zip").ToString());
			await app.WaitFor(() => IsXmlWriterTraceListenerLogIsLoaded(app));
		}

		[IntegrationTest]
		public async Task CanOpenPasswordProtectedZipExtractAndFindKnownLogFormatInArchive(TestAppInstance app)
		{
			app.Mocks.CredentialsCache.QueryCredentials(
				Arg.Is<Uri>(v => v.ToString().Contains("XmlWriterTraceListener1AndImage.PasswordProtected.zip")), null)
				.ReturnsForAnyArgs(new System.Net.NetworkCredential("", "Pa$$w0rd"));
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1AndImage.PasswordProtected.zip"));
				await app.WaitFor(() => IsXmlWriterTraceListenerLogIsLoaded(app));
			});
		}

		[IntegrationTest]
		public async Task DisplaysProgressDuringUnzipping(TestAppInstance app)
		{
			var preprocessorTask = app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("network_trace_with_keys_1.as_pdml.zip"));
			int lastPercent = 0;
			for (int iter = 0; iter < 3; ++iter)
			{
				await app.WaitFor(() =>
				{
					var displayText = app.ViewModel.SourcesList.RootItem.Children.ElementAtOrDefault(0)?.ToString() ?? "";
					var m = Regex.Match(displayText, @"Unpacking (\d+)\%");
					if (m.Success)
					{
						var percent = int.Parse(m.Groups[1].Value);
						if (percent > lastPercent && percent < 100)
						{
							lastPercent = percent;
							return true;
						}
					}
					return false;
				});
			}
		}

		[IntegrationTest]
		public async Task UnzippingCanBeCancelled(TestAppInstance app)
		{
			var preprocessorTask = app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("network_trace_with_keys_1.as_pdml.zip"));
			await app.WaitFor(() => (app.ViewModel.SourcesList.RootItem.Children.ElementAtOrDefault(0)?.ToString() ?? "").Contains("Unpacking"));
			app.ViewModel.SourcesList.OnSelectAllShortcutPressed();
			app.Mocks.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(
				UI.Presenters.AlertFlags.Yes);
			var stopwatch = Stopwatch.StartNew();
			app.ViewModel.SourcesList.OnDeleteButtonPressed();
			await app.WaitFor(() => app.ViewModel.SourcesList.RootItem.Children.Count == 0);
			stopwatch.Stop();
			Check.That(stopwatch.ElapsedMilliseconds).IsStrictlyLessThan(1000);
			Check.That(preprocessorTask.IsCanceled).IsTrue();
		}

		[IntegrationTest]
		public async Task CanExtractGZippedLog(TestAppInstance app)
		{
			await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml.gz"));
			await app.WaitFor(() => IsXmlWriterTraceListenerLogIsLoaded(app));
		}


		[IntegrationTest]
		public async Task CanDownloadZipExtractFindManyKnownLogsAndAskUserWhatToOpen(TestAppInstance app)
		{
			var preprocTask = app.EmulateUrlDragAndDrop(app.Samples.GetSampleAsUri("XmlWriterTraceListenerAndTextWriterTraceListener.zip").ToString());

			await app.WaitFor(() => app.ViewModel.PreprocessingUserInteractions.DialogData != null);

			var userQueryItems = app.ViewModel.PreprocessingUserInteractions.DialogData.Items;
			Check.That(userQueryItems.Count).IsEqualTo(2);
			Check.That(userQueryItems.Any(x => x.Title.Contains("Microsoft\\XmlWriterTraceListener") && x.Title.Contains("XmlWriterTraceListenerAndTextWriterTraceListener.zip\\XmlWriterTraceListener1.xml"))).IsTrue();
			Check.That(userQueryItems.Any(x => x.Title.Contains("Microsoft\\TextWriterTraceListener") && x.Title.Contains("XmlWriterTraceListenerAndTextWriterTraceListener.zip\\TextWriterTraceListener.log"))).IsTrue();
			Check.That(preprocTask.IsCompleted).IsFalse();

			app.ViewModel.PreprocessingUserInteractions.OnCloseDialog(accept: true);
			await preprocTask;
		}

		[IntegrationTest]
		public async Task CanQuitAppWhileHavingActivePreprocessingUserInteraction(TestAppInstance app)
		{
			var preprocTask = app.EmulateUrlDragAndDrop(app.Samples.GetSampleAsUri("XmlWriterTraceListenerAndTextWriterTraceListener.zip").ToString());

			await app.WaitFor(() => app.ViewModel.PreprocessingUserInteractions.DialogData != null);

			Check.That(app.ViewModel.PreprocessingUserInteractions.DialogData.Items.Count).IsEqualTo(2);

			await app.Dispose();
		}

		[IntegrationTest]
		public async Task OpeningTheSameLogTwiceHasNoEffect(TestAppInstance app)
		{
			await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
			await app.WaitFor(() => IsXmlWriterTraceListenerLogIsLoaded(app));

			Check.That(app.Model.LogSourcesManager.Items.Count()).IsEqualTo(1);

			await app.EmulateFileDragAndDrop(await app.Samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
			Check.That(app.Model.LogSourcesManager.Items.Count()).IsEqualTo(1);
		}

		[IntegrationTest]
		public async Task CanQuitAppWhilePreprocessingIsActive(TestAppInstance app)
		{
			var downloadingPreprocessing = app.EmulateUrlDragAndDrop(app.Samples.GetSampleAsUri("chrome_debug_1.log").ToString());

			await app.Dispose();

			Check.That(downloadingPreprocessing.IsFaulted).IsTrue();
			var webEx = downloadingPreprocessing.Exception.InnerException as System.Net.WebException;
			Check.That(webEx).IsNotNull();
			Check.That(webEx.Status).IsEqualTo(System.Net.WebExceptionStatus.RequestCanceled);
		}
	}
}

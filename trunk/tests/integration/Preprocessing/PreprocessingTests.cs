using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Preprocessing;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace LogJoint.Tests.Integration
{
	[TestFixture]
	class PreprocessingTests
	{
		readonly SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;

		[SetUp]
		public async Task BeforeEach()
		{
			app = await TestAppInstance.Create();
		}

		[TearDown]
		public async Task AfterEach()
		{
			await app.Dispose();
		}

		bool IsXmlWriterTraceListenerLogIsLoaded()
		{
			var viewLines = app.ViewModel.LoadedMessagesLogViewer.ViewLines;
			return !viewLines.IsEmpty && viewLines[0].TextLineValue == "File cannot be open which means that it was handled";
		}

		[Test]
		public async Task CanLoadAndDetectFormatOfLocalLog()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);
			});
		}

		[Test]
		public async Task CanDownloadAndDetectFormatOfLogFromTheWeb()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListener1.xml").ToString());
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);
			});
		}

		[Test]
		public async Task CanDownloadZipExtractAndFindKnownLogFormatInArchive()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListener1AndImage.zip").ToString());
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);
			});
		}

		[Test]
		public async Task CanOpenPasswordProtectedZipExtractAndFindKnownLogFormatInArchive()
		{
			app.Mocks.CredentialsCache.QueryCredentials(
				Arg.Is<Uri>(v => v.ToString().Contains("XmlWriterTraceListener1AndImage.PasswordProtected.zip")), null)
				.ReturnsForAnyArgs(new System.Net.NetworkCredential("", "Pa$$w0rd"));
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1AndImage.PasswordProtected.zip"));
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);
			});
		}

		[Test]
		public async Task DisplaysProgressDuringUnzipping()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				var preprocessorTask = app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("network_trace_with_keys_1.as_pdml.zip"));
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
			});
		}

		[Test]
		public async Task UnzippingCanBeCancelled()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				var preprocessorTask = app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("network_trace_with_keys_1.as_pdml.zip"));
				await app.WaitFor(() => (app.ViewModel.SourcesList.RootItem.Children.ElementAtOrDefault(0)?.ToString() ?? "").Contains("Unpacking"));
				app.ViewModel.SourcesList.OnSelectAllShortcutPressed();
				app.Mocks.AlertPopup.ShowPopup(null, null, UI.Presenters.AlertFlags.None).ReturnsForAnyArgs(
					UI.Presenters.AlertFlags.Yes);
				var stopwatch = Stopwatch.StartNew();
				app.ViewModel.SourcesList.OnDeleteButtonPressed();
				await app.WaitFor(() => app.ViewModel.SourcesList.RootItem.Children.Count == 0);
				stopwatch.Stop();
				Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
				Assert.IsTrue(preprocessorTask.IsCanceled);
			});
		}

		[Test]
		public async Task CanExtractGZippedLog()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml.gz"));
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);
			});
		}


		[Test]
		public async Task CanDownloadZipExtractFindManyKnownLogsAndAskUserWhatToOpen()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				var preprocTask = app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListenerAndTextWriterTraceListener.zip").ToString());

				await app.WaitFor(() => app.ViewModel.PreprocessingUserInteractions.DialogData != null);

				var userQueryItems = app.ViewModel.PreprocessingUserInteractions.DialogData.Items;
				Assert.AreEqual(2, userQueryItems.Count);
				Assert.IsTrue(userQueryItems.Any(x => x.Title.Contains("Microsoft\\XmlWriterTraceListener") && x.Title.Contains("XmlWriterTraceListenerAndTextWriterTraceListener.zip\\XmlWriterTraceListener1.xml")));
				Assert.IsTrue(userQueryItems.Any(x => x.Title.Contains("Microsoft\\TextWriterTraceListener") && x.Title.Contains("XmlWriterTraceListenerAndTextWriterTraceListener.zip\\TextWriterTraceListener.log")));
				Assert.IsFalse(preprocTask.IsCompleted);

				app.ViewModel.PreprocessingUserInteractions.OnCloseDialog(accept: true);
				await preprocTask;
			});
		}

		[Test]
		public async Task CanQuitAppWhileHavingActivePreprocessingUserInteraction()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				var preprocTask = app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListenerAndTextWriterTraceListener.zip").ToString());

				await app.WaitFor(() => app.ViewModel.PreprocessingUserInteractions.DialogData != null);

				Assert.AreEqual(2, app.ViewModel.PreprocessingUserInteractions.DialogData.Items.Count);

				await app.Dispose();
			});
		}

		[Test]
		public async Task OpeningTheSameLogTwiceHasNoEffect()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);

				Assert.AreEqual(1, app.Model.LogSourcesManager.Items.Count());

				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("XmlWriterTraceListener1.xml"));
				Assert.AreEqual(1, app.Model.LogSourcesManager.Items.Count());
			});
		}

		[Test]
		public async Task CanQuitAppWhilePreprocessingIsActive()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				var downloadingPreprocessing = app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("chrome_debug_1.log").ToString());

				await app.Dispose();

				Assert.IsTrue(downloadingPreprocessing.IsFaulted);
				var webEx = downloadingPreprocessing.Exception.InnerException as System.Net.WebException;
				Assert.IsNotNull(webEx);
				Assert.AreEqual(System.Net.WebExceptionStatus.RequestCanceled, webEx.Status);
			});
		}
	}
}

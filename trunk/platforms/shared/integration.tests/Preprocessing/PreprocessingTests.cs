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

				return 0;
			});
		}

		[Test]
		public async Task CanDownloadAndDetectFormatOfLogFromTheWeb()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListener1.xml").ToString());
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);

				return 0;
			});
		}

		[Test]
		public async Task CanDownloadZipExtractAndFindKnownLogFormatInArchive()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListener1AndImage.zip").ToString());
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);

				return 0;
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

				return 0;
			});
		}
	}
}

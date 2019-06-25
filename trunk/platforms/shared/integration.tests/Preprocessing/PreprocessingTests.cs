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
		public async Task CanDownloadZipExtractAndFindKnownLogFormatsInArchive()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateUrlDragAndDrop(samples.GetSampleAsUri("XmlWriterTraceListener1AndImage.zip").ToString());
				await app.WaitFor(IsXmlWriterTraceListenerLogIsLoaded);

				return 0;
			});
		}
	}
}

using System;
using LogJoint.UI.Presenters.LogViewer;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;

namespace LogJoint.UI.Presenters.Tests
{
	[TestFixture]
	public class ScreenBufferTests
	{
		CancellationToken cancel = CancellationToken.None;
		IBookmarksFactory bmks = new BookmarksFactory();

		static DummyModel.DummySource CreateTestSource(int messageSize = 123, int linesPerMessage = 1, int messagesCount = 10)
		{
			var src = new DummyModel.DummySource();
			for (var i = 0; i < messagesCount; ++i)
			{
				var txt = new StringBuilder();
				for (var ln = 0; ln < linesPerMessage; ++ln)
					txt.AppendFormat("{2}{0}-ln_{1}", i, ln, ln > 0 ? Environment.NewLine : "");
				src.messages.Add(new Content(i * messageSize, (i + 1) * messageSize, null,
					new MessageTimestamp(new DateTime(2016, 1, 1).AddMilliseconds(i)), new StringSlice(txt.ToString()), SeverityFlag.Info));
			}
			return src;
		}

		[Test]
		public async Task BufferPositionGetterTest_1()
		{
			var src = CreateTestSource();

			IScreenBuffer screenBuffer = new ScreenBuffer(3);
			await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.Nowhere, cancel);

			await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[0], 0), BookmarkLookupMode.ExactMatch, cancel);
			Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);

			await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[9], 0), BookmarkLookupMode.ExactMatch, cancel);
			Assert.AreEqual(1.0, screenBuffer.BufferPosition, 1e-3);

			await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[3], 0), BookmarkLookupMode.ExactMatch, cancel);
			screenBuffer.TopLineScrollValue = 0.5;
			Assert.AreEqual(0.5, screenBuffer.BufferPosition, 1e-3);
		}

		[Test]
		public async Task BufferPositionGetterTest_2()
		{
			var src = CreateTestSource(messagesCount: 2);

			IScreenBuffer screenBuffer = new ScreenBuffer(1);
			await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.SourcesBegin, cancel);

			Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);

			screenBuffer.TopLineScrollValue = 0.5;
			Assert.AreEqual(0.5, screenBuffer.BufferPosition, 1e-3);

			await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[1], 0), BookmarkLookupMode.ExactMatch, cancel);
			screenBuffer.TopLineScrollValue = 0;
			Assert.AreEqual(1, screenBuffer.BufferPosition, 1e-3);
		}

		[Test]
		public async Task BufferPositionGetterTest_3()
		{
			var src = CreateTestSource(messagesCount: 1, linesPerMessage: 2);

			IScreenBuffer screenBuffer = new ScreenBuffer(1);
			await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.SourcesBegin, cancel);

			Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);

			screenBuffer.TopLineScrollValue = 0.5;
			Assert.AreEqual(0.5, screenBuffer.BufferPosition, 1e-3);

			await screenBuffer.MoveToStreamsEnd(cancel);
			Assert.AreEqual(1, screenBuffer.BufferPosition, 1e-3);
		}

		[Test]
		public async Task BufferPositionGetterTest_4()
		{
			var src = CreateTestSource(messagesCount: 1, linesPerMessage: 3);

			IScreenBuffer screenBuffer = new ScreenBuffer(2);
			await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.SourcesBegin, cancel);

			Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);

			screenBuffer.TopLineScrollValue = 0.5;
			Assert.AreEqual(0.5, screenBuffer.BufferPosition, 1e-3);

			screenBuffer.TopLineScrollValue = 0.2;
			Assert.AreEqual(0.2, screenBuffer.BufferPosition, 1e-3);

			screenBuffer.TopLineScrollValue = 0.7;
			Assert.AreEqual(0.7, screenBuffer.BufferPosition, 1e-3);

			await screenBuffer.MoveToStreamsEnd(cancel);
			Assert.AreEqual(1, screenBuffer.BufferPosition, 1e-3);
		}

		[Test]
		//[Ignore("")]
		public async Task BufferPositionGetterTest_5()
		{
			var src = CreateTestSource(linesPerMessage: 6);

			IScreenBuffer screenBuffer = new ScreenBuffer(3);
			await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.Nowhere, cancel);

			await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[0], 0), BookmarkLookupMode.ExactMatch, cancel);
			Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);

			await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[9], 5), BookmarkLookupMode.ExactMatch, cancel);
			Assert.AreEqual(1.0, screenBuffer.BufferPosition, 1e-3);

			await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[3], 0), BookmarkLookupMode.ExactMatch, cancel);
			Assert.AreEqual(0.315, screenBuffer.BufferPosition, 1e-3);
		}

		[Test]
		public async Task BufferPositionGetterTest_6()
		{
			var src = CreateTestSource(messagesCount: 1, linesPerMessage: 11);

			IScreenBuffer screenBuffer = new ScreenBuffer(10);
			await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.SourcesBegin,cancel);

			screenBuffer.TopLineScrollValue = 0.5;
			Assert.AreEqual(0.5, screenBuffer.BufferPosition, 1e-3);

			screenBuffer.TopLineScrollValue = 0.2;
			Assert.AreEqual(0.2, screenBuffer.BufferPosition, 1e-3);

			screenBuffer.TopLineScrollValue = 0.7;
			Assert.AreEqual(0.7, screenBuffer.BufferPosition, 1e-3);
		}

		async Task TestPositionSetter(int messagesCount, int linesPerMessage, float viewSize)
		{
			Func<bool, Task> testCore = async disableSingleLogPositioningOptimization =>
			{
				var src = CreateTestSource(messagesCount: messagesCount, linesPerMessage: linesPerMessage);

				IScreenBuffer screenBuffer = new ScreenBuffer(viewSize,
					disableSingleLogPositioningOptimization: disableSingleLogPositioningOptimization);
				await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.Nowhere, cancel);

				await screenBuffer.MoveToPosition(0, cancel);
				Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);

				await screenBuffer.MoveToPosition(0.5, cancel);
				Assert.AreEqual(0.5, screenBuffer.BufferPosition, 1e-3);

				await screenBuffer.MoveToPosition(0.51, cancel);
				Assert.AreEqual(0.51, screenBuffer.BufferPosition, 1e-3);

				await screenBuffer.MoveToPosition(0.2, cancel);
				Assert.AreEqual(0.2, screenBuffer.BufferPosition, 1e-3);

				await screenBuffer.MoveToPosition(0.78, cancel);
				Assert.AreEqual(0.78, screenBuffer.BufferPosition, 1e-3);

				await screenBuffer.MoveToPosition(1.0, cancel);
				Assert.AreEqual(1.0, screenBuffer.BufferPosition, 1e-3);
			};
			await testCore(false);
			await testCore(true);
		}

		[Test]
		public async Task BufferPositionSetterTest_1()
		{
			await TestPositionSetter(messagesCount: 10, linesPerMessage: 1, viewSize: 3);
		}

		[Test]
		public async Task BufferPositionSetterTest_2()
		{
			await TestPositionSetter(messagesCount: 2, viewSize: 1f, linesPerMessage: 1);
		}

		[Test]
		public async Task BufferPositionSetterTest_3()
		{
			await TestPositionSetter(messagesCount: 100, viewSize: 99, linesPerMessage: 1);
		}

		[Test]
		public async Task BufferPositionSetterTest_4()
		{
			await TestPositionSetter(messagesCount: 34, viewSize: 12, linesPerMessage: 11);
		}

		[Test]
		public async Task BufferPositionSetterTest_5()
		{
			await TestPositionSetter(messagesCount: 2, viewSize: 22, linesPerMessage: 35);
		}

		[Test]
		[Ignore("SUT needs fixing to handle such case")]
		public async Task BufferPositionSetterTest_6()
		{
			var src = CreateTestSource(messagesCount: 1);

			IScreenBuffer screenBuffer = new ScreenBuffer(10);
			await screenBuffer.SetSources(new[] { src }, DefaultBufferPosition.Nowhere, cancel);

			await screenBuffer.MoveToPosition(0, cancel);
			Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);
			Assert.AreEqual(0, screenBuffer.TopLineScrollValue, 1e-3);

			await screenBuffer.MoveToPosition(0.3, cancel);
			Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);
			Assert.AreEqual(0, screenBuffer.TopLineScrollValue, 1e-3);
		}
	}
}

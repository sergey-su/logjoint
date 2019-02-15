using System;
using System.Linq;
using LogJoint.UI.Presenters.LogViewer;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;
using NSubstitute;

namespace LogJoint.UI.Presenters.Tests.ScreenBufferTests
{
	[TestFixture]
	public class ScreenBufferTests
	{
		static readonly CancellationToken cancel = CancellationToken.None;
		static readonly IBookmarksFactory bmks = new BookmarksFactory();

		static DummyModel.DummySource CreateTestSource(int messageSize = 123, int linesPerMessage = 1, int messagesCount = 10, int timestampShiftMillis = 0, string messagesPrefix = "")
		{
			var messagesThread = Substitute.For<IThread>();
			var linesSource = new DummyModel.DummySource()
			{
				logSourceHint = messagesThread.LogSource
			};
			var connectionId = string.IsNullOrEmpty(messagesPrefix) ? messagesThread.GetHashCode().ToString("x") : messagesPrefix;
			messagesThread.LogSource.ConnectionId.Returns(connectionId);
			messagesThread.LogSource.Provider.ConnectionId.Returns(connectionId);
			for (var i = 0; i < messagesCount; ++i)
			{
				var txt = new StringBuilder();
				for (var ln = 0; ln < linesPerMessage; ++ln)
					txt.AppendFormat("{2}{3}{0}-ln_{1}", i, ln, ln > 0 ? Environment.NewLine : "", messagesPrefix);
				linesSource.messages.Add(new Content(
					i * messageSize, (i + 1) * messageSize,
					messagesThread,
					new MessageTimestamp(new DateTime(2016, 1, 1).AddMilliseconds(timestampShiftMillis + i)),
					new StringSlice(txt.ToString()),
					SeverityFlag.Info)
				);
			}
			return linesSource;
		}

		static void VerifyMessages(IScreenBuffer screenBuffer, string expected, double? expectedTopLineScroll = null)
		{
			var actual = string.Join("\r\n",
				screenBuffer.Messages.Select(m => m.Message.TextAsMultilineText.GetNthTextLine(m.TextLineIndex)));
			Assert.AreEqual(StringUtils.NormalizeLinebreakes(expected.Replace("\t", "")), actual);
			if (expectedTopLineScroll != null)
				Assert.AreEqual(expectedTopLineScroll.Value, screenBuffer.TopLineScrollValue, 1e-3);
		}

		static void VerifyIsEmpty(IScreenBuffer screenBuffer)
		{
			Assert.AreEqual(0, screenBuffer.Sources.Count());
			Assert.AreEqual(0, screenBuffer.Messages.Count);
		}

		[TestFixture]
		public class RandomPositioniong
		{
			[Test]
			public async Task BufferPositionGetterTest_1()
			{
				var src = CreateTestSource();

				IScreenBuffer screenBuffer = new ScreenBuffer(3);
				await screenBuffer.SetSources(new[] { src }, cancel);

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
				await screenBuffer.SetSources(new[] { src }, cancel);
				await screenBuffer.MoveToStreamsBegin(cancel);

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
				await screenBuffer.SetSources(new[] { src }, cancel);
				await screenBuffer.MoveToStreamsBegin(cancel);

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
				await screenBuffer.SetSources(new[] { src }, cancel);
				await screenBuffer.MoveToStreamsBegin(cancel);

				Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);

				screenBuffer.TopLineScrollValue = 0.5;
				Assert.AreEqual(0.5, screenBuffer.BufferPosition, 1e-3);

				screenBuffer.TopLineScrollValue = 0.2;
				Assert.AreEqual(0.2, screenBuffer.BufferPosition, 1e-3);

				screenBuffer.TopLineScrollValue = 0.7;
				// Assert.AreEqual(0.7, screenBuffer.BufferPosition, 1e-3); todo

				await screenBuffer.MoveToStreamsEnd(cancel);
				Assert.AreEqual(1, screenBuffer.BufferPosition, 1e-3);
			}

			[Test]
			//[Ignore("")]
			public async Task BufferPositionGetterTest_5()
			{
				var src = CreateTestSource(linesPerMessage: 6);

				IScreenBuffer screenBuffer = new ScreenBuffer(3);
				await screenBuffer.SetSources(new[] { src }, cancel);

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
				await screenBuffer.SetSources(new[] { src }, cancel);
				await screenBuffer.MoveToStreamsBegin(cancel);

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
					await screenBuffer.SetSources(new[] { src }, cancel);

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
				await screenBuffer.SetSources(new[] { src }, cancel);

				await screenBuffer.MoveToPosition(0, cancel);
				Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);
				Assert.AreEqual(0, screenBuffer.TopLineScrollValue, 1e-3);

				await screenBuffer.MoveToPosition(0.3, cancel);
				Assert.AreEqual(0, screenBuffer.BufferPosition, 1e-3);
				Assert.AreEqual(0, screenBuffer.TopLineScrollValue, 1e-3);
			}
		}

		[TestFixture]
		class SetSources
		{
			[TestFixture]
			class OneMessagesSource
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 20);
					screenBuffer = new ScreenBuffer(5.3);
					await screenBuffer.SetSources(new[] { src }, cancel);
				}


				[Test]
				public void SourceIsAddedButNothingIsLoaded()
				{
					Assert.AreSame(src, screenBuffer.Sources.Single().Source);
					Assert.AreEqual(0, screenBuffer.Sources.Single().Begin);
					Assert.AreEqual(0, screenBuffer.Sources.Single().End);
					Assert.AreEqual(0, screenBuffer.Messages.Count);
				}

				[Test]
				public async Task SourceCanBeDeleted()
				{
					await screenBuffer.SetSources(new IMessagesSource[0], cancel);
					VerifyIsEmpty(screenBuffer);
				}

				[Test]
				public async Task CanBeLoadedFromBeginning()
				{
					await screenBuffer.MoveToStreamsBegin(cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_0
						1-ln_0
						2-ln_0
						3-ln_0
						4-ln_0
						5-ln_0", 0);
				}

				[Test]
				public async Task CanBeLoadedFromEnd()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					VerifyMessages(screenBuffer,
						@"14-ln_0
						15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0
						19-ln_0", 0.7);
				}

				[Test]
				public async Task CanLoadBookmark()
				{
					await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[9], 0), BookmarkLookupMode.ExactMatch, cancel);
					VerifyMessages(screenBuffer,
						@"9-ln_0
						10-ln_0
						11-ln_0
						12-ln_0
						13-ln_0
						14-ln_0", 0);
				}

				[Test]
				public async Task CanBeDeletedAfterLoading()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetSources(new IMessagesSource[0], cancel);
					VerifyIsEmpty(screenBuffer);
				}
			};

			[TestFixture]
			class TwoNonOverlappingMessagesSources
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src1, src2;

				[SetUp]
				public async Task Setup()
				{
					src1 = CreateTestSource(messagesCount: 18, messagesPrefix: "a:");
					src2 = CreateTestSource(messagesCount: 15, timestampShiftMillis: 50, messagesPrefix: "b:");
					screenBuffer = new ScreenBuffer(5.3);
					await screenBuffer.SetSources(new[] { src1, src2 }, cancel);
				}

				[Test]
				public void SourceIsAddedButNothingIsLoaded()
				{
					Assert.AreSame(src1, screenBuffer.Sources.ElementAt(0).Source);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(0).Begin);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(0).End);
					Assert.AreSame(src2, screenBuffer.Sources.ElementAt(1).Source);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(1).Begin);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(1).End);
					Assert.AreEqual(0, screenBuffer.Messages.Count);
				}

				[Test]
				public async Task LoadingStreamsEndTakesMessagesFromB()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					VerifyMessages(screenBuffer,
						@"b:9-ln_0
						b:10-ln_0
						b:11-ln_0
						b:12-ln_0
						b:13-ln_0
						b:14-ln_0", 0.7);
				}

				[Test]
				public async Task WhenLoadedStreamsEndAndDeletedA_MessagesShouldStayTheSame()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetSources(new[] { src2 }, cancel);
					VerifyMessages(screenBuffer,
						@"b:9-ln_0
						b:10-ln_0
						b:11-ln_0
						b:12-ln_0
						b:13-ln_0
						b:14-ln_0", 0.7);
				}

				[Test]
				public async Task WhenLoadedStreamsEndAndDeletedB_ShouldLoadMessagesFromA()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetSources(new[] { src1 }, cancel);
					VerifyMessages(screenBuffer,
						@"a:12-ln_0
						a:13-ln_0
						a:14-ln_0
						a:15-ln_0
						a:16-ln_0
						a:17-ln_0", 0.7);
				}
			}

			[TestFixture]
			class TwoOverlappingMessagesSources
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src1, src2;

				[SetUp]
				public async Task Setup()
				{
					src1 = CreateTestSource(messagesCount: 20, messagesPrefix: "a:");
					src2 = CreateTestSource(messagesCount: 18, timestampShiftMillis: 7, messagesPrefix: "b:");
					screenBuffer = new ScreenBuffer(6.3);
					await screenBuffer.SetSources(new[] { src1, src2 }, cancel);
				}

				[Test]
				public void SourceIsAddedButNothingIsLoaded()
				{
					Assert.AreSame(src1, screenBuffer.Sources.ElementAt(0).Source);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(0).Begin);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(0).End);
					Assert.AreSame(src2, screenBuffer.Sources.ElementAt(1).Source);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(1).Begin);
					Assert.AreEqual(0, screenBuffer.Sources.ElementAt(1).End);
					Assert.AreEqual(0, screenBuffer.Messages.Count);
				}

				[Test]
				public async Task LoadStreamsEnd()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					VerifyMessages(screenBuffer,
						@"a:19-ln_0
						b:12-ln_0
						b:13-ln_0
						b:14-ln_0
						b:15-ln_0
						b:16-ln_0
						b:17-ln_0", 0.7);
				}

				[Test]
				public async Task LoadStreamsEndAndDeleteA()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetSources(new[] { src2 }, cancel);
					VerifyMessages(screenBuffer,
						@"b:11-ln_0
						b:12-ln_0
						b:13-ln_0
						b:14-ln_0
						b:15-ln_0
						b:16-ln_0
						b:17-ln_0", 0.7);
				}

				[Test]
				public async Task LoadStreamsEndAndDeleteB()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetSources(new[] { src1 }, cancel);
					VerifyMessages(screenBuffer,
						@"a:13-ln_0
						a:14-ln_0
						a:15-ln_0
						a:16-ln_0
						a:17-ln_0
						a:18-ln_0
						a:19-ln_0", 0.7);
				}
			}
		}

		[TestFixture]
		class ViewSize
		{
			[TestFixture]
			class ViewSizeAndMultilineMessages
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 2, linesPerMessage: 6);
					screenBuffer = new ScreenBuffer(6.8);
					await screenBuffer.SetSources(new[] { src }, cancel);
				}

				[Test]
				public async Task CanLoadAtEnd()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_5
						1-ln_0
						1-ln_1
						1-ln_2
						1-ln_3
						1-ln_4
						1-ln_5", 0.2);
				}

				[Test]
				public async Task CanLoadAtEndAndEnlargeView()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetViewSize(7.6, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_4
						0-ln_5
						1-ln_0
						1-ln_1
						1-ln_2
						1-ln_3
						1-ln_4
						1-ln_5", 0.4);
				}

				[Test]
				public async Task CanLoadAtMiddle()
				{
					await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[0], 2), BookmarkLookupMode.ExactMatch, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_2
						0-ln_3
						0-ln_4
						0-ln_5
						1-ln_0
						1-ln_1
						1-ln_2", 0);
				}

				[Test]
				public async Task CanLoadAtMiddleAndEnlargeView()
				{
					await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[0], 2), BookmarkLookupMode.ExactMatch, cancel);
					await screenBuffer.SetViewSize(7.6, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_2
						0-ln_3
						0-ln_4
						0-ln_5
						1-ln_0
						1-ln_1
						1-ln_2
						1-ln_3", 0);
				}

				[Test]
				public async Task CanLoadAtMiddleAndEnlargeViewUpToEnd()
				{
					await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[0], 4), BookmarkLookupMode.ExactMatch, cancel);
					await screenBuffer.SetViewSize(8.6, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_3
						0-ln_4
						0-ln_5
						1-ln_0
						1-ln_1
						1-ln_2
						1-ln_3
						1-ln_4
						1-ln_5", 0.4);
				}

				[Test]
				public async Task CanLoadAtMiddleAndShrinkView()
				{
					await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[0], 2), BookmarkLookupMode.ExactMatch, cancel);
					await screenBuffer.SetViewSize(4.6, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_2
						0-ln_3
						0-ln_4
						0-ln_5
						1-ln_0", 0);
				}

				[Test]
				public async Task CanLoadAtEndAndEnlargeViewToFitAllLines()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetViewSize(15, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_0
						0-ln_1
						0-ln_2
						0-ln_3
						0-ln_4
						0-ln_5
						1-ln_0
						1-ln_1
						1-ln_2
						1-ln_3
						1-ln_4
						1-ln_5", 0);
				}

				[Test]
				public async Task CanLoadAtEndAndShrinkView()
				{
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetViewSize(5.1, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_5
						1-ln_0
						1-ln_1
						1-ln_2
						1-ln_3
						1-ln_4", 0.2);
				}

				[Test]
				public async Task CanLoadAtEndAndEnlargeView_ViewSizeSmallerThanNrOfLines()
				{
					await screenBuffer.SetViewSize(1, cancel);
					await screenBuffer.MoveToStreamsEnd(cancel);
					await screenBuffer.SetViewSize(2, cancel);
					VerifyMessages(screenBuffer,
						@"1-ln_4
						1-ln_5", 0.0);
				}

			}

			[TestFixture]
			class ViewSizeGreaterThanNrOfLines
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 7);
					screenBuffer = new ScreenBuffer(9.2);
					await screenBuffer.SetSources(new[] { src }, cancel);
					await screenBuffer.MoveToStreamsEnd(cancel);
				}

				[Test]
				public void AllMessagesShouleBeLoaded()
				{
					VerifyMessages(screenBuffer,
						@"0-ln_0
						1-ln_0
						2-ln_0
						3-ln_0
						4-ln_0
						5-ln_0
						6-ln_0", 0);
				}


				[Test]
				public async Task CanMakeViewLarger()
				{
					await screenBuffer.SetViewSize(11.9, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_0
						1-ln_0
						2-ln_0
						3-ln_0
						4-ln_0
						5-ln_0
						6-ln_0", 0);
				}

				[Test]
				public async Task CanMakeViewSmaller()
				{
					await screenBuffer.SetViewSize(4.4, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_0
						1-ln_0
						2-ln_0
						3-ln_0
						4-ln_0", 0);
				}

				[Test]
				public async Task CanResizeViewBackAndForth()
				{
					await screenBuffer.SetViewSize(4.4, cancel);
					await screenBuffer.SetViewSize(10, cancel);
					VerifyMessages(screenBuffer,
						@"0-ln_0
						1-ln_0
						2-ln_0
						3-ln_0
						4-ln_0
						5-ln_0
						6-ln_0", 0);
				}
			}

			[TestFixture]
			class NrOfLinesGreaterThanViewSize
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 20);
					screenBuffer = new ScreenBuffer(4.6);
					await screenBuffer.SetSources(new[] { src }, cancel);
					await screenBuffer.MoveToStreamsEnd(cancel);
				}

				[Test]
				public void MessagesLoaded()
				{
					VerifyMessages(screenBuffer,
						@"15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0
						19-ln_0", 0.4);
				}

				[Test]
				public async Task CanEnlargeView()
				{
					await screenBuffer.SetViewSize(7.6, cancel);
					VerifyMessages(screenBuffer,
						@"12-ln_0
						13-ln_0
						14-ln_0
						15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0
						19-ln_0", 0.4);
				}

				[Test]
				public async Task CanEnlargeViewUpToWholeViewSize()
				{
					await screenBuffer.SetViewSize(9, cancel);
					VerifyMessages(screenBuffer,
						@"11-ln_0
						12-ln_0
						13-ln_0
						14-ln_0
						15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0
						19-ln_0", 0);
				}

				[Test]
				public async Task CanMakeViewSmaller()
				{
					await screenBuffer.SetViewSize(3.2, cancel);
					VerifyMessages(screenBuffer,
						@"15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0", 0.4);
				}
			}
		}

		[TestFixture]
		class Bookmark
		{
			[TestFixture]
			class SingleSourceBookmarking
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 10, linesPerMessage: 3);
					screenBuffer = new ScreenBuffer(4.4);
					await screenBuffer.SetSources(new[] { src }, cancel);
				}

				[Test]
				public async Task CanLoadExactMessageInMiddleOfLog()
				{
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[2], 2), BookmarkLookupMode.ExactMatch, cancel));
					VerifyMessages(screenBuffer,
						@"2-ln_2
						3-ln_0
						3-ln_1
						3-ln_2
						4-ln_0", 0);
				}

				[Test]
				public async Task CanLoadExactMessageInMiddleOfLog_WithScrollingToTopMiddleOfScreen()
				{
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[2], 2), BookmarkLookupMode.ExactMatch | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, cancel));
					VerifyMessages(screenBuffer,
						@"2-ln_0
						2-ln_1
						2-ln_2
						3-ln_0
						3-ln_1", 0.3);
				}

				[Test]
				public async Task CanLoadExactMessageAtEndOfLog()
				{
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[8], 2), BookmarkLookupMode.ExactMatch, cancel));
					VerifyMessages(screenBuffer,
						@"8-ln_1
						8-ln_2
						9-ln_0
						9-ln_1
						9-ln_2", 0.6);
				}

				[Test]
				public async Task CanLoadExactMessageAtEndOfLog_WithScrollingToTopMiddleOfScreen()
				{
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[9], 1), BookmarkLookupMode.ExactMatch | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, cancel));
					VerifyMessages(screenBuffer,
						@"8-ln_1
						8-ln_2
						9-ln_0
						9-ln_1
						9-ln_2", 0.6);
				}

				[Test]
				public async Task CanLoadExactMessageWhenViewIsLargerThanLog()
				{
					src = CreateTestSource(messagesCount: 2, linesPerMessage: 3);
					screenBuffer = new ScreenBuffer(8.2);
					await screenBuffer.SetSources(new[] { src }, cancel);

					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[1], 2), BookmarkLookupMode.ExactMatch, cancel));
					VerifyMessages(screenBuffer,
						@"0-ln_0
						0-ln_1
						0-ln_2
						1-ln_0
						1-ln_1
						1-ln_2", 0);
				}

				[Test]
				public async Task CanLoadExactMessageWhenViewIsLargerThanLogg_WithScrollingToTopMiddleOfScreen()
				{
					src = CreateTestSource(messagesCount: 2, linesPerMessage: 3);
					screenBuffer = new ScreenBuffer(8.2);
					await screenBuffer.SetSources(new[] { src }, cancel);

					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src.messages.Items[1], 2), BookmarkLookupMode.ExactMatch | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, cancel));
					VerifyMessages(screenBuffer,
						@"0-ln_0
						0-ln_1
						0-ln_2
						1-ln_0
						1-ln_1
						1-ln_2", 0);
				}

				[Test]
				public async Task WontMoveToNonExistingMessage()
				{
					await screenBuffer.MoveToStreamsBegin(cancel);

					 Assert.IsFalse(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(
						 src.messages.Items[1].Time, src.messages.Items[1].GetLogSource().ConnectionId, 1000000, 2), BookmarkLookupMode.ExactMatch, cancel));

					// must stay in old state
					VerifyMessages(screenBuffer,
						@"0-ln_0
						0-ln_1
						0-ln_2
						1-ln_0
						1-ln_1", 0);
				}

				[Test]
				public async Task WontMoveToNonExistingLine()
				{
					await screenBuffer.MoveToStreamsBegin(cancel);

					Assert.IsFalse(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(
						src.messages.Items[1], 4), BookmarkLookupMode.ExactMatch, cancel));

					// must stay in old state
					VerifyMessages(screenBuffer,
						@"0-ln_0
						0-ln_1
						0-ln_2
						1-ln_0
						1-ln_1", 0);
				}

				[Test]
				public async Task CanLoadNereastMessageInMiddleOfLog()
				{
					var nearestMsg = src.messages.Items[2];
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(
						new MessageTimestamp(nearestMsg.Time.ToLocalDateTime().AddMilliseconds(0.5)), nearestMsg.GetConnectionId(), nearestMsg.Position + 5, 0
					), BookmarkLookupMode.FindNearestMessage, cancel));
					VerifyMessages(screenBuffer,
						@"3-ln_0
						3-ln_1
						3-ln_2
						4-ln_0
						4-ln_1", 0);
				}

				[Test]
				public async Task CanLoadNereastMessagAtBeginningOfLog()
				{
					var nearestMsg = src.messages.Items.First();
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(
						new MessageTimestamp(nearestMsg.Time.ToLocalDateTime().AddMilliseconds(-100)), nearestMsg.GetConnectionId(), 0, 0
					), BookmarkLookupMode.FindNearestMessage, cancel));
					VerifyMessages(screenBuffer,
						@"0-ln_0
						0-ln_1
						0-ln_2
						1-ln_0
						1-ln_1", 0);
				}

				[Test]
				public async Task CanLoadNereastMessagAtEndOfLog()
				{
					var nearestMsg = src.messages.Items.Last();
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(
						new MessageTimestamp(nearestMsg.Time.ToLocalDateTime().AddMilliseconds(100)), nearestMsg.GetConnectionId(), nearestMsg.Position + 100, 0
					), BookmarkLookupMode.FindNearestMessage, cancel));
					VerifyMessages(screenBuffer,
						@"8-ln_1
						8-ln_2
						9-ln_0
						9-ln_1
						9-ln_2", 0.6);
				}

			};

			[TestFixture]
			class OverlappingSourcesBookmarking
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src1, src2;

				[SetUp]
				public async Task Setup()
				{
					src1 = CreateTestSource(messagesCount: 10, linesPerMessage: 3, messagesPrefix: "a");
					src2 = CreateTestSource(messagesCount: 10, linesPerMessage: 2, messagesPrefix: "b");
					screenBuffer = new ScreenBuffer(4.4);
					await screenBuffer.SetSources(new[] { src1, src2 }, cancel);
				}

				[Test]
				public async Task CanLoadExactMessageInMiddleOfLog1()
				{
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src1.messages.Items[2], 2), BookmarkLookupMode.ExactMatch, cancel));
					VerifyMessages(screenBuffer,
						@"a2-ln_2
						b2-ln_0
						b2-ln_1
						a3-ln_0
						a3-ln_1", 0);
				}

				[Test]
				public async Task CanLoadExactMessageInMiddleOfLog2()
				{
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src2.messages.Items[2], 1), BookmarkLookupMode.ExactMatch, cancel));
					VerifyMessages(screenBuffer,
						@"b2-ln_1
						a3-ln_0
						a3-ln_1
						a3-ln_2
						b3-ln_0", 0);
				}

				[Test]
				public async Task CanLoadExactMessageInMiddleOfLog_WithScrollingToTopMiddleOfScreen()
				{
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(src2.messages.Items[2], 1), BookmarkLookupMode.ExactMatch | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, cancel));
					VerifyMessages(screenBuffer,
						@"a2-ln_2
						b2-ln_0
						b2-ln_1
						a3-ln_0
						a3-ln_1", 0.3);
				}

				[Test]
				public async Task CanLoadNereastMessageInMiddleOfLog()
				{
					var nearestMsg = src2.messages.Items[2];
					Assert.IsTrue(await screenBuffer.MoveToBookmark(bmks.CreateBookmark(
						new MessageTimestamp(nearestMsg.Time.ToLocalDateTime().AddMilliseconds(0.5)), nearestMsg.GetConnectionId(), nearestMsg.Position + 5, 0
					), BookmarkLookupMode.FindNearestMessage, cancel));
					VerifyMessages(screenBuffer,
						@"b3-ln_0
						b3-ln_1
						a4-ln_0
						a4-ln_1
						a4-ln_2", 0);
				}
			}
		}

		[TestFixture]
		class Shifting
		{
			[TestFixture]
			class SingleLineSrc
			{

				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 20);
					screenBuffer = new ScreenBuffer(6.8);
					await screenBuffer.SetSources(new[] { src }, cancel);
					await screenBuffer.MoveToStreamsEnd(cancel);
					VerifyMessages(screenBuffer,
						@"13-ln_0
						14-ln_0
						15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0
						19-ln_0", 0.2);
				}

				[Test]
				public async Task ShiftDownHasNoEffect()
				{
					Assert.AreEqual(0d, await screenBuffer.ShiftBy(7, cancel), 1e-3);
					VerifyMessages(screenBuffer,
						@"13-ln_0
						14-ln_0
						15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0
						19-ln_0", 0.2);
				}

				[Test]
				public async Task ShiftUpHasNoEffect()
				{
					await screenBuffer.MoveToStreamsBegin(cancel);
					Assert.AreEqual(0d, await screenBuffer.ShiftBy(-2, cancel), 1e-3);
					VerifyMessages(screenBuffer,
						@"0-ln_0
						1-ln_0
						2-ln_0
						3-ln_0
						4-ln_0
						5-ln_0
						6-ln_0", 0);
				}

				[Test]
				public async Task ShiftUp()
				{
					Assert.AreEqual(-4.4, await screenBuffer.ShiftBy(-4.4, cancel), 1e-3);
					VerifyMessages(screenBuffer,
						@"8-ln_0
						9-ln_0
						10-ln_0
						11-ln_0
						12-ln_0
						13-ln_0
						14-ln_0
						15-ln_0", 0.8);
				}

				[Test]
				public async Task ShiftUpLessThenOneLine()
				{
					Assert.AreEqual(-0.4, await screenBuffer.ShiftBy(-0.4, cancel), 1e-3);
					VerifyMessages(screenBuffer,
						@"12-ln_0
						13-ln_0
						14-ln_0
						15-ln_0
						16-ln_0
						17-ln_0
						18-ln_0
						19-ln_0", 0.8);
				}
			}

			[TestFixture]
			class MultiLineSrc
			{

				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 5, linesPerMessage: 10);
					screenBuffer = new ScreenBuffer(6.8);
					await screenBuffer.SetSources(new[] { src }, cancel);
					await screenBuffer.MoveToStreamsEnd(cancel);
					VerifyMessages(screenBuffer,
						@"4-ln_3
						4-ln_4
						4-ln_5
						4-ln_6
						4-ln_7
						4-ln_8
						4-ln_9", 0.2);
				}

				[Test]
				public async Task ShiftUpByLessThanView()
				{
					Assert.AreEqual(-4.1, await screenBuffer.ShiftBy(-4.1, cancel), 1e-3);
					VerifyMessages(screenBuffer,
						@"3-ln_9
						4-ln_0
						4-ln_1
						4-ln_2
						4-ln_3
						4-ln_4
						4-ln_5", 0.1);
				}

				[Test]
				public async Task ShiftUpByMoreThanView()
				{
					Assert.AreEqual(-10.1, await screenBuffer.ShiftBy(-10.1, cancel), 1e-3);
					VerifyMessages(screenBuffer,
						@"3-ln_3
						3-ln_4
						3-ln_5
						3-ln_6
						3-ln_7
						3-ln_8
						3-ln_9", 0.1);
				}
			}
		}

		[TestFixture]
		class MoveToTimestamp
		{
			[TestFixture]
			class SingleSource
			{
				IScreenBuffer screenBuffer;
				DummyModel.DummySource src;

				[SetUp]
				public async Task Setup()
				{
					src = CreateTestSource(messagesCount: 10, linesPerMessage: 5);
					screenBuffer = new ScreenBuffer(5.6);
					await screenBuffer.SetSources(new[] { src }, cancel);
				}

				[Test]
				public async Task CanFindMessageWithNearestTimeInTheMiddle1()
				{
					await screenBuffer.MoveToTimestamp(src.messages.Items[2].Time.ToLocalDateTime().AddMilliseconds(0.7), cancel);
					VerifyMessages(screenBuffer,
						@"2-ln_2
						2-ln_3
						2-ln_4
						3-ln_0
						3-ln_1
						3-ln_2
						3-ln_3", 0.7);
				}

				[Test]
				public async Task CanFindMessageWithNearestTimeInTheMiddle2()
				{
					await screenBuffer.MoveToTimestamp(src.messages.Items[2].Time.ToLocalDateTime().AddMilliseconds(0.2), cancel);
					VerifyMessages(screenBuffer,
						@"1-ln_2
						1-ln_3
						1-ln_4
						2-ln_0
						2-ln_1
						2-ln_2
						2-ln_3", 0.7);
				}

			}

			[TestFixture]
			class MultipleSources
			{

				IScreenBuffer screenBuffer;
				DummyModel.DummySource src1, src2;

				[SetUp]
				public async Task Setup()
				{
					src1 = CreateTestSource(messagesCount: 5, linesPerMessage: 2, messagesPrefix: "a");
					src2 = CreateTestSource(messagesCount: 5, linesPerMessage: 2, messagesPrefix: "b");
					screenBuffer = new ScreenBuffer(5.3);
					await screenBuffer.SetSources(new[] { src1, src2 }, cancel);
				}

				[Test]
				public async Task CanFindMessageWithNearestTimeInTheMiddle()
				{
					await screenBuffer.MoveToTimestamp(src1.messages.Items[2].Time.ToLocalDateTime().AddMilliseconds(0.7), cancel);
					VerifyMessages(screenBuffer,
						@"a2-ln_1
						b2-ln_0
						b2-ln_1
						a3-ln_0
						a3-ln_1
						b3-ln_0
						b3-ln_1", 0.85);
				}

				[Test]
				public async Task CanMoveToTimestampAtEndOfLog()
				{
					await screenBuffer.MoveToTimestamp(src1.messages.Items[4].Time.ToLocalDateTime().AddMilliseconds(-0.1), cancel);
					VerifyMessages(screenBuffer,
						@"a3-ln_1
						b3-ln_0
						b3-ln_1
						a4-ln_0
						a4-ln_1
						b4-ln_0
						b4-ln_1", 0.85);
				}

				[Test]
				public async Task CanMoveToTimestampAtBeginningOfLog()
				{
					await screenBuffer.MoveToTimestamp(src1.messages.Items[0].Time.ToLocalDateTime().AddMilliseconds(-0.7), cancel);
					VerifyMessages(screenBuffer,
						@"a0-ln_0
						a0-ln_1
						b0-ln_0
						b0-ln_1
						a1-ln_0
						a1-ln_1
						b1-ln_0", 0);
				}
			}
		}
	}
}

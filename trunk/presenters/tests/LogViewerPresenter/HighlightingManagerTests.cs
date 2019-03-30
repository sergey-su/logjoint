using System;
using System.Linq;
using LogJoint.UI.Presenters.LogViewer;
using System.Text;
using NUnit.Framework;
using NSubstitute;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Tests.HighlightingManagerTests
{
	[TestFixture]
	public class HighlightingManagerTests
	{
		IHighlightingManager highlightingManager;
		ISearchResultModel searchResultModel;
		IFiltersList highlightFilters;
		ISelectionManager selectionManager;
		IWordSelection wordSelection;
		bool isRawMessagesMode;
		int viewSize;
		IMessage msg1, msg2;

		[SetUp]
		public void BeforeEach()
		{
			searchResultModel = Substitute.For<ISearchResultModel>();
			highlightFilters = Substitute.For<IFiltersList>();
			selectionManager = Substitute.For<ISelectionManager>();
			wordSelection = Substitute.For<IWordSelection>();
			isRawMessagesMode = false;
			viewSize = 3;
			highlightFilters.FilteringEnabled.Returns(true);
			msg1 = new Message(0, 1, null, new MessageTimestamp(), StringSlice.Empty, SeverityFlag.Info);
			msg2 = new Message(0, 1, null, new MessageTimestamp(), StringSlice.Empty, SeverityFlag.Info);
		}

		void CreateHighlightingManager()
		{
			highlightingManager = new HighlightingManager(searchResultModel,
				() => isRawMessagesMode, () => viewSize, highlightFilters, selectionManager, wordSelection);
		}

		IFilter CreateFilter(FilterAction action, bool expectRawMessagesMode, bool enabed,
			params (IMessage, int?, Search.MatchedTextRange)[] matches)
		{
			var filter = Substitute.For<IFilter>();
			filter.Enabled.Returns(true);
			filter.Action.Returns(action);
			var processing = Substitute.For<IFilterBulkProcessing>();
			filter.StartBulkProcessing(expectRawMessagesMode, false).Returns(processing);
			foreach (var m in matches)
			{
				processing.Match(m.Item1, m.Item2).Returns(m.Item3);
			}
			return filter;
		}

		void VerifyRanges(IEnumerable<(int, int, FilterAction)> actual, params (int, int, FilterAction)[] expected)
		{
			CollectionAssert.AreEqual(expected, actual);
		}

		[TestFixture]
		public class HighlightingFiltersTests: HighlightingManagerTests
		{
			[SetUp]
			public new void BeforeEach()
			{
				var f1 = CreateFilter(FilterAction.Include, false, true, (msg1, null, new Search.MatchedTextRange(StringSlice.Empty, 3, 4, true)));
				highlightFilters.Items.Returns(ImmutableList.Create(f1));
			}

			[Test]
			public void HappyPath()
			{
				CreateHighlightingManager();
				VerifyRanges(highlightingManager.HighlightingFiltersHandler.GetHighlightingRanges(msg1, 0, 10),
					(3, 4, FilterAction.Include)
				);
			}
		};
	}
}
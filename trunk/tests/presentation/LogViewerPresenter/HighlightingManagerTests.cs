﻿using System.Linq;
using LogJoint.UI.Presenters.LogViewer;
using NUnit.Framework;
using NSubstitute;
using System.Collections.Immutable;
using System.Collections.Generic;
using LogJoint.Drawing;

namespace LogJoint.UI.Presenters.Tests.HighlightingManagerTests
{
    [TestFixture]
    public class HighlightingManagerTests
    {
        IHighlightingManager highlightingManager;
        ISearchResultModel searchResultModel;
        IFiltersList highlightFilters;
        ISelectionManager selectionManager;
        bool isRawMessagesMode;
        int viewSize;
        IMessage msg1, msg2;
        IMessage msgWithMultilineText;
        IMessagesSource messagesSource;
        IColorTheme colorTheme;

        [SetUp]
        public void BeforeEach()
        {
            searchResultModel = Substitute.For<ISearchResultModel>();
            highlightFilters = Substitute.For<IFiltersList>();
            selectionManager = Substitute.For<ISelectionManager>();
            messagesSource = Substitute.For<IMessagesSource>();
            colorTheme = Substitute.For<IColorTheme>();
            isRawMessagesMode = false;
            viewSize = 3;
            highlightFilters.FilteringEnabled.Returns(true);
            colorTheme.HighlightingColors.Returns(ImmutableArray.CreateRange(Enumerable.Range(
                (int)FilterAction.IncludeAndColorizeFirst, FilterAction.IncludeAndColorizeLast - FilterAction.IncludeAndColorizeFirst + 1)
                .Select(i => MakeHightlightingColor((FilterAction)i))
            ));
            msg1 = new Message(0, 1, null, new MessageTimestamp(), new StringSlice("test message 1"), SeverityFlag.Info);
            msg2 = new Message(0, 1, null, new MessageTimestamp(), new StringSlice("foo foo foo"), SeverityFlag.Info);
            msgWithMultilineText = new Message(0, 1, null, new MessageTimestamp(), new StringSlice(
@"2019/03/27 06:27:52.143 T#1 I app: model creation finished
import { imock, instance, when, anything, verify, resetCalls, MockPropertyPolicy, deepEqual } from 'ts-mockito';
import * as expect from 'expect';
import { Map, OrderedSet } from 'immutable';
import { ChangeNotification } from '../../../../client/model/utils';
import { PodStore, Pod } from '../../../../client/model/pod/store';
import { MeetingSession } from '../../../../client/model/meeting/impl/meeting/meetingSession';
import { Meeting, MeetingParticipant, LocalMeetingParticipant } from '../../../../client/model/meeting';
import { Conversation, ConversationStore, StreamId } from '../../../../client/model/conversation/store';
import { MeetingStartupOptions, Meeting as MeetingBase, MediaDevices, ParticipantTracks, ScreenTrack } from '../../../../client/model/meeting';
import { Protocol } from '../../../../client/model/meeting/protocol';
import { MeetingImpl } from '../../../../client/model/meeting/impl/meeting/impl/meetingImpl';
import { RtcManager, RtcManagerConfig } from '../../../../client/model/rtcManager';
import { MeetingAnalytics, LeaveSources } from '../../../../client/model/analytics';
import { MeetingParticipants } from '../../../../client/model/meeting/impl/meeting/meetingParticipants';
import { UncaughtExceptionsTrap } from '../../../utils/uncaughtExceptionsTrap';
import { AutoResetEvent, waiter } from '../../../utils/promise-utils';
import { MeetingLocalMedia } from '../../../../client/model/meeting/impl/media/meetingLocalMedia';
import { MeetingRemoteMedia } from '../../../../client/model/meeting/impl/media/meetingRemoteMedia';
import { LolexClock, install } from 'lolex';
import { RtcTokenProvider } from '../../../../client/model/meeting/impl/protocol/impl/rtcTokenProviderImpl';
import { User, UserStore, CurrentUser, UserId } from '../../../../client/model/users/store';
import { fireAndForget } from '../../../utils/promise-utils';
import { RtcSettingsStore, RtcSettings } from '../../../../client/model/settings/store';
import { GeoRouter } from '../../../../client/model/georouting/geoRouter';
import { UserBackend } from '../../../../client/model/users/backend';

describe('MeetingV2', () => {
    let meeting: Meeting;"
), SeverityFlag.Info);
        }

        static Color MakeHightlightingColor(FilterAction a)
        {
            return new Color((int)a);
        }

        void CreateHighlightingManager()
        {
            highlightingManager = new HighlightingManager(searchResultModel,
                () => MessageTextGetters.Get(isRawMessagesMode), () => viewSize, highlightFilters, selectionManager, colorTheme, RegularExpressions.FCLRegexFactory.Instance);
        }

        static IFilter CreateFilter(FilterAction action, bool expectRawMessagesMode, bool enabed,
            params (IMessage, int?, Search.MatchedTextRange)[] matches)
        {
            var filter = Substitute.For<IFilter>();
            filter.Enabled.Returns(enabed);
            filter.Action.Returns(action);
            var processing = Substitute.For<IFilterBulkProcessing>();
            filter.StartBulkProcessing(MessageTextGetters.Get(expectRawMessagesMode), false).Returns(processing);
            foreach (var m in matches)
            {
                processing.Match(m.Item1, m.Item2).Returns(m.Item3);
            }
            return filter;
        }

        static ViewLine CreateViewLine(IMessage message, int textLineIndex, int lineIndex)
        {
            return new ViewLine()
            {
                Message = message,
                Text = new MultilineText(message.Text),
                TextLineIndex = textLineIndex,
                LineIndex = lineIndex
            };
        }

        static void VerifyRanges(IEnumerable<(int, int, Color)> actual, params (int, int, Color)[] expected)
        {
            Assert.That(expected.OrderBy(x => x.Item1), Is.EqualTo(actual.OrderBy(x => x.Item1)));
        }

        [TestFixture]
        public class HighlightingFiltersTests : HighlightingManagerTests
        {
            [Test]
            public void HappyPath()
            {
                var f1 = CreateFilter(FilterAction.IncludeAndColorize11, false, true, (msg1, null, new Search.MatchedTextRange(StringSlice.Empty, 3, 4, true)));
                highlightFilters.Items.Returns(ImmutableList.Create(f1));
                CreateHighlightingManager();
                VerifyRanges(
                    highlightingManager.HighlightingFiltersHandler.GetHighlightingRanges(CreateViewLine(msg1, 0, 10)),
                    (3, 4, MakeHightlightingColor(FilterAction.IncludeAndColorize11))
                );
            }

            [Test]
            public void ExclusionFromHighlighting()
            {
                var f1 = CreateFilter(FilterAction.IncludeAndColorize11, false, true, (msg1, null, new Search.MatchedTextRange(StringSlice.Empty, 3, 4, true)));
                var f2 = CreateFilter(FilterAction.Exclude, false, true, (msg1, null, new Search.MatchedTextRange(StringSlice.Empty, 7, 8, true)));
                highlightFilters.Items.Returns(ImmutableList.Create(f1, f2));
                CreateHighlightingManager();
                VerifyRanges(
                    highlightingManager.HighlightingFiltersHandler.GetHighlightingRanges(CreateViewLine(msg1, 0, 10))
                );
            }
        };

        [TestFixture]
        public class SelectionFiltersTests : HighlightingManagerTests
        {
            [SetUp]
            public new void BeforeEach()
            {
            }

            [Test]
            public void EmptySelection()
            {
                selectionManager.Selection.Returns(new SelectionInfo(
                    new CursorPosition(msg1, messagesSource, 0, 1),
                    new CursorPosition(msg1, messagesSource, 0, 1),
                    MessageTextGetters.Get(false)
                ));
                CreateHighlightingManager();
                Assert.That(highlightingManager.SelectionHandler, Is.Null);
            }

            [Test]
            public void SubstringSelection()
            {
                selectionManager.Selection.Returns(new SelectionInfo(
                    new CursorPosition(msg1, messagesSource, 0, 1),
                    new CursorPosition(msg1, messagesSource, 0, 3),
                    MessageTextGetters.Get(false)
                ));
                CreateHighlightingManager();
                VerifyRanges(highlightingManager.SelectionHandler.GetHighlightingRanges(CreateViewLine(msg1, 0, 10)),
                    (6, 8, new Color()) // two matches "es" but first original one is not highlighted
                );
            }

            [Test]
            public void SelectionWithMultipleMatches()
            {
                selectionManager.Selection.Returns(new SelectionInfo(
                    new CursorPosition(msg2, messagesSource, 0, 0),
                    new CursorPosition(msg2, messagesSource, 0, 2),
                    MessageTextGetters.Get(false)
                ));
                CreateHighlightingManager();
                VerifyRanges(highlightingManager.SelectionHandler.GetHighlightingRanges(CreateViewLine(msg2, 0, 10)),
                    (4, 6, new Color()),
                    (8, 10, new Color())
                );
            }
        };

        [TestFixture]
        public class SelectionResultsTextTests : HighlightingManagerTests
        {
            IFiltersList searchFilters;

            [SetUp]
            public new void BeforeEach()
            {
                CreateHighlightingManager();
                searchFilters = new FiltersList(FilterAction.Exclude, FiltersListPurpose.Search, Substitute.For<IChangeNotification>());
            }

            [Test]
            public void SingleLineMatch()
            {
                searchFilters.Insert(0, new Filter(FilterAction.Include, "test", true, new Search.Options() { Template = "meeting" }, new FilterTimeRange(null, null),
                    Substitute.For<IFiltersFactory>(), RegularExpressions.FCLRegexFactory.Instance));
                var textInfo = highlightingManager.GetSearchResultMessageText(msgWithMultilineText, MessageTextGetters.SummaryTextGetter, searchFilters);
                Assert.That(StringUtils.NormalizeLinebreakes(
@"import { MeetingSession } from '../../../../client/model/meeting/impl/meeting/meetingSession';
import { Meeting, MeetingParticipant, LocalMeetingParticipant } from '../../../../client/model/meeting';
import { MeetingStartupOptions, Meeting as MeetingBase, MediaDevices, ParticipantTracks, ScreenTrack } from '../../../../client/model/meeting';
import { Protocol } from '../../../../client/model/meeting/protocol';
import { MeetingImpl } from '../../../../client/model/meeting/impl/meeting/impl/meetingImpl';
import { MeetingAnalytics, LeaveSources } from '../../../../client/model/analytics';
import { MeetingParticipants } from '../../../../client/model/meeting/impl/meeting/meetingParticipants';
import { MeetingLocalMedia } from '../../../../client/model/meeting/impl/media/meetingLocalMedia';
import { MeetingRemoteMedia } from '../../../../client/model/meeting/impl/media/meetingRemoteMedia';
import { RtcTokenProvider } from '../../../../client/model/meeting/impl/protocol/impl/rtcTokenProviderImpl';
describe('MeetingV2', () => {
    let meeting: Meeting;"), Is.EqualTo(StringUtils.NormalizeLinebreakes(textInfo.DisplayText.ToString())));
            }

            [Test]
            public void MultilineLineMatch()
            {
                searchFilters.Insert(0, new Filter(FilterAction.Include, "test", true, new Search.Options()
                {
                    Template = @"Impl\'\;\r\n+import",
                    Regexp = true
                }, new FilterTimeRange(null, null), Substitute.For<IFiltersFactory>(), RegularExpressions.FCLRegexFactory.Instance));
                var textInfo = highlightingManager.GetSearchResultMessageText(msgWithMultilineText, MessageTextGetters.SummaryTextGetter, searchFilters);
                Assert.That(StringUtils.NormalizeLinebreakes(
@"import { MeetingImpl } from '../../../../client/model/meeting/impl/meeting/impl/meetingImpl';
import { RtcManager, RtcManagerConfig } from '../../../../client/model/rtcManager';
import { RtcTokenProvider } from '../../../../client/model/meeting/impl/protocol/impl/rtcTokenProviderImpl';
import { User, UserStore, CurrentUser, UserId } from '../../../../client/model/users/store';"),
                    Is.EqualTo(StringUtils.NormalizeLinebreakes(textInfo.DisplayText.ToString())));
            }

        }
    }
}
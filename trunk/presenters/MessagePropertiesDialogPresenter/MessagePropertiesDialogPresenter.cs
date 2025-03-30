using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LogJoint.UI.Presenters.MessagePropertiesDialog
{
    public class Presenter : IPresenter, IDialogViewModel, IExtensionsRegistry
    {
        public Presenter(
            IBookmarks bookmarks,
            IFiltersList hlFilters,
            IView view,
            LogViewer.IPresenterInternal viewerPresenter,
            IPresentersFacade navHandler,
            IColorTheme theme,
            IChangeNotification parentChangeNotification,
            Telemetry.ITelemetryCollector telemetryCollector,
            IAnnotationsRegistry annotations
        )
        {
            this.hlFilters = hlFilters;
            this.bookmarks = bookmarks;
            this.view = view;
            this.viewerPresenter = viewerPresenter;
            this.navHandler = navHandler;
            this.changeNotification = parentChangeNotification.CreateChainedChangeNotification(false);
            this.inlineSearch = new InlineSearch.Presenter(changeNotification);

            inlineSearch.OnSearch += (s, e) =>
            {
                inlineSearchText = e.Query;
                if (e.Reverse)
                    inlineSearchIndex--;
                else
                    inlineSearchIndex++;
                changeNotification.Post();
            };

            this.getFocusedMessage = Selectors.Create(() => viewerPresenter.FocusedMessage,
                message => message?.GetLogSource() == null ? null : message);
            var getBookmarkData = bookmarks == null ? () => (null, null) :
                Selectors.Create(getFocusedMessage, () => bookmarks.Items, (focused, bmks) =>
                {
                    if (focused == null)
                        return (noSelection, null);
                    var isBookmarked = IsMessageBookmarked(focused);
                    return (isBookmarked ? "yes" : "no", isBookmarked ? "clear bookmark" : "set bookmark");
                });
            bool getHlFilteringEnabled() => hlFilters.FilteringEnabled && hlFilters.Items.Count > 0;
            static List<ContentViewMode> getContentViewModes(IMessage msg)
            {
                var contentViewModes = new List<ContentViewMode>();
                if (msg != null)
                {
                    contentViewModes.Add(ContentViewMode.Summary);
                    if (msg.RawText.IsInitialized)
                        contentViewModes.Add(ContentViewMode.RawText);
                }
                return contentViewModes;
            }
            var getExtensionViewModes = Selectors.Create(
                getFocusedMessage,
                msg => ImmutableArray.CreateRange(extensions.Select(ext =>
                {
                    if (msg == null)
                        return null;
                    try
                    {
                        var customPresenter = ext.CreateContentPresenter(new ContentPresenterParams
                        {
                            Message = msg,
                            ChangeNotification = changeNotification
                        });
                        if (customPresenter == null)
                            return null;
                        return new ContentViewMode
                        {
                            CustomPresenter = customPresenter,
                            Name = customPresenter.ContentViewModeName
                        };
                    }
                    catch (Exception e)
                    {
                        telemetryCollector.ReportException(e,
                            $"Extension {ext.GetType().Name} failed to create message content presenter");
                        return null;
                    }
                }).Where(contentPresenter => contentPresenter != null))
            );
            var getEffectiveInlineSearchData = Selectors.Create(
                () => inlineSearchIndex, () => inlineSearch.IsVisible, () => inlineSearchText,
                (index, visible, text) => visible && text != "" ?
                    new InlineSearchData() { Index = index, Query = text } : null);
            var getEffectiveViewModeData = Selectors.Create(
                getFocusedMessage, getExtensionViewModes, () => lastSetContentViewModeIndex,
                static (message, extensionViewModes, setContentViewMode) =>
            {
                List<ContentViewMode> contentViewModes = getContentViewModes(message);
                contentViewModes.AddRange(extensionViewModes);
                int? effectiveContentViewMode =
                    contentViewModes.Count == 0 ? new int?() :
                    RangeUtils.PutInRange(0, contentViewModes.Count - 1, setContentViewMode);
                ContentViewMode contentViewMode = effectiveContentViewMode == null ? null :
                        contentViewModes[effectiveContentViewMode.Value];
                object customView = contentViewMode?.CustomPresenter?.View;
                return (contentViewModes, contentViewMode, effectiveContentViewMode, customView);
            });
            var getTextValue = Selectors.Create(getFocusedMessage, getEffectiveViewModeData, 
                static (message, viewModeData) =>
            {
                object customView = viewModeData.customView;
                ContentViewMode contentViewMode = viewModeData.contentViewMode;
                string textValue =
                    contentViewMode == null ? "" :
                    customView is string customViewStr ? customViewStr :
                    contentViewMode.TextGetter == null ? null :
                    contentViewMode.TextGetter(message).Text.Value;
                return textValue;
            });
            this.getInlineSearchHitCount = Selectors.Create(
                getTextValue, getEffectiveInlineSearchData, GetInlineSearchHitCount);
            this.getDialogData = Selectors.Create(
                getFocusedMessage,
                getTextValue,
                getBookmarkData,
                getHlFilteringEnabled,
                getEffectiveViewModeData,
                getEffectiveInlineSearchData,
                () => annotations.Annotations,
                (message, textValue, bmk, hlEnabled, viewModeData, inlineSearchData, annotations) =>
            {
                var (bookmarkedStatus, bookmarkAction) = bmk;
                ILogSource ls = message?.GetLogSource();
                StringSlice messageLink = (message?.Link) ?? StringSlice.Empty;

                return new DialogData()
                {
                    TimeValue = message != null ? message.Time.ToUserFrendlyString() : noSelection,

                    ThreadLinkValue = message != null ? message.Thread.DisplayName : noSelection,
                    ThreadLinkBkColor = theme.ThreadColors.GetByIndex(message?.Thread?.ThreadColorIndex),
                    ThreadLinkEnabled = message != null && navHandler?.CanShowThreads == true,

                    SourceLinkValue = ls != null ? ls.DisplayName : noSelection,
                    SourceLinkBkColor = theme.ThreadColors.GetByIndex(ls?.ColorIndex),
                    SourceLinkEnabled = ls != null && navHandler != null,

                    BookmarkedStatusText = bookmarkedStatus ?? "N/A",
                    BookmarkActionLinkText = bookmarkAction,
                    BookmarkActionLinkEnabled = !string.IsNullOrEmpty(bookmarkAction),

                    SeverityValue = (message?.Severity)?.ToString() ?? noSelection,

                    ContentViewModes = [.. viewModeData.contentViewModes.Select(m => m.Name)],
                    ContentViewModeIndex = viewModeData.effectiveContentViewMode,
                    TextSegments = CreateTextFragments(textValue, inlineSearchData, annotations),
                    CustomView = viewModeData.customView is string ? null : viewModeData.customView,

                    HighlightedCheckboxEnabled = hlEnabled,

                    MessageLinkEnabled = !messageLink.IsEmpty,
                    MessageLinkValue = messageLink
                };
            });
        }
        void IPresenter.Show()
        {
            if (GetPropertiesForm() == null)
            {
                propertiesForm = view.CreateDialog(this);
            }
            changeNotification.Active = true;
            propertiesForm.Show();
        }

        IExtensionsRegistry IPresenter.ExtensionsRegistry => this;

        IReadOnlyList<string> IPresenter.ContentViewModes => getDialogData().ContentViewModes;

        int? IPresenter.SelectedContentViewMode
        {
            get => getDialogData().ContentViewModeIndex;
            set => ((IDialogViewModel)this).OnContentViewModeChange(value.GetValueOrDefault(0));
        }

        IChangeNotification IDialogViewModel.ChangeNotification => changeNotification;

        InlineSearch.IViewModel IDialogViewModel.InlineSearch => inlineSearch.ViewModel;

        DialogData IDialogViewModel.Data
        {
            get { return getDialogData(); }
        }

        bool IsMessageBookmarked(IMessage msg)
        {
            return bookmarks != null && bookmarks.GetMessageBookmarks(msg).Length > 0;
        }

        void IDialogViewModel.OnBookmarkActionClicked()
        {
            var msg = getFocusedMessage();
            if (msg == null)
                return;
            var msgBmks = bookmarks.GetMessageBookmarks(msg);
            if (msgBmks.Length == 0)
                bookmarks.ToggleBookmark(bookmarks.Factory.CreateBookmark(msg, 0));
            else foreach (var b in msgBmks)
                    bookmarks.ToggleBookmark(b);
        }

        void IDialogViewModel.OnNextClicked(bool highlightedChecked)
        {
            if (highlightedChecked)
                viewerPresenter.GoToNextHighlightedMessage().IgnoreCancellation();
            else
                viewerPresenter.GoToNextMessage().IgnoreCancellation();
        }

        void IDialogViewModel.OnPrevClicked(bool highlightedChecked)
        {
            if (highlightedChecked)
                viewerPresenter.GoToPrevHighlightedMessage().IgnoreCancellation();
            else
                viewerPresenter.GoToPrevMessage().IgnoreCancellation();
        }

        void IDialogViewModel.OnThreadLinkClicked()
        {
            var msg = getFocusedMessage();
            if (msg != null && navHandler != null)
                navHandler.ShowThread(msg.Thread);
        }

        void IDialogViewModel.OnSourceLinkClicked()
        {
            var msg = getFocusedMessage();
            if (msg?.GetLogSource() != null && navHandler != null)
                navHandler.ShowLogSource(msg.GetLogSource());
        }

        void IDialogViewModel.OnContentViewModeChange(int value)
        {
            if (value != lastSetContentViewModeIndex)
            {
                lastSetContentViewModeIndex = value;
                changeNotification.Post();
            }
        }

        void IDialogViewModel.OnClosed()
        {
            changeNotification.Active = false;
        }

        void IDialogViewModel.OnSearchShortcutPressed(string selection)
        {
            inlineSearchText = selection ?? "";
            inlineSearchIndex = 0;
            inlineSearch.Show(inlineSearchText, () =>
            {
                int hitCount = getInlineSearchHitCount();
                if (hitCount == 0)
                    return new InlineSearch.HitCounts(0, hitCount);
                else
                    return new InlineSearch.HitCounts(SafeMod(inlineSearchIndex, hitCount) + 1, hitCount);
            });
        }

        void IExtensionsRegistry.Register(IExtension extension)
        {
            extensions.Add(extension);
        }

        void IExtensionsRegistry.Unregister(IExtension extension)
        {
            extensions.Remove(extension);
        }

        IDialog GetPropertiesForm()
        {
            if (propertiesForm != null)
                if (propertiesForm.IsDisposed)
                    propertiesForm = null;
            return propertiesForm;
        }

        static int GetInlineSearchHitCount(string textValue, InlineSearchData inlineSearchData)
        {
            if (inlineSearchData == null)
                return 0;
            int matchCount = 0;
            for (int textPos = 0; ; )
            {
                var matchPos = textValue.IndexOf(inlineSearchData.Query, textPos);
                if (matchPos == -1)
                    break;
                ++matchCount;
                textPos = matchPos + inlineSearchData.Query.Length;
            }
            return matchCount;
        }

        internal static IReadOnlyList<TextSegment> CreateTextFragments(
            string textValue, InlineSearchData inlineSearchData, IAnnotationsSnapshot annotations)
        {
            IReadOnlyList<TextSegment> textSegments = ImmutableList<TextSegment>.Empty;
            if (textValue != null)
            {
                var builder = ImmutableList.CreateBuilder<TextSegment>();

                if (inlineSearchData != null)
                {
                    int matchCount = 0;
                    int textPos = 0;
                    for (; ;)
                    {
                        var matchPos = textValue.IndexOf(inlineSearchData.Query, textPos);
                        if (matchPos == -1)
                            break;
                        if (matchPos > textPos)
                            builder.Add(new TextSegment(TextSegmentType.Plain,
                                new StringSlice(textValue, textPos, matchPos - textPos)));
                        builder.Add(new TextSegment(TextSegmentType.SecondarySearchResult,
                            new StringSlice(textValue, matchPos, inlineSearchData.Query.Length)));
                        ++matchCount;
                        textPos = matchPos + inlineSearchData.Query.Length;
                    }
                    if (textPos < textValue.Length)
                    {
                        builder.Add(new TextSegment(TextSegmentType.Plain,
                            new StringSlice(textValue, textPos, textValue.Length - textPos)));
                    }
                    if (matchCount > 0)
                    {
                        int primaryMatchIndex = SafeMod(inlineSearchData.Index, matchCount);
                        int matchIndex = 0;
                        for (int i = 0; i < builder.Count; ++i)
                        {
                            if (builder[i].Type == TextSegmentType.SecondarySearchResult)
                            {
                                if (matchIndex == primaryMatchIndex)
                                {
                                    builder[i] = builder[i] with { Type = TextSegmentType.PrimarySearchResult };
                                    break;
                                }
                                ++matchIndex;
                            }
                        }
                    }
                }
                else
                {
                    builder.Add(new TextSegment(TextSegmentType.Plain, new StringSlice(textValue)));
                }

                if (!annotations.IsEmpty)
                {
                    int segmentIndex = 0;
                    foreach (StringAnnotationEntry ann in annotations.FindAnnotations(textValue))
                    {
                        while (builder[segmentIndex].Value.EndIndex < ann.BeginIndex)
                            ++segmentIndex;
                        TextSegment originalSegment = builder[segmentIndex];
                        builder.RemoveAt(segmentIndex);
                        if (ann.BeginIndex > originalSegment.Value.StartIndex)
                        {
                            builder.Insert(segmentIndex, originalSegment with
                            {
                                Value = originalSegment.Value.SubString(0, ann.BeginIndex - originalSegment.Value.StartIndex)
                            });
                            ++segmentIndex;
                        }
                        builder.Insert(segmentIndex, new TextSegment(TextSegmentType.Annotation, new StringSlice(ann.Annotation)));
                        ++segmentIndex;

                        if (originalSegment.Value.EndIndex > ann.BeginIndex)
                        {
                            builder.Insert(segmentIndex, originalSegment with
                            {
                                Value = originalSegment.Value.SubString(ann.BeginIndex - originalSegment.Value.StartIndex)
                            });
                        }
                    }
                }

                textSegments = builder.ToImmutable();
            }

            return textSegments;
        }

        // supports negative i
        static int SafeMod(int i, int q) => ((i % q) + q) % q; 

        class ContentViewMode
        {
            public string Name;
            public MessageTextGetter TextGetter;
            public IMessageContentPresenter CustomPresenter;

            public static readonly ContentViewMode Summary = new ContentViewMode()
            {
                Name = "Summary",
                TextGetter = MessageTextGetters.SummaryTextGetter,
            };
            public static readonly ContentViewMode RawText = new ContentViewMode()
            {
                Name = "Raw text",
                TextGetter = MessageTextGetters.RawTextGetter,
            };
        };

        internal class InlineSearchData
        {
            public string Query;
            public int Index;
        };

        readonly IChainedChangeNotification changeNotification;
        readonly IFiltersList hlFilters;
        readonly IBookmarks bookmarks;
        readonly IView view;
        readonly LogViewer.IPresenterInternal viewerPresenter;
        readonly IPresentersFacade navHandler;
        readonly Func<IMessage> getFocusedMessage;
        readonly Func<DialogData> getDialogData;
        readonly HashSet<IExtension> extensions = [];
        readonly InlineSearch.IPresenter inlineSearch;
        readonly Func<int> getInlineSearchHitCount;
        int lastSetContentViewModeIndex;
        int inlineSearchIndex;
        string inlineSearchText = "";
        IDialog propertiesForm;
        static readonly string noSelection = "<no selection>";
    };
};
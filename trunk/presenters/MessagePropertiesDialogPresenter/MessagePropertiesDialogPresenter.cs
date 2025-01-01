using Newtonsoft.Json.Schema;
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
            Telemetry.ITelemetryCollector telemetryCollector
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
            List<ContentViewMode> getContentViewModes(IMessage msg)
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
            var effectiveInlineSearchData = Selectors.Create(
                () => inlineSearchIndex, () => inlineSearch.IsVisible, () => inlineSearchText,
                (index, visible, text) => visible && text != "" ?
                    new InlineSearchData() { Index = index, Query = text } : null);
            this.getDialogData = Selectors.Create(
                getFocusedMessage,
                getBookmarkData,
                getHlFilteringEnabled,
                getExtensionViewModes,
                () => lastSetContentViewModeIndex,
                effectiveInlineSearchData,
                (message, bmk, hlEnabled, extensionViewModes, setContentViewMode, inlineSearchData) =>
            {
                var (bookmarkedStatus, bookmarkAction) = bmk;
                ILogSource ls = message?.GetLogSource();
                var contentViewModes = getContentViewModes(message);
                contentViewModes.AddRange(extensionViewModes);
                int? effectiveContentViewMode =
                    contentViewModes.Count == 0 ? new int?() :
                    RangeUtils.PutInRange(0, contentViewModes.Count - 1, setContentViewMode);
                ContentViewMode contentViewMode = effectiveContentViewMode == null ? null :
                        contentViewModes[effectiveContentViewMode.Value];
                var customView = contentViewMode?.CustomPresenter?.View;
                var textValue =
                    contentViewMode == null ? "" :
                    customView is string customViewStr ? customViewStr :
                    contentViewMode.TextGetter == null ? null :
                    contentViewMode.TextGetter(message).Text.Value;

                int Mod(int i, int q) => ((i % q) + q) % q; // supports negative i

                IReadOnlyList<TextHighlight> textHighlights = ImmutableList<TextHighlight>.Empty;
                if (inlineSearchData != null && textValue != null)
                {
                    var builder = ImmutableList.CreateBuilder<TextHighlight>();
                    for (int textPos = 0; ;)
                    {
                        var matchPos = textValue.IndexOf(inlineSearchData.Query, textPos);
                        if (matchPos == -1)
                            break;
                        builder.Add(new TextHighlight { Begin = matchPos, End = matchPos + inlineSearchData.Query.Length });
                        textPos = matchPos + inlineSearchData.Query.Length;
                    }
                    if (builder.Count > 0)
                        builder[Mod(inlineSearchData.Index, builder.Count)].IsPrimary = true;
                    textHighlights = builder.ToImmutable();
                }

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

                    ContentViewModes = ImmutableArray.CreateRange(contentViewModes.Select(m => m.Name)),
                    ContentViewModeIndex = effectiveContentViewMode,
                    TextValue = textValue,
                    TextHighlights = textHighlights,
                    CustomView = customView is string ? null : customView,

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
            inlineSearch.Show(inlineSearchText);
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

        class InlineSearchData
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
        readonly HashSet<IExtension> extensions = new HashSet<IExtension>();
        readonly InlineSearch.IPresenter inlineSearch;
        int lastSetContentViewModeIndex;
        int inlineSearchIndex;
        string inlineSearchText = "";
        IDialog propertiesForm;
        static readonly string noSelection = "<no selection>";
    };
};
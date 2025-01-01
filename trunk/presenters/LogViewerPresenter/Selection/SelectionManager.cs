using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using LogJoint.Drawing;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;

namespace LogJoint.UI.Presenters.LogViewer
{
    internal class SelectionManager : ISelectionManager, IDisposable
    {
        readonly Func<IView> view;
        readonly IScreenBuffer screenBuffer;
        readonly LJTraceSource tracer;
        readonly IClipboardAccess clipboard;
        readonly IPresentationProperties presentationProperties;
        readonly IWordSelection wordSelection;
        readonly IScreenBufferFactory screenBufferFactory;
        readonly IBookmarksFactory bookmarksFactory;
        readonly IChangeNotification changeNotification;
        readonly IColorTheme theme;
        readonly CancellationTokenSource disposed = new CancellationTokenSource();
        readonly Task cursorThread;
        readonly Func<int?> cursorViewLine;
        readonly Func<(int, int)> viewLinesRange;
        readonly Func<SelectionInfo> selection;

        SelectionInfo setSelection;
        bool cursorState;
        int crosslineNavigationCharIdx;

        public SelectionManager(
            Func<IView> view,
            IScreenBuffer screenBuffer,
            LJTraceSource tracer,
            IPresentationProperties presentationProperties,
            IClipboardAccess clipboard,
            IScreenBufferFactory screenBufferFactory,
            IBookmarksFactory bookmarksFactory,
            IChangeNotification changeNotification,
            IWordSelection wordSelection,
            IColorTheme theme
        )
        {
            this.view = view;
            this.screenBuffer = screenBuffer;
            this.clipboard = clipboard;
            this.presentationProperties = presentationProperties;
            this.tracer = tracer;
            this.screenBufferFactory = screenBufferFactory;
            this.bookmarksFactory = bookmarksFactory;
            this.changeNotification = changeNotification;
            this.wordSelection = wordSelection;
            this.theme = theme;

            this.selection = Selectors.Create(
                () => setSelection,
                () => screenBuffer.Messages,
                () => screenBuffer.Sources,
                () => screenBuffer.DisplayTextGetter,
                ComputeSelection
            );
            this.cursorViewLine = Selectors.Create(
                selection,
                () => screenBuffer.Messages,
                (selection, messages) => GetViewLineIndex(selection?.First, messages, bookmarksFactory, exactMode: true));
            this.viewLinesRange = Selectors.Create(
                selection,
                () => screenBuffer.Messages,
                (selection, messages) =>
                {
                    var i1 = GetViewLineIndex(selection?.First, messages, bookmarksFactory, exactMode: false).Value;
                    var i2 = GetViewLineIndex(selection?.Last, messages, bookmarksFactory, exactMode: false).Value;
                    return (Math.Min(i1, i2), Math.Max(i1, i2));
                }
            );

            this.cursorThread = this.CursorThread();
        }

        void IDisposable.Dispose()
        {
            disposed.Cancel();
        }

        SelectionInfo ISelectionManager.Selection => selection();

        int? ISelectionManager.CursorViewLine => cursorViewLine();
        (int, int) ISelectionManager.ViewLinesRange => viewLinesRange();


        async Task ISelectionManager.CopySelectionToClipboard()
        {
            if (clipboard == null)
                return;
            var txt = await GetSelectedTextInternal(includeTime: presentationProperties.ShowTime);
            if (txt.Length > 0)
                clipboard.SetClipboard(txt, await GetSelectedTextAsHtml(includeTime: presentationProperties.ShowTime));
        }

        Task<string> ISelectionManager.GetSelectedText()
        {
            return GetSelectedTextInternal(includeTime: false);
        }

        bool ISelectionManager.SelectWordBoundaries(ViewLine viewLine, int charIndex)
        {
            var word = wordSelection.FindWordBoundaries(
                screenBuffer.DisplayTextGetter(viewLine.Message).GetNthTextLine(viewLine.TextLineIndex), charIndex);
            if (word != null)
            {
                SetSelection(viewLine.LineIndex, SelectionFlag.NoHScrollToSelection, word.Item1);
                SetSelection(viewLine.LineIndex, SelectionFlag.PreserveSelectionEnd, word.Item2);
                return true;
            }
            return false;
        }

        void ISelectionManager.SetSelection(int displayIndex, SelectionFlag flag, int? textCharIndex)
        {
            SetSelection(displayIndex, flag, textCharIndex);
        }

        void ISelectionManager.SetSelection(int messageDisplayIndex, int textCharIndex1, int textCharIndex2)
        {
            var anchor = screenBuffer.Messages[messageDisplayIndex];
            var mtxt = screenBuffer.DisplayTextGetter(anchor.Message);
            var txt = mtxt.Text;
            foreach (var (line, lineIdx) in mtxt.Lines)
            {
                var lineDisplayIndex = messageDisplayIndex + lineIdx - anchor.TextLineIndex;
                if (lineDisplayIndex >= screenBuffer.Messages.Count)
                    break;
                var lineBegin = line.StartIndex - txt.StartIndex;
                var lineEnd = lineBegin + line.Length;
                if (textCharIndex1 >= lineBegin && textCharIndex1 <= lineEnd)
                    SetSelection(lineDisplayIndex, SelectionFlag.None, textCharIndex1 - lineBegin);
                if (textCharIndex2 >= lineBegin && textCharIndex2 <= lineEnd)
                    SetSelection(lineDisplayIndex, SelectionFlag.PreserveSelectionEnd, textCharIndex2 - lineBegin);
            };
        }

        static SelectionInfo ComputeSelection(SelectionInfo setSelection, IReadOnlyList<ScreenBufferEntry> viewLines, IReadOnlyList<SourceScreenBuffer> sources, MessageTextGetter messageTextGetter)
        {
            SelectionInfo createForViewLineIndex(int idx1, int idx2)
            {
                if (viewLines.Count == 0)
                    return null;
                if (idx1 != idx2)
                    return new SelectionInfo(CursorPosition.FromScreenBufferEntry(viewLines[idx1], 0), null, messageTextGetter);
                return new SelectionInfo(new CursorPosition(setSelection.First.Message, setSelection.First.Source, 0, 0), null, messageTextGetter);
            }
            bool belongsToNonExistingSource(CursorPosition pos) => pos != null && !sources.Any(s => s.Source == pos.Source);

            if (setSelection == null)
            {
                return null;
            }
            else if (belongsToNonExistingSource(setSelection.First) || belongsToNonExistingSource(setSelection.Last))
            {
                IComparer<IMessage> cmp = new DatesComparer(setSelection.First.Message.Time.ToLocalDateTime());
                var idx1 = viewLines.BinarySearch(0, viewLines.Count, dl => cmp.Compare(dl.Message, null) < 0);
                var idx2 = viewLines.BinarySearch(idx1, viewLines.Count, dl => cmp.Compare(dl.Message, null) <= 0);
                return createForViewLineIndex(idx1, idx2);
            }
            else if (setSelection.MessageTextGetter != messageTextGetter)
            {
                var idx1 = viewLines.BinarySearch(0, viewLines.Count, m => MessagesComparer.Compare(m.Message, setSelection.First.Message) < 0);
                var idx2 = viewLines.BinarySearch(idx1, viewLines.Count, m => MessagesComparer.Compare(m.Message, setSelection.First.Message) <= 0);
                return createForViewLineIndex(idx1, idx2);
            }
            else
            {
                return setSelection;
            }
        }

        bool ISelectionManager.CursorState => cursorState && (view()?.HasInputFocus).GetValueOrDefault(false);

        static int? GetViewLineIndex(CursorPosition pos, IReadOnlyList<ScreenBufferEntry> screenBufferEntries, IBookmarksFactory bookmarksFactory, bool exactMode)
        {
            if (pos == null || screenBufferEntries.Count == 0)
                return exactMode ? new int?() : -1;
            var cursorBmk = bookmarksFactory.CreateBookmark(pos.Message, pos.TextLineIndex);
            foreach (var m in screenBufferEntries)
                if (MessagesComparer.Compare(cursorBmk, bookmarksFactory.CreateBookmark(m.Message, m.TextLineIndex)) == 0)
                    return m.Index;
            if (exactMode)
                return null;
            if (MessagesComparer.Compare(cursorBmk, bookmarksFactory.CreateBookmark(screenBufferEntries[0].Message, screenBufferEntries[0].TextLineIndex)) < 0)
                return -1;
            else
                return screenBufferEntries.Count;
        }

        void SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null)
        {
            var dmsg = screenBuffer.Messages[displayIndex];
            var msg = dmsg.Message;
            var line = screenBuffer.DisplayTextGetter(msg).GetNthTextLine(dmsg.TextLineIndex);
            int newLineCharIndex;
            if ((flag & SelectionFlag.SelectBeginningOfLine) != 0)
                newLineCharIndex = 0;
            else if ((flag & SelectionFlag.SelectEndOfLine) != 0)
                newLineCharIndex = line.Length;
            else
            {
                newLineCharIndex = RangeUtils.PutInRange(0, line.Length,
                    (flag & SelectionFlag.CrosslineNavigation) != 0 ?
                        crosslineNavigationCharIdx :
                        textCharIndex.GetValueOrDefault((selection()?.First?.LineCharIndex).GetValueOrDefault(0)));
                if ((flag & SelectionFlag.SelectBeginningOfNextWord) != 0)
                    newLineCharIndex = StringUtils.FindNextWordInString(line, newLineCharIndex);
                else if ((flag & SelectionFlag.SelectBeginningOfPrevWord) != 0)
                    newLineCharIndex = StringUtils.FindPrevWordInString(line, newLineCharIndex);
            }

            tracer.Info("Selecting line {0}. Display position = {1}", msg.GetHashCode(), displayIndex);

            bool resetEnd = (flag & SelectionFlag.PreserveSelectionEnd) == 0;

            void doScrolling()
            {
                if (displayIndex == 0 && screenBuffer.TopLineScrollValue > 1e-3)
                {
                    screenBuffer.MakeFirstLineFullyVisible();
                }
                if ((flag & SelectionFlag.NoHScrollToSelection) == 0)
                {
                    view()?.HScrollToSelectedText((selection()?.First?.LineCharIndex).GetValueOrDefault());
                }
                cursorState = true;
            }

            var oldSelection = selection();
            if (oldSelection?.First?.Message != msg
                || cursorViewLine() != displayIndex
                || oldSelection?.First?.LineCharIndex != newLineCharIndex
                || resetEnd != oldSelection?.IsEmpty)
            {
                var tmp = CursorPosition.FromScreenBufferEntry(dmsg, newLineCharIndex);

                setSelection = new SelectionInfo(tmp, resetEnd ? tmp : setSelection.Last, screenBuffer.DisplayTextGetter);
                changeNotification.Post();

                if ((flag & SelectionFlag.CrosslineNavigation) == 0)
                    crosslineNavigationCharIdx = newLineCharIndex;

                doScrolling();
            }
            else if ((flag & SelectionFlag.ScrollToViewEventIfSelectionDidNotChange) != 0)
            {
                doScrolling();
            }
        }

        async Task<List<ScreenBufferEntry>> GetSelectedDisplayMessagesEntries()
        {
            var viewLines = screenBuffer.Messages;

            var normSelection = selection()?.Normalize();
            if (normSelection.IsEmpty != false)
            {
                return new List<ScreenBufferEntry>();
            }

            bool isGoodDisplayPosition(int p) => p >= 0 && p < viewLines.Count;

            var (b, e) = viewLinesRange();
            if (isGoodDisplayPosition(b) && isGoodDisplayPosition(e))
            {
                // most common case: both positions are in the screen buffer at the moment
                return viewLines.Skip(b).Take(e - b + 1).ToList();
            }

            CancellationToken cancellation = CancellationToken.None;

            IScreenBuffer tmpBuf = screenBufferFactory.CreateScreenBuffer(1);
            await tmpBuf.SetSources(screenBuffer.Sources.Select(s => s.Source), cancellation);
            if (!await tmpBuf.MoveToBookmark(bookmarksFactory.CreateBookmark(normSelection.First.Message, 0),
                BookmarkLookupMode.ExactMatch, cancellation))
            {
                // Impossible to load selected message into screen buffer. Rather impossible.
                return new List<ScreenBufferEntry>();
            }

            var tasks = screenBuffer.Sources.Select(async sourceBuf =>
            {
                var sourceMessages = new List<IMessage>();

                await sourceBuf.Source.EnumMessages(
                    tmpBuf.Sources.First(sb => sb.Source == sourceBuf.Source).Begin,
                    m =>
                    {
                        if (MessagesComparer.Compare(m, normSelection.Last.Message) > 0)
                            return false;
                        sourceMessages.Add(m);
                        return true;
                    },
                    EnumMessagesFlag.Forward,
                    LogProviderCommandPriority.AsyncUserAction,
                    cancellation
                );

                return new { sourceBuf.Source, Messages = sourceMessages };
            }).ToList();

            await Task.WhenAll(tasks);
            cancellation.ThrowIfCancellationRequested();

            var messagesToSource = tasks.ToDictionary(
                t => (IMessagesCollection)new MessagesContainers.SimpleCollection(t.Result.Messages), t => t.Result.Source);

            return
                new MessagesContainers.SimpleMergingCollection(messagesToSource.Keys)
                .Forward(0, int.MaxValue)
                .SelectMany(m =>
                    Enumerable.Range(0, screenBuffer.DisplayTextGetter(m.Message.Message).GetLinesCount()).Select(
                        lineIdx => new ScreenBufferEntry()
                        {
                            TextLineIndex = lineIdx,
                            Message = m.Message.Message,
                            Source = messagesToSource[m.SourceCollection],
                        }
                    )
                )
                .Where(m =>
                    CursorPosition.Compare(CursorPosition.FromScreenBufferEntry(m, screenBuffer.DisplayTextGetter(m.Message).GetNthTextLine(m.TextLineIndex).Length), normSelection.First) >= 0
                 && CursorPosition.Compare(CursorPosition.FromScreenBufferEntry(m, 0), normSelection.Last) <= 0)
                .ToList();
        }

        async Task<List<SelectedTextLine>> GetSelectedTextLines(bool includeTime)
        {
            var ret = new List<SelectedTextLine>();
            var normSelection = selection()?.Normalize();
            if (normSelection?.IsEmpty != false)
                return ret;
            var showMilliseconds = presentationProperties.ShowMilliseconds;
            var selectedDisplayEntries = await GetSelectedDisplayMessagesEntries();
            IMessage prevMessage = null;
            var sb = new StringBuilder();
            foreach (var i in selectedDisplayEntries.ZipWithIndex())
            {
                sb.Clear();
                var line = screenBuffer.DisplayTextGetter(i.Value.Message).GetNthTextLine(i.Value.TextLineIndex);
                bool isFirstLine = i.Key == 0;
                bool isLastLine = i.Key == selectedDisplayEntries.Count - 1;
                int beginIdx = isFirstLine ? normSelection.First.LineCharIndex : 0;
                int endIdx = isLastLine ? normSelection.Last.LineCharIndex : line.Length;
                if (i.Value.Message != prevMessage)
                {
                    if (includeTime)
                    {
                        if (beginIdx == 0 && (endIdx - beginIdx) > 0)
                        {
                            sb.AppendFormat("{0}\t", i.Value.Message.Time.ToUserFrendlyString(showMilliseconds));
                        }
                    }
                    prevMessage = i.Value.Message;
                }
                line.SubString(beginIdx, endIdx - beginIdx).Append(sb);
                if (isLastLine && sb.Length == 0)
                    break;
                ret.Add(new SelectedTextLine()
                {
                    Str = sb.ToString(),
                    Message = i.Value.Message,
                    LineIndex = i.Key,
                    IsSingleLineSelectionFragment = isFirstLine && isLastLine && (beginIdx != 0 || endIdx != line.Length)
                });
            }
            return ret;
        }

        private async Task<string> GetSelectedTextInternal(bool includeTime)
        {
            var sb = new StringBuilder();
            foreach (var line in await GetSelectedTextLines(includeTime))
            {
                if (line.LineIndex != 0)
                    sb.AppendLine();
                sb.Append(line.Str);
            }
            return sb.ToString();
        }

        string GetBackgroundColorAsHtml(IMessage msg)
        {
            var ls = msg.GetLogSource();
            var cl = "white";
            if (ls != null)
                if (presentationProperties.Coloring == ColoringMode.Threads)
                    cl = theme.ThreadColors.GetByIndex(msg.Thread.ThreadColorIndex).ToHtmlColor();
                else if (presentationProperties.Coloring == ColoringMode.Sources)
                    cl = theme.ThreadColors.GetByIndex(ls.ColorIndex).ToHtmlColor();
            return cl;
        }

        async Task<string> GetSelectedTextAsHtml(bool includeTime)
        {
            var sb = new StringBuilder();
            sb.Append("<pre style='font-size:8pt; font-family: monospace; padding:0; margin:0;'>");
            foreach (var line in await GetSelectedTextLines(includeTime))
            {
                if (line.IsSingleLineSelectionFragment)
                {
                    sb.Clear();
                    sb.AppendFormat("<font style='font-size:8pt; font-family: monospace; padding:0; margin:0; background: {1}'>{0}</font>&nbsp;",
                        System.Security.SecurityElement.Escape(line.Str), GetBackgroundColorAsHtml(line.Message));
                    return sb.ToString();
                }
                if (line.LineIndex != 0)
                    sb.AppendLine();
                sb.AppendFormat("<font style='background: {1}'>{0}</font>",
                    System.Security.SecurityElement.Escape(line.Str), GetBackgroundColorAsHtml(line.Message));
            }
            sb.Append("</pre><br/>");
            return sb.ToString();
        }

        async Task CursorThread()
        {
            while (!disposed.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(500, disposed.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                this.cursorState = !this.cursorState;
                changeNotification.Post();
            }
        }

        struct SelectedTextLine
        {
            public string Str;
            public IMessage Message;
            public int LineIndex;
            public bool IsSingleLineSelectionFragment;
        };
    };
};
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal interface ISelectionManager
	{
		SelectionInfo Selection { get; }
		Func<IMessage, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler { get; }
		Func<IMessage, IEnumerable<Tuple<int, int>>> SearchInplaceHighlightHandler { get; }

		void SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null);
		void SetSelection(int displayIndex, int textCharIndex1, int textCharInde2);
		bool SelectWordBoundaries(CursorPosition pos);
		void Clear();
		Task CopySelectionToClipboard();
		Task<string> GetSelectedText();
		bool PickNewSelection();
		void UpdateSelectionDisplayIndexes();
		void InvalidateTextLineUnderCursor();
		void UpdateSelectionInplaceHighlightingFields();
		IBookmark GetFocusedMessageBookmark();

		event EventHandler SelectionChanged;
		event EventHandler FocusedMessageChanged;
		event EventHandler FocusedMessageBookmarkChanged;
	};

	internal enum SelectionFlag
	{
		None = 0,
		PreserveSelectionEnd = 1,
		SelectBeginningOfLine = 2,
		SelectEndOfLine = 4,
		SelectBeginningOfNextWord = 8,
		SelectBeginningOfPrevWord = 16,
		SuppressOnFocusedMessageChanged = 32,
		NoHScrollToSelection = 64,
		ScrollToViewEventIfSelectionDidNotChange = 128
	};

	internal class SelectionManager: ISelectionManager
	{
		readonly IView view;
		readonly IScreenBuffer screenBuffer;
		readonly LJTraceSource tracer;
		readonly IClipboardAccess clipboard;
		readonly IPresentationDataAccess presentationDataAccess;
		readonly IWordSelection wordSelection = new WordSelection();
		readonly IScreenBufferFactory screenBufferFactory;
		readonly ISearchResultModel searchResultModel;
		readonly IBookmarksFactory bookmarksFactory;

		SelectionInfo selection;
		IBookmark focusedMessageBookmark;

		Func<IMessage, IEnumerable<Tuple<int, int>>> selectionInplaceHighlightingHandler;
		Func<IMessage, IEnumerable<Tuple<int, int>>> searchResultInplaceHightlightHandler;
		int lastSearchOptionsHash;
		struct SearchOptionsCacheEntry
		{
			public SearchAllOptions Options;
			public Search.PreprocessedOptions PreprocessedOptions;
			public Search.BulkSearchState State;
		};
		readonly List<SearchOptionsCacheEntry> lastSearchOptionPreprocessed = new List<SearchOptionsCacheEntry>();

		public SelectionManager(
			IView view,
			ISearchResultModel searchResultModel, // todo: try get rid of this dependency
			IScreenBuffer screenBuffer,
			LJTraceSource tracer,
			IPresentationDataAccess presentationDataAccess,
			IClipboardAccess clipboard,
			IScreenBufferFactory screenBufferFactory,
			IBookmarksFactory bookmarksFactory
		)
		{
			this.view = view;
			this.searchResultModel = searchResultModel;
			this.screenBuffer = screenBuffer;
			this.clipboard = clipboard;
			this.presentationDataAccess = presentationDataAccess;
			this.tracer = tracer;
			this.searchResultModel = searchResultModel;
			this.screenBufferFactory = screenBufferFactory;
			this.bookmarksFactory = bookmarksFactory;

			this.searchResultInplaceHightlightHandler = SearchResultInplaceHightlightHandler;
		}

		public event EventHandler SelectionChanged;
		public event EventHandler FocusedMessageChanged;
		public event EventHandler FocusedMessageBookmarkChanged;

		SelectionInfo ISelectionManager.Selection
		{
			get { return selection; }
		}

		void ISelectionManager.Clear()
		{
			if (selection.First.Message == null)
				return;

			InvalidateTextLineUnderCursor();
			foreach (var displayIndexToInvalidate in selection.GetDisplayIndexesRange()
				.Where(idx => idx < screenBuffer.Messages.Count))
				view.InvalidateLine(screenBuffer.Messages[displayIndexToInvalidate].ToViewLine());

			SetSelection(new CursorPosition(), new CursorPosition());
			OnSelectionChanged();
			OnFocusedMessageChanged();
			OnFocusedMessageBookmarkChanged();
		}

		void ISelectionManager.InvalidateTextLineUnderCursor()
		{
			InvalidateTextLineUnderCursor();
		}

		async Task ISelectionManager.CopySelectionToClipboard()
		{
			if (clipboard == null)
				return;
			var txt = await GetSelectedTextInternal(includeTime: presentationDataAccess.ShowTime);
			if (txt.Length > 0)
				clipboard.SetClipboard(txt, await GetSelectedTextAsHtml(includeTime: presentationDataAccess.ShowTime));
		}

		Task<string> ISelectionManager.GetSelectedText()
		{
			return GetSelectedTextInternal(includeTime: false);
		}

		void ISelectionManager.UpdateSelectionDisplayIndexes()
		{
			selection.first.DisplayIndex = GetDisplayIndex(selection.First);
			selection.last.DisplayIndex = GetDisplayIndex(selection.Last);
		}

		void ISelectionManager.UpdateSelectionInplaceHighlightingFields()
		{
			UpdateSelectionInplaceHighlightingFields();
		}

		bool ISelectionManager.SelectWordBoundaries(CursorPosition pos)
		{
			var dmsg = screenBuffer.Messages[pos.DisplayIndex];
			var word = wordSelection.FindWordBoundaries(
				GetTextToDisplay(dmsg.Message).GetNthTextLine(dmsg.TextLineIndex), pos.LineCharIndex);
			if (word != null)
			{
				SetSelection(pos.DisplayIndex, SelectionFlag.NoHScrollToSelection, word.Item1);
				SetSelection(pos.DisplayIndex, SelectionFlag.PreserveSelectionEnd, word.Item2);
				return true;
			}
			return false;
		}

		void ISelectionManager.SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null)
		{
			SetSelection(displayIndex, flag, textCharIndex);
		}

		void ISelectionManager.SetSelection(int displayIndex, int textCharIndex1, int textCharIndex2)
		{
			var mtxt = GetTextToDisplay(screenBuffer.Messages[displayIndex].Message);
			var txt = mtxt.Text;
			mtxt.EnumLines((line, lineIdx) =>
			{
				var lineBegin = line.StartIndex - txt.StartIndex;
				var lineEnd = lineBegin + line.Length;
				if (textCharIndex1 >= lineBegin && textCharIndex1 <= lineEnd)
					SetSelection(displayIndex + lineIdx, SelectionFlag.None, textCharIndex1 - lineBegin);
				if (textCharIndex2 >= lineBegin && textCharIndex2 <= lineEnd)
					SetSelection(displayIndex + lineIdx, SelectionFlag.PreserveSelectionEnd, textCharIndex2 - lineBegin);
				return true;
			});
		}

		bool ISelectionManager.PickNewSelection()
		{
			var viewLines = screenBuffer.Messages;
			if (selection.First.Message == null)
			{
				if (viewLines.Count > 0)
				{
					SetSelection(0, SelectionFlag.SelectBeginningOfLine);
				}
				return true;
			}
			if (BelongsToNonExistentSource(selection.First) || BelongsToNonExistentSource(selection.Last))
			{
				if (viewLines.Count > 0)
				{
					var maxIdx = Math.Min(screenBuffer.FullyVisibleLinesCount, viewLines.Count);
					IComparer<IMessage> cmp = new DatesComparer(selection.First.Message.Time.ToLocalDateTime());
					var idx = ListUtils.BinarySearch(viewLines, 0, maxIdx, dl => cmp.Compare(dl.Message, null) < 0);
					if (idx != maxIdx)
						SetSelection(idx, SelectionFlag.SelectBeginningOfLine);
					else
						SetSelection(0, SelectionFlag.SelectBeginningOfLine);
				}
				else
				{
					SetSelection(new CursorPosition(), new CursorPosition());
				}
				return true;
			}
			return false;
		}

		IBookmark ISelectionManager.GetFocusedMessageBookmark()
		{
			if (focusedMessageBookmark == null)
			{
				var f = selection.First;
				if (f.Message != null)
				{
					focusedMessageBookmark = bookmarksFactory.CreateBookmark(
						f.Message, f.TextLineIndex, useRawText: presentationDataAccess.ShowRawMessages);
				}
			}
			return focusedMessageBookmark;
		}

		Func<IMessage, IEnumerable<Tuple<int, int>>> ISelectionManager.InplaceHighlightHandler
		{
			get { return selectionInplaceHighlightingHandler; }
		}

		Func<IMessage, IEnumerable<Tuple<int, int>>> ISelectionManager.SearchInplaceHighlightHandler
		{
			get { return searchResultInplaceHightlightHandler; }
		}


		void SetSelection(CursorPosition begin, CursorPosition? end)
		{
			selection.first = begin;
			if (end.HasValue)
				selection.last = end.Value;
			selection.normalized = CursorPosition.Compare(selection.first, selection.last) <= 0;
		}

		int GetDisplayIndex(CursorPosition pos)
		{
			if (pos.Message == null)
				return 0;

			Func<IMessage, IMessage, bool> messagesAreSame = (m1, m2) =>
			{
				if (m1 == m2)
					return true;
				if (m1 == null || m2 == null)
					return false;
				return m1.Position == m2.Position && m1.Thread == m2.Thread;
			};

			var viewLines = screenBuffer.Messages;
			int didx = 0;
			ScreenBufferEntry? sameMessageEntry = null;
			foreach (var m in viewLines)
			{
				if (m.Message != null && messagesAreSame(m.Message, pos.Message))
				{
					sameMessageEntry = m;
					if (m.TextLineIndex == pos.TextLineIndex)
						return didx;
				}
				++didx;
			}
			if (sameMessageEntry != null)
			if (pos.TextLineIndex < sameMessageEntry.Value.TextLineIndex)
				return -1;
			else
				return viewLines.Count;
			if (pos.Message.Position < 
				screenBuffer.Sources.Where(s => s.Source == pos.Source).Select(s => s.Begin).FirstOrDefault())
				return -1;
			else
				return viewLines.Count;
		}

		void InvalidateTextLineUnderCursor()
		{
			if (selection.First.Message != null)
			{
				view.InvalidateLine(selection.First.ToDisplayLine());
			}
		}

		void OnFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		void OnFocusedMessageBookmarkChanged()
		{
			focusedMessageBookmark = null;
			if (FocusedMessageBookmarkChanged != null)
				FocusedMessageBookmarkChanged(this, EventArgs.Empty);
		}

		void OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, EventArgs.Empty);
		}

		void UpdateSelectionInplaceHighlightingFields()
		{
			Func<IMessage, IEnumerable<Tuple<int, int>>> newHandler = null;

			if (selection.IsSingleLine)
			{
				var normSelection = selection.Normalize();
				var line = GetTextToDisplay(normSelection.First.Message).GetNthTextLine(normSelection.First.TextLineIndex);
				int beginIdx = normSelection.First.LineCharIndex;
				int endIdx = normSelection.Last.LineCharIndex;
				if (wordSelection.IsWordBoundary(line, beginIdx, endIdx))
				{
					var selectedPart = line.SubString(beginIdx, endIdx - beginIdx);
					if (wordSelection.IsWord(selectedPart))
					{
						var options = new LogJoint.Search.Options() 
						{
							Template = selectedPart,
							SearchInRawText = presentationDataAccess.ShowRawMessages
						};
						var optionsPreprocessed = options.Preprocess();
						var searchState = new Search.BulkSearchState();
						newHandler = msg =>
							FindAllHightlighRanges(msg, optionsPreprocessed, searchState, options.ReverseSearch, wordSelection);
					}
				}
			}

			if ((selectionInplaceHighlightingHandler != null) != (newHandler != null))
				view.Invalidate();
			else if (newHandler != null)
				view.Invalidate();

			selectionInplaceHighlightingHandler = newHandler;
		}

		StringUtils.MultilineText GetTextToDisplay(IMessage msg)
		{
			return msg.GetDisplayText(presentationDataAccess.ShowRawMessages);
		}

		static IEnumerable<Tuple<int, int>> FindAllHightlighRanges(
			IMessage msg, 
			Search.PreprocessedOptions searchOpts, 
			Search.BulkSearchState searchState,
			bool reverseSearch,
			IWordSelection wordSelection)
		{
			for (int? startPos = null; ; )
			{
				var matchedTextRangle = LogJoint.Search.SearchInMessageText(msg, searchOpts, searchState, startPos);
				if (!matchedTextRangle.HasValue)
					yield break;
				var r = matchedTextRangle.Value;
				if (r.WholeTextMatched)
					yield break;
				if (wordSelection == null || wordSelection.IsWordBoundary(r.SourceText, r.MatchBegin, r.MatchEnd))
					yield return new Tuple<int, int>(r.MatchBegin, r.MatchEnd);
				startPos = reverseSearch ? r.MatchBegin : r.MatchEnd;
			}
		}

		IEnumerable<Tuple<int, int>> SearchResultInplaceHightlightHandler(IMessage msg)
		{
			if (searchResultModel == null)
				yield break;
			var showRawMessages = presentationDataAccess.ShowRawMessages;
			int currentSearchOptionsHash = searchResultModel.SearchParams.Aggregate(101063, 
				(hash, opts) => hash ^ opts.GetHashCode() ^ showRawMessages.GetHashCode());
			if (lastSearchOptionsHash != currentSearchOptionsHash)
			{
				lastSearchOptionsHash = currentSearchOptionsHash;
				lastSearchOptionPreprocessed.Clear();
				lastSearchOptionPreprocessed.AddRange(searchResultModel.SearchParams.Select(opts =>
				{
					var tmp = opts.CoreOptions;
					tmp.SearchInRawText = showRawMessages;
					try
					{
						return new SearchOptionsCacheEntry()
						{
							Options = opts, 
							PreprocessedOptions = tmp.Preprocess(),
							State = new Search.BulkSearchState()
						};
					}
					catch (Search.TemplateException)
					{
						return new SearchOptionsCacheEntry();
					}
				}).Where(x => x.Options != null));
			}
			foreach (var opts in lastSearchOptionPreprocessed)
				foreach (var r in FindAllHightlighRanges(msg, opts.PreprocessedOptions, opts.State, opts.Options.CoreOptions.ReverseSearch, null))
					yield return r;
		}

		void SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null)
		{
			var dmsg = screenBuffer.Messages[displayIndex];
			var msg = dmsg.Message;
			var line = GetTextToDisplay(msg).GetNthTextLine(dmsg.TextLineIndex);
			int newLineCharIndex;
			if ((flag & SelectionFlag.SelectBeginningOfLine) != 0)
				newLineCharIndex = 0;
			else if ((flag & SelectionFlag.SelectEndOfLine) != 0)
				newLineCharIndex = line.Length;
			else
			{
				newLineCharIndex = RangeUtils.PutInRange(0, line.Length,
					textCharIndex.GetValueOrDefault(selection.First.LineCharIndex));
				if ((flag & SelectionFlag.SelectBeginningOfNextWord) != 0)
					newLineCharIndex = StringUtils.FindNextWordInString(line, newLineCharIndex);
				else if ((flag & SelectionFlag.SelectBeginningOfPrevWord) != 0)
					newLineCharIndex = StringUtils.FindPrevWordInString(line, newLineCharIndex);
			}

			tracer.Info("Selecting line {0}. Display position = {1}", msg.GetHashCode(), displayIndex);

			bool resetEnd = (flag & SelectionFlag.PreserveSelectionEnd) == 0;

			Action doScrolling = () =>
			{
				if (displayIndex == 0 && screenBuffer.TopLineScrollValue > 1e3)
				{
					screenBuffer.TopLineScrollValue = 0;
					view.Invalidate();
				}
				if ((flag & SelectionFlag.NoHScrollToSelection) == 0)
				{
					view.HScrollToSelectedText(selection);
				}
				view.RestartCursorBlinking();
			};

			if (selection.First.Message != msg 
				|| selection.First.DisplayIndex != displayIndex 
				|| selection.First.LineCharIndex != newLineCharIndex
				|| resetEnd != selection.IsEmpty)
			{
				var oldSelection = selection;

				InvalidateTextLineUnderCursor();

				var tmp = new CursorPosition() 
				{
					Message = msg,
					Source = dmsg.Source,
					DisplayIndex = displayIndex,
					TextLineIndex = dmsg.TextLineIndex,
					LineCharIndex = newLineCharIndex
				};

				SetSelection(tmp, resetEnd ? tmp : new CursorPosition?());

				OnSelectionChanged();

				foreach (var displayIndexToInvalidate in oldSelection.GetDisplayIndexesRange().SymmetricDifference(selection.GetDisplayIndexesRange())
					.Where(idx => idx < screenBuffer.Messages.Count && idx >= 0))
				{
					view.InvalidateLine(screenBuffer.Messages[displayIndexToInvalidate].ToViewLine());
				}

				InvalidateTextLineUnderCursor();

				doScrolling();

				UpdateSelectionInplaceHighlightingFields();

				if (selection.First.Message != oldSelection.First.Message)
				{
					tracer.Info("focused message changed");
					OnFocusedMessageChanged();
					OnFocusedMessageBookmarkChanged();
				}
				else if (selection.First.TextLineIndex != oldSelection.First.TextLineIndex)
				{
					tracer.Info("focused message's line changed");
					OnFocusedMessageBookmarkChanged();
				}
			}
			else if ((flag & SelectionFlag.ScrollToViewEventIfSelectionDidNotChange) != 0)
			{
				doScrolling();
			}
		}

		bool BelongsToNonExistentSource(CursorPosition pos)
		{
			return pos.Message != null && !screenBuffer.ContainsSource(pos.Source);
		}

		async Task<List<ScreenBufferEntry>> GetSelectedDisplayMessagesEntries()
		{
			var viewLines = screenBuffer.Messages;

			Func<CursorPosition, bool> isGoodDisplayPosition = p =>
			{
				if (p.Message == null)
					return true;
				return p.DisplayIndex >= 0 && p.DisplayIndex < viewLines.Count;
			};

			var normSelection = selection.Normalize();
			if (normSelection.IsEmpty)
			{
				return new List<ScreenBufferEntry>();
			}

			Func<List<ScreenBufferEntry>> defaultGet = () =>
			{
				int selectedLinesCount = normSelection.Last.DisplayIndex - normSelection.First.DisplayIndex + 1;
				return viewLines.Skip(normSelection.First.DisplayIndex).Take(selectedLinesCount).ToList();
			};

			if (isGoodDisplayPosition(normSelection.First) && isGoodDisplayPosition(normSelection.Last))
			{
				// most common case: both positions are in the screen buffer at the moment
				return defaultGet();
			}

			CancellationToken cancellation = CancellationToken.None;

			IScreenBuffer tmpBuf = screenBufferFactory.CreateScreenBuffer(initialBufferPosition: InitialBufferPosition.Nowhere);
			await tmpBuf.SetSources(screenBuffer.Sources.Select(s => s.Source), cancellation);
			if (!await tmpBuf.MoveToBookmark(bookmarksFactory.CreateBookmark(normSelection.First.Message, 0), 
				BookmarkLookupMode.ExactMatch, cancellation))
			{
				// Impossible to load selected message into screen buffer. Rather impossible.
				return defaultGet();
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

				return new {Source = sourceBuf.Source, Messages = sourceMessages};
			}).ToList();

			await Task.WhenAll(tasks);
			cancellation.ThrowIfCancellationRequested();

			var messagesToSource = tasks.ToDictionary(
				t => (IMessagesCollection)new MessagesContainers.SimpleCollection(t.Result.Messages), t => t.Result.Source);

			return 
				new MessagesContainers.SimpleMergingCollection(messagesToSource.Keys)
				.Forward(0, int.MaxValue)
				.SelectMany(m =>
					Enumerable.Range(0, GetTextToDisplay(m.Message.Message).GetLinesCount()).Select(
						lineIdx => new ScreenBufferEntry()
						{
							TextLineIndex = lineIdx,
							Message = m.Message.Message,
							Source = messagesToSource[m.SourceCollection],
						}
					)
				)
				.TakeWhile(m => CursorPosition.Compare(CursorPosition.FromViewLine(m.ToViewLine(), 0), normSelection.Last) <= 0)
				.ToList();
		}

		async Task<List<SelectedTextLine>> GetSelectedTextLines(bool includeTime)
		{
			var ret = new List<SelectedTextLine>();
			if (selection.IsEmpty)
				return ret;
			var showRawMessages = presentationDataAccess.ShowRawMessages;
			var showMilliseconds = presentationDataAccess.ShowMilliseconds;
			var selectedDisplayEntries = await GetSelectedDisplayMessagesEntries();
			var normSelection = selection.Normalize();
			IMessage prevMessage = null;
			var sb = new StringBuilder();
			foreach (var i in selectedDisplayEntries.ZipWithIndex())
			{
				sb.Clear();
				var line = i.Value.Message.GetDisplayText(showRawMessages).GetNthTextLine(i.Value.TextLineIndex);
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
			if (presentationDataAccess.Coloring == ColoringMode.Threads)
				cl = msg.Thread.ThreadColor.ToHtmlColor();
			else if (presentationDataAccess.Coloring == ColoringMode.Sources)
				cl = ls.Color.ToHtmlColor();
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

		struct SelectedTextLine
		{
			public string Str;
			public IMessage Message;
			public int LineIndex;
			public bool IsSingleLineSelectionFragment;
		};
	};
};
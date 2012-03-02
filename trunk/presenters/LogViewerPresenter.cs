using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IView
	{
		void SetPresenter(Presenter presenter);
		void UpdateStarted();
		void UpdateFinished();
		void ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage);
		void UpdateScrollSizeToMatchVisibleCount();
		void Invalidate();
		void InvalidateMessage(MessageBase msg, int displayPosition);
		IEnumerable<Presenter.DisplayLine> GetVisibleMessagesIterator();
		void HScrollToSelectedText();
		void SetClipboard(string text);
		void DisplayEverythingFilteredOutMessage(bool displayOrHide);
		void DisplayNothingLoadedMessage(string messageToDisplayOrNull);
		void PopupContextMenu(object contextMenuPopupData);
		void RestartCursorBlinking();
	};

	public interface IModel
	{
		IMessagesCollection Messages { get; }
		IThreads Threads { get; }
		FiltersList DisplayFilters { get; }
		FiltersList HighlightFilters { get; }
		IBookmarks Bookmarks { get; }
		IUINavigationHandler UINavigationHandler { get; }
		LJTraceSource Tracer { get; }
		string MessageToDisplayWhenMessagesCollectionIsEmpty { get; }
		void ShiftUp();
		bool IsShiftableUp { get; }
		void ShiftDown();
		bool IsShiftableDown { get; }
		void ShiftAt(DateTime t);
		void ShiftHome();
		void ShiftToEnd();

		event EventHandler<Model.MessagesChangedEventArgs> OnMessagesChanged;
	};

	public interface ISearchResultModel: IModel
	{
		SearchAllOccurencesParams SearchParams { get; }
	};

	public struct CursorPosition
	{
		public MessageBase Message;
		public int DisplayIndex;
		public int TextLineIndex;
		public int LineCharIndex;

		public static int Compare(CursorPosition p1, CursorPosition p2)
		{
			if (p1.Message == null && p2.Message == null)
				return 0;
			if (p1.Message == null)
				return -1;
			if (p2.Message == null)
				return 1;
			int i;
			i = p1.DisplayIndex - p2.DisplayIndex;
			if (i != 0)
				return i;
			i = p1.TextLineIndex - p2.TextLineIndex;
			if (i != 0)
				return i;
			i = p1.LineCharIndex - p2.LineCharIndex;
			return i;
		}

		public static CursorPosition FromDisplayLine(Presenter.DisplayLine l, int charIndex)
		{
			return new CursorPosition()
			{
				Message = l.Message,
				DisplayIndex = l.DisplayLineIndex,
				TextLineIndex = l.TextLineIndex,
				LineCharIndex = charIndex
			};
		}
	};

	public struct SelectionInfo
	{
		public CursorPosition First { get { return first; } }
		public CursorPosition Last { get { return last; } }

		public MessageBase Message { get { return First.Message; } }
		public int DisplayPosition { get { return First.DisplayIndex; } }

		public void SetSelection(CursorPosition begin, CursorPosition? end)
		{
			this.first = begin;
			if (end.HasValue)
				this.last = end.Value;
		}

		public bool IsEmpty
		{
			get
			{
				if (First.Message == null || Last.Message == null)
					return true;
				return CursorPosition.Compare(First, Last) == 0;
			}
		}

		public bool IsInsideSelection(CursorPosition pos)
		{
			var normalized = this.Normalize();
			if (normalized.IsEmpty)
				return false;
			return CursorPosition.Compare(normalized.First, pos) <= 0 && CursorPosition.Compare(normalized.Last, pos) >= 0;
		}

		public SelectionInfo Normalize()
		{
			SelectionInfo ret = this;
			if (CursorPosition.Compare(ret.First, ret.Last) > 0)
			{
				var tmp = ret.Last;
				ret.last = ret.First;
				ret.first = tmp;
			}
			return ret;
		}

		public IEnumerable<int> GetDisplayIndexesRange()
		{
			if (IsEmpty)
			{
				yield return first.DisplayIndex;
			}
			else
			{
				SelectionInfo norm = Normalize();
				for (int i = norm.first.DisplayIndex; i <= norm.last.DisplayIndex; ++i)
					yield return i;
			}
		}

		CursorPosition first;
		CursorPosition last;
	};

	public struct SearchOptions
	{
		public Search.Options CoreOptions;
		public bool SearchHiddenText;
		public bool HighlightResult;
		public bool SearchOnlyWithinFirstMessage;
	};

	public struct SearchResult
	{
		public bool Succeeded;
		public int Position;
		public MessageBase Message;
	};

	public class SearchTemplateException : Exception
	{
	};

	public class Presenter : INextBookmarkCallback
	{
		#region Public interface

		public interface ICallback
		{
			void EnsureViewUpdated();
		};

		public Presenter(IModel model, IView view, ICallback callback)
		{
			this.model = model;
			this.searchResultModel = model as ISearchResultModel;
			this.view = view;
			this.callback = callback;
			
			this.tracer = model.Tracer;

			loadedMessagesCollection = new LoadedMessagesCollection(this);
			displayMessagesCollection = new DisplayMessagesCollection(this);

			this.model.OnMessagesChanged += (sender, e) =>
			{
				displayFiltersPreprocessingResultCacheIsValid = false;
				highlightFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.DisplayFilters.OnFiltersListChanged += (sender, e) =>
			{
				displayFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.DisplayFilters.OnFilteringEnabledChanged += (sender, e) =>
			{
				displayFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.DisplayFilters.OnPropertiesChanged += (sender, e) =>
			{
				if (e.ChangeAffectsPreprocessingResult)
					displayFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.HighlightFilters.OnFiltersListChanged += (sender, e) =>
			{
				highlightFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.HighlightFilters.OnPropertiesChanged += (sender, e) =>
			{
				if (e.ChangeAffectsPreprocessingResult)
					highlightFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.HighlightFilters.OnFilteringEnabledChanged += (sender, e) =>
			{
				highlightFiltersPreprocessingResultCacheIsValid = false;
			};
			DisplayHintIfMessagesIsEmpty();

			inplaceHightlightHandler = msg =>
			{
				if (searchResultModel == null)
					return null;
				var opts = searchResultModel.SearchParams;
				if (opts == null)
					return null;
				if (lastSearchOptions != opts)
				{
					lastSearchOptions = opts;
					lastSearchOptionPreprocessed = opts.Options.Preprocess();
					inplaceHightlightHandlerState = new Search.BulkSearchState();
				}
				var matchedTextRangle = LogJoint.Search.SearchInMessageText(msg, 
					lastSearchOptionPreprocessed, inplaceHightlightHandlerState);
				if (!matchedTextRangle.HasValue)
					return null;
				if (matchedTextRangle.Value.WholeTextMatched)
					return null;
				return new Tuple<int, int>(matchedTextRangle.Value.MatchBegin, matchedTextRangle.Value.MatchEnd);
			};
		}

		public void UpdateView()
		{
			InternalUpdate();
		}

		public event EventHandler FocusedMessageChanged;
		public event EventHandler BeginShifting;
		public event EventHandler EndShifting;
		public event EventHandler DefaultFocusedMessageAction;

		public SelectionInfo Selection { get { return selection; } }
		public LJTraceSource Tracer { get { return tracer; } }

		public DisplayMessagesCollection DisplayMessages { get { return displayMessagesCollection; } }

		public struct DisplayLine
		{
			public int DisplayLineIndex;
			public MessageBase Message;
			public int TextLineIndex;
		};

		public IEnumerable<DisplayLine> GetDisplayLines(int beginIdx, int endIdx)
		{
			int i = beginIdx;
			for (; i != endIdx; ++i)
			{
				var dm = displayMessages[i];
				yield return new DisplayLine() { DisplayLineIndex = i, Message = dm.DisplayMsg, TextLineIndex = dm.TextLineIndex };
			}
		}

		public int LoadedMessagesCount { get { return loadedMessagesCollection.Count; } }
		public int VisibleMessagesCount { get { return displayMessagesCollection.Count; } }

		public string DefaultFocusedMessageActionCaption { get { return defaultFocusedMessageActionCaption; } set { defaultFocusedMessageActionCaption = value; } }

		public Func<MessageBase, Tuple<int, int>> InplaceHighlightHandler 
		{ 
			get 
			{
				if (searchResultModel != null && searchResultModel.SearchParams != null)
					return inplaceHightlightHandler;
				return null;
			} 
		}

		public bool OulineBoxClicked(MessageBase msg, bool controlIsHeld)
		{
			if (!(msg is FrameBegin))
				return false;
			DoExpandCollapse(msg, controlIsHeld, new bool?());
			return true;
		}

		public void MessageRectClicked(
			CursorPosition pos,
			bool rightMouseButton, 
			bool shiftIsHeld,
			object preparedContextMenuPopupData)
		{
			if (rightMouseButton)
			{
				if (!selection.IsInsideSelection(pos))
					SetSelection(pos.DisplayIndex, SelectionFlag.None, pos.LineCharIndex);
				view.PopupContextMenu(preparedContextMenuPopupData);
			}
			else
			{
				SetSelection(pos.DisplayIndex, shiftIsHeld ? SelectionFlag.PreserveSelectionEnd : SelectionFlag.None, pos.LineCharIndex);
			}
		}

		public void GoToParentFrame()
		{
			if (selection.Message == null)
				return;
			bool inFrame = false;
			int level = 0;
			IThread focusedThread = selection.Message.Thread;
			bool found = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, int.MinValue, new ShiftPermissions(true, false)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (!inFrame)
				{
					if (it.Message == selection.Message)
						inFrame = true;
				}
				else
				{
					MessageBase.MessageFlag type = it.Message.Flags & MessageBase.MessageFlag.TypeMask;
					if (type == MessageBase.MessageFlag.EndFrame)
					{
						--level;
					}
					else if (type == MessageBase.MessageFlag.StartFrame)
					{
						if (level != 0)
						{
							++level;
						}
						else
						{
							SelectOnlyByLoadedMessageIndex(it.Index);
							found = true;
							break;
						}
					}
				}
			}
			if (!found)
			{
				foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, 1))
				{
					SelectOnlyByLoadedMessageIndex(it.Index);
				}
			}
		}

		public void GoToEndOfFrame()
		{
			if (selection.Message == null)
				return;
			bool inFrame = false;
			int level = 0;
			IThread focusedThread = selection.Message.Thread;
			bool found = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, int.MaxValue, new ShiftPermissions(false, true)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (!inFrame)
				{
					if (it.Message == selection.Message)
						inFrame = true;
				}
				else
				{
					MessageBase.MessageFlag type = it.Message.Flags & MessageBase.MessageFlag.TypeMask;
					if (type == MessageBase.MessageFlag.StartFrame)
					{
						++level;
					}
					else if (type == MessageBase.MessageFlag.EndFrame)
					{
						if (level != 0)
						{
							--level;
						}
						else
						{
							SelectOnlyByLoadedMessageIndex(it.Index);
							found = true;
							break;
						}
					}
				}
			}
			if (!found)
			{
				foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, -1))
				{
					SelectOnlyByLoadedMessageIndex(it.Index);
					break;
				}
			}
		}

		public void GoToNextMessageInThread()
		{
			if (selection.Message == null)
				return;
			IThread focusedThread = selection.Message.Thread;
			bool afterFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, int.MaxValue, new ShiftPermissions(false, true)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!afterFocused)
				{
					afterFocused = it.Message == selection.Message;
				}
				else
				{
					SelectOnlyByLoadedMessageIndex(it.Index);
					break;
				}
			}
		}

		public void GoToPrevMessageInThread()
		{
			if (selection.Message == null)
				return;
			IThread focusedThread = selection.Message.Thread;
			bool beforeFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, int.MinValue, new ShiftPermissions(true, false)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!beforeFocused)
				{
					beforeFocused = it.Message == selection.Message;
				}
				else
				{
					SelectOnlyByLoadedMessageIndex(it.Index);
					break;
				}
			}
		}

		public void GoToNextHighlightedMessage()
		{
			if (selection.Message == null || model.HighlightFilters == null)
				return;
			bool afterFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, int.MaxValue, new ShiftPermissions(false, true)))
			{
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!afterFocused)
				{
					afterFocused = it.Message == selection.Message;
				}
				else if (it.Message.IsHighlighted)
				{
					SelectOnlyByLoadedMessageIndex(it.Index);
					break;
				}
			}
		}

		public void GoToPrevHighlightedMessage()
		{
			if (selection.Message == null)
				return;
			IThread focusedThread = selection.Message.Thread;
			bool beforeFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, int.MinValue, new ShiftPermissions(true, false)))
			{
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!beforeFocused)
				{
					beforeFocused = it.Message == selection.Message;
				}
				else if (it.Message.IsHighlighted)
				{
					SelectOnlyByLoadedMessageIndex(it.Index);
					break;
				}
			}
		}

		public void InvalidateFocusedMessage()
		{
			if (selection.Message != null)
			{
				view.InvalidateMessage(selection.Message, selection.DisplayPosition);
			}
		}

		public enum SelectionFlag
		{
			None = 0,
			PreserveSelectionEnd = 1,
			SelectBeginningOfLine = 2,
			SelectEndOfLine = 4,
			ShowExtraLinesAroundSelection = 8,
			SuppressOnFocusedMessageChanged = 16,
			NoHScrollToSelection = 32
		};

		static IEnumerable<int> EnumFileRange(FileRange.Range r)
		{
			for (long i = r.Begin; i < r.End; ++i)
				yield return (int)i;
		}

		public void SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null)
		{
			using (tracer.NewFrame)
			{
				var dmsg = displayMessages[displayIndex];
				var msg = dmsg.DisplayMsg;
				var line = msg.GetNthTextLine(dmsg.TextLineIndex);
				int newLineCharIndex;
				if ((flag & SelectionFlag.SelectBeginningOfLine) != 0)
					newLineCharIndex = 0;
				else if ((flag & SelectionFlag.SelectEndOfLine) != 0)
					newLineCharIndex = line.Length;
				else
					newLineCharIndex  = Utils.PutInRange(0, line.Length,
						textCharIndex.GetValueOrDefault(selection.First.LineCharIndex));

				tracer.Info("Selecting line {0}. Display position = {1}", msg.GetHashCode(), displayIndex);

				bool resetEnd = (flag & SelectionFlag.PreserveSelectionEnd) == 0;

				if (selection.First.Message != msg 
					|| selection.First.DisplayIndex != displayIndex 
					|| selection.First.LineCharIndex != newLineCharIndex
					|| resetEnd != selection.IsEmpty)
				{
					var oldSelection = selection;

					InvalidateFocusedMessage();

					var tmp = new CursorPosition() { 
						Message = msg, DisplayIndex = displayIndex, TextLineIndex = dmsg.TextLineIndex, LineCharIndex = newLineCharIndex };

					selection.SetSelection(tmp, resetEnd ? tmp : new CursorPosition?());

					foreach (var displayIndexToInvalidate in oldSelection.GetDisplayIndexesRange().SymmetricDifference(selection.GetDisplayIndexesRange()))
					{
						view.InvalidateMessage(displayMessages[displayIndexToInvalidate].DisplayMsg, displayIndexToInvalidate);
					}

					InvalidateFocusedMessage();

					view.ScrollInView(displayIndex, (flag & SelectionFlag.ShowExtraLinesAroundSelection) != 0);
					if ((flag & SelectionFlag.NoHScrollToSelection) == 0)
						view.HScrollToSelectedText();
					view.RestartCursorBlinking();

					if (selection.First.Message != oldSelection.First.Message)
					{
						OnFocusedMessageChanged();
						tracer.Info("Focused line changed to the new selection");
					}
				}
			}
		}

		public void CopySelectionToClipboard()
		{
			if (selection.IsEmpty)
				return;
			StringBuilder sb = new StringBuilder();
			var normSelection = selection.Normalize();
			int selectedLinesCount = normSelection.Last.DisplayIndex - normSelection.First.DisplayIndex + 1;
			foreach (var i in displayMessages.Skip(normSelection.First.DisplayIndex).Take(selectedLinesCount).ZipWithIndex())
			{
				if (i.Key > 0)
					sb.AppendLine();
				var line = i.Value.DisplayMsg.GetNthTextLine(i.Value.TextLineIndex);
				int beginIdx = i.Key == 0 ? normSelection.First.LineCharIndex : 0;
				int endIdx = i.Key == selectedLinesCount - 1 ? normSelection.Last.LineCharIndex : line.Length;
				line.SubString(beginIdx, endIdx - beginIdx).Append(sb);
			}
			if (sb.Length > 0)
				view.SetClipboard(sb.ToString());
		}

		public void SelectMessageAt(DateTime d, NavigateFlag alignFlag)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Date={0}, alag={1}", d, alignFlag);

				if (visibleCount == 0)
				{
					tracer.Info("No visible messages. Returning");
					return;				
				}

				int lowerBound = ListUtils.BinarySearch(displayMessages, 0, displayMessages.Count, x => x.DisplayMsg.Time < d);
				int upperBound = ListUtils.BinarySearch(displayMessages, 0, displayMessages.Count, x => x.DisplayMsg.Time <= d);

				int idx;
				switch (alignFlag & NavigateFlag.AlignMask)
				{
					case NavigateFlag.AlignTop:
						if (upperBound != lowerBound) // if the date in question is present among messages
							idx = lowerBound; // return the first message with this date
						else // ok, no messages with date (d), lowerBound points to the message with date greater than (d)
							idx = lowerBound - 1; // return the message before lowerBound, it will have date less than (d)
						break;
					case NavigateFlag.AlignBottom:
						if (upperBound > lowerBound) // if the date in question is present among messages
							idx = upperBound - 1; // return the last message with date (d)
						else // no messages with date (d), lowerBound points to the message with date greater than (d)
							idx = upperBound; // we bound to return upperBound
						break;
					case NavigateFlag.AlignCenter:
						if (upperBound > lowerBound) // if the date in question is present among messages
						{
							idx = (lowerBound + upperBound - 1) / 2; // return the middle of the range
						}
						else
						{
							// otherwise choose the message nearest message

							int p1 = Math.Max(lowerBound - 1, 0);
							int p2 = Math.Min(upperBound, visibleCount - 1);
							MessageBase m1 = displayMessages[p1].DisplayMsg;
							MessageBase m2 = displayMessages[p2].DisplayMsg;
							if (Math.Abs((m1.Time - d).Ticks) < Math.Abs((m2.Time - d).Ticks))
							{
								idx = p1;
							}
							else
							{
								idx = p2;
							}
						}
						break;
					default:
						throw new ArgumentException();
				}


				if (idx < 0)
					idx = 0;
				else if (idx >= visibleCount)
					idx = visibleCount - 1;

				tracer.Info("Index of the line to be selected: {0}", idx);

				if (idx >= 0 && idx < visibleCount)
				{
					SetSelection(idx, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
				}
				else
				{
					tracer.Warning("The index is out of visible range [0-{0})", visibleCount);
				}
			}
		}

		IEnumerable<IndexedMessage> MakeSearchScope(bool wrapAround, bool reverseSearch, int startFromMessage)
		{
			if (wrapAround)
			{
				if (reverseSearch)
				{
					using (Shifter shifter = loadedMessagesCollection.CreateShifter(new ShiftPermissions(true, false)))
					{
						int? firstMessageHash = new int?();
						foreach (var m in loadedMessagesCollection.Reverse(startFromMessage, int.MinValue, shifter))
						{
							if (!firstMessageHash.HasValue)
								firstMessageHash = m.Message.GetHashCode();
							yield return m;
						}
						shifter.ShiftToEnd();
						foreach (var m in loadedMessagesCollection.Reverse(int.MaxValue, startFromMessage, shifter))
						{
							if (firstMessageHash.GetValueOrDefault(0) == m.Message.GetHashCode())
								break;
							yield return m;
						}
					}
				}
				else
				{
					using (Shifter shifter = loadedMessagesCollection.CreateShifter(new ShiftPermissions(false, true)))
					{
						int? firstMessageHash = new int?();
						foreach (var m in loadedMessagesCollection.Forward(startFromMessage, int.MaxValue, shifter))
						{
							if (!firstMessageHash.HasValue)
								firstMessageHash = m.Message.GetHashCode();
							yield return m;
						}
						shifter.ShiftHome();
						foreach (var m in loadedMessagesCollection.Forward(int.MinValue, int.MaxValue, shifter))
						{
							if (firstMessageHash.GetValueOrDefault(0) == m.Message.GetHashCode())
								break;
							yield return m;
						}
					}
				}
			}
			else
			{
				var scope = reverseSearch ? 
					loadedMessagesCollection.Reverse(startFromMessage, int.MinValue, new ShiftPermissions(true, false)) :
					loadedMessagesCollection.Forward(startFromMessage, int.MaxValue, new ShiftPermissions(false, true));
				foreach (var m in scope)
					yield return m;
			}
		}

		private void EnsureViewUpdated()
		{
			if (callback != null)
				callback.EnsureViewUpdated();
			else
				InternalUpdate();
		}

		public SearchResult Search(SearchOptions opts)
		{
			// init return value with default value (rv.Succeeded = false)
			SearchResult rv = new SearchResult();

			bool emptyTemplate = string.IsNullOrEmpty(opts.CoreOptions.Template);
			bool reverseSearch = opts.CoreOptions.ReverseSearch;

			CursorPosition startFrom = new CursorPosition();
			var normSelection = selection.Normalize();
			if (!reverseSearch)
			{
				var tmp = normSelection.Last;
				if (tmp.Message == null)
					tmp = normSelection.First;
				if (tmp.Message != null)
				{
					startFrom = tmp;
				}
			}
			else
			{
				if (normSelection.First.Message != null)
				{
					startFrom = normSelection.First;
				}
			}

			int startFromMessage = 0;
			int startFromTextPosition = 0;

			if (startFrom.Message != null)
			{
				startFromMessage = DisplayIndex2LoadedMessageIndex(startFrom.DisplayIndex);
				var startLine = startFrom.Message.GetNthTextLine(startFrom.TextLineIndex);
				startFromTextPosition = (startLine.StartIndex - startFrom.Message.Text.StartIndex) + startFrom.LineCharIndex;
			}

			Search.PreprocessedOptions preprocessedOptions = opts.CoreOptions.Preprocess();
			Search.BulkSearchState bulkSearchState = new Search.BulkSearchState();
			
			int messagesProcessed = 0;

			foreach (IndexedMessage it in MakeSearchScope(opts.CoreOptions.WrapAround, reverseSearch, startFromMessage))
			{
				if (opts.SearchOnlyWithinFirstMessage && messagesProcessed == 1)
					break;

				++messagesProcessed;

				MessageBase.MessageFlag f = it.Message.Flags;

				if ((f & (MessageBase.MessageFlag.HiddenBecauseOfInvisibleThread | MessageBase.MessageFlag.HiddenAsFilteredOut)) != 0)
					continue; // dont search in excluded lines

				if ((f & MessageBase.MessageFlag.HiddenAsCollapsed) != 0)
					if (!opts.SearchHiddenText) // if option is specified
						continue; // dont search in lines hidden by collapsing

				int? startFromTextPos = null;
				if (!emptyTemplate && messagesProcessed == 1)
					startFromTextPos = startFromTextPosition;

				var match = LogJoint.Search.SearchInMessageText(it.Message, preprocessedOptions, bulkSearchState, startFromTextPos);

				if (!match.HasValue)
					continue;

				if (emptyTemplate && messagesProcessed == 1)
					continue;

				// init successful return value
				rv.Succeeded = true;
				rv.Position = it.Index;
				rv.Message = it.Message;

				int displayIndex = LoadedIndex2DisplayIndex(it.Index);

				if (opts.HighlightResult)
				{
					var txt = it.Message.Text;
					var m = match.Value;
					it.Message.EnumLines((line, lineIdx) =>
					{
						var lineBegin = line.StartIndex - txt.StartIndex;
						var lineEnd = lineBegin + line.Length;
						if (m.MatchBegin >= lineBegin && m.MatchBegin <= lineEnd)
							SetSelection(displayIndex + lineIdx, SelectionFlag.None, m.MatchBegin - lineBegin);
						if (m.MatchEnd >= lineBegin && m.MatchEnd <= lineEnd)
							SetSelection(displayIndex + lineIdx, SelectionFlag.PreserveSelectionEnd, m.MatchEnd - lineBegin);
						return true;
					});
				}
				else
				{
					SetSelection(displayIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
				}

				view.HScrollToSelectedText();

				view.Invalidate();

				break;
			}

			if (!rv.Succeeded && selection.Message == null)
				MoveSelection(reverseSearch ? 0 : visibleCount - 1, 
					MoveSelectionFlag.ForwardShiftingMode, 
					SelectionFlag.ShowExtraLinesAroundSelection);

			// return value initialized by-default as non-successful search

			return rv;
		}

		public enum MoveSelectionFlag
		{
			None = 0,
			ForwardShiftingMode = 1,
			BackwardShiftingMode = 2
		};

		public void MoveSelection(int newDisplayPosition, MoveSelectionFlag moveFlags, SelectionFlag selFlags)
		{
			bool forwardMode = (moveFlags & MoveSelectionFlag.BackwardShiftingMode) == 0;
			using (var shifter = displayMessagesCollection.CreateShifter(GetShiftPermissions()))
			{
				foreach (var l in
					forwardMode ?
						displayMessagesCollection.Forward(newDisplayPosition, int.MaxValue, shifter) :
						displayMessagesCollection.Reverse(newDisplayPosition, int.MinValue, shifter)
				)
				{
					SetSelection(l.Index, selFlags);
					break;
				}
			}
		}

		public void Next()
		{
			MoveSelection(selection.DisplayPosition + 1, 
				MoveSelectionFlag.BackwardShiftingMode,
				SelectionFlag.ShowExtraLinesAroundSelection);
		}

		public void Prev()
		{
			MoveSelection(selection.DisplayPosition - 1, 
				MoveSelectionFlag.ForwardShiftingMode,
				SelectionFlag.ShowExtraLinesAroundSelection);
		}


		public DateTime? FocusedMessageTime
		{
			get
			{
				if (selection.Message != null)
					return selection.Message.Time;
				return null;
			}
		}

		public MessageBase FocusedMessage
		{
			get
			{
				return selection.Message;
			}
		}

		public LoadedMessagesCollection LoadedMessages
		{
			get { return loadedMessagesCollection; }
		}

		// It takes O(n) time
		// todo: optimize by using binary rearch over mergedMessages
		public int DisplayIndex2LoadedMessageIndex(int dposition)
		{
			MessageBase l = displayMessages[dposition].DisplayMsg;
			if (l == null)
				return -1;
			foreach (IndexedMessage i in loadedMessagesCollection.Forward(0, int.MaxValue))
				if (i.Message == l)
					return i.Index;
			return -1;
		}

		public bool NextBookmark(bool forward)
		{
			if (selection.Message == null || model.Bookmarks == null)
				return false;
			IBookmark bmk = model.Bookmarks.GetNext(selection.Message, forward, this);
			SelectMessageAt(bmk);
			return bmk != null;
		}

		public bool SelectMessageAt(IBookmark bmk)
		{
			return SelectMessageAt(bmk, null);
		}

		public bool SelectMessageAt(IBookmark bmk, Predicate<MessageBase> messageMatcherWhenNoHashIsSpecified)
		{
			if (bmk == null)
				return false;

			int begin;
			int end;
			GetDateEqualRange(bmk.Time, out begin, out end);

			if (begin == end)
			{
				// If we are at the boundary positions
				if (end == 0 || begin == mergedMessages.Count)
				{
					// We can try to load the messages around the time of the bookmark
					OnBeginShifting();
					model.ShiftAt(bmk.Time);
					OnEndShifting();

					// Refresh the lines. The messages are expected to be changed.
					this.InternalUpdate();

					// Find a new equal range
					GetDateEqualRange(bmk.Time, out begin, out end);
				}
				else
				{
					// We found empty equal range of bookmark's time,
					// this range points to the middle of mergedMessages array.
					// That means that the bookmark is invalid: it points 
					// into the time that doesn't exist among messages.
				}
			}

			if (bmk.MessageHash != 0)
			{
				// Search for the bookmarked message by hash
				for (; begin != end; ++begin)
				{
					MessageBase l = mergedMessages[begin].LoadedMsg;
					if (l.GetHashCode() == bmk.MessageHash)
						break;
				}
			}
			else if (messageMatcherWhenNoHashIsSpecified != null)
			{
				// Search for the bookmarked message by user provided criteria
				for (; begin != end; ++begin)
				{
					MessageBase l = mergedMessages[begin].LoadedMsg;
					if (messageMatcherWhenNoHashIsSpecified(l))
						break;
				}
			}
			else
			{
				begin = end;
			}

			if (begin == end)
				return false;

			int idx = LoadedIndex2DisplayIndex(begin);
			if (idx < 0)
				return false;

			SetSelection(idx, SelectionFlag.SelectEndOfLine | SelectionFlag.ShowExtraLinesAroundSelection | SelectionFlag.NoHScrollToSelection);
			SetSelection(idx, SelectionFlag.SelectBeginningOfLine | SelectionFlag.PreserveSelectionEnd);
			return true;
		}

		public bool DoExpandCollapse(MessageBase line, bool recursive, bool? collapse)
		{
			using (tracer.NewFrame)
			{
				FrameBegin fb = line as FrameBegin;
				if (fb == null)
					return false;

				if (!recursive && collapse.HasValue)
					if (collapse.Value == fb.Collapsed)
						return false;

				fb.Collapsed = !fb.Collapsed;
				if (recursive)
					foreach (MessageBase i in EnumFrameContent(fb))
					{
						FrameBegin fb2 = i as FrameBegin;
						if (fb2 != null)
							fb2.Collapsed = fb.Collapsed;
					}

				InternalUpdate();

				return true;
			}
		}

		public IEnumerable<MessageBase> EnumFrameContent(FrameBegin fb)
		{
			bool inFrame = false;
			foreach (IndexedMessage il in loadedMessagesCollection.Forward(0, int.MaxValue))
			{
				if (il.Message.Thread != fb.Thread)
					continue;
				bool stop = false;
				if (!inFrame)
					inFrame = il.Message == fb;
				if (il.Message == fb.End)
					stop = true;
				if (inFrame)
					yield return il.Message;
				if (stop)
					break;
			}
		}

		public void EnsureNotCollapsed(int position)
		{
			IEnumerator<IndexedMessage> it = loadedMessagesCollection.Reverse(position, -1).GetEnumerator();
			if (!it.MoveNext())
				return;
			IThread thread = it.Current.Message.Thread;
			int frameCount = 0;
			for (; it.MoveNext(); )
			{
				if (it.Current.Message.Thread != thread)
					continue;
				MessageBase.MessageFlag f = it.Current.Message.Flags;
				switch (f & MessageBase.MessageFlag.TypeMask)
				{
					case MessageBase.MessageFlag.StartFrame:
						if (frameCount == 0)
							DoExpandCollapse(it.Current.Message, false, false);
						else
							--frameCount;
						break;
					case MessageBase.MessageFlag.EndFrame:
						++frameCount;
						break;
				}
			}
		}

		void SelectOnlyByLoadedMessageIndex(int loadedMessageIndex)
		{
			EnsureNotCollapsed(loadedMessageIndex);
			int displayIndex = LoadedIndex2DisplayIndex(loadedMessageIndex);
			SetSelection(displayIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
		}


		public void SelectFirstMessage()
		{
			using (tracer.NewFrame)
			{
				MoveSelection(0,
					MoveSelectionFlag.ForwardShiftingMode, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
			}
		}

		public void SelectLastMessage()
		{
			using (tracer.NewFrame)
			{
				MoveSelection(visibleCount - 1,
					MoveSelectionFlag.BackwardShiftingMode, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
			}
		}

		public void ShiftHome()
		{
			using (tracer.NewFrame)
			{
				model.ShiftHome();
			}
		}

		public void ShiftToEnd()
		{
			using (tracer.NewFrame)
			{
				model.ShiftToEnd();
			}
		}

		public bool BookmarksAvailable
		{
			get { return model.Bookmarks != null; }
		}

		public void ToggleBookmark(MessageBase line)
		{
			if (model.Bookmarks == null)
				return;
			model.Bookmarks.ToggleBookmark(line);
			InternalUpdate();
		}

		public void PerformDefaultFocusedMessageAction()
		{
			if (DefaultFocusedMessageAction != null)
				DefaultFocusedMessageAction(this, EventArgs.Empty);
		}

		public abstract class MessagesCollection : IMessagesCollection
		{
			protected readonly Presenter control;

			public MessagesCollection(Presenter control)
			{
				this.control = control;
			}

			#region IMessagesCollection

			public abstract int Count { get; }

			public IEnumerable<IndexedMessage> Forward(int begin, int end)
			{
				return Forward(begin, end, new ShiftPermissions());
			}

			public IEnumerable<IndexedMessage> Reverse(int begin, int end)
			{
				return Reverse(begin, end, new ShiftPermissions());
			}

			#endregion

			public IEnumerable<IndexedMessage> Forward(int begin, int end, ShiftPermissions shiftPerm)
			{
				using (Shifter shifter = CreateShifter(shiftPerm))
				{
					foreach (var m in Forward(begin, end, shifter))
						yield return m;
				}
			}

			public IEnumerable<IndexedMessage> Reverse(int begin, int end, ShiftPermissions shiftPerm)
			{
				using (Shifter shifter = CreateShifter(shiftPerm))
				{
					foreach (var m in Reverse(begin, end, shifter))
						yield return m;
				}
			}

			internal Shifter CreateShifter(ShiftPermissions shiftPerm)
			{
				return new Shifter(control, this, shiftPerm);
			}

			internal abstract IEnumerable<IndexedMessage> Forward(int begin, int end, Shifter shifter);
			internal abstract IEnumerable<IndexedMessage> Reverse(int begin, int end, Shifter shifter);
		};

		public class LoadedMessagesCollection : MessagesCollection
		{
			public LoadedMessagesCollection(Presenter control): base(control)
			{
			}

			public override int Count
			{
				get { return control.mergedMessages.Count; }
			}

			internal override IEnumerable<IndexedMessage> Forward(int begin, int end, Shifter shifter)
			{
				shifter.InitialValidatePositions(ref begin, ref end, false);
				for (; begin < end; ++begin)
				{
					if (!shifter.ValidatePositions(ref begin, ref end, false))
						yield break;
					yield return new IndexedMessage(begin, control.mergedMessages[begin].LoadedMsg);
				}
			}

			internal override IEnumerable<IndexedMessage> Reverse(int begin, int end, Shifter shifter)
			{
				shifter.InitialValidatePositions(ref begin, ref end, true);

				for (; begin > end; --begin)
				{
					if (!shifter.ValidatePositions(ref begin, ref end, true))
						yield break;
					yield return new IndexedMessage(begin, control.mergedMessages[begin].LoadedMsg);
				}
			}
		};

		public class DisplayMessagesCollection : MessagesCollection
		{
			public DisplayMessagesCollection(Presenter control)
				: base(control)
			{
			}

			public override int Count
			{
				get { return control.displayMessages.Count; }
			}

			internal struct IndexedDisplayMessage
			{
				public int Index;
				public MessageBase Message;
				public int TextLineIndex;
				public IndexedDisplayMessage(int index, MessageBase message, int textLineIndex)
				{
					Index = index;
					Message = message;
					TextLineIndex = textLineIndex;
				}

				public IndexedMessage ToIndexedMessage() { return new IndexedMessage(Index, Message); }
			};

			internal IEnumerable<IndexedDisplayMessage> ForwardInternal(int begin, int end, Shifter shifter)
			{
				shifter.InitialValidatePositions(ref begin, ref end, false);
				for (; begin < end; ++begin)
				{
					if (!shifter.ValidatePositions(ref begin, ref end, false))
						yield break;
					var displayMsg = control.displayMessages[begin];
					yield return new IndexedDisplayMessage(begin, displayMsg.DisplayMsg, displayMsg.TextLineIndex);
				}
			}

			internal override IEnumerable<IndexedMessage> Forward(int begin, int end, Shifter shifter)
			{
				return ForwardInternal(begin, end, shifter).Select(i => i.ToIndexedMessage());
			}

			internal IEnumerable<IndexedDisplayMessage> ReverseInternal(int begin, int end, Shifter shifter)
			{
				shifter.InitialValidatePositions(ref begin, ref end, true);
				for (; begin > end; --begin)
				{
					if (!shifter.ValidatePositions(ref begin, ref end, true))
						yield break;
					var displayMsg = control.displayMessages[begin];
					yield return new IndexedDisplayMessage(begin, displayMsg.DisplayMsg, displayMsg.TextLineIndex);
				}
			}

			internal override IEnumerable<IndexedMessage> Reverse(int begin, int end, Shifter shifter)
			{
				return ReverseInternal(begin, end, shifter).Select(i => i.ToIndexedMessage());
			}
		};

		public ShiftPermissions GetShiftPermissions()
		{
			return new ShiftPermissions(selection.DisplayPosition == 0, selection.DisplayPosition == (visibleCount - 1));
		}

		public void OnShowFiltersClicked()
		{
			model.UINavigationHandler.ShowFiltersView();
		}

		#endregion

		#region Protected methods

		protected virtual void OnFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		protected virtual void OnBeginShifting()
		{
			if (BeginShifting != null)
				BeginShifting(this, EventArgs.Empty);
		}

		protected virtual void OnEndShifting()
		{
			if (EndShifting != null)
				EndShifting(this, EventArgs.Empty);
		}

		#endregion

		#region Implementation

		struct MergedMessagesEntry
		{
			public MessageBase LoadedMsg;
			public FiltersList.PreprocessingResult DisplayFiltersPreprocessingResult;
			public FiltersList.PreprocessingResult HighlightFiltersPreprocessingResult;
		};

		struct DisplayMessagesEntry
		{
			public MessageBase DisplayMsg;
			public int TextLineIndex;
		};

		public struct ShiftPermissions
		{
			public readonly bool AllowUp;
			public readonly bool AllowDown;
			public ShiftPermissions(bool allowUp, bool allowDown)
			{
				this.AllowUp = allowUp;
				this.AllowDown = allowDown;
			}
		};

		internal class Shifter : IDisposable
		{
			bool active;
			IMessagesCollection collection;
			Presenter control;
			ShiftPermissions shiftPerm;

			public Shifter(Presenter control, IMessagesCollection collection, ShiftPermissions shiftPerm)
			{
				this.control = control;
				this.shiftPerm = shiftPerm;
				this.collection = collection;
			}

			public void InitialValidatePositions(ref int begin, ref int end, bool reverse)
			{
				if (!reverse)
				{
					if (!shiftPerm.AllowUp)
						begin = Math.Max(0, begin);
					if (!shiftPerm.AllowDown)
						end = Math.Min(collection.Count, end);
				}
				else
				{
					if (!shiftPerm.AllowUp)
						end = Math.Max(-1, end);
					if (!shiftPerm.AllowDown)
						begin = Math.Min(collection.Count - 1, begin);
				}
			}

			public bool ValidatePositions(ref int begin, ref int end, bool reverse)
			{
				if (shiftPerm.AllowUp)
				{
					while (begin < 0)
					{
						int delta = ShiftUp();
						if (delta == 0)
							return false;
						end += delta;
						begin += delta;
					}
				}
				if (shiftPerm.AllowDown)
				{
					while (begin >= collection.Count)
					{
						int delta = ShiftDown();
						if (delta == 0)
							return false;
						end -= delta;
						begin -= delta;
					}
				}
				return true;
			}

			public int ShiftDown()
			{
				// Check if it is possible to shift down. Return immediately if not.
				// Just to avoid annoying flickering.
				if (!control.model.IsShiftableDown)
					return 0;

				// Parameters of the current last message
				long lastPosition = 0; // hash
				int lastIndex = -1; // display index

				MessageBase lastMessage = null;

				int countBeforeShifting = collection.Count;

				// Get the info about the last (pivot) message.
				// Later after actual shifting it will be used to find the position 
				// where to start the iterating from.
				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
				{
					lastPosition = l.Message.Position;
					lastIndex = l.Index;
					lastMessage = l.Message;
					break;
				}

				// That can happen if there are no messages loaded yet. Shifting cannot be done.
				if (lastIndex < 0)
					return 0;

				// Start shifting it has not been started yet
				EnsureActive();

				// Ask the host to do actual shifting
				control.model.ShiftDown();

				// Remerge the messages. 
				control.EnsureViewUpdated();

				// Check if the last message has changed. 
				// It is an optimization for the case when there is no room to shift.
				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
				{
					if (lastPosition == l.Message.Position)
						return 0;
					break;
				}

				// Search for the pivot message among new merged collection
				foreach (IndexedMessage l in collection.Forward(0, int.MaxValue))
					if (l.Message.Position == lastPosition)
						return lastIndex - l.Index;

				// We didn't find our pivot message after the shifting.
				return 0;
			}

			public int ShiftUp()
			{
				// This function is symmetric to ShiftDown(). See comments there.

				if (!control.model.IsShiftableUp)
					return 0;

				long firstPosition = 0;
				int firstIndex = -1;

				int countBeforeShifting = collection.Count;

				foreach (IndexedMessage l in collection.Forward(0, 1))
				{
					firstPosition = l.Message.Position;
					firstIndex = l.Index;
					break;
				}

				if (firstIndex < 0)
					return 0;

				EnsureActive();

				control.model.ShiftUp();

				control.EnsureViewUpdated();

				foreach (IndexedMessage l in collection.Forward(0, 1))
				{
					if (firstPosition == l.Message.Position)
						return 0;
					break;
				}

				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
					if (l.Message.Position == firstPosition)
						return l.Index - firstIndex;

				return 0;
			}

			public void ShiftHome()
			{
				if (control.model.IsShiftableUp)
					EnsureActive();
				control.model.ShiftHome();
				control.EnsureViewUpdated();
			}

			public void ShiftToEnd()
			{
				if (control.model.IsShiftableDown)
					EnsureActive();
				control.model.ShiftToEnd();
				control.EnsureViewUpdated();
			}

			void EnsureActive()
			{
				if (active)
					return;
				active = true;
				control.OnBeginShifting();
			}

			#region IDisposable Members

			public void Dispose()
			{
				if (!active)
					return;
				control.OnEndShifting();
			}

			#endregion
		};

		static MergedMessagesEntry InitMergedMessagesEntry(
			MessageBase message, 
			MergedMessagesEntry cachedMessageEntry,
			ThreadLocal<FiltersList> displayFilters,
			ThreadLocal<FiltersList> highlighFilters,
			bool displayFiltersPreprocessingResultCacheIsValid,
			bool highlightFiltersPreprocessingResultCacheIsValid)
		{
			MergedMessagesEntry ret;

			ret.LoadedMsg = message;

			//displayFiltersPreprocessingResultCacheIsValid = false;

			if (displayFiltersPreprocessingResultCacheIsValid)
				ret.DisplayFiltersPreprocessingResult = cachedMessageEntry.DisplayFiltersPreprocessingResult;
			else
				ret.DisplayFiltersPreprocessingResult = displayFilters.Value.PreprocessMessage(message);

			if (highlightFiltersPreprocessingResultCacheIsValid)
				ret.HighlightFiltersPreprocessingResult = cachedMessageEntry.HighlightFiltersPreprocessingResult;
			else
				ret.HighlightFiltersPreprocessingResult = highlighFilters.Value.PreprocessMessage(message);

			return ret;
		}

		static IEnumerable<IndexedMessage> ToEnumerable(IEnumerator<IndexedMessage> enumerator, int count)
		{
			for (; ; )
			{
				if (count == 0)
					break;
				yield return enumerator.Current;
				--count;
				if (count == 0)
					break;
				enumerator.MoveNext();
			}
		}

		struct FocusedMessageFinder
		{
			Presenter presenter;
			SelectionInfo prevFocused;
			long prevFocusedPosition1;
			long prevFocusedPosition2;
			IndexedMessage newFocused1;
			IndexedMessage newFocused2;

			public FocusedMessageFinder(Presenter presenter)
			{
				this.presenter = presenter;
				prevFocused = presenter.selection;
				prevFocusedPosition1 = prevFocused.First.Message != null ? prevFocused.First.Message.Position : long.MinValue;
				prevFocusedPosition2 = prevFocused.Last.Message != null ? prevFocused.Last.Message.Position : long.MinValue;
				newFocused1 = new IndexedMessage();
				newFocused2 = new IndexedMessage();
			}
			public void HandleNewDisplayMessage(MessageBase msg, int displayIndex)
			{
				if (prevFocusedPosition1 == msg.Position)
					newFocused1 = new IndexedMessage() { Message = msg, Index = displayIndex };
				else if (prevFocusedPosition2 == msg.Position)
					newFocused2 = new IndexedMessage() { Message = msg, Index = displayIndex };
			}
			public void SetFoundSelection()
			{
				if (newFocused2.Message != null)
				{
					presenter.SetSelection(newFocused2.Index + prevFocused.Last.TextLineIndex, 
						SelectionFlag.SuppressOnFocusedMessageChanged, prevFocused.Last.LineCharIndex);
				}
				if (newFocused1.Message != null)
				{
					presenter.SetSelection(newFocused1.Index + prevFocused.First.TextLineIndex,
						SelectionFlag.PreserveSelectionEnd, prevFocused.First.LineCharIndex);
				}
				else
				{
					presenter.OnFocusedMessageChanged();
				}

				if (presenter.selection.First.Message != null)
				{
					if (prevFocused.First.Message == null)
					{
						presenter.view.ScrollInView(presenter.selection.First.DisplayIndex, true);
					}
				}
			}
		};

		void InternalUpdate()
		{
			using (tracer.NewFrame)
			using (new ScopedGuard(view.UpdateStarted, view.UpdateFinished))
			using (var threadsBulkProcessing = model.Threads.StartBulkProcessing())
			{
				++mergedMessagesVersion;			

				var focusedMessageFinder = new FocusedMessageFinder(this);
				
				int modelMessagesCount = model.Messages.Count;

				ResizeMergedMessages(modelMessagesCount);
				UpdateDisplayMessagesCapacity(modelMessagesCount);

				int loadedCount = 0;

				FiltersList displayFilters = model.DisplayFilters;
				FiltersList.BulkProcessingHandle displayFiltersProcessingHandle = BeginBulkProcessing(displayFilters);

				FiltersList hlFilters = model.HighlightFilters;
				FiltersList.BulkProcessingHandle hlFiltersProcessingHandle = BeginBulkProcessing(hlFilters);

				IBookmarksHandler bmk = CreateBookmarksHandler();

				using (var enumerator = model.Messages.Forward(0, int.MaxValue).GetEnumerator())
				using (ThreadLocal<FiltersList> displayFiltersThreadLocal = new ThreadLocal<FiltersList>(() => displayFilters.Clone()))
				using (ThreadLocal<FiltersList> highlightFiltersThreadLocal = new ThreadLocal<FiltersList>(() => hlFilters.Clone()))
				{
					enumerator.MoveNext();
					foreach (MergedMessagesEntry preprocessedMessage in 
						ToEnumerable(enumerator, modelMessagesCount)
						.AsParallel().AsOrdered()
						.Select(im => InitMergedMessagesEntry(
							im.Message, mergedMessages[im.Index],
							displayFiltersThreadLocal, highlightFiltersThreadLocal, 
							displayFiltersPreprocessingResultCacheIsValid,
							highlightFiltersPreprocessingResultCacheIsValid)))
					{
						MessageBase loadedMessage = preprocessedMessage.LoadedMsg;
						IThread messageThread = loadedMessage.Thread;

						bool excludedBecauseOfInvisibleThread = !messageThread.ThreadMessagesAreVisible;
						var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(loadedMessage);
						bool collapsed = threadsBulkProcessingResult.ThreadWasInCollapsedRegion;

						FilterAction filterAction = displayFilters.ProcessNextMessageAndGetItsAction(loadedMessage,
							preprocessedMessage.DisplayFiltersPreprocessingResult,
							threadsBulkProcessingResult.DisplayFilterContext);
						bool excludedAsFilteredOut = filterAction == FilterAction.Exclude;

						loadedMessage.SetHidden(collapsed, excludedBecauseOfInvisibleThread, excludedAsFilteredOut);

						bool isHighlighted = false;
						if (!loadedMessage.IsHiddenAsFilteredOut)
						{
							FilterAction hlFilterAction = hlFilters.ProcessNextMessageAndGetItsAction(loadedMessage,
								preprocessedMessage.HighlightFiltersPreprocessingResult,
								threadsBulkProcessingResult.HighlightFilterContext);
							isHighlighted = hlFilterAction == FilterAction.Include;
						}
						loadedMessage.SetHighlighted(isHighlighted);

						HandleBookmarks(bmk, loadedMessage);

						mergedMessages[loadedCount] = preprocessedMessage;
						++loadedCount;

						if (loadedMessage.IsVisible)
						{
							focusedMessageFinder.HandleNewDisplayMessage(loadedMessage, visibleCount);
							AddDisplayMessage(loadedMessage);
						}
					}
				}

				System.Diagnostics.Debug.Assert(loadedCount == modelMessagesCount);

				threadsBulkProcessing.HandleHangingFrames(loadedMessagesCollection);

				tracer.Info("Update finished: visibleCount={0}, loaded lines={1}", visibleCount, mergedMessages.Count);

				if (displayFilters != null)
					displayFilters.EndBulkProcessing(displayFiltersProcessingHandle);
				if (hlFilters != null)
					hlFilters.EndBulkProcessing(hlFiltersProcessingHandle);

				displayFiltersPreprocessingResultCacheIsValid = true;
				highlightFiltersPreprocessingResultCacheIsValid = true;
				view.UpdateScrollSizeToMatchVisibleCount();
				focusedMessageFinder.SetFoundSelection();
				if (!DisplayHintIfMessagesIsEmpty())
					DisplayEverythingFilteredOutMessageIfNeeded();
				view.Invalidate();
			}
		}

		private void ResizeMergedMessages(int modelMessagesCount)
		{
			if (mergedMessages.Capacity < modelMessagesCount)
				mergedMessages.Capacity = modelMessagesCount;
			int missingElementsCount = modelMessagesCount - mergedMessages.Count;
			if (missingElementsCount > 0)
				mergedMessages.AddRange(Enumerable.Repeat(new MergedMessagesEntry(), missingElementsCount));
			else if (missingElementsCount < 0)
				mergedMessages.RemoveRange(modelMessagesCount, -missingElementsCount);
		}

		private void UpdateDisplayMessagesCapacity(int modelMessagesCount)
		{
			displayMessages.Clear();
			displayMessages.Capacity = modelMessagesCount;
		}

		private bool DisplayHintIfMessagesIsEmpty()
		{
			if (loadedMessagesCollection.Count == 0)
			{
				string msg = model.MessageToDisplayWhenMessagesCollectionIsEmpty;
				if (!string.IsNullOrEmpty(msg))
				{
					view.DisplayNothingLoadedMessage(msg);
					return true;
				}
			}
			view.DisplayNothingLoadedMessage(null);
			return false;
		}

		private void DisplayEverythingFilteredOutMessageIfNeeded()
		{
			bool everythingFilteredOut =
				displayMessagesCollection.Count == 0
			 && loadedMessagesCollection.Count > 0
			 && model.DisplayFilters != null;
			view.DisplayEverythingFilteredOutMessage(everythingFilteredOut);
		}

		private void AddDisplayMessage(MessageBase m)
		{
			int linesCount = m.GetLinesCount();
			for (int i = 0; i < linesCount; ++i)
				displayMessages.Add(new DisplayMessagesEntry() { DisplayMsg = m, TextLineIndex = i });
		}

		private IBookmarksHandler CreateBookmarksHandler()
		{
			return model.Bookmarks != null ? model.Bookmarks.CreateHandler() : null;
		}

		private static FiltersList.BulkProcessingHandle BeginBulkProcessing(FiltersList filters)
		{
			if (filters != null)
			{
				filters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				return filters.BeginBulkProcessing();
			}
			return null;
		}

		private static void HandleBookmarks(IBookmarksHandler bmk, MessageBase m)
		{
			if (bmk != null)
			{
				m.SetBookmarked(bmk.ProcessNextMessageAndCheckIfItIsBookmarked(m));
			}
			else
			{
				m.SetBookmarked(false);
			}
		}
		
		IEnumerable<MessageBase> INextBookmarkCallback.EnumMessages(DateTime time, bool forward)
		{
			if (forward)
			{
				foreach (IndexedMessage l in loadedMessagesCollection.Forward(
					GetDateLowerBound(time), int.MaxValue,
						new ShiftPermissions(false, true)))
				{
					if (l.Message.Time > time)
						break;
					yield return l.Message;
				}
			}
			else
			{
				foreach (IndexedMessage l in loadedMessagesCollection.Reverse(
					GetDateUpperBound(time) - 1, int.MinValue,
						new ShiftPermissions(true, false)))
				{
					if (l.Message.Time < time)
						break;
					yield return l.Message;
				}
			}
		}

		int GetDateLowerBound(DateTime d)
		{
			return ListUtils.BinarySearch(mergedMessages, 0, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.LoadedMsg.Time < d; });
		}

		int GetDateUpperBound(DateTime d)
		{
			return ListUtils.BinarySearch(mergedMessages, 0, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.LoadedMsg.Time <= d; });
		}

		void GetDateEqualRange(DateTime d, out int begin, out int end)
		{
			begin = GetDateLowerBound(d);
			end = ListUtils.BinarySearch(mergedMessages, begin, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.LoadedMsg.Time <= d; });
		}

		// It takes O(n) time
		// todo: optimize by using binary rearch over mergedMessages
		int LoadedIndex2DisplayIndex(int position)
		{
			MessageBase l = this.mergedMessages[position].LoadedMsg;
			foreach (IndexedMessage it2 in displayMessagesCollection.Forward(0, int.MaxValue))
				if (it2.Message == l)
					return it2.Index;
			return -1;
		}

		readonly IModel model;
		readonly ISearchResultModel searchResultModel;
		readonly IView view;
		readonly ICallback callback;

		readonly LJTraceSource tracer;
		readonly List<MergedMessagesEntry> mergedMessages = new List<MergedMessagesEntry>();
		readonly List<DisplayMessagesEntry> displayMessages = new List<DisplayMessagesEntry>();
		int visibleCount { get { return displayMessages.Count; } } // todo: get rid of this prop
		int mergedMessagesVersion;
		LoadedMessagesCollection loadedMessagesCollection;
		DisplayMessagesCollection displayMessagesCollection;
		SelectionInfo selection;
		bool displayFiltersPreprocessingResultCacheIsValid;
		bool highlightFiltersPreprocessingResultCacheIsValid;
		string defaultFocusedMessageActionCaption;
		
		Func<MessageBase, Tuple<int, int>> inplaceHightlightHandler;
		SearchAllOccurencesParams lastSearchOptions;
		Search.PreprocessedOptions lastSearchOptionPreprocessed;
		Search.BulkSearchState inplaceHightlightHandlerState = new Search.BulkSearchState();

		#endregion
	};

	public class LoadedMessagesModel : IModel
	{
		Model model;

		public LoadedMessagesModel(Model model)
		{
			this.model = model;
			this.model.OnMessagesChanged += delegate(object sender, Model.MessagesChangedEventArgs e) 
			{ 
				if (OnMessagesChanged != null) 
					OnMessagesChanged(sender, e); 
			};
		}

		public IMessagesCollection Messages
		{
			get { return model.LoadedMessages; }
		}

		public IThreads Threads
		{
			get { return model.Threads; }
		}

		public FiltersList DisplayFilters
		{
			get { return model.DisplayFilters; }
		}

		public FiltersList HighlightFilters
		{
			get { return model.HighlightFilters; }
		}

		public IBookmarks Bookmarks
		{
			get { return model.Bookmarks; }
		}

		public IUINavigationHandler UINavigationHandler
		{
			get { return model.UINavigationHandler; }
		}

		public LJTraceSource Tracer
		{
			get { return model.Tracer; }
		}

		public string MessageToDisplayWhenMessagesCollectionIsEmpty 
		{
			get
			{
				if (model.SourcesCount > 0)
					return null;
				return "No log sources open. To add new log source:\n  - Press Add... button on Log Sources tab\n  - or drag&&drop (possibly zipped) log file from Windows Explorer\n  - or drag&&drop URL from a browser to download (possibly zipped) log file";
			}
		}

		public void ShiftUp()
		{
			model.SourcesManager.ShiftUp();
		}

		public bool IsShiftableUp
		{
			get { return model.SourcesManager.IsShiftableUp; }
		}

		public void ShiftDown()
		{
			model.SourcesManager.ShiftDown();
		}

		public bool IsShiftableDown
		{
			get { return model.SourcesManager.IsShiftableDown; }
		}

		public void ShiftAt(DateTime t)
		{
			model.SourcesManager.ShiftAt(t);
		}

		public void ShiftHome()
		{
			model.SourcesManager.ShiftHome();
		}

		public void ShiftToEnd()
		{
			model.SourcesManager.ShiftToEnd();
		}

		public event EventHandler<Model.MessagesChangedEventArgs> OnMessagesChanged;
	};
};
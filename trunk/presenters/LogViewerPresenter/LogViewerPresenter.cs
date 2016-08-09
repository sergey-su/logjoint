using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using LogJoint;
using System.Threading;
using System.Diagnostics;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public class Presenter : IPresenter, IViewEvents, IPresentationDataAccess
	{
		public Presenter(
			IModel model,
			IView view,
			IHeartBeatTimer heartbeat,
			IPresentersFacade navHandler,
			IClipboardAccess clipboard,
			IBookmarksFactory bookmarksFactory,
			Telemetry.ITelemetryCollector telemetry
		)
		{
			this.model = model;
			this.searchResultModel = model as ISearchResultModel;
			this.view = view;
			this.presentationFacade = navHandler;
			this.clipboard = clipboard;
			this.bookmarksFactory = bookmarksFactory;
			this.telemetry = telemetry;

			this.tracer = new LJTraceSource("UI", "ui.lv");

			ReadGlobalSettings(model);

			AttachToView(view);

			this.model.OnSourcesChanged += (sender, e) => 
			{
				HandleSourcesListChange();
			};
			this.model.HighlightFilters.OnFiltersListChanged += (sender, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};
			this.model.HighlightFilters.OnPropertiesChanged += (sender, e) =>
			{
				if (e.ChangeAffectsFilterResult)
					pendingUpdateFlag.Invalidate();
			};
			this.model.HighlightFilters.OnFilteringEnabledChanged += (sender, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};

			this.model.OnLogSourceColorChanged += (s, e) =>
			{
				view.Invalidate();
			};

			this.model.OnSourceMessagesChanged += (sender, e) => 
			{
				pendingUpdateFlag.Invalidate();
			};

			heartbeat.OnTimer += (sender, e) => 
			{
				if (e.IsNormalUpdate && pendingUpdateFlag.Validate())
				{
					NavigateView(async cancellation => 
					{
						await screenBuffer.Reload(cancellation);
						InternalUpdate();
					}).IgnoreCancellation();
				}
			};

			model.GlobalSettings.Changed += (sender, e) =>
			{
				if ((e.ChangedPieces & Settings.SettingsPiece.Appearance) != 0)
				{
					ReadGlobalSettings(model);
					view.UpdateFontDependentData(fontName, fontSize);
					view.Invalidate();
				}
			};

			if (model.Bookmarks != null)
			{
				model.Bookmarks.OnBookmarksChanged += (s, e) => view.Invalidate();
			}

			DisplayHintIfMessagesIsEmpty();

			searchResultInplaceHightlightHandler = SearchResultInplaceHightlightHandler;

			view.UpdateFontDependentData(fontName, fontSize);
		}

		#region Interfaces implementation

		public event EventHandler SelectionChanged;
		public event EventHandler FocusedMessageChanged;
		public event EventHandler DefaultFocusedMessageAction;
		public event EventHandler ManualRefresh;
		public event EventHandler RawViewModeChanged;
		public event EventHandler ColoringModeChanged;
		public event EventHandler NavigationIsInProgressChanged;

		LogFontSize IPresenter.FontSize
		{
			get { return fontSize; }
			set { SetFontSize(value); }
		}

		string IPresenter.FontName
		{
			get { return fontName; }
			set { SetFontName(value); }
		}

		IMessage IPresenter.FocusedMessage
		{
			get { return selection.Message; }
		}

		bool IPresenter.NavigationIsInProgress
		{
			get { return currentNavigationTask != null; }
		}

		PreferredDblClickAction IPresenter.DblClickAction { get; set; }

		FocusedMessageDisplayModes IPresenter.FocusedMessageDisplayMode { get { return focusedMessageDisplayMode; } set { focusedMessageDisplayMode = value; } }

		string IPresenter.DefaultFocusedMessageActionCaption { get { return defaultFocusedMessageActionCaption; } set { defaultFocusedMessageActionCaption = value; } }

		bool IPresenter.ShowTime
		{
			get { return showTime; }
			set
			{
				if (showTime == value)
					return;
				showTime = value;
				view.Invalidate();
			}
		}

		bool IPresenter.ShowMilliseconds
		{
			get
			{
				return showMilliseconds;
			}
			set
			{
				if (showMilliseconds == value)
					return;
				showMilliseconds = value;
				view.UpdateMillisecondsModeDependentData();
				if (showTime)
				{
					view.Invalidate();
				}
			}
		}

		bool IPresenter.ShowRawMessages
		{
			get
			{
				return showRawMessages;
			}
			set
			{
				if (showRawMessages == value)
					return;
				if (showRawMessages && !rawViewAllowed)
					return;
				showRawMessages = value;
				screenBuffer.SetRawLogMode(showRawMessages);
				InternalUpdate();
				UpdateSelectionInplaceHighlightingFields();
				if (RawViewModeChanged != null)
					RawViewModeChanged(this, EventArgs.Empty);
			}
		}

		bool IPresenter.RawViewAllowed
		{
			get
			{
				return rawViewAllowed;
			}
			set
			{
				if (rawViewAllowed == value)
					return;
				rawViewAllowed = value;
				if (!rawViewAllowed && showRawMessages)
					ThisIntf.ShowRawMessages = false;
			}
		}

		UserInteraction IPresenter.DisabledUserInteractions
		{
			get { return disabledUserInteractions; }
			set { disabledUserInteractions = value; }
		}

		ColoringMode IPresenter.Coloring
		{
			get
			{
				return coloring;
			}
			set
			{
				if (coloring == value)
					return;
				coloring = value;
				view.Invalidate();
				if (ColoringModeChanged != null)
					ColoringModeChanged(this, EventArgs.Empty);
			}
		}

		async Task<IMessage> IPresenter.Search(SearchOptions opts)
		{
			bool isEmptyTemplate = string.IsNullOrEmpty(opts.CoreOptions.Template);

			return await Scan(
				opts.CoreOptions.ReverseSearch,
				opts.SearchOnlyWithinFocusedMessage,
				opts.HighlightResult,
				(source) =>
				{
					Search.PreprocessedOptions preprocessedOptions = opts.CoreOptions.Preprocess();
					Search.BulkSearchState bulkSearchState = new Search.BulkSearchState();

					return (m, messagesProcessed, startFromTextPos) =>
					{
						if (isEmptyTemplate)
							startFromTextPos = null;

						var match = LogJoint.Search.SearchInMessageText(
							m, preprocessedOptions, bulkSearchState, startFromTextPos);

						if (!match.HasValue)
							return null;

						if (isEmptyTemplate && messagesProcessed == 1)
							return null;

						return Tuple.Create(match.Value.MatchBegin, match.Value.MatchEnd);
					};
				}
			);
		}

		void IPresenter.ToggleBookmark(IMessage line)
		{
			if (model.Bookmarks == null)
				return;
			model.Bookmarks.ToggleBookmark(line);
			view.Invalidate();
		}

		Task IPresenter.SelectMessageAt(DateTime date, ILogSource preferredSource)
		{
			// todo: handle preferred source
			return NavigateView(async cancellation =>
			{
				await screenBuffer.MoveToBookmark(bookmarksFactory.CreateBookmark(new MessageTimestamp(date)),
					MessageMatchingMode.MatchNearestTime, cancellation);
				InternalUpdate();
				ThisIntf.SelectFirstMessage();
			});
		}

		Task IPresenter.GoToNextMessageInThread()
		{
			return FindMessageInCurrentThread (EnumMessagesFlag.Forward);
		}

		Task IPresenter.GoToPrevMessageInThread()
		{
			return FindMessageInCurrentThread(EnumMessagesFlag.Backward);
		}

		Task IPresenter.GoToNextHighlightedMessage()
		{
			return NextGoToHighlightedMessage(reverse: false);
		}

		Task IPresenter.GoToPrevHighlightedMessage()
		{
			return NextGoToHighlightedMessage(reverse: true);
		}

		IBookmark IPresenter.NextBookmark(bool forward)
		{
			if (selection.Message == null || model.Bookmarks == null)
				return null;
			return model.Bookmarks.GetNext(selection.Message, forward);
		}

		async Task<bool> IPresenter.SelectMessageAt(IBookmark bmk)
		{
			if (bmk == null)
				return false;

			bool ret = false;
			await NavigateView(async cancellation => 
			{
				var idx = await LoadMessageAt(bmk, MessageMatchingMode.ExactMatch, cancellation);
				if (idx != null)
					SelectFullLine(idx.Value);
				ret = idx != null;
			});

			return ret;
		}

		Task IPresenter.GoHome()
		{
			return NavigateView(async cancellation =>
			{
				await screenBuffer.MoveToStreamsBegin(cancellation);
				InternalUpdate();
				ThisIntf.SelectFirstMessage();
			});
		}

		Task IPresenter.GoToEnd()
		{
			return NavigateView(async cancellation =>
			{
				await screenBuffer.MoveToStreamsEnd(cancellation);
				InternalUpdate();
				ThisIntf.SelectLastMessage();
			});
		}

		Task IPresenter.GoToNextMessage()
		{
			return MoveSelection(+1, SelectionFlag.None);
		}

		Task IPresenter.GoToPrevMessage()
		{
			return MoveSelection(-1, SelectionFlag.None);
		}

		void IPresenter.ClearSelection()
		{
			if (selection.First.Message == null)
				return;

			InvalidateTextLineUnderCursor();
			foreach (var displayIndexToInvalidate in selection.GetDisplayIndexesRange().Where(idx => idx < viewLines.Count))
				view.InvalidateLine(viewLines[displayIndexToInvalidate].ToViewLine());

			selection.SetSelection(new CursorPosition(), new CursorPosition());
			OnSelectionChanged();
			OnFocusedMessageChanged();
		}

		Task<string> IPresenter.GetSelectedText()
		{
			return GetSelectedTextInternal(includeTime: false);
		}

		async Task IPresenter.CopySelectionToClipboard()
		{
			if (clipboard == null)
				return;
			var txt = await GetSelectedTextInternal(includeTime: showTime);
			if (txt.Length > 0)
				clipboard.SetClipboard(txt, await GetSelectedTextAsHtml(includeTime: showTime));
		}

		IMessage IPresenter.SlaveModeFocusedMessage
		{
			get
			{
				return slaveModeFocusedMessage;
			}
			set
			{
				if (value == slaveModeFocusedMessage)
					return;
				slaveModeFocusedMessage = value;
				view.Invalidate();
			}
		}

		Task IPresenter.SelectSlaveModeFocusedMessage()
		{
			return NavigateView(async cancellation =>
			{
				if (slaveModeFocusedMessage == null)
					return;
				await LoadMessageAt(slaveModeFocusedMessage, MessageMatchingMode.MatchNearest, cancellation);
				var position = FindSlaveModeFocusedMessagePositionInternal(0, viewLines.Count);
				if (position == null)
					return;
				var idxToSelect = position.Item1;
				if (idxToSelect == viewLines.Count)
					--idxToSelect;
				SetSelection(idxToSelect,
					SelectionFlag.SelectBeginningOfLine | SelectionFlag.ScrollToViewEventIfSelectionDidNotChange);
				view.AnimateSlaveMessagePosition();
			});
		}

		void IPresenter.SelectFirstMessage()
		{
			if (viewLines.Count > 0)
			{
				SetSelection(0, SelectionFlag.SelectBeginningOfLine);
			}
		}

		void IPresenter.SelectLastMessage()
		{
			if (viewLines.Count > 0)
			{
				SetSelection(viewLines.Count - 1, SelectionFlag.SelectBeginningOfLine);
			}
		}


		void IViewEvents.OnMouseWheelWithCtrl(int delta)
		{
			if ((disabledUserInteractions & UserInteraction.FontResizing) == 0)
			{
				if (delta > 0 && fontSize != LogFontSize.Maximum)
					SetFontSize(fontSize + 1);
				else if (delta < 0 && fontSize != LogFontSize.Minimum)
					SetFontSize(fontSize - 1);
			}
		}

		void IViewEvents.OnCursorTimerTick()
		{
			InvalidateTextLineUnderCursor();
		}

		void IViewEvents.OnKeyPressed(Key k)
		{
			OnKeyPressedAsync(k).IgnoreCancellation();
		}

		void IViewEvents.OnMenuOpening(out ContextMenuItem visibleItems, out ContextMenuItem checkedItems, out string defaultItemText)
		{
			visibleItems =
				ContextMenuItem.ShowTime |
				ContextMenuItem.GotoNextMessageInTheThread |
				ContextMenuItem.GotoPrevMessageInTheThread;
			checkedItems = ContextMenuItem.None;

			if ((disabledUserInteractions & UserInteraction.CopyMenu) == 0)
				visibleItems |= ContextMenuItem.Copy;

			if (ThisIntf.ShowTime)
				checkedItems |= ContextMenuItem.ShowTime;

			if (ThisIntf.RawViewAllowed && (ThisIntf.DisabledUserInteractions & UserInteraction.RawViewSwitching) == 0)
				visibleItems |= ContextMenuItem.ShowRawMessages;
			if (ThisIntf.ShowRawMessages)
				checkedItems |= ContextMenuItem.ShowRawMessages;

			if (model.Bookmarks != null)
				visibleItems |= ContextMenuItem.ToggleBmk;

			defaultItemText = ThisIntf.DefaultFocusedMessageActionCaption;
			if (!string.IsNullOrEmpty(defaultItemText))
				visibleItems |= ContextMenuItem.DefaultAction;
		}

		void IViewEvents.OnMenuItemClicked(ContextMenuItem menuItem, bool? itemChecked)
		{
			if (menuItem == ContextMenuItem.Copy)
				ThisIntf.CopySelectionToClipboard().IgnoreCancellation();
			else if (menuItem == ContextMenuItem.ShowTime)
				ThisIntf.ShowTime = itemChecked.GetValueOrDefault(false);
			else if (menuItem == ContextMenuItem.ShowRawMessages)
				ThisIntf.ShowRawMessages = itemChecked.GetValueOrDefault(false);
			else if (menuItem == ContextMenuItem.DefaultAction)
				PerformDefaultFocusedMessageAction();
			else if (menuItem == ContextMenuItem.ToggleBmk)
				ThisIntf.ToggleBookmark(selection.Message);
			else if (menuItem == ContextMenuItem.GotoNextMessageInTheThread)
				ThisIntf.GoToNextMessageInThread();
			else if (menuItem == ContextMenuItem.GotoPrevMessageInTheThread)
				ThisIntf.GoToPrevMessageInThread();
		}

		void IViewEvents.OnDisplayLinesPerPageChanged()
		{
			NavigateView(async cancellation => 
			{
				SetScreenBufferSize();
				await screenBuffer.Reload(cancellation);
				InternalUpdate();
			}).IgnoreCancellation();
		}

		void IViewEvents.OnIncrementalVScroll(float nrOfDisplayLines)
		{
			NavigateView(async cancellation =>
			{
				await ShiftViewBy(nrOfDisplayLines, cancellation);
			}).IgnoreCancellation();
		}

		void IViewEvents.OnVScroll(double value, bool isRealtimeScroll)
		{
			//if (!isRealtimeScroll)
			NavigateView(async cancellation =>
			{
				await screenBuffer.MoveToPosition(value, cancellation);
				InternalUpdate();
			}).IgnoreCancellation();
		}

		void IViewEvents.OnHScroll()
		{
			InvalidateTextLineUnderCursor();
		}

		void IViewEvents.OnMessageMouseEvent(
			ViewLine line,
			int charIndex,
			MessageMouseEventFlag flags,
			object preparedContextMenuPopupData)
		{
			var pos = CursorPosition.FromViewLine(line, charIndex);
			if ((flags & MessageMouseEventFlag.RightMouseButton) != 0)
			{
				if (!selection.IsInsideSelection(pos))
					SetSelection(pos.DisplayIndex, SelectionFlag.None, pos.LineCharIndex);
				view.PopupContextMenu(preparedContextMenuPopupData);
			}
			else
			{
				if ((flags & MessageMouseEventFlag.CapturedMouseMove) == 0 && (flags & MessageMouseEventFlag.OulineBoxesArea) != 0)
				{
					if ((flags & MessageMouseEventFlag.ShiftIsHeld) == 0)
						SetSelection(pos.DisplayIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.NoHScrollToSelection);
					SetSelection(pos.DisplayIndex, SelectionFlag.SelectEndOfLine | SelectionFlag.PreserveSelectionEnd | SelectionFlag.NoHScrollToSelection);
					if ((flags & MessageMouseEventFlag.DblClick) != 0)
						PerformDefaultFocusedMessageAction();
				}
				else
				{
					bool defaultSelection = true;
					if ((flags & MessageMouseEventFlag.DblClick) != 0)
					{
						PreferredDblClickAction action = ThisIntf.DblClickAction;
						if ((flags & MessageMouseEventFlag.AltIsHeld) != 0)
							action = PreferredDblClickAction.SelectWord;
						if (action == PreferredDblClickAction.DoDefaultAction)
						{
							PerformDefaultFocusedMessageAction();
							defaultSelection = false;
						}
						else if (action == PreferredDblClickAction.SelectWord)
						{
							defaultSelection = !SelectWordBoundaries(pos);
						}
					}
					if (defaultSelection)
					{
						SetSelection(pos.DisplayIndex, (flags & MessageMouseEventFlag.ShiftIsHeld) != 0
							? SelectionFlag.PreserveSelectionEnd : SelectionFlag.None, pos.LineCharIndex);
					}
				}
			}
		}


		bool IPresentationDataAccess.ShowTime { get { return showTime; } }
		bool IPresentationDataAccess.ShowMilliseconds { get { return showMilliseconds; } }
		bool IPresentationDataAccess.ShowRawMessages { get { return showRawMessages; } }
		SelectionInfo IPresentationDataAccess.Selection { get { return selection; } }
		ColoringMode IPresentationDataAccess.Coloring { get { return coloring; } }

		Func<IMessage, IEnumerable<Tuple<int, int>>> IPresentationDataAccess.InplaceHighlightHandler1
		{
			get
			{
				if (searchResultModel != null && searchResultModel.SearchParams != null)
					return searchResultInplaceHightlightHandler;
				return null;
			}
		}

		Func<IMessage, IEnumerable<Tuple<int, int>>> IPresentationDataAccess.InplaceHighlightHandler2
		{
			get { return selectionInplaceHighlightingHandler; }
		}

		FocusedMessageDisplayModes IPresentationDataAccess.FocusedMessageDisplayMode
		{
			get { return focusedMessageDisplayMode; }
		}

		Tuple<int, int> IPresentationDataAccess.FindSlaveModeFocusedMessagePosition(int beginIdx, int endIdx)
		{
			return FindSlaveModeFocusedMessagePositionInternal(beginIdx, endIdx);
		}

		IEnumerable<ViewLine> IPresentationDataAccess.GetViewLines(int beginIdx, int endIdx)
		{
			using (var bookmarksHandler = (model.Bookmarks != null ? model.Bookmarks.CreateHandler() : new DummyBookmarksHandler()))
			{
				int i = beginIdx;
				for (; i != endIdx; ++i)
				{
					var vl = viewLines[i].ToViewLine();
					if (!vl.Message.LogSource.IsDisposed)
						vl.IsBookmarked = bookmarksHandler.ProcessNextMessageAndCheckIfItIsBookmarked(vl.Message);
					yield return vl;
				}
			}
		}

		int IPresentationDataAccess.ViewLinesCount
		{
			get { return viewLines.Count; }
		}

		double IPresentationDataAccess.GetFirstDisplayMessageScrolledLines()
		{
			return screenBuffer.TopMessageScrolledLines;
		}


		#endregion



		async Task OnKeyPressedAsync(Key keyFlags)
		{
			var k = keyFlags & Key.KeyCodeMask;
			if (k == Key.Refresh)
			{
				OnRefresh();
				return;
			}

			CursorPosition cur = selection.First;

			var preserveSelectionFlag = (keyFlags & Key.ModifySelectionModifier) != 0 
				? SelectionFlag.PreserveSelectionEnd : SelectionFlag.None;
			var alt = (keyFlags & Key.AlternativeModeModifier) != 0;

			if (selection.Message != null)
			{
				if (k == Key.Up)
				if (alt)
					await ThisIntf.GoToPrevMessageInThread();
				else
					await MoveSelection(-1, preserveSelectionFlag);
				else if (k == Key.Down)
				if (alt)
					await ThisIntf.GoToNextMessageInThread();
				else
					await MoveSelection(+1, preserveSelectionFlag);
				else if (k == Key.PageUp)
					await MoveSelection(-DisplayLinesPerPage, preserveSelectionFlag);
				else if (k == Key.PageDown)
					await MoveSelection(+DisplayLinesPerPage, preserveSelectionFlag);
				else if (k == Key.Left || k == Key.Right)
					await HandleLeftRightArrow(
						left: k == Key.Left, 
						jumpOverWords: (keyFlags & Key.JumpOverWordsModifier) != 0,
						preserveSelectionFlag: preserveSelectionFlag
					);
				else if (k == Key.ContextMenu)
					view.PopupContextMenu(view.GetContextMenuPopupDataForCurrentSelection(selection));
				else if (k == Key.Enter)
					PerformDefaultFocusedMessageAction();
				else if (k == Key.BookmarkShortcut)
					ThisIntf.ToggleBookmark(selection.Message);
			}
			if (k == Key.Copy)
			{
				if ((disabledUserInteractions & UserInteraction.CopyShortcut) == 0)
					await ThisIntf.CopySelectionToClipboard();
			}
			else if (k == Key.BeginOfLine)
			{
				await MoveSelection(0, preserveSelectionFlag | Presenter.SelectionFlag.SelectBeginningOfLine);
			}
			else if (k == Key.BeginOfDocument)
			{
				await ThisIntf.GoHome();
			}
			else if (k == Key.EndOfLine)
			{
				await MoveSelection(0, preserveSelectionFlag | Presenter.SelectionFlag.SelectEndOfLine);
			}
			else if (k == Key.EndOfDocument)
			{
				await ThisIntf.GoToEnd();
			}
			else if (k == Key.NextHighlightedMessage)
			{
				await ThisIntf.GoToNextHighlightedMessage();
			}
			else if (k == Key.PrevHighlightedMessage)
			{
				await ThisIntf.GoToPrevHighlightedMessage();
			}
		}

		Task NavigateView(Func<CancellationToken, Task> navigate)
		{
			bool wasInProgress = false;
			if (currentNavigationTask != null)
			{
				wasInProgress = true;
				currentNavigationTaskCancellation.Cancel();
				currentNavigationTask = null;
			}
			var taskId = ++currentNavigationTaskId;
			currentNavigationTaskCancellation = new CancellationTokenSource();
			Func<Task> wrapper = async () => 
			{
				// todo: have perf op for navigation
				Console.WriteLine("nav begin {0} ", taskId);
				var cancellation = currentNavigationTaskCancellation.Token;
				try
				{
					await navigate(cancellation);
				}
				catch (OperationCanceledException)
				{
					throw; // fail navigation task with same exception. don't telemetrize it.
				}
				catch (Exception e)
				{
					telemetry.ReportException(e, "LogViewer navigation");
					throw;
				}
				finally
				{
					Console.WriteLine("nav end {0}{1}", taskId, cancellation.IsCancellationRequested ? " (cancelled)" : "");
					if (taskId == currentNavigationTaskId && currentNavigationTask != null)
					{
						currentNavigationTask = null;
						if (NavigationIsInProgressChanged != null)
							NavigationIsInProgressChanged(this, EventArgs.Empty);
					}
				}
			};
			var tmp = wrapper();
			if (!tmp.IsCompleted)
			{
				currentNavigationTask = tmp;
			}
			if (wasInProgress != (currentNavigationTask != null))
			if (NavigationIsInProgressChanged != null)
				NavigationIsInProgressChanged(this, EventArgs.Empty);
			return tmp;
		}

		async Task<int?> LoadMessageAt(IMessage msg, MessageMatchingMode mode, CancellationToken cancellation)
		{
			return await LoadMessageAt(bookmarksFactory.CreateBookmark(msg), mode, cancellation);
		}

		void SelectFullLine(int displayIndex)
		{
			SetSelection(displayIndex, SelectionFlag.SelectEndOfLine | SelectionFlag.NoHScrollToSelection);
			SetSelection(displayIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.PreserveSelectionEnd);
		}

		static async Task<IMessage> ScanMessages(
			IMessagesSource p, long startFrom, EnumMessagesFlag flags, Predicate<IMessage> predicate, CancellationToken cancellation)
		{
			IMessage ret = null;
			await p.EnumMessages(startFrom, msg => 
				{
					if (!predicate(msg))
						return true;
					ret = msg;
					return false;
				}, flags, LogProviderCommandPriority.AsyncUserAction, cancellation);
			cancellation.ThrowIfCancellationRequested();
			return ret;
		}

		private Tuple<int, int> FindSlaveModeFocusedMessagePositionInternal(int beginIdx, int endIdx)
		{
			if (slaveModeFocusedMessage == null)
				return null;
			int lowerBound = ListUtils.BinarySearch(viewLines, beginIdx, endIdx, dm => MessagesComparer.Compare(dm.Message, slaveModeFocusedMessage) < 0);
			int upperBound = ListUtils.BinarySearch(viewLines, lowerBound, endIdx, dm => MessagesComparer.Compare(dm.Message, slaveModeFocusedMessage) <= 0);
			return new Tuple<int, int>(lowerBound, upperBound);
		}

		StringUtils.MultilineText GetTextToDisplay(IMessage msg)
		{
			return msg.GetDisplayText(showRawMessages);
		}

		public void InvalidateTextLineUnderCursor()
		{
			if (selection.First.Message != null)
			{
				view.InvalidateLine(selection.First.ToDisplayLine());
			}
		}

		void SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null)
		{
			var dmsg = viewLines[displayIndex];
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
				if (displayIndex == 0 && screenBuffer.MakeFirstLineFullyVisible())
				{
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

				selection.SetSelection(tmp, resetEnd ? tmp : new CursorPosition?());

				OnSelectionChanged();

				foreach (var displayIndexToInvalidate in oldSelection.GetDisplayIndexesRange().SymmetricDifference(selection.GetDisplayIndexesRange())
						.Where(idx => idx < viewLines.Count && idx >= 0))
				{
					view.InvalidateLine(viewLines[displayIndexToInvalidate].ToViewLine());
				}

				InvalidateTextLineUnderCursor();

				doScrolling();

				UpdateSelectionInplaceHighlightingFields();

				if (selection.First.Message != oldSelection.First.Message)
				{
					OnFocusedMessageChanged();
					tracer.Info("Focused line changed to the new selection");
				}
			}
			else if ((flag & SelectionFlag.ScrollToViewEventIfSelectionDidNotChange) != 0)
			{
				doScrolling();
			}
		}

		async Task<int> ShiftViewBy(float nrOfDisplayLines, CancellationToken cancellation)
		{
			var shiftedBy = await screenBuffer.ShiftBy(nrOfDisplayLines, cancellation);

			if (shiftedBy == 0)
				view.Invalidate();
			else
				InternalUpdate();

			return shiftedBy;
		}

		async Task<int?> LoadMessageAt(
			IBookmark bookmark,
			MessageMatchingMode matchingMode,
			CancellationToken cancellation)
		{
			Func<int?> findDisplayLine = () =>
			{
				int fullyVisibleViewLines = (int)Math.Ceiling(view.DisplayLinesPerPage);
				int topScrolledLines = (int)screenBuffer.TopMessageScrolledLines;
				return viewLines
					.Where(x => x.Message.GetConnectionId() == bookmark.LogSourceConnectionId && x.Message.Position == bookmark.Position)
					.Where(x => (x.LineIndex - topScrolledLines) < fullyVisibleViewLines && x.LineIndex >= topScrolledLines)
					.Select(x => new int?(x.LineIndex))
					.FirstOrDefault();
			};

			var idx = findDisplayLine();
			if (idx != null)
				return idx;

			if (!await screenBuffer.MoveToBookmark(bookmark, matchingMode, cancellation))
				return null;

			InternalUpdate();

			idx = findDisplayLine();
			return idx;
		}

		async Task<bool> ScrollSelectionIntoScreenBuffer(CancellationToken cancellation)
		{
			if (selection.Message == null || viewLines.Count == 0)
				return false;
			if (selection.First.DisplayIndex >= 0 && selection.First.DisplayIndex < viewLines.Count)
				return true;
			var idx = await LoadMessageAt(selection.Message, MessageMatchingMode.ExactMatch, cancellation);
			if (idx == null)
				return false;
			return true;
		}

		async Task MoveSelectionCore(int selectionDelta, SelectionFlag selFlags, CancellationToken cancellation)
		{
			var dlpp = this.DisplayLinesPerPage;
			int shiftBy = 0;
			int shiftedBy = 0;
			var newDisplayPosition = selection.First.DisplayIndex + selectionDelta;
			if (newDisplayPosition < 0)
				shiftBy = newDisplayPosition;
			else if (newDisplayPosition >= dlpp)
				shiftBy = newDisplayPosition - dlpp + 1;
			if (shiftBy != 0)
				shiftedBy = await ShiftViewBy(shiftBy, cancellation);
			cancellation.ThrowIfCancellationRequested();
			newDisplayPosition -= shiftedBy;
			if (newDisplayPosition >= 0 && newDisplayPosition < viewLines.Count)
			{
				SetSelection (newDisplayPosition, selFlags);
			}
		}

		Task MoveSelection(int selectionDelta, SelectionFlag selFlags)
		{
			return NavigateView(async cancellation =>
			{
				if (!await ScrollSelectionIntoScreenBuffer(cancellation))
					return;
				cancellation.ThrowIfCancellationRequested();
				await MoveSelectionCore(selectionDelta, selFlags, cancellation);
			});
		}

		public void PerformDefaultFocusedMessageAction()
		{
			if (DefaultFocusedMessageAction != null)
				DefaultFocusedMessageAction(this, EventArgs.Empty);
		}

		protected virtual void OnFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		protected virtual void OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, EventArgs.Empty);
		}

		protected virtual void OnRefresh()
		{
			if (ManualRefresh != null)
				ManualRefresh(this, EventArgs.Empty);
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

			int didx = 0;
			ViewLineEntry? sameMessageEntry = null;
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

		void UpdateSelectionDisplayIndexes()
		{
			selection.SetDisplayIndexes(GetDisplayIndex(selection.First), GetDisplayIndex(selection.Last));
		}

		void InternalUpdate()
		{
			IFiltersList hlFilters = model.HighlightFilters;
			FiltersBulkProcessingHandle hlFiltersProcessingHandle = BeginBulkProcessing(hlFilters);

			using (var threadsBulkProcessing = model.Threads.StartBulkProcessing())
			{
				IMessage lastMessage = null;
				viewLines.Clear();
				foreach (var m in screenBuffer.Messages)
				{
					if (m.Message.Thread.IsDisposed)
						continue;

					var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(m.Message);

					if (m.Message != lastMessage && hlFilters != null)
					{
						var hlPreproc = hlFilters.PreprocessMessage(m.Message, showRawMessages);
						bool isHighlighted = false;
						FilterAction hlFilterAction = hlFilters.ProcessNextMessageAndGetItsAction(
							m.Message, hlPreproc,
							threadsBulkProcessingResult.HighlightFilterContext, showRawMessages);
						isHighlighted = hlFilterAction == FilterAction.Include;
						m.Message.SetHighlighted(isHighlighted);
					}

					viewLines.Add(new ViewLineEntry()
					{
						Message = m.Message,
						TextLineIndex = m.LineIndex,
						LineIndex = viewLines.Count,
						Source = m.Source,
					});

					lastMessage = m.Message;
				}
			}

			if (hlFilters != null)
				hlFilters.EndBulkProcessing(hlFiltersProcessingHandle);

			DisplayHintIfMessagesIsEmpty();

			UpdateSelectionDisplayIndexes();
			view.Invalidate();
			view.SetVScroll(viewLines.Count > 0 ? screenBuffer.BufferPosition : new double?());
		}

		private bool DisplayHintIfMessagesIsEmpty()
		{
			if (viewLines.Count == 0)
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

		private static FiltersBulkProcessingHandle BeginBulkProcessing(IFiltersList filters)
		{
			if (filters != null)
			{
				filters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				return filters.BeginBulkProcessing();
			}
			return null;
		}

		IEnumerable<Tuple<int, int>> SearchResultInplaceHightlightHandler(IMessage msg)
		{
			if (searchResultModel == null)
				yield break;
			var opts = searchResultModel.SearchParams;
			if (opts == null)
				yield break;
			int currentSearchOptionsHash = opts.GetHashCode() ^ showRawMessages.GetHashCode();
			if (lastSearchOptionsHash != currentSearchOptionsHash)
			{
				lastSearchOptionsHash = currentSearchOptionsHash;
				var tmp = opts.Options;
				tmp.SearchInRawText = showRawMessages;
				inplaceHightlightHandlerState = new Search.BulkSearchState();
				try
				{
					lastSearchOptionPreprocessed = tmp.Preprocess();
				}
				catch (Search.TemplateException)
				{
					yield break;
				}
			}
			foreach (var r in FindAllHightlighRanges(msg, lastSearchOptionPreprocessed, inplaceHightlightHandlerState, opts.Options.ReverseSearch, null))
				yield return r;
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

		bool SelectWordBoundaries(CursorPosition pos)
		{
			var dmsg = viewLines[pos.DisplayIndex];
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

		async Task FindMessageInCurrentThread(EnumMessagesFlag directionFlag)
		{
			var message = selection.First.Message;
			var messageSource = selection.First.Source;
			if (message == null || messageSource == null)
				return;
			await NavigateView (async cancellation =>
			{
				var msg = await ScanMessages (
					messageSource,
					selection.First.Message.Position,
					directionFlag | EnumMessagesFlag.IsSequentialScanningHint,
					m => m.Position != message.Position && m.Thread == message.Thread,
					cancellation
				);
				if (msg != null)
					await SelectFoundMessageHelper (msg, cancellation);
			});
		}

		private async Task SelectFoundMessageHelper(IMessage foundMessage, CancellationToken cancellation)
		{
			var idx = await LoadMessageAt(foundMessage, MessageMatchingMode.ExactMatch, cancellation);
			if (idx != null)
				SetSelection(idx.Value, SelectionFlag.SelectBeginningOfLine);
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
							SearchInRawText = showRawMessages
						};
						var optionsPreprocessed = options.Preprocess();
						newHandler = msg =>
							FindAllHightlighRanges(msg, optionsPreprocessed, inplaceHightlightHandlerState, options.ReverseSearch, wordSelection);
					}
				}
			}

			if ((selectionInplaceHighlightingHandler != null) != (newHandler != null))
				view.Invalidate();
			else if (newHandler != null)
				view.Invalidate();

			selectionInplaceHighlightingHandler = newHandler;
		}

		private void SetFontSize(LogFontSize value)
		{
			if (value != fontSize)
			{
				fontSize = value;
				view.UpdateFontDependentData(fontName, fontSize);
				view.Invalidate();
			}
		}

		private void SetFontName(string value)
		{
			if (value != fontName)
			{
				fontName = value;
				view.UpdateFontDependentData(fontName, fontSize);
				view.Invalidate();
			}
		}

		void AttachToView(IView view)
		{
			view.SetViewEvents(this);
			view.SetPresentationDataAccess(this);
		}

		private void ReadGlobalSettings(IModel model)
		{
			this.coloring = model.GlobalSettings.Appearance.Coloring;
			this.fontSize = model.GlobalSettings.Appearance.FontSize;
			this.fontName = model.GlobalSettings.Appearance.FontFamily;
		}

		struct SelectedTextLine
		{
			public string Str;
			public IMessage Message;
			public int LineIndex;
			public bool IsSingleLineSelectionFragment;
		};

		async Task<List<ViewLineEntry>> GetSelectedDisplayMessagesEntries()
		{
			Func<CursorPosition, bool> isGoodDisplayPosition = p =>
			{
				if (p.Message == null)
					return true;
				return p.DisplayIndex >= 0 && p.DisplayIndex < viewLines.Count;
			};

			var normSelection = selection.Normalize();
			if (normSelection.IsEmpty)
			{
				return new List<ViewLineEntry>();
			}

			Func<List<ViewLineEntry>> defaultGet = () =>
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

			IScreenBuffer tmpBuf = new ScreenBuffer(viewSize: 0, initialBufferPosition: InitialBufferPosition.Nowhere);
			await tmpBuf.SetSources(screenBuffer.Sources.Select(s => s.Source), cancellation);
			if (!await tmpBuf.MoveToBookmark(bookmarksFactory.CreateBookmark(normSelection.First.Message), 
				MessageMatchingMode.ExactMatch, cancellation))
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
					Enumerable.Range(0, m.Message.Message.GetDisplayText(showRawMessages).GetLinesCount()).Select(
						lineIdx => new ViewLineEntry()
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

		public string GetBackgroundColorAsHtml(IMessage msg)
		{
			var ls = msg.GetLogSource();
			var cl = "white";
			if (ls != null)
				if (coloring == ColoringMode.Threads)
					cl = msg.Thread.ThreadColor.ToHtmlColor();
				else if (coloring == ColoringMode.Sources)
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

		void HandleSourcesListChange()
		{
			NavigateView(async cancellation =>
			{
				SetScreenBufferSize();

				// here is the only place where sources are read from the model.
				// by design presenter won't see changes in sources list until this method is run.
				await screenBuffer.SetSources(model.Sources, cancellation);
				InternalUpdate();
			}).IgnoreCancellation();
		}

		void SetScreenBufferSize()
		{
			screenBuffer.SetViewSize(view.DisplayLinesPerPage);
		}

		Task HandleLeftRightArrow(bool left, bool jumpOverWords, SelectionFlag preserveSelectionFlag)
		{
			return NavigateView(async cancellation => 
			{
				if (!await ScrollSelectionIntoScreenBuffer(cancellation))
					return;
				cancellation.ThrowIfCancellationRequested();
				CursorPosition cur = selection.First;
				if (cur.Message == null)
					return;
				if (jumpOverWords)
				{
					var wordFlag = left ? Presenter.SelectionFlag.SelectBeginningOfPrevWord : Presenter.SelectionFlag.SelectBeginningOfNextWord;
					SetSelection(cur.DisplayIndex, preserveSelectionFlag | wordFlag, cur.LineCharIndex);
				}
				else
				{
					SetSelection(cur.DisplayIndex, preserveSelectionFlag, cur.LineCharIndex + (left ? -1 : +1));
				}
				if (selection.First.LineCharIndex == cur.LineCharIndex)
				{
					await MoveSelectionCore(
						left ? -1 : +1,
						preserveSelectionFlag | (left ? Presenter.SelectionFlag.SelectEndOfLine : Presenter.SelectionFlag.SelectBeginningOfLine),
						cancellation
					);
				}
			});
		}

		delegate Tuple<int, int> ScanMatcher(IMessage message, int messagesProcessed, int? startFromChar);

		async Task<IMessage> Scan(
			bool reverse, bool searchOnlyWithinFocusedMessage, bool highlightResult,
			Func<IMessagesSource, ScanMatcher> makeMatcher
		)
		{
			bool isReverseSearch = reverse;
			IMessage scanResult = null;

			CursorPosition startFrom = new CursorPosition();
			var normSelection = selection.Normalize();
			if (!isReverseSearch)
			{
				var tmp = normSelection.Last;
				if (tmp.Message == null)
					tmp = normSelection.First;
				if (tmp.Message != null)
					startFrom = tmp;
			}
			else
			{
				if (normSelection.First.Message != null)
					startFrom = normSelection.First;
			}

			int startFromTextPosition = 0;
			if (startFrom.Message != null)
			{
				var startLine = GetTextToDisplay(startFrom.Message).GetNthTextLine(startFrom.TextLineIndex);
				startFromTextPosition = (startLine.StartIndex - GetTextToDisplay(startFrom.Message).Text.StartIndex) + startFrom.LineCharIndex;
			}

			await NavigateView(async cancellation =>
			{
				IScreenBuffer tmpBuf = new ScreenBuffer(viewSize: 0, initialBufferPosition: InitialBufferPosition.Nowhere);
				await tmpBuf.SetSources(screenBuffer.Sources.Select(s => s.Source), cancellation);
				if (startFrom.Message != null)
				{
					if (!await tmpBuf.MoveToBookmark(bookmarksFactory.CreateBookmark(startFrom.Message),
						MessageMatchingMode.ExactMatch, cancellation))
					{
						return;
					}
				}
				else
				{
					if (isReverseSearch)
						await tmpBuf.MoveToStreamsEnd(cancellation);
					else
						await tmpBuf.MoveToStreamsBegin(cancellation);
				}

				var startPositions = tmpBuf.Sources.ToDictionary(sb => sb.Source, sb => isReverseSearch ? sb.End : sb.Begin);

				IMessage firstMatch = null;

				var tasks = screenBuffer.Sources.Select(async sourceBuf => 
				{
					IMessage sourceMatchingMessage = null;
					Tuple<int, int> sourceMatch = null;

					if (searchOnlyWithinFocusedMessage && sourceBuf.Source != startFrom.Source)
					{
						return new { Message = sourceMatchingMessage, Match = sourceMatch };
					}

					var matcher = makeMatcher(sourceBuf.Source);

					int messagesProcessed = 0;
					await sourceBuf.Source.EnumMessages(
						startPositions[sourceBuf.Source],
						m =>
						{
							++messagesProcessed;

							if (searchOnlyWithinFocusedMessage && messagesProcessed > 1)
								return false;

							int? startFromTextPos = null;
							if (messagesProcessed == 1 && sourceBuf.Source == startFrom.Source)
								startFromTextPos = startFromTextPosition;

							var alreadyFoundMessage = firstMatch;
							if (alreadyFoundMessage != null && MessagesComparer.Compare(m, alreadyFoundMessage) > 0)
								return false;

							var match = matcher(m, messagesProcessed, startFromTextPos);
							if (match == null)
								return true;

							sourceMatchingMessage = m;
							sourceMatch = match;

							Interlocked.CompareExchange(ref firstMatch, m, null);

							return false;
						},
						(isReverseSearch ? EnumMessagesFlag.Backward : EnumMessagesFlag.Forward) | EnumMessagesFlag.IsSequentialScanningHint,
						LogProviderCommandPriority.AsyncUserAction,
						cancellation
					);

					return new { Message = sourceMatchingMessage, Match = sourceMatch };
				}).ToList();

				await Task.WhenAll(tasks);
				cancellation.ThrowIfCancellationRequested();

				var matchedMessageAndRange = 
					tasks
					.Where(t => !t.IsFaulted)
					.Select(t => t.Result)
					.Where(m => m.Message != null)
					.OrderBy(m => m.Message, MessagesComparer.Instance)
					.FirstOrDefault();
				if (matchedMessageAndRange != null)
				{
					IMessage matchedMessage = matchedMessageAndRange.Message;
					var matchedTextRange = matchedMessageAndRange.Match;

					var displayIndex = await LoadMessageAt(matchedMessage, MessageMatchingMode.ExactMatch, cancellation);
					if (displayIndex != null)
					{
						scanResult = matchedMessage;
						if (highlightResult)
						{
							var txt = GetTextToDisplay(matchedMessage).Text;
							var m = matchedTextRange;
							GetTextToDisplay(matchedMessage).EnumLines((line, lineIdx) =>
							{
								var lineBegin = line.StartIndex - txt.StartIndex;
								var lineEnd = lineBegin + line.Length;
								if (m.Item1 >= lineBegin && m.Item1 <= lineEnd)
									SetSelection(displayIndex.Value + lineIdx, SelectionFlag.None, m.Item1 - lineBegin);
								if (m.Item2 >= lineBegin && m.Item2 <= lineEnd)
									SetSelection(displayIndex.Value + lineIdx, SelectionFlag.PreserveSelectionEnd, m.Item2 - lineBegin);
								return true;
							});
						}
						else
						{
							SetSelection(displayIndex.Value, SelectionFlag.SelectBeginningOfLine);
						}
					}
				}
			});

			if (scanResult != null)
			{
				view.HScrollToSelectedText(selection);
			}

			return scanResult;
		}

		async Task NextGoToHighlightedMessage(bool reverse)
		{
			if (selection.Message == null || model.HighlightFilters == null)
				return;
			var hlFilters = model.HighlightFilters;
			await Scan(
				reverse: reverse,
				searchOnlyWithinFocusedMessage: false,
				highlightResult: false,
				makeMatcher: source =>
				{
					var ctx = new FilterContext();
					return (m, messagesProcessed, startFromTextPos) =>
					{
						if (messagesProcessed == 1)
							return null;
						var action = hlFilters.ProcessNextMessageAndGetItsAction(m, ctx, showRawMessages);
						if (action == FilterAction.Include)
							return Tuple.Create(0, GetTextToDisplay(m).Text.Length);
						return null;
					};
				}
			);
		}
		struct ViewLineEntry
		{
			public IMessage Message;
			public int LineIndex;
			public int TextLineIndex;
			public IMessagesSource Source;

			public ViewLine ToViewLine()
			{
				return new ViewLine()
				{
					Message = Message,
					LineIndex = LineIndex,
					TextLineIndex = TextLineIndex,
				};
			}
		};

		enum SelectionFlag
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

		private IPresenter ThisIntf { get { return this; } }
		private int DisplayLinesPerPage { get { return (int) view.DisplayLinesPerPage; }}

		readonly IModel model;
		readonly ISearchResultModel searchResultModel;
		readonly IView view;
		readonly IPresentersFacade presentationFacade;
		readonly IClipboardAccess clipboard;
		readonly LJTraceSource tracer;
		readonly IBookmarksFactory bookmarksFactory;
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly IWordSelection wordSelection = new WordSelection();
		readonly LazyUpdateFlag pendingUpdateFlag = new LazyUpdateFlag();
		readonly List<ViewLineEntry> viewLines = new List<ViewLineEntry>();
		readonly IScreenBuffer screenBuffer = new ScreenBuffer();

		Task currentNavigationTask;
		CancellationTokenSource currentNavigationTaskCancellation;
		int currentNavigationTaskId;

		SelectionInfo selection;
		IMessage slaveModeFocusedMessage;

		string defaultFocusedMessageActionCaption;
		LogFontSize fontSize;
		string fontName;
		bool showTime;
		bool showMilliseconds;
		bool showRawMessages;
		bool rawViewAllowed = true;
		UserInteraction disabledUserInteractions = UserInteraction.None;
		ColoringMode coloring = ColoringMode.Threads;
		FocusedMessageDisplayModes focusedMessageDisplayMode;

		Func<IMessage, IEnumerable<Tuple<int, int>>> searchResultInplaceHightlightHandler;
		int lastSearchOptionsHash;
		Search.PreprocessedOptions lastSearchOptionPreprocessed;
		Search.BulkSearchState inplaceHightlightHandlerState = new Search.BulkSearchState();

		Func<IMessage, IEnumerable<Tuple<int, int>>> selectionInplaceHighlightingHandler;
	};
};
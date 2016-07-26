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
			IClipboardAccess clipboard
		)
		{
			this.model = model;
			this.searchResultModel = model as ISearchResultModel;
			this.view = view;
			this.presentationFacade = navHandler;
			this.clipboard = clipboard;

			this.tracer = new LJTraceSource("UI", "ui.lv");

			if (searchResultModel != null)
			{
				screenBuffer.PositioningMethod = BufferPositioningMethod.MessageIndexes;
			}

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
					view.SaveViewScrollState(selection);
					try
					{
						ReadGlobalSettings(model);
						view.UpdateFontDependentData(fontName, fontSize);
						view.UpdateInnerViewSize();
					}
					finally
					{
						view.RestoreViewScrollState(selection);
					}
					view.Invalidate();
				}
			};

			DisplayHintIfMessagesIsEmpty();

			searchResultInplaceHightlightHandler = SearchResultInplaceHightlightHandler;

			view.UpdateFontDependentData(fontName, fontSize);
		}


		public event EventHandler SelectionChanged;
		public event EventHandler FocusedMessageChanged;
		public event EventHandler BeginShifting;
		public event EventHandler EndShifting;
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

		DateTime? IPresenter.FocusedMessageTime
		{
			get
			{
				if (selection.Message != null)
					return selection.Message.Time.ToLocalDateTime();
				return null;
			}
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

		void HandleSourcesListChange()
		{
			NavigateView(async cancellation =>
			{
				// here is the only place where sources are read from the model.
				// by design presenter won't see changes in sources list until this method is run.
				screenBuffer.SetSources(model.Sources);
				SetScreenBufferSize();

				await screenBuffer.MoveToStreamsBegin(cancellation);
				InternalUpdate();
			}).IgnoreCancellation();
		}

		void SetScreenBufferSize()
		{
			screenBuffer.SetViewSize(view.DisplayLinesPerPage);
		}

		async Task<SearchResult> IPresenter.Search(SearchOptions opts)
		{
			var rv = new SearchResult(); // default inited value has Succeeded = false

			bool isEmptyTemplate = string.IsNullOrEmpty(opts.CoreOptions.Template);
			bool isReverseSearch = opts.CoreOptions.ReverseSearch;

			opts.CoreOptions.SearchInRawText = showRawMessages;

			CursorPosition startFrom = new CursorPosition();
			var normSelection = selection.Normalize();
			if (!isReverseSearch)
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


			int startFromTextPosition = 0;
			if (startFrom.Message != null)
			{
				var startLine = GetTextToDisplay(startFrom.Message).GetNthTextLine(startFrom.TextLineIndex);
				startFromTextPosition = (startLine.StartIndex - GetTextToDisplay(startFrom.Message).Text.StartIndex) + startFrom.LineCharIndex;
			}


			await NavigateView(async cancellation =>
			{
				var innerCancellation = new CancellationTokenSource(); // todo: consider using CreateLinkedTokenSource
				using (cancellation.Register(innerCancellation.Cancel))
				{
					var tasks = screenBuffer.Sources.Select(async sourceBuf => 
					{
						Search.PreprocessedOptions preprocessedOptions = opts.CoreOptions.Preprocess();
						Search.BulkSearchState bulkSearchState = new Search.BulkSearchState();

						int messagesProcessed = 0;

						IMessage sourceMatchingMessage = null;
						var sourceMatch = new Search.MatchedTextRange();

						// todo: use correct starting position
						// idea: create an off-screen screen buffer of size 1 with asyncOp prio,
						// load start message into it, use it's positions as starting ones.

						await sourceBuf.Source.EnumMessages(
							isReverseSearch ? sourceBuf.End : sourceBuf.Begin, 
							m =>
							{
								++messagesProcessed;

								if (opts.SearchOnlyWithinFirstMessage && messagesProcessed > 1)
									return false;

								int? startFromTextPos = null;
								if (!isEmptyTemplate && messagesProcessed == 1)
									startFromTextPos = startFromTextPosition;

								var match = LogJoint.Search.SearchInMessageText(
									m.Message, preprocessedOptions, bulkSearchState, startFromTextPos);

								if (!match.HasValue)
									return true;

								if (isEmptyTemplate && messagesProcessed == 1)
									return true;

								sourceMatchingMessage = m.Message;
								sourceMatch = match.Value;

								return false;
							},
							(isReverseSearch ? EnumMessagesFlag.Backward : EnumMessagesFlag.Forward) 
								| EnumMessagesFlag.IsSequentialScanningHint,
							LogProviderCommandPriority.AsyncUserAction,
							innerCancellation.Token
						);

						return new { Message = sourceMatchingMessage, Match = sourceMatch };
					}).ToList();

					IMessage matchedMessage = null;
					var matchedTextRange = new Search.MatchedTextRange();
					while (tasks.Count > 0)
					{
						var t = await Task.WhenAny(tasks);
						cancellation.ThrowIfCancellationRequested();
						if (!t.IsFaulted && t.Result.Message != null)
						{
							matchedMessage = t.Result.Message;
							matchedTextRange = t.Result.Match;
							// todo: wrong! random first match may be not the right one
							innerCancellation.Cancel();
							break;
						}
						tasks.Remove(t);
					}

					if (matchedMessage != null)
					{
						var displayIndex = await SelectMessageAtCore(matchedMessage.Time, 
							matchedMessage.Position, matchedMessage.GetConnectionId(), cancellation);
						if (displayIndex != null)
						{
							rv.Succeeded = true;
							rv.Message = matchedMessage;

							if (opts.HighlightResult)
							{
								var txt = GetTextToDisplay(matchedMessage).Text;
								var m = matchedTextRange;
								GetTextToDisplay(matchedMessage).EnumLines((line, lineIdx) =>
								{
									var lineBegin = line.StartIndex - txt.StartIndex;
									var lineEnd = lineBegin + line.Length;
									if (m.MatchBegin >= lineBegin && m.MatchBegin <= lineEnd)
										SetSelection(displayIndex.Value + lineIdx, SelectionFlag.ShowExtraLinesAroundSelection, m.MatchBegin - lineBegin);
									if (m.MatchEnd >= lineBegin && m.MatchEnd <= lineEnd)
										SetSelection(displayIndex.Value + lineIdx, SelectionFlag.PreserveSelectionEnd | SelectionFlag.ShowExtraLinesAroundSelection,
											m.MatchEnd - lineBegin);
									return true;
								});
							}
							else
							{
								SetSelection(displayIndex.Value, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
							}
						}
					}
				}
			});

			// todo: if success view.HScrollToSelectedText(selection);

			return rv;
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
				await screenBuffer.MoveToDate(date, cancellation);
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

		async Task IPresenter.GoToNextHighlightedMessage()
		{
			if (selection.Message == null || model.HighlightFilters == null)
				return;
			// todo: implemement
		}

		async Task IPresenter.GoToPrevHighlightedMessage()
		{
			// todo: implemement
		}

		SelectionInfo IPresenter.Selection { get { return selection; } }

		void IPresenter.InvalidateView()
		{
			view.Invalidate();
		}

		IBookmark IPresenter.NextBookmark(bool forward)
		{
			if (selection.Message == null || model.Bookmarks == null)
				return null;
			return model.Bookmarks.GetNext(selection.Message, forward);
		}

		async Task<BookmarkSelectionStatus> IPresenter.SelectMessageAt(IBookmark bmk)
		{
			if (bmk == null)
				return BookmarkSelectionStatus.BookmarkedMessageNotFound;

			var ret = BookmarkSelectionStatus.ActionCancelled;
			await NavigateView(async cancellation => 
			{
				var idx = await SelectMessageAtCore(bmk.Time, bmk.Position, 
					bmk.LogSourceConnectionId, cancellation);
				cancellation.ThrowIfCancellationRequested();
				if (idx != null)
					SelectFullLine(idx.Value);
				if (idx != null)
					ret = BookmarkSelectionStatus.Success;
				else
					ret = BookmarkSelectionStatus.BookmarkedMessageNotFound;
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
			return MoveSelection(+1, SelectionFlag.ShowExtraLinesAroundSelection);
		}

		Task IPresenter.GoToPrevMessage()
		{
			return MoveSelection(-1, SelectionFlag.ShowExtraLinesAroundSelection);
		}

		void IPresenter.ClearSelection()
		{
			if (selection.First.Message == null)
				return;

			InvalidateTextLineUnderCursor();
			foreach (var displayIndexToInvalidate in selection.GetDisplayIndexesRange().Where(idx => idx < displayMessages.Count))
				view.InvalidateMessage(displayMessages[displayIndexToInvalidate].ToDisplayLine(displayIndexToInvalidate));

			selection.SetSelection(new CursorPosition(), new CursorPosition());
			OnSelectionChanged();
			OnFocusedMessageChanged();
		}

		string IPresenter.GetSelectedText()
		{
			return GetSelectedTextInternal(includeTime: false);
		}

		void IPresenter.CopySelectionToClipboard()
		{
			if (clipboard == null)
				return;
			var txt = GetSelectedTextInternal(includeTime: showTime);
			if (txt.Length > 0)
				clipboard.SetClipboard(txt, GetSelectedTextAsHtml(includeTime: showTime));
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

		void IPresenter.SelectSlaveModeFocusedMessage()
		{
			if (displayMessages.Count == 0)
				return;
			var position = FindSlaveModeFocusedMessagePositionInternal(0, displayMessages.Count);
			if (position == null)
				return;
			var idxToSelect = position.Item1;
			if (idxToSelect == displayMessages.Count)
				--idxToSelect;
			SetSelection(idxToSelect,
				SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection | SelectionFlag.ScrollToViewEventIfSelectionDidNotChange);
			view.AnimateSlaveMessagePosition();
		}

		void IPresenter.SelectFirstMessage()
		{
			if (displayMessages.Count > 0)
			{
				SetSelection(0, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
			}
		}

		void IPresenter.SelectLastMessage()
		{
			if (displayMessages.Count > 0)
			{
				SetSelection(displayMessages.Count - 1, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
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

		void IViewEvents.OnShowFiltersLinkClicked()
		{
			if (presentationFacade != null)
				presentationFacade.ShowFiltersView();
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

		void IViewEvents.OnKeyPressed(Key k)
		{
			OnKeyPressedAsync(k).IgnoreCancellation();
		}

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
					ThisIntf.CopySelectionToClipboard();
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
				catch (OperationCanceledException cancellationException)
				{
					throw; // fail navigation task with same exception
				}
				catch (Exception e)
				{
					// todo: log exception
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

		async Task<int?> SelectMessageAtCore(IMessage msg, CancellationToken cancellation)
		{
			return await SelectMessageAtCore(msg.Time, msg.Position, msg.GetConnectionId(), cancellation);
		}

		void SelectFullLine(int displayIndex)
		{
			SetSelection(displayIndex, SelectionFlag.SelectEndOfLine | SelectionFlag.ShowExtraLinesAroundSelection | SelectionFlag.NoHScrollToSelection);
			SetSelection(displayIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.PreserveSelectionEnd);
		}

		static async Task<IMessage> ScanMessages(
			IMessagesSource p, long startFrom, EnumMessagesFlag flags, Predicate<IMessage> predicate, CancellationToken cancellation)
		{
			IMessage ret = null;
			await p.EnumMessages(startFrom, msg => 
			{
				if (!predicate(msg.Message))
					return true;
				ret = msg.Message;
				return false;
			}, flags, LogProviderCommandPriority.AsyncUserAction, cancellation);
			cancellation.ThrowIfCancellationRequested();
			return ret;
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
				ThisIntf.CopySelectionToClipboard();
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

		void IViewEvents.OnVScroll(double value)
		{
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
			CursorPosition pos,
			MessageMouseEventFlag flags,
			object preparedContextMenuPopupData)
		{
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
		FocusedMessageDisplayModes IPresentationDataAccess.FocusedMessageDisplayMode { get { return focusedMessageDisplayMode; } }
		Tuple<int, int> IPresentationDataAccess.FindSlaveModeFocusedMessagePosition(int beginIdx, int endIdx)
		{
			return FindSlaveModeFocusedMessagePositionInternal(beginIdx, endIdx);
		}

		IEnumerable<DisplayLine> IPresentationDataAccess.GetDisplayLines(int beginIdx, int endIdx)
		{
			int i = beginIdx;
			for (; i != endIdx; ++i)
			{
				var dm = displayMessages[i];
				yield return new DisplayLine() { DisplayLineIndex = i, Message = dm.DisplayMsg, TextLineIndex = dm.TextLineIndex };
			}
		}

		int IPresentationDataAccess.DisplayLinesCount
		{
			get { return displayMessages.Count; }
		}

		double IPresentationDataAccess.GetFirstDisplayMessageScrolledLines()
		{
			return screenBuffer.TopMessageScrolledLines;
		}

		IBookmarksHandler IPresentationDataAccess.CreateBookmarksHandler()
		{
			return model.Bookmarks != null ? model.Bookmarks.CreateHandler() : new DummyBookmarksHandler();
		}



		private Tuple<int, int> FindSlaveModeFocusedMessagePositionInternal(int beginIdx, int endIdx)
		{
			if (slaveModeFocusedMessage == null)
				return null;
			int lowerBound = ListUtils.BinarySearch(displayMessages, beginIdx, endIdx, dm => CompareMessages(dm.DisplayMsg, slaveModeFocusedMessage) < 0);
			int upperBound = ListUtils.BinarySearch(displayMessages, lowerBound, endIdx, dm => CompareMessages(dm.DisplayMsg, slaveModeFocusedMessage) <= 0);
			return new Tuple<int, int>(lowerBound, upperBound);
		}

		static int CompareMessages(IMessage msg1, IMessage msg2)
		{
			int ret = MessageTimestamp.Compare(msg1.Time, msg2.Time);
			if (ret != 0)
				return ret;
			ret = Math.Sign(msg1.Position - msg2.Position);
			return ret;
		}

		public static StringUtils.MultilineText GetTextToDisplay(IMessage msg, bool showRawMessages)
		{
			if (showRawMessages)
			{
				var r = msg.RawTextAsMultilineText;
				if (r.Text.IsInitialized)
					return r;
				return msg.TextAsMultilineText;
			}
			else
			{
				return msg.TextAsMultilineText;
			}
		}

		StringUtils.MultilineText GetTextToDisplay(IMessage msg)
		{
			return GetTextToDisplay(msg, showRawMessages);
		}

		public void InvalidateTextLineUnderCursor()
		{
			if (selection.First.Message != null)
			{
				view.InvalidateMessage(selection.First.ToDisplayLine());
			}
		}

		public enum SelectionFlag
		{
			None = 0,
			PreserveSelectionEnd = 1,
			SelectBeginningOfLine = 2,
			SelectEndOfLine = 4,
			SelectBeginningOfNextWord = 8,
			SelectBeginningOfPrevWord = 16,
			ShowExtraLinesAroundSelection = 32, // todo: imlemement it
			SuppressOnFocusedMessageChanged = 64,
			NoHScrollToSelection = 128,
			ScrollToViewEventIfSelectionDidNotChange = 256
		};

		void SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null)
		{
			var dmsg = displayMessages[displayIndex];
			var msg = dmsg.DisplayMsg;
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
						.Where(idx => idx < displayMessages.Count && idx >= 0))
				{
					view.InvalidateMessage(displayMessages[displayIndexToInvalidate].ToDisplayLine(displayIndexToInvalidate));
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

		static bool MessagesAreSame(IMessage m1, IMessage m2)
		{
			if (m1 == m2)
				return true;
			if (m1 == null || m2 == null)
				return false;
			return m1.Position == m2.Position && m1.Thread == m2.Thread;
		}

		async Task<int?> SelectMessageAtCore(
			MessageTimestamp dt,
			long position,
			string logSourceCollectionId,
			CancellationToken cancellation)
		{
			if (!await screenBuffer.MoveToMessage(dt, position, logSourceCollectionId, cancellation))
				return null;

			InternalUpdate();

			foreach (var x in displayMessages.Where(
				x => x.DisplayMsg.GetConnectionId() == logSourceCollectionId && x.DisplayMsg.Position == position))
			{
				return x.DisplayIndex;
			}
			return null;
		}

		async Task<bool> ScrollSelectionIntoScreenBuffer(CancellationToken cancellation)
		{
			if (selection.Message == null || displayMessages.Count == 0)
				return false;
			if (selection.First.DisplayIndex >= 0 && selection.First.DisplayIndex < displayMessages.Count)
				return true;
			var idx = await SelectMessageAtCore(selection.Message, cancellation);
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
			if (newDisplayPosition >= 0 && newDisplayPosition < displayMessages.Count)
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

		protected virtual void OnRefresh()
		{
			if (ManualRefresh != null)
				ManualRefresh(this, EventArgs.Empty);
		}

		struct DisplayMessagesEntry
		{
			public IMessage DisplayMsg;
			public int DisplayIndex;
			public int TextLineIndex;
			public IMessagesSource Source;
			public DisplayLine ToDisplayLine(int index)
			{
				return new DisplayLine()
				{
					Message = DisplayMsg,
					DisplayLineIndex = index,
					TextLineIndex = TextLineIndex
				};
			}
		};

		/*
		static MergedMessagesEntry InitMergedMessagesEntry(
			IMessage message, 
			MergedMessagesEntry cachedMessageEntry,
			ThreadLocal<IFiltersList> highlighFilters,
			bool highlightFiltersPreprocessingResultCacheIsValid,
			bool matchRawMessages)
		{
			MergedMessagesEntry ret;

			ret.LoadedMsg = message;

			if (highlightFiltersPreprocessingResultCacheIsValid)
				ret.HighlightFiltersPreprocessingResult = cachedMessageEntry.HighlightFiltersPreprocessingResult;
			else
				ret.HighlightFiltersPreprocessingResult = highlighFilters.Value.PreprocessMessage(message, matchRawMessages);

			return ret;
		}*/

		struct FocusedMessageFinder
		{
			Presenter presenter;
			SelectionInfo prevFocused;
			long prevFocusedPosition1;
			ILogSource prevFocusedSource1;
			long prevFocusedPosition2;
			ILogSource prevFocusedSource2;
			IndexedMessage newFocused1;
			IndexedMessage newFocused2;

			public FocusedMessageFinder(Presenter presenter)
			{
				this.presenter = presenter;
				prevFocused = presenter.selection;
				prevFocusedPosition1 = prevFocused.First.Message != null ? prevFocused.First.Message.Position : long.MinValue;
				prevFocusedSource1 = prevFocused.First.Message != null ? prevFocused.First.Message.LogSource : null;
				prevFocusedPosition2 = prevFocused.Last.Message != null ? prevFocused.Last.Message.Position : long.MinValue;
				prevFocusedSource2 = prevFocused.Last.Message != null ? prevFocused.Last.Message.LogSource : null;
				newFocused1 = new IndexedMessage();
				newFocused2 = new IndexedMessage();
			}
			public void HandleNewDisplayMessage(IMessage msg, int displayIndex)
			{
				if (prevFocusedPosition1 == msg.Position && msg.LogSource == prevFocusedSource1)
					newFocused1 = new IndexedMessage() { Message = msg, Index = displayIndex };
				if (prevFocusedPosition2 == msg.Position && msg.LogSource == prevFocusedSource2)
					newFocused2 = new IndexedMessage() { Message = msg, Index = displayIndex };
			}
			private int ComposeNewDisplayIndex(int newBaseMessageIndex, int originalTextLineIndex)
			{
				var dmsg = presenter.displayMessages[newBaseMessageIndex].DisplayMsg;
				var currentLinesCount = presenter.GetTextToDisplay(dmsg).GetLinesCount();

				// when switching raw/normal views the amount of display lines for one IMessage may change.
				// make sure line index is still valid.
				var newLineIndex = Math.Min(originalTextLineIndex, currentLinesCount - 1);

				return newBaseMessageIndex + newLineIndex;
			}
			public void SetFoundSelection()
			{
				if (newFocused2.Message != null)
				{
					presenter.SetSelection(
						ComposeNewDisplayIndex(newFocused2.Index, prevFocused.Last.TextLineIndex),
						SelectionFlag.SuppressOnFocusedMessageChanged, prevFocused.Last.LineCharIndex);
				}
				if (newFocused1.Message != null)
				{
					presenter.SetSelection(
						ComposeNewDisplayIndex(newFocused1.Index, prevFocused.First.TextLineIndex),
						SelectionFlag.PreserveSelectionEnd, prevFocused.First.LineCharIndex);
				}
				else
				{
					if (prevFocused.First.Message != null)
					{
						var prevFocusedMsg = prevFocused.First.Message;
						int messageNearestToPrevSelection = 0;//presenter.DisplayMessages.BinarySearch(0, presenter.DisplayMessages.Count,
							//dm => MessagesComparer.Compare(dm, prevFocusedMsg, false) < 0);
						//if (messageNearestToPrevSelection != presenter.DisplayMessages.Count)
						if (false)
						{
							presenter.SetSelection(messageNearestToPrevSelection, SelectionFlag.SelectBeginningOfLine);
						}
						else
						{
							presenter.ThisIntf.ClearSelection();
						}
					}
					else
					{
						presenter.ThisIntf.ClearSelection();
					}
				}

				if (presenter.selection.First.Message != null)
				{
					if (prevFocused.First.Message == null)
					{
						//presenter.view.ScrollInView(presenter.selection.First.DisplayIndex, true);
					}
				}
			}
		};

		int GetDisplayIndex(CursorPosition pos)
		{
			if (pos.Message == null)
				return 0;
			int didx = 0;
			foreach (var m in displayMessages)
			{
				if (m.DisplayMsg != null && MessagesAreSame(m.DisplayMsg, pos.Message))
					return didx;
				++didx;
			}
			if (pos.Message.Position < 
					screenBuffer.Sources.Where(s => s.Source == pos.Source).Select(s => s.Begin).FirstOrDefault())
				return -1;
			else
				return displayMessages.Count;
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
				displayMessages.Clear();
				foreach (var m in screenBuffer.Messages)
				{
					var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(m.Message);

					if (hlFilters != null)
					{
						var hlPreproc = hlFilters.PreprocessMessage(m.Message, showRawMessages);
						bool isHighlighted = false;
						FilterAction hlFilterAction = hlFilters.ProcessNextMessageAndGetItsAction(
							m.Message, hlPreproc,
							threadsBulkProcessingResult.HighlightFilterContext, showRawMessages);
						isHighlighted = hlFilterAction == FilterAction.Include;
						m.Message.SetHighlighted(isHighlighted);
					}

					int linesCount = GetTextToDisplay(m.Message).GetLinesCount();
					for (int i = 0; i < linesCount; ++i)
					{
						displayMessages.Add(new DisplayMessagesEntry()
						{
							DisplayMsg = m.Message,
							DisplayIndex = displayMessages.Count,
							TextLineIndex = i,
							Source = m.Source
						});
					}
				}
			}

			if (hlFilters != null)
				hlFilters.EndBulkProcessing(hlFiltersProcessingHandle);

			DisplayHintIfMessagesIsEmpty();

			UpdateSelectionDisplayIndexes();
			view.Invalidate();
			view.SetVScroll(displayMessages.Count > 0 ? screenBuffer.BufferPosition : new double?());
		}

		private bool DisplayHintIfMessagesIsEmpty()
		{
			if (displayMessages.Count == 0)
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

		private IBookmarksHandler CreateBookmarksHandler()
		{
			return model.Bookmarks != null ? model.Bookmarks.CreateHandler() : null;
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
			var dmsg = displayMessages[pos.DisplayIndex];
			var word = wordSelection.FindWordBoundaries(
				GetTextToDisplay(dmsg.DisplayMsg).GetNthTextLine(dmsg.TextLineIndex), pos.LineCharIndex);
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
			var idx = await SelectMessageAtCore(foundMessage, cancellation);
			if (idx != null)
				SetSelection(idx.Value, SelectionFlag.SelectBeginningOfLine | SelectionFlag.ShowExtraLinesAroundSelection);
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
				view.SaveViewScrollState(selection);
				try
				{
					fontSize = value;
					view.UpdateFontDependentData(fontName, fontSize);
					view.UpdateInnerViewSize();
				}
				finally
				{
					view.RestoreViewScrollState(selection);
				}
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

		struct SelectTextLine
		{
			public string Str;
			public IMessage Message;
			public int LineIndex;
			public bool IsSingleLineSelectionFragment;
		};

		private IEnumerable<SelectTextLine> GetSelectedTextLines(bool includeTime)
		{
			if (selection.IsEmpty)
				yield break;
			var normSelection = selection.Normalize();
			int selectedLinesCount = normSelection.Last.DisplayIndex - normSelection.First.DisplayIndex + 1;
			IMessage prevMessage = null;
			var sb = new StringBuilder();
			foreach (var i in displayMessages.Skip(normSelection.First.DisplayIndex).Take(selectedLinesCount).ZipWithIndex())
			{
				sb.Clear();
				var line = GetTextToDisplay(i.Value.DisplayMsg).GetNthTextLine(i.Value.TextLineIndex);
				bool isFirstLine = i.Key == 0;
				bool isLastLine = i.Key == selectedLinesCount - 1;
				int beginIdx = isFirstLine ? normSelection.First.LineCharIndex : 0;
				int endIdx = isLastLine ? normSelection.Last.LineCharIndex : line.Length;
				if (i.Value.DisplayMsg != prevMessage)
				{
					if (includeTime)
					{
						if (beginIdx == 0 && (endIdx - beginIdx) > 0)
						{
							sb.AppendFormat("{0}\t", i.Value.DisplayMsg.Time.ToUserFrendlyString(showMilliseconds));
						}
					}
					prevMessage = i.Value.DisplayMsg;
				}
				line.SubString(beginIdx, endIdx - beginIdx).Append(sb);
				if (isLastLine && sb.Length == 0)
					break;
				yield return new SelectTextLine()
				{
					Str = sb.ToString(),
					Message = i.Value.DisplayMsg,
					LineIndex = i.Key,
					IsSingleLineSelectionFragment = isFirstLine && isLastLine && (beginIdx != 0 || endIdx != line.Length)
				};
			}
		}

		private string GetSelectedTextInternal(bool includeTime)
		{
			var sb = new StringBuilder();
			foreach (var line in GetSelectedTextLines(includeTime))
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
			var cl = "black";
			if (ls != null)
				if (coloring == ColoringMode.Threads)
					cl = msg.Thread.ThreadColor.ToHtmlColor();
				else if (coloring == ColoringMode.Sources)
					cl = ls.Color.ToHtmlColor();
			return cl;
		}

		private string GetSelectedTextAsHtml(bool includeTime)
		{
			var sb = new StringBuilder();
			sb.Append("<pre style='font-size:8pt; font-family: monospace; padding:0; margin:0;'>");
			foreach (var line in GetSelectedTextLines(includeTime))
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

		private IPresenter ThisIntf { get { return this; } }
		private int DisplayLinesPerPage { get { return (int) view.DisplayLinesPerPage; }}

		readonly IModel model;
		readonly ISearchResultModel searchResultModel;
		readonly IView view;
		readonly IPresentersFacade presentationFacade;
		readonly IClipboardAccess clipboard;
		readonly LJTraceSource tracer;
		readonly IWordSelection wordSelection = new WordSelection();
		readonly LazyUpdateFlag pendingUpdateFlag = new LazyUpdateFlag();
		readonly List<DisplayMessagesEntry> displayMessages = new List<DisplayMessagesEntry>();
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
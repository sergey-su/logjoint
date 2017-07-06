using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
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
			Telemetry.ITelemetryCollector telemetry,
			IScreenBufferFactory screenBufferFactory
		)
		{
			this.model = model;
			this.searchResultModel = model as ISearchResultModel;
			this.view = view;
			this.presentationFacade = navHandler;
			this.bookmarksFactory = bookmarksFactory;
			this.telemetry = telemetry;
			this.screenBufferFactory = screenBufferFactory;

			this.tracer = new LJTraceSource("UI", "ui.lv");

			this.screenBuffer = screenBufferFactory.CreateScreenBuffer(InitialBufferPosition.StreamsEnd);
			this.selectionManager = new SelectionManager(
				view, searchResultModel, screenBuffer, tracer, this, clipboard, screenBufferFactory, bookmarksFactory);
			this.navigationManager = new NavigationManager(
				tracer, telemetry);

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
					navigationManager.NavigateView(async cancellation => 
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

			view.UpdateFontDependentData(fontName, fontSize);
		}

		#region Interfaces implementation

		public event EventHandler DefaultFocusedMessageAction;
		public event EventHandler ManualRefresh;
		public event EventHandler RawViewModeChanged;
		public event EventHandler ColoringModeChanged;
		public event EventHandler<ContextMenuEventArgs> ContextMenuOpening;

		event EventHandler IPresenter.SelectionChanged
		{
			add { selectionManager.SelectionChanged += value; }
			remove { selectionManager.SelectionChanged -= value; }
		}
		event EventHandler IPresenter.FocusedMessageChanged
		{
			add { selectionManager.FocusedMessageChanged += value; }
			remove { selectionManager.FocusedMessageChanged -= value; }
		}
		event EventHandler IPresenter.FocusedMessageBookmarkChanged
		{
			add { selectionManager.FocusedMessageBookmarkChanged += value; }
			remove { selectionManager.FocusedMessageBookmarkChanged -= value; }
		}
		event EventHandler IPresenter.NavigationIsInProgressChanged
		{
			add { navigationManager.NavigationIsInProgressChanged += value; }
			remove { navigationManager.NavigationIsInProgressChanged -= value; }
		}

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
			get { return Selection.Message; }
		}

		IBookmark IPresenter.GetFocusedMessageBookmark()
		{
			return selectionManager.GetFocusedMessageBookmark();
		}

		bool IPresenter.NavigationIsInProgress
		{
			get { return navigationManager.NavigationIsInProgress; }
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
				selectionManager.HandleRawModeChange();
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

		async Task<Dictionary<IMessagesSource, long>> IPresenter.GetCurrentPositions(CancellationToken cancellation)
		{
			if (Selection.Message == null)
				return null;
			var tmp = screenBufferFactory.CreateScreenBuffer(InitialBufferPosition.Nowhere);
			await tmp.SetSources(screenBuffer.Sources.Select(s => s.Source), cancellation);
			await tmp.MoveToBookmark(ThisIntf.GetFocusedMessageBookmark(), BookmarkLookupMode.ExactMatch, cancellation);
			return tmp.Sources.ToDictionary(s => s.Source, s => s.Begin);
		}

		async Task<IMessage> IPresenter.Search(SearchOptions opts)
		{
			bool isEmptyTemplate = string.IsNullOrEmpty(opts.CoreOptions.Template);

			return await Scan(
				opts.CoreOptions.ReverseSearch,
				opts.SearchOnlyWithinFocusedMessage,
				opts.HighlightResult,
				opts.CoreOptions.SearchWithinThisLog,
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

		Task IPresenter.SelectMessageAt(DateTime date, ILogSource[] preferredSources)
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				bool handled = false;
				if (preferredSources != null && preferredSources.Length != 0)
				{
					var candidates = (await Task.WhenAll(preferredSources.Select(async preferredSource =>  
					{
						var lowerDatePos = await preferredSource.Provider.GetDateBoundPosition(
							date, ListUtils.ValueBound.Lower, 
							getDate: true,
							priority: LogProviderCommandPriority.RealtimeUserAction, 
							cancellation: cancellation);
						var upperDatePos = await preferredSource.Provider.GetDateBoundPosition(
							date, ListUtils.ValueBound.UpperReversed, 
							getDate: true,
							priority: LogProviderCommandPriority.RealtimeUserAction, 
							cancellation: cancellation);
						return new []
						{
							new { rsp = lowerDatePos, ls = preferredSource},
							new { rsp = upperDatePos, ls = preferredSource},
						};
					})))
						.SelectMany(batch => batch)
						.Where(candidate => candidate.rsp.Date.HasValue)
						.ToList();
					if (candidates.Count > 0)
					{
						var bestCandidate = candidates.OrderBy(
							c => (date - c.rsp.Date.Value.ToLocalDateTime()).Abs()).First();
						var msgIdx = await LoadMessageAt(bookmarksFactory.CreateBookmark(
								bestCandidate.rsp.Date.Value,
								bestCandidate.ls.ConnectionId,
								bestCandidate.rsp.Position,
								0
							), 
							BookmarkLookupMode.ExactMatch,
							cancellation
						);
						if (msgIdx != null)
							SelectFullLine(msgIdx.Value);
						handled = msgIdx != null;
					}
				}
				if (!handled)
				{
					await screenBuffer.MoveToBookmark(
						bookmarksFactory.CreateBookmark(new MessageTimestamp(date)),
						BookmarkLookupMode.FindNearestTime | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, 
						cancellation
					);
					InternalUpdate();
					ThisIntf.SelectFirstMessage();
				}
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

		async Task<bool> IPresenter.SelectMessageAt(IBookmark bmk)
		{
			if (bmk == null)
				return false;

			bool ret = false;
			await navigationManager.NavigateView(async cancellation => 
			{
				var idx = await LoadMessageAt(bmk, BookmarkLookupMode.ExactMatch, cancellation);
				if (idx != null)
					SelectFullLine(idx.Value);
				ret = idx != null;
			});

			return ret;
		}

		Task IPresenter.GoHome()
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				await screenBuffer.MoveToStreamsBegin(cancellation);
				InternalUpdate();
				ThisIntf.SelectFirstMessage();
			});
		}

		Task IPresenter.GoToEnd()
		{
			return navigationManager.NavigateView(async cancellation =>
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
			selectionManager.Clear();
		}

		Task<string> IPresenter.GetSelectedText()
		{
			return selectionManager.GetSelectedText();
		}

		Task IPresenter.CopySelectionToClipboard()
		{
			return selectionManager.CopySelectionToClipboard();
		}

		bool IPresenter.IsSinglelineNonEmptySelection
		{
			get { return !selectionManager.Selection.IsEmpty && selectionManager.Selection.IsSingleLine; }
		}

		bool IPresenter.HasInputFocus
		{
			get { return view.HasInputFocus; }
		}

		void IPresenter.ReceiveInputFocus()
		{
			view.ReceiveInputFocus();
		}

		IBookmark IPresenter.SlaveModeFocusedMessage
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
			return navigationManager.NavigateView(async cancellation =>
			{
				if (slaveModeFocusedMessage == null)
					return;
				await LoadMessageAt(slaveModeFocusedMessage, BookmarkLookupMode.FindNearestBookmark, cancellation);
				var position = FindSlaveModeFocusedMessagePositionInternal(0, screenBuffer.Messages.Count);
				if (position == null)
					return;
				var idxToSelect = position.Item1;
				if (idxToSelect == screenBuffer.Messages.Count)
					--idxToSelect;
				selectionManager.SetSelection(idxToSelect,
					SelectionFlag.SelectBeginningOfLine | SelectionFlag.ScrollToViewEventIfSelectionDidNotChange);
				view.AnimateSlaveMessagePosition();
			});
		}

		void IPresenter.SelectFirstMessage()
		{
			if (screenBuffer.Messages.Count > 0)
			{
				selectionManager.SetSelection(0, SelectionFlag.SelectBeginningOfLine);
			}
		}

		void IPresenter.SelectLastMessage()
		{
			if (screenBuffer.Messages.Count > 0)
			{
				selectionManager.SetSelection(screenBuffer.Messages.Count - 1, SelectionFlag.SelectBeginningOfLine);
			}
		}

		void IPresenter.MakeFirstLineFullyVisible()
		{
			screenBuffer.TopLineScrollValue = 0;
			view.Invalidate();
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
			selectionManager.InvalidateTextLineUnderCursor();
		}

		void IViewEvents.OnKeyPressed(Key k)
		{
			OnKeyPressedAsync(k).IgnoreCancellation();
		}

		MenuData IViewEvents.OnMenuOpening()
		{
			var ret = new MenuData();
			ret.VisibleItems =
				ContextMenuItem.ShowTime |
				ContextMenuItem.GotoNextMessageInTheThread |
				ContextMenuItem.GotoPrevMessageInTheThread;
			ret.CheckedItems = ContextMenuItem.None;

			if ((disabledUserInteractions & UserInteraction.CopyMenu) == 0)
				ret.VisibleItems |= ContextMenuItem.Copy;

			if (ThisIntf.ShowTime)
				ret.CheckedItems |= ContextMenuItem.ShowTime;

			if (ThisIntf.RawViewAllowed && (ThisIntf.DisabledUserInteractions & UserInteraction.RawViewSwitching) == 0)
				ret.VisibleItems |= ContextMenuItem.ShowRawMessages;
			if (ThisIntf.ShowRawMessages)
				ret.CheckedItems |= ContextMenuItem.ShowRawMessages;

			if (model.Bookmarks != null)
				ret.VisibleItems |= ContextMenuItem.ToggleBmk;

			ret.DefaultItemText = ThisIntf.DefaultFocusedMessageActionCaption;
			if (!string.IsNullOrEmpty(ret.DefaultItemText))
				ret.VisibleItems |= ContextMenuItem.DefaultAction;

			if (ContextMenuOpening != null)
			{
				var args = new ContextMenuEventArgs();
				ContextMenuOpening(this, args);
				ret.ExtendededItems = args.items;
			}

			return ret;
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
				model.Bookmarks.ToggleBookmark(ThisIntf.GetFocusedMessageBookmark());
			else if (menuItem == ContextMenuItem.GotoNextMessageInTheThread)
				ThisIntf.GoToNextMessageInThread();
			else if (menuItem == ContextMenuItem.GotoPrevMessageInTheThread)
				ThisIntf.GoToPrevMessageInThread();
		}

		void IViewEvents.OnDisplayLinesPerPageChanged()
		{
			navigationManager.NavigateView(async cancellation => 
			{
				SetScreenBufferSize();
				await screenBuffer.Reload(cancellation);
				InternalUpdate();
			}).IgnoreCancellation();
		}

		void IViewEvents.OnIncrementalVScroll(float nrOfDisplayLines)
		{
			navigationManager.NavigateView(async cancellation =>
			{
				await ShiftViewBy(nrOfDisplayLines, cancellation);
			}).IgnoreCancellation();
		}

		void IViewEvents.OnVScroll(double value, bool isRealtimeScroll)
		{
			//if (!isRealtimeScroll)
			navigationManager.NavigateView(async cancellation =>
			{
				await screenBuffer.MoveToPosition(value, cancellation);
				InternalUpdate();
			}).IgnoreCancellation();
		}

		void IViewEvents.OnHScroll()
		{
			selectionManager.InvalidateTextLineUnderCursor();
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
				if (!Selection.IsInsideSelection(pos))
					selectionManager.SetSelection(pos.DisplayIndex, SelectionFlag.None, pos.LineCharIndex);
				view.PopupContextMenu(preparedContextMenuPopupData);
			}
			else
			{
				if ((flags & MessageMouseEventFlag.CapturedMouseMove) == 0 && (flags & MessageMouseEventFlag.OulineBoxesArea) != 0)
				{
					if ((flags & MessageMouseEventFlag.ShiftIsHeld) == 0)
						selectionManager.SetSelection(pos.DisplayIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.NoHScrollToSelection);
					selectionManager.SetSelection(pos.DisplayIndex, SelectionFlag.SelectEndOfLine | SelectionFlag.PreserveSelectionEnd | SelectionFlag.NoHScrollToSelection);
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
							defaultSelection = !selectionManager.SelectWordBoundaries(pos);
						}
					}
					if (defaultSelection)
					{
						selectionManager.SetSelection(pos.DisplayIndex, (flags & MessageMouseEventFlag.ShiftIsHeld) != 0
							? SelectionFlag.PreserveSelectionEnd : SelectionFlag.None, pos.LineCharIndex);
					}
				}
			}
		}

		void IViewEvents.OnDrawingError(Exception e)
		{
			if (!drawingErrorReported)
			{
				drawingErrorReported = true; // report first error only
				telemetry.ReportException(e, "log viewer drawing");
			}
		}


		bool IPresentationDataAccess.ShowTime { get { return showTime; } }
		bool IPresentationDataAccess.ShowMilliseconds { get { return showMilliseconds; } }
		bool IPresentationDataAccess.ShowRawMessages { get { return showRawMessages; } }
		SelectionInfo IPresentationDataAccess.Selection { get { return Selection; } }
		ColoringMode IPresentationDataAccess.Coloring { get { return coloring; } }

		Func<IMessage, IEnumerable<Tuple<int, int>>> IPresentationDataAccess.InplaceHighlightHandler1
		{
			get
			{
				if (searchResultModel != null && searchResultModel.SearchParams != null)
					return selectionManager.SearchInplaceHighlightHandler;
				return null;
			}
		}

		Func<IMessage, IEnumerable<Tuple<int, int>>> IPresentationDataAccess.InplaceHighlightHandler2
		{
			get { return selectionManager.InplaceHighlightHandler; }
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
					var vl = screenBuffer.Messages[i].ToViewLine();
					if (!vl.Message.Thread.IsDisposed)
						vl.IsBookmarked = bookmarksHandler.ProcessNextMessageAndCheckIfItIsBookmarked(vl.Message, vl.TextLineIndex);
					yield return vl;
				}
			}
		}

		int IPresentationDataAccess.ViewLinesCount
		{
			get { return screenBuffer.Messages.Count; }
		}

		double IPresentationDataAccess.GetFirstDisplayMessageScrolledLines()
		{
			return screenBuffer.TopLineScrollValue;
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

			CursorPosition cur = Selection.First;

			var preserveSelectionFlag = (keyFlags & Key.ModifySelectionModifier) != 0 
				? SelectionFlag.PreserveSelectionEnd : SelectionFlag.None;
			var alt = (keyFlags & Key.AlternativeModeModifier) != 0;

			if (Selection.Message != null)
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
					view.PopupContextMenu(view.GetContextMenuPopupDataForCurrentSelection(Selection));
				else if (k == Key.Enter)
					PerformDefaultFocusedMessageAction();
				else if (k == Key.BookmarkShortcut)
					model.Bookmarks.ToggleBookmark(ThisIntf.GetFocusedMessageBookmark());
			}
			if (k == Key.Copy)
			{
				if ((disabledUserInteractions & UserInteraction.CopyShortcut) == 0)
					await ThisIntf.CopySelectionToClipboard();
			}
			else if (k == Key.BeginOfLine)
			{
				await MoveSelection(0, preserveSelectionFlag | SelectionFlag.SelectBeginningOfLine);
			}
			else if (k == Key.BeginOfDocument)
			{
				await ThisIntf.GoHome();
			}
			else if (k == Key.EndOfLine)
			{
				await MoveSelection(0, preserveSelectionFlag | SelectionFlag.SelectEndOfLine);
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

		async Task<int?> LoadMessageAt(IMessage msg, BookmarkLookupMode mode, CancellationToken cancellation)
		{
			return await LoadMessageAt(bookmarksFactory.CreateBookmark(msg, 0), mode, cancellation);
		}

		void SelectFullLine(int displayIndex)
		{
			selectionManager.SetSelection(displayIndex, SelectionFlag.SelectEndOfLine | SelectionFlag.NoHScrollToSelection);
			selectionManager.SetSelection(displayIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.PreserveSelectionEnd);
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
			Func<ScreenBufferEntry, int> cmp = e =>
				MessagesComparer.Compare(bookmarksFactory.CreateBookmark(e.Message, e.TextLineIndex), slaveModeFocusedMessage);
			int lowerBound = ListUtils.BinarySearch(screenBuffer.Messages, beginIdx, endIdx, e => cmp(e) < 0);
			int upperBound = ListUtils.BinarySearch(screenBuffer.Messages, lowerBound, endIdx, e => cmp(e) <= 0);
			return new Tuple<int, int>(lowerBound, upperBound);
		}

		StringUtils.MultilineText GetTextToDisplay(IMessage msg)
		{
			return msg.GetDisplayText(showRawMessages);
		}

		async Task<int> ShiftViewBy(float nrOfDisplayLines, CancellationToken cancellation)
		{
			var shiftedBy = await screenBuffer.ShiftBy(nrOfDisplayLines, cancellation);

			InternalUpdate();

			return shiftedBy;
		}

		async Task<int?> LoadMessageAt(
			IBookmark bookmark,
			BookmarkLookupMode matchingMode,
			CancellationToken cancellation)
		{
			Func<int?> findDisplayLine = () =>
			{
				int fullyVisibleViewLines = (int)Math.Ceiling(view.DisplayLinesPerPage);
				int topScrolledLines = (int)screenBuffer.TopLineScrollValue;
				return screenBuffer
					.Messages
					.Where(x => x.Message.GetConnectionId() == bookmark.LogSourceConnectionId && x.Message.Position == bookmark.Position && x.TextLineIndex == bookmark.LineIndex)
					.Where(x => (x.Index - topScrolledLines) < fullyVisibleViewLines && x.Index >= topScrolledLines)
					.Select(x => new int?(x.Index))
					.FirstOrDefault();
			};

			var idx = findDisplayLine();
			if (idx != null)
				return idx;

			if (!await screenBuffer.MoveToBookmark(bookmark, matchingMode | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, cancellation))
				return null;

			InternalUpdate();

			idx = findDisplayLine();
			return idx;
		}

		async Task<bool> ScrollSelectionIntoScreenBuffer(CancellationToken cancellation)
		{
			if (Selection.Message == null || screenBuffer.Messages.Count == 0)
				return false;
			if (Selection.First.DisplayIndex >= 0 && Selection.First.DisplayIndex < screenBuffer.Messages.Count)
				return true;
			var idx = await LoadMessageAt(Selection.Message, BookmarkLookupMode.ExactMatch, cancellation);
			if (idx == null)
				return false;
			return true;
		}

		async Task MoveSelectionCore(int selectionDelta, SelectionFlag selFlags, CancellationToken cancellation)
		{
			var dlpp = this.DisplayLinesPerPage;
			int shiftBy = 0;
			int shiftedBy = 0;
			var newDisplayPosition = Selection.First.DisplayIndex + selectionDelta;
			if (newDisplayPosition < 0)
				shiftBy = newDisplayPosition;
			else if (newDisplayPosition >= dlpp)
				shiftBy = newDisplayPosition - dlpp + 1;
			if (shiftBy != 0)
				shiftedBy = await ShiftViewBy(shiftBy, cancellation);
			cancellation.ThrowIfCancellationRequested();
			newDisplayPosition -= shiftedBy;
			var viewLines = screenBuffer.Messages;
			if (viewLines.Count > 0)
			{
				newDisplayPosition = RangeUtils.PutInRange(0, viewLines.Count - 1, newDisplayPosition);
				selectionManager.SetSelection (newDisplayPosition, selFlags);
			}
		}

		Task MoveSelection(int selectionDelta, SelectionFlag selFlags)
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				if (!await ScrollSelectionIntoScreenBuffer(cancellation))
					return;
				cancellation.ThrowIfCancellationRequested();
				await MoveSelectionCore(selectionDelta, selFlags, cancellation);
			});
		}

		void PerformDefaultFocusedMessageAction()
		{
			if (DefaultFocusedMessageAction != null)
				DefaultFocusedMessageAction(this, EventArgs.Empty);
		}

		void OnRefresh()
		{
			if (ManualRefresh != null)
				ManualRefresh(this, EventArgs.Empty);
		}

		void InternalUpdate()
		{
			IFiltersList hlFilters = model.HighlightFilters;
			hlFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();

			using (var threadsBulkProcessing = model.Threads.StartBulkProcessing())
			{
				IMessage lastMessage = null;
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
							m.Message, hlPreproc, showRawMessages);
						isHighlighted = hlFilterAction == FilterAction.Include;
						m.Message.SetHighlighted(isHighlighted);
					}

					lastMessage = m.Message;
				}
			}

			DisplayHintIfMessagesIsEmpty();

			if (!selectionManager.PickNewSelection())
			{
				selectionManager.UpdateSelectionDisplayIndexes();
			}

			view.Invalidate();

			view.SetVScroll(screenBuffer.Messages.Count > 0 ? screenBuffer.BufferPosition : new double?());
		}

		bool DisplayHintIfMessagesIsEmpty()
		{
			if (screenBuffer.Messages.Count == 0)
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

		async Task FindMessageInCurrentThread(EnumMessagesFlag directionFlag)
		{
			var message = Selection.First.Message;
			var messageSource = Selection.First.Source;
			if (message == null || messageSource == null)
				return;
			await navigationManager.NavigateView (async cancellation =>
			{
				var msg = await ScanMessages (
					messageSource,
					Selection.First.Message.Position,
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
			var idx = await LoadMessageAt(foundMessage, BookmarkLookupMode.ExactMatch, cancellation);
			if (idx != null)
				selectionManager.SetSelection(idx.Value, SelectionFlag.SelectBeginningOfLine);
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

		void HandleSourcesListChange()
		{
			navigationManager.NavigateView(async cancellation =>
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
			return navigationManager.NavigateView(async cancellation => 
			{
				if (!await ScrollSelectionIntoScreenBuffer(cancellation))
					return;
				cancellation.ThrowIfCancellationRequested();
				CursorPosition cur = Selection.First;
				if (cur.Message == null)
					return;
				if (jumpOverWords)
				{
					var wordFlag = left ? SelectionFlag.SelectBeginningOfPrevWord : SelectionFlag.SelectBeginningOfNextWord;
					selectionManager.SetSelection(cur.DisplayIndex, preserveSelectionFlag | wordFlag, cur.LineCharIndex);
				}
				else
				{
					selectionManager.SetSelection(cur.DisplayIndex, preserveSelectionFlag, cur.LineCharIndex + (left ? -1 : +1));
				}
				if (Selection.First.LineCharIndex == cur.LineCharIndex)
				{
					await MoveSelectionCore(
						left ? -1 : +1,
						preserveSelectionFlag | (left ? SelectionFlag.SelectEndOfLine : SelectionFlag.SelectBeginningOfLine),
						cancellation
					);
				}
			});
		}

		delegate Tuple<int, int> ScanMatcher(IMessage message, int messagesProcessed, int? startFromChar);

		async Task<IMessage> Scan(
			bool reverse, bool searchOnlyWithinFocusedMessage, bool highlightResult,
			ILogSource scanOnlyThisLogSource,
			Func<IMessagesSource, ScanMatcher> makeMatcher
		)
		{
			bool isReverseSearch = reverse;
			IMessage scanResult = null;

			CursorPosition startFrom = new CursorPosition();
			var normSelection = Selection.Normalize();
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
				var txt = startFrom.Message.GetDisplayText(showRawMessages);
				var startLine = txt.GetNthTextLine(startFrom.TextLineIndex);
				startFromTextPosition = (startLine.StartIndex - txt.Text.StartIndex) + startFrom.LineCharIndex;
			}

			await navigationManager.NavigateView(async cancellation =>
			{
				var searchSources = screenBuffer.Sources.ToArray();
				if (scanOnlyThisLogSource != null)
					searchSources = searchSources.Where(ss => ss.Source.LogSourceHint == scanOnlyThisLogSource || ss.Source.LogSourceHint == null).ToArray();

				IScreenBuffer tmpBuf = screenBufferFactory.CreateScreenBuffer(initialBufferPosition: InitialBufferPosition.Nowhere);
				await tmpBuf.SetSources(searchSources.Select(s => s.Source), cancellation);
				if (startFrom.Message != null)
				{
					if (!await tmpBuf.MoveToBookmark(bookmarksFactory.CreateBookmark(startFrom.Message, startFrom.TextLineIndex),
						BookmarkLookupMode.ExactMatch | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, cancellation))
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

				var tasks = searchSources.Select(async sourceBuf => 
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

					var displayIndex = await LoadMessageAt(matchedMessage, BookmarkLookupMode.ExactMatch, cancellation);
					if (displayIndex != null)
					{
						scanResult = matchedMessage;
						if (highlightResult)
							selectionManager.SetSelection(displayIndex.Value, matchedTextRange.Item1, matchedTextRange.Item2);
						else
							selectionManager.SetSelection(displayIndex.Value, SelectionFlag.SelectBeginningOfLine);
					}
				}
			});

			if (scanResult != null)
			{
				view.HScrollToSelectedText(Selection);
			}

			return scanResult;
		}

		async Task NextGoToHighlightedMessage(bool reverse)
		{
			if (Selection.Message == null || model.HighlightFilters == null)
				return;
			var hlFilters = model.HighlightFilters;
			await Scan(
				reverse: reverse,
				searchOnlyWithinFocusedMessage: false,
				highlightResult: false,
				scanOnlyThisLogSource: null,
				makeMatcher: source =>
				{
					return (m, messagesProcessed, startFromTextPos) =>
					{
						if (messagesProcessed == 1)
							return null;
						var action = hlFilters.ProcessNextMessageAndGetItsAction(m, showRawMessages);
						if (action == FilterAction.Include)
							return Tuple.Create(0, GetTextToDisplay(m).Text.Length);
						return null;
					};
				}
			);
		}

		private IPresenter ThisIntf { get { return this; } }
		private int DisplayLinesPerPage { get { return screenBuffer.FullyVisibleLinesCount; }}
		private SelectionInfo Selection { get { return selectionManager.Selection; }}

		readonly IModel model;
		readonly ISearchResultModel searchResultModel;
		readonly IView view;
		readonly IPresentersFacade presentationFacade;
		readonly LJTraceSource tracer;
		readonly IBookmarksFactory bookmarksFactory;
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly IScreenBufferFactory screenBufferFactory;
		readonly LazyUpdateFlag pendingUpdateFlag = new LazyUpdateFlag();
		readonly IScreenBuffer screenBuffer;
		readonly INavigationManager navigationManager;
		readonly ISelectionManager selectionManager;

		IBookmark slaveModeFocusedMessage;

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

		bool drawingErrorReported;
	};
};
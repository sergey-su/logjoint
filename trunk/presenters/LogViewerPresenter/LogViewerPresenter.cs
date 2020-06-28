using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.LogViewer
{
	public class Presenter : IPresenterInternal, IViewModel, IPresentationProperties, IDisposable
	{
		public Presenter(
			IModel model,
			IView view,
			IHeartBeatTimer heartbeat,
			IPresentersFacade navHandler,
			IClipboardAccess clipboard,
			Settings.IGlobalSettingsAccessor settings,
			IFiltersList highlightFilters,
			IBookmarks bookmarks,
			IBookmarksFactory bookmarksFactory,
			Telemetry.ITelemetryCollector telemetry,
			IScreenBufferFactory screenBufferFactory,
			IChangeNotification changeNotification,
			IColorTheme theme,
			RegularExpressions.IRegexFactory regexFactory,
			ITraceSourceFactory traceSourceFactory,
			IViewModeStrategy viewModeStrategy,
			IColoringModeStrategy coloringModeStrategy
		)
		{
			this.model = model;
			this.changeNotification = changeNotification;
			this.searchResultModel = model as ISearchResultModel;
			this.view = view;
			this.presentationFacade = navHandler;
			this.bookmarks = bookmarks;
			this.bookmarksFactory = bookmarksFactory;
			this.telemetry = telemetry;
			this.screenBufferFactory = screenBufferFactory;
			this.highlightFilters = highlightFilters;
			this.settings = settings;
			this.theme = theme;
			this.viewModeStrategy = viewModeStrategy;
			this.coloringModeStrategy = coloringModeStrategy;

			this.tracer = traceSourceFactory.CreateTraceSource("UI", "ui.lv" + (this.searchResultModel != null ? "s" : ""));

			this.screenBuffer = screenBufferFactory.CreateScreenBuffer(view.DisplayLinesPerPage, this.tracer);
			var wordSelection = new WordSelection(regexFactory);
			this.selectionManager = new SelectionManager(
				view, screenBuffer, tracer, this, clipboard, screenBufferFactory, bookmarksFactory, changeNotification, wordSelection, theme);
			this.navigationManager = new NavigationManager(
				tracer, telemetry, changeNotification);
			this.highlightingManager = new HighlightingManager(
				searchResultModel, () => this.screenBuffer.DisplayTextGetter, () => this.screenBuffer.Messages.Count,
				highlightFilters, this.selectionManager, wordSelection, theme, regexFactory
			);
			this.displayTextGetterSelector = MakeDisplayTextGetterSelector();

			timeMaxLength = Selectors.Create(
				() => showTime,
				() => showMilliseconds,
				(showTime, showMilliseconds) => showTime ? (new MessageTimestamp(new DateTime(2011, 11, 11, 11, 11, 11, 111))).ToUserFrendlyString(showMilliseconds).Length : 0
			);

			focusedMessageMark = CreateFocusedMessageMarkSelector();
			viewLines = CreateViewLinesSelector();
			viewLinesText = Selectors.Create(viewLines, lines => lines.Aggregate(
				new StringBuilder(), (sb, vl) => sb.AppendLine(vl.TextLineValue)).ToString());

			visibleLines = Selectors.Create(viewLines, lines => ImmutableArray.CreateRange(lines.Select(line => new VisibleLine { Value = line.TextLineValue })));

			this.focusedMessageBookmark = Selectors.Create(
				() => selectionManager.Selection,
				displayTextGetterSelector,
				() => viewModeStrategy.IsRawMessagesMode,
				(sel, displayTextGetter, rawMode) =>
				{
					var f = sel?.First;
					if (f == null)
						return null;
					return bookmarksFactory.CreateBookmark(
							f.Message,
							displayTextGetter(f.Message).LinesMapper(f.TextLineIndex),
							useRawText: rawMode);
				}
			);

			ReadGlobalSettings();

			AttachToView(view);

			this.model.OnSourcesChanged += (sender, e) =>
			{
				HandleSourcesListChange();
			};
			this.model.OnLogSourceColorChanged += (s, e) =>
			{
				++logSourceColorsRevision;
				changeNotification.Post();
			};

			this.model.OnSourceMessagesChanged += (sender, e) =>
			{
				if (e.IsIncrementalChange)
					pendingIncrementalUpdateFlag.Invalidate();
				else
					pendingFullUpdateFlag.Invalidate();
			};

			heartbeat.OnTimer += (sender, e) =>
			{
				if (e.IsNormalUpdate)
				{
					bool isIncrementalUpdate = pendingIncrementalUpdateFlag.Validate();
					bool isFullUpdate = pendingFullUpdateFlag.Validate();
					if (isFullUpdate || isIncrementalUpdate)
					{
						if (viewTailMode || isFullUpdate)
							ThisIntf.GoToEnd().IgnoreCancellation();
						else
							Refresh().IgnoreCancellation();
					}
				}
			};

			settings.Changed += HandleSettingsChange;

			var viewSizeObserver = Updaters.Create(
				() => view.DisplayLinesPerPage,
				dlpp => navigationManager.NavigateView(cancel => screenBuffer.SetViewSize(dlpp, cancel)).IgnoreCancellation()
			);

			var linesObserver = Updaters.Create(() => screenBuffer.Messages, messages =>
			{
				foreach (var m in messages)
					m.Message.Thread?.RegisterKnownMessage(m.Message);
			});

			var displayTextGetterObserver = Updaters.Create(
				displayTextGetterSelector,
				value =>
					navigationManager
					.NavigateView(cancel => screenBuffer.SetDisplayTextGetter(msg => value(msg).DisplayText, cancel))
					.IgnoreCancellation()
			);

			var fireFocusedMessageChanged = Updaters.Create(
				() => selectionManager.Selection?.First?.Message,
				(_) => FocusedMessageChanged?.Invoke(this, EventArgs.Empty)
			);

			var fireFocusedMessageBookmarkChanged = Updaters.Create(
				focusedMessageBookmark,
				(_) => FocusedMessageBookmarkChanged?.Invoke(this, EventArgs.Empty)
			);

			subscription = changeNotification.CreateSubscription(() =>
			{
				viewSizeObserver();
				linesObserver();
				displayTextGetterObserver();
				fireFocusedMessageChanged();
				fireFocusedMessageBookmarkChanged();
			});
		}

		void IDisposable.Dispose()
		{
			settings.Changed -= HandleSettingsChange;
			selectionManager.Dispose();
			viewModeStrategy.Dispose();
			subscription.Dispose();
		}

		#region Interfaces implementation

		public event EventHandler DefaultFocusedMessageAction;
		public event EventHandler ManualRefresh;
		public event EventHandler<ContextMenuEventArgs> ContextMenuOpening;
		public event EventHandler FocusedMessageChanged;
		public event EventHandler FocusedMessageBookmarkChanged;

		LogFontSize IPresenterInternal.FontSize
		{
			get { return font.Size; }
			set { SetFontSize(value); }
		}

		string IPresenterInternal.FontName
		{
			get { return font.Name; }
			set { SetFontName(value); }
		}

		IMessage IPresenter.FocusedMessage
		{
			get { return selectionManager.Selection?.First?.Message; }
		}

		IBookmark IPresenterInternal.FocusedMessageBookmark => focusedMessageBookmark();

		bool IPresenterInternal.NavigationIsInProgress
		{
			get { return navigationManager.NavigationIsInProgress; }
		}

		PreferredDblClickAction IPresenterInternal.DblClickAction { get; set; }

		FocusedMessageDisplayModes IPresenterInternal.FocusedMessageDisplayMode
		{
			get { return focusedMessageDisplayMode; }
			set { focusedMessageDisplayMode = value; changeNotification.Post(); }
		}

		string IPresenterInternal.DefaultFocusedMessageActionCaption { get { return defaultFocusedMessageActionCaption; } set { defaultFocusedMessageActionCaption = value; } }

		bool IPresenterInternal.ShowTime
		{
			get { return showTime; }
			set
			{
				if (showTime == value)
					return;
				showTime = value;
				changeNotification.Post();
			}
		}

		bool IPresenterInternal.ShowMilliseconds
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
				changeNotification.Post();
			}
		}

		bool IPresenterInternal.ShowRawMessages
		{
			get => viewModeStrategy.IsRawMessagesMode;
			set => viewModeStrategy.IsRawMessagesMode = value;
		}

		bool IPresenterInternal.RawViewAllowed => viewModeStrategy.IsRawMessagesModeAllowed;

		bool IPresenterInternal.ViewTailMode
		{
			get { return viewTailMode; }
			set { SetViewTailMode(value, externalCall: true); }
		}

		UserInteraction IPresenterInternal.DisabledUserInteractions
		{
			get { return disabledUserInteractions; }
			set { disabledUserInteractions = value; }
		}

		ColoringMode IPresenterInternal.Coloring
		{
			get => coloringModeStrategy.Coloring;
			set => coloringModeStrategy.Coloring = value;
		}

		async Task<Dictionary<IMessagesSource, long>> IPresenterInternal.GetCurrentPositions(CancellationToken cancellation)
		{
			if (selectionManager.Selection == null)
				return null;
			var tmp = screenBufferFactory.CreateScreenBuffer(1);
			await tmp.SetSources(screenBuffer.Sources.Select(s => s.Source), cancellation);
			await tmp.MoveToBookmark(ThisIntf.FocusedMessageBookmark, BookmarkLookupMode.ExactMatch, cancellation);
			return tmp.Sources.ToDictionary(s => s.Source, s => s.Begin);
		}

		async Task<IMessage> IPresenterInternal.Search(SearchOptions opts)
		{
			using (var bulkProcessing = opts.Filters.StartBulkProcessing(screenBuffer.DisplayTextGetter, opts.ReverseSearch))
			{
				var positiveFilters = opts.Filters.GetPositiveFilters();
				bool hasEmptyTemplate = positiveFilters.Any(f => f == null ||
					string.IsNullOrEmpty(f.Options.Template));
				return await Scan(
					opts.ReverseSearch,
					opts.SearchOnlyWithinFocusedMessage,
					opts.HighlightResult,
					positiveFilters,
					(source) =>
					{
						return (m, messagesProcessed, startFromTextPos) =>
						{
							if (hasEmptyTemplate)
								startFromTextPos = null;
							var rslt = bulkProcessing.ProcessMessage(m, startFromTextPos);
							if (rslt.Action == FilterAction.Exclude)
								return null;
							if (hasEmptyTemplate && messagesProcessed == 1)
								return null;
							if (rslt.MatchedRange != null)
								return Tuple.Create(rslt.MatchedRange.Value.MatchBegin, rslt.MatchedRange.Value.MatchEnd);
							return Tuple.Create(0, 0); // todo: what to return here?
						};
					}
				);
			}
		}

		Task IPresenterInternal.SelectMessageAt(DateTime date, ILogSource[] preferredSources)
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				bool handled = false;
				if (preferredSources != null && preferredSources.Length != 0)
				{
					var candidates = (await Task.WhenAll(preferredSources
						.Where(preferredSource => !preferredSource.IsDisposed).Select(async preferredSource =>
					{
						var lowerDatePos = await preferredSource.Provider.GetDateBoundPosition(
							date, ValueBound.Lower,
							getMessage: true,
							priority: LogProviderCommandPriority.RealtimeUserAction,
							cancellation: cancellation);
						var upperDatePos = await preferredSource.Provider.GetDateBoundPosition(
							date, ValueBound.UpperReversed,
							getMessage: true,
							priority: LogProviderCommandPriority.RealtimeUserAction,
							cancellation: cancellation);
						return new[]
						{
							new { rsp = lowerDatePos, ls = preferredSource},
							new { rsp = upperDatePos, ls = preferredSource},
						};
					})))
						.SelectMany(batch => batch)
						.Where(candidate => candidate.rsp.Message != null)
						.ToList();
					if (candidates.Count > 0)
					{
						var bestCandidate = candidates.OrderBy(
							c => (date - c.rsp.Message.Time.ToLocalDateTime()).Abs()).First();
						var msgIdx = await LoadMessageAt(bookmarksFactory.CreateBookmark(bestCandidate.rsp.Message, 0),
							BookmarkLookupMode.ExactMatch | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen,
							cancellation
						);
						if (msgIdx != null)
							SelectFullLine(msgIdx.Value);
						handled = msgIdx != null;
					}
				}
				if (!handled)
				{
					var screenBufferEntry = await screenBuffer.MoveToTimestamp(date, cancellation);
					SetViewTailMode(false);
					int? idx = screenBufferEntry != null ? FindDisplayLine(bookmarksFactory.CreateBookmark(screenBufferEntry.Value.Message, screenBufferEntry.Value.TextLineIndex)) : null;
					if (idx != null)
						SelectFullLine(idx.Value);
					else
						ThisIntf.SelectFirstMessage();
				}
			});
		}

		Task IPresenterInternal.GoToNextMessageInThread()
		{
			return FindMessageInCurrentThread(EnumMessagesFlag.Forward);
		}

		Task IPresenterInternal.GoToPrevMessageInThread()
		{
			return FindMessageInCurrentThread(EnumMessagesFlag.Backward);
		}

		Task IPresenterInternal.GoToNextHighlightedMessage()
		{
			return GoToNextHighlightedMessage(reverse: false);
		}

		Task IPresenterInternal.GoToPrevHighlightedMessage()
		{
			return GoToNextHighlightedMessage(reverse: true);
		}

		async Task<bool> IPresenterInternal.SelectMessageAt(IBookmark bmk)
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
				SetViewTailMode(false);
				await screenBuffer.MoveToStreamsBegin(cancellation);
				ThisIntf.SelectFirstMessage();
			});
		}

		Task IPresenter.GoToEnd()
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				SetViewTailMode(true);
				await screenBuffer.MoveToStreamsEnd(cancellation);
				ThisIntf.SelectLastMessage();
			});
		}

		async Task IPresenterInternal.GoToNextMessage()
		{
			if (selectionManager.Selection != null)
				await MoveSelection(
					screenBuffer.DisplayTextGetter(selectionManager.Selection.First.Message).GetLinesCount() - selectionManager.Selection.First.TextLineIndex,
					SelectionFlag.None
				);
		}

		async Task IPresenterInternal.GoToPrevMessage()
		{
			if (selectionManager.Selection != null)
				await MoveSelection(
					-(selectionManager.Selection.First.TextLineIndex + 1),
					SelectionFlag.None
				);
		}

		Task<string> IPresenterInternal.GetSelectedText()
		{
			return selectionManager.GetSelectedText();
		}

		Task IPresenterInternal.CopySelectionToClipboard()
		{
			return selectionManager.CopySelectionToClipboard();
		}

		bool IPresenterInternal.IsSinglelineNonEmptySelection
		{
			get { return selectionManager.Selection != null && !selectionManager.Selection.IsEmpty && selectionManager.Selection.IsSingleLine; }
		}

		bool IPresenterInternal.HasInputFocus
		{
			get { return view.HasInputFocus; }
		}

		void IPresenterInternal.ReceiveInputFocus()
		{
			view.ReceiveInputFocus();
		}

		IBookmark IPresenterInternal.SlaveModeFocusedMessage
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
				changeNotification.Post();
			}
		}

		Task IPresenterInternal.SelectSlaveModeFocusedMessage()
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				if (slaveModeFocusedMessage == null)
					return;
				await LoadMessageAt(slaveModeFocusedMessage, BookmarkLookupMode.FindNearestMessage, cancellation);
				var screenBufferEntry = screenBuffer.Messages.FirstOrDefault(entry => MessagesComparer.Compare(
					bookmarksFactory.CreateBookmark(entry.Message, slaveModeFocusedMessage.LineIndex), slaveModeFocusedMessage) == 0);
				if (screenBufferEntry.Message != null)
				{
					await LoadMessageAt(bookmarksFactory.CreateBookmark(
						screenBufferEntry.Message,
						displayTextGetterSelector()(screenBufferEntry.Message).ReverseLinesMapper(slaveModeFocusedMessage.LineIndex)
					), BookmarkLookupMode.FindNearestMessage, cancellation);
				}
				var position = FindSlaveModeFocusedMessagePosition(
					slaveModeFocusedMessage, screenBuffer.Messages,
					bookmarksFactory, m => displayTextGetterSelector()(m).LinesMapper);
				if (position == null)
					return;
				var idxToSelect = position[0];
				if (idxToSelect == screenBuffer.Messages.Count)
					--idxToSelect;
				selectionManager.SetSelection(idxToSelect,
					SelectionFlag.SelectBeginningOfLine | SelectionFlag.ScrollToViewEventIfSelectionDidNotChange);
				slaveMessageAnimationThreadCancellation?.Cancel();
				slaveMessageAnimationThreadCancellation = new CancellationTokenSource();
				SlaveMessageAnimation(slaveMessageAnimationThreadCancellation.Token);
			});
		}

		void IPresenterInternal.SelectFirstMessage()
		{
			if (screenBuffer.Messages.Count > 0)
			{
				selectionManager.SetSelection(0, SelectionFlag.SelectBeginningOfLine);
			}
		}

		void IPresenterInternal.SelectLastMessage()
		{
			if (screenBuffer.Messages.Count > 0)
			{
				selectionManager.SetSelection(screenBuffer.Messages.Count - 1, SelectionFlag.SelectBeginningOfLine);
			}
		}

		void IPresenterInternal.MakeFirstLineFullyVisible()
		{
			screenBuffer.MakeFirstLineFullyVisible();
		}


		void IViewModel.OnMouseWheelWithCtrl(int delta)
		{
			if ((disabledUserInteractions & UserInteraction.FontResizing) == 0)
			{
				if (delta > 0 && font.Size != LogFontSize.Maximum)
					SetFontSize(font.Size + 1);
				else if (delta < 0 && font.Size != LogFontSize.Minimum)
					SetFontSize(font.Size - 1);
			}
		}

		void IViewModel.OnKeyPressed(Key k)
		{
			OnKeyPressedAsync(k).IgnoreCancellation();
		}

		MenuData IViewModel.OnMenuOpening()
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

			if (bookmarks != null)
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

		void IViewModel.OnMenuItemClicked(ContextMenuItem menuItem, bool? itemChecked)
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
				bookmarks.ToggleBookmark(ThisIntf.FocusedMessageBookmark);
			else if (menuItem == ContextMenuItem.GotoNextMessageInTheThread)
				ThisIntf.GoToNextMessageInThread();
			else if (menuItem == ContextMenuItem.GotoPrevMessageInTheThread)
				ThisIntf.GoToPrevMessageInThread();
		}

		void IViewModel.OnIncrementalVScroll(float nrOfDisplayLines)
		{
			navigationManager.NavigateView(async cancellation =>
			{
				await ShiftViewBy(nrOfDisplayLines, cancellation);
			}).IgnoreCancellation();
		}

		void IViewModel.OnVScroll(double value, bool isRealtimeScroll)
		{
			//if (!isRealtimeScroll)
			navigationManager.NavigateView(cancellation => screenBuffer.MoveToPosition(value, cancellation)).IgnoreCancellation();
		}

		void IViewModel.OnHScroll()
		{
		}

		void IViewModel.OnMessageMouseEvent(
			ViewLine line,
			int charIndex,
			MessageMouseEventFlag flags,
			object preparedContextMenuPopupData)
		{
			if ((flags & MessageMouseEventFlag.RightMouseButton) != 0)
			{
				var screeBufferEntry = screenBuffer.Messages.ElementAtOrDefault(line.LineIndex);
				if (screeBufferEntry.Message != null && !selectionManager.Selection?.Contains(CursorPosition.FromScreenBufferEntry(screeBufferEntry, charIndex)) == true)
					selectionManager.SetSelection(line.LineIndex, SelectionFlag.None, charIndex);
				view.PopupContextMenu(preparedContextMenuPopupData);
			}
			else
			{
				if ((flags & MessageMouseEventFlag.CapturedMouseMove) == 0 && (flags & MessageMouseEventFlag.OulineBoxesArea) != 0)
				{
					if ((flags & MessageMouseEventFlag.ShiftIsHeld) == 0)
						selectionManager.SetSelection(line.LineIndex, SelectionFlag.SelectBeginningOfLine | SelectionFlag.NoHScrollToSelection);
					selectionManager.SetSelection(line.LineIndex, SelectionFlag.SelectEndOfLine | SelectionFlag.PreserveSelectionEnd | SelectionFlag.NoHScrollToSelection);
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
							defaultSelection = !selectionManager.SelectWordBoundaries(line, charIndex);
						}
					}
					if (defaultSelection)
					{
						selectionManager.SetSelection(line.LineIndex, (flags & MessageMouseEventFlag.ShiftIsHeld) != 0
							? SelectionFlag.PreserveSelectionEnd : SelectionFlag.None, charIndex);
					}
				}
			}
			SetViewTailMode(false);
		}

		void IViewModel.OnDrawingError(Exception e)
		{
			if (!drawingErrorReported)
			{
				drawingErrorReported = true; // report first error only
				telemetry.ReportException(e, "log viewer drawing");
			}
		}


		int IViewModel.TimeMaxLength => timeMaxLength();

		int[] IViewModel.FocusedMessageMark => focusedMessageMark();

		ImmutableArray<ViewLine> IViewModel.ViewLines => viewLines();

		IReadOnlyList<VisibleLine> IPresenter.VisibleLines => visibleLines();

		string IViewModel.ViewLinesAggregaredText => viewLinesText();

		double IViewModel.FirstDisplayMessageScrolledLines => screenBuffer.TopLineScrollValue;

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		FontData IViewModel.Font => font;

		LJTraceSource IViewModel.Trace => tracer;

		ColorThemeMode IViewModel.ColorTheme => theme.Mode;

		double? IViewModel.VerticalScrollerPosition => screenBuffer.Messages.Count > 0 ? screenBuffer.BufferPosition : new double?();

		string IViewModel.EmptyViewMessage =>
			screenBuffer.Messages.Count != 0 ? null :
			searchResultModel != null ? null :
			"No log sources open. To add new log source:\n  - Press Add... button on Log Sources tab\n  - or drag&&drop (possibly zipped) log file from Windows Explorer\n  - or drag&&drop URL from a browser to download (possibly zipped) log file";

		ColoringMode IPresentationProperties.Coloring => coloringModeStrategy.Coloring;
		bool IPresentationProperties.ShowMilliseconds => showMilliseconds;
		bool IPresentationProperties.ShowTime => showTime;

		#endregion



		async Task OnKeyPressedAsync(Key keyFlags)
		{
			var k = keyFlags & Key.KeyCodeMask;
			if (k == Key.Refresh)
			{
				OnRefresh();
				return;
			}

			var preserveSelectionFlag = (keyFlags & Key.ModifySelectionModifier) != 0 
				? SelectionFlag.PreserveSelectionEnd : SelectionFlag.None;
			var alt = (keyFlags & Key.AlternativeModeModifier) != 0;

			if (selectionManager.Selection != null)
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
					view.PopupContextMenu(view.GetContextMenuPopupData(selectionManager.CursorViewLine));
				else if (k == Key.Enter)
					PerformDefaultFocusedMessageAction();
				else if (k == Key.BookmarkShortcut)
					bookmarks.ToggleBookmark(ThisIntf.FocusedMessageBookmark);
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

		async Task<int?> LoadMessageAt(IMessage msg, int lineIdx, BookmarkLookupMode mode, CancellationToken cancellation)
		{
			return await LoadMessageAt(bookmarksFactory.CreateBookmark(msg, lineIdx), mode, cancellation);
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

		static private int[] FindSlaveModeFocusedMessagePosition(
			IBookmark slaveModeFocusedMessage,
			IReadOnlyList<ScreenBufferEntry> screenBufferMessages,
			IBookmarksFactory bookmarksFactory,
			Func<IMessage, MessageTextLinesMapper> displayTextLinesMapper)
		{
			if (slaveModeFocusedMessage == null)
				return null;
			if (screenBufferMessages.Count == 0)
				return null;
			int cmp(ScreenBufferEntry e) => MessagesComparer.Compare(
				bookmarksFactory.CreateBookmark(e.Message, displayTextLinesMapper(e.Message)(e.TextLineIndex)),
				slaveModeFocusedMessage
			);
			int lowerBound = ListUtils.BinarySearch(screenBufferMessages, 0, screenBufferMessages.Count, e => cmp(e) < 0);
			int upperBound = ListUtils.BinarySearch(screenBufferMessages, lowerBound, screenBufferMessages.Count, e => cmp(e) <= 0);
			return new[] { lowerBound, upperBound };
		}

		async Task<int> ShiftViewBy(float nrOfDisplayLines, CancellationToken cancellation)
		{
			var shiftedBy = await screenBuffer.ShiftBy(nrOfDisplayLines, cancellation);

			return (int)shiftedBy;
		}

		int? FindDisplayLine(IBookmark bookmark)
		{
			int fullyVisibleViewLines = (int)Math.Ceiling(view.DisplayLinesPerPage);
			int topScrolledLines = (int)screenBuffer.TopLineScrollValue;
			return screenBuffer
				.Messages
				.Where(x => x.Message.GetConnectionId() == bookmark.LogSourceConnectionId && x.Message.Position == bookmark.Position && x.TextLineIndex == bookmark.LineIndex)
				.Where(x => (x.Index - topScrolledLines) < fullyVisibleViewLines && x.Index >= topScrolledLines)
				.Select(x => new int?(x.Index))
				.FirstOrDefault();
		}

		async Task<int?> LoadMessageAt(
			IBookmark bookmark,
			BookmarkLookupMode matchingMode,
			CancellationToken cancellation)
		{
			var idx = FindDisplayLine(bookmark);
			if (idx != null)
				return idx;

			if (!await screenBuffer.MoveToBookmark(bookmark, matchingMode | BookmarkLookupMode.MoveBookmarkToMiddleOfScreen, cancellation))
				return null;

			SetViewTailMode(false);

			idx = FindDisplayLine(bookmark);
			return idx;
		}

		async Task<int?> ScrollSelectionIntoScreenBuffer(CancellationToken cancellation)
		{
			if (selectionManager.Selection == null || screenBuffer.Messages.Count == 0)
				return null;
			if (selectionManager.CursorViewLine != null)
				return selectionManager.CursorViewLine;
			return await LoadMessageAt(selectionManager.Selection.First.Message, selectionManager.Selection.First.TextLineIndex, BookmarkLookupMode.ExactMatch, cancellation);
		}

		async Task MoveSelectionCore(int selectionDelta, SelectionFlag selFlags, CancellationToken cancellation)
		{
			var selDidx = selectionManager.CursorViewLine;
			if (selDidx == null)
				return;
			var dlpp = this.DisplayLinesPerPage;
			int shiftBy = 0;
			int shiftedBy = 0;
			var newDisplayPosition = selDidx.Value + selectionDelta;
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
			SetViewTailMode(false);
		}

		Task MoveSelection(int selectionDelta, SelectionFlag selFlags)
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				if (await ScrollSelectionIntoScreenBuffer(cancellation) == null)
					return;
				cancellation.ThrowIfCancellationRequested();
				await MoveSelectionCore(selectionDelta, selFlags, cancellation);
			});
		}

		Task Refresh()
		{
			return navigationManager.NavigateView(async cancellation =>
			{
				await screenBuffer.Refresh(cancellation);
			});
		}

		void PerformDefaultFocusedMessageAction()
		{
			DefaultFocusedMessageAction?.Invoke(this, EventArgs.Empty);
		}

		void OnRefresh()
		{
			ManualRefresh?.Invoke(this, EventArgs.Empty);
		}

		async Task FindMessageInCurrentThread(EnumMessagesFlag directionFlag)
		{
			var cursorPos = selectionManager.Selection?.First;
			if (cursorPos == null)
				return;
			await navigationManager.NavigateView (async cancellation =>
			{
				var msg = await ScanMessages (
					cursorPos.Source,
					cursorPos.Message.Position,
					directionFlag | EnumMessagesFlag.IsSequentialScanningHint,
					m => m.Position != cursorPos.Message.Position && m.Thread == cursorPos.Message.Thread,
					cancellation
				);
				if (msg != null)
				{
					SetViewTailMode(false);
					await SelectFoundMessageHelper (msg, cancellation);
				}
			});
		}

		private async Task SelectFoundMessageHelper(IMessage foundMessage, CancellationToken cancellation)
		{
			var idx = await LoadMessageAt(foundMessage, 0, BookmarkLookupMode.ExactMatch, cancellation);
			if (idx != null)
				selectionManager.SetSelection(idx.Value, SelectionFlag.SelectBeginningOfLine);
		}

		private void SetFontSize(LogFontSize value)
		{
			if (value != font.Size)
			{
				font = new FontData(font.Name, value);
				changeNotification.Post();
			}
		}

		private void SetFontName(string value)
		{
			if (value != font.Name)
			{
				font = new FontData(value, font.Size);
				changeNotification.Post();
			}
		}

		void AttachToView(IView view)
		{
			view.SetViewModel(this);
		}

		private void ReadGlobalSettings()
		{
			this.coloringModeStrategy.Coloring = settings.Appearance.Coloring;
			this.font = new FontData(settings.Appearance.FontFamily, settings.Appearance.FontSize);
			changeNotification.Post();
		}

		void HandleSourcesListChange()
		{
			navigationManager.NavigateView(async cancellation =>
			{
				var wasEmpty = screenBuffer.Messages.Count == 0;

				// here is the only place where sources are read from the model.
				// by design presenter won't see changes in sources list until this method is run.
				await screenBuffer.SetSources(model.Sources, cancellation);

				if (viewTailMode)
					await screenBuffer.MoveToStreamsEnd(cancellation);
				else if (wasEmpty && screenBuffer.Sources.Any())
					await screenBuffer.MoveToStreamsEnd(cancellation);

				if (viewTailMode)
					ThisIntf.SelectLastMessage();
			}).IgnoreCancellation();
		}

		Task HandleLeftRightArrow(bool left, bool jumpOverWords, SelectionFlag preserveSelectionFlag)
		{
			return navigationManager.NavigateView(async cancellation => 
			{
				var didx = await ScrollSelectionIntoScreenBuffer(cancellation);
				if (didx == null)
					return;
				cancellation.ThrowIfCancellationRequested();
				if (selectionManager.Selection == null)
					return;
				CursorPosition cur = selectionManager.Selection.First;
				if (jumpOverWords)
				{
					var wordFlag = left ? SelectionFlag.SelectBeginningOfPrevWord : SelectionFlag.SelectBeginningOfNextWord;
					selectionManager.SetSelection(didx.Value, preserveSelectionFlag | wordFlag, cur.LineCharIndex);
				}
				else
				{
					selectionManager.SetSelection(didx.Value, preserveSelectionFlag, cur.LineCharIndex + (left ? -1 : +1));
				}
				if (selectionManager.Selection?.First?.LineCharIndex == cur.LineCharIndex)
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
			List<IFilter> positiveFilters,
			Func<IMessagesSource, ScanMatcher> makeMatcher
		)
		{
			bool isReverseSearch = reverse;
			IMessage scanResult = null;

			CursorPosition startFrom = null;
			var normSelection = selectionManager.Selection?.Normalize();
			if (!isReverseSearch)
			{
				startFrom = normSelection?.Last ?? normSelection?.First;
			}
			else
			{
				startFrom = normSelection?.First;
			}

			int startFromTextPosition = 0;
			if (startFrom != null)
			{
				var txt = screenBuffer.DisplayTextGetter(startFrom.Message);
				var startLine = txt.GetNthTextLine(startFrom.TextLineIndex);
				startFromTextPosition = (startLine.StartIndex - txt.Text.StartIndex) + startFrom.LineCharIndex;
			}

			await navigationManager.NavigateView(async cancellation =>
			{
				var searchSources = screenBuffer.Sources;
				searchSources = searchSources.Where(
					ss => ss.Source.LogSourceHint == null || positiveFilters == null || positiveFilters.Any(f =>
						f == null || f.Options.Scope.ContainsAnythingFromSource(ss.Source.LogSourceHint))).ToArray();

				IScreenBuffer tmpBuf = screenBufferFactory.CreateScreenBuffer(1);
				await tmpBuf.SetSources(searchSources.Select(s => s.Source), cancellation);
				if (startFrom != null)
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

					var lineIdx = screenBuffer.DisplayTextGetter(matchedMessage).CharIndexToLineIndex(matchedTextRange.Item1);
					if (lineIdx != null)
					{
						var displayIndex = await LoadMessageAt(matchedMessage, lineIdx.Value, BookmarkLookupMode.ExactMatch, cancellation);
						if (displayIndex != null)
						{
							scanResult = matchedMessage;
							if (highlightResult)
								selectionManager.SetSelection(displayIndex.Value, matchedTextRange.Item1, matchedTextRange.Item2);
							else
								selectionManager.SetSelection(displayIndex.Value, SelectionFlag.SelectBeginningOfLine);
						}
					}
				}
			});

			if (scanResult != null)
			{
				if (selectionManager.Selection != null)
					view.HScrollToSelectedText(selectionManager.Selection.First.LineCharIndex);
				SetViewTailMode(false);
			}

			return scanResult;
		}

		async Task GoToNextHighlightedMessage(bool reverse)
		{
			if (selectionManager.Selection == null || highlightFilters == null)
				return;
			using (var hlFiltersBulkProcessing = highlightFilters.StartBulkProcessing(
				screenBuffer.DisplayTextGetter, reverseMatchDirection: false))
			{
				await Scan(
					reverse: reverse,
					searchOnlyWithinFocusedMessage: false,
					highlightResult: false,
					positiveFilters: null,
					makeMatcher: source =>
					{
						return (m, messagesProcessed, startFromTextPos) =>
						{
							if (messagesProcessed == 1)
								return null;
							var rslt = hlFiltersBulkProcessing.ProcessMessage(m, null);
							if (rslt.Action != FilterAction.Exclude)
								return Tuple.Create(0, screenBuffer.DisplayTextGetter(m).Text.Length);
							return null;
						};
					}
				);
			}
		}

		void SetViewTailMode(bool value, bool externalCall = false)
		{
			if (viewTailMode == value)
				return;
			viewTailMode = value;
			changeNotification.Post();
			if (viewTailMode && externalCall)
				ThisIntf.GoToEnd().IgnoreCancellation();
		}

		Func<int[]> CreateFocusedMessageMarkSelector()
		{
			var getSelector = Selectors.Create(
				() => focusedMessageDisplayMode,
				mode =>
				{
					if (mode == FocusedMessageDisplayModes.Master)
					{
						return Selectors.Create(
							() => selectionManager.CursorViewLine,
							idx => idx != null ? new[] { idx.Value } : null
						);
					}
					else
					{
						return Selectors.Create(
							() => slaveModeFocusedMessage,
							() => screenBuffer.Messages,
							() => slaveMessagePositionAnimationStep,
							displayTextGetterSelector,
							(slaveModeFocusedMessage, screenBufferMessages, animationStep, displayTextGetter) =>
							{
								var tmp = FindSlaveModeFocusedMessagePosition(
									slaveModeFocusedMessage, screenBufferMessages, bookmarksFactory,
									m => displayTextGetter(m).LinesMapper);
								if (tmp == null)
									return null;
								return new[] { tmp[0], tmp[1], animationStep };
							}
						);
					}
				}
			);
			return () => getSelector()();
		}

		Func<ImmutableArray<ViewLine>> CreateViewLinesSelector()
		{
			return Selectors.Create(
				() => (screenBuffer.Messages, bookmarks?.Items),
				() => (displayTextGetter: displayTextGetterSelector(), showTime, showMilliseconds, coloring: coloringModeStrategy.Coloring, logSourceColorsRevision, threadColors: theme.ThreadColors),
				() => (highlightingManager.SearchResultHandler, highlightingManager.SelectionHandler, highlightingManager.HighlightingFiltersHandler),
				() => (selectionManager.Selection, selectionManager.ViewLinesRange, selectionManager.CursorViewLine, selectionManager.CursorState),
				(data, displayProps, highlightingProps, selectionProps) =>
				{
					var list = ImmutableArray.CreateBuilder<ViewLine>();
					using (var bookmarksHandler = (bookmarks != null ? bookmarks.CreateHandler() : new DummyBookmarksHandler()))
					{
						var normalizedSelection = selectionProps.Selection?.Normalize();

						foreach (var screenBufferEntry in data.Messages)
						{
							var displayTextInfo = displayProps.displayTextGetter(screenBufferEntry.Message);
							list.Add(screenBufferEntry.ToViewLine(
								displayTextInfo.DisplayText,
								displayProps.showTime,
								displayProps.showMilliseconds,
								selectionViewLinesRange: selectionProps.ViewLinesRange,
								normalizedSelection: normalizedSelection,
								isBookmarked: !screenBufferEntry.Message.Thread.IsDisposed && bookmarksHandler.ProcessNextMessageAndCheckIfItIsBookmarked(
									screenBufferEntry.Message, displayTextInfo.LinesMapper(screenBufferEntry.TextLineIndex)),
								coloring: displayProps.coloring,
								threadColors: displayProps.threadColors,
								cursorCharIndex: selectionProps.CursorViewLine == screenBufferEntry.Index ? selectionProps.Selection?.First?.LineCharIndex : new int?(),
								cursorVisible: selectionProps.CursorState,
								searchResultHighlightingHandler: highlightingProps.SearchResultHandler,
								selectionHighlightingHandler: highlightingProps.SelectionHandler,
								highlightingFiltersHandler: highlightingProps.HighlightingFiltersHandler
							));
						}
					}
					return list.ToImmutable();
				}
			);
		}

		async void SlaveMessageAnimation(CancellationToken cancellation)
		{
			slaveMessagePositionAnimationStep = 0;
			changeNotification.Post();
			for (; ; )
			{
				await Task.Delay(50);
				if (cancellation.IsCancellationRequested)
					break;
				if (slaveMessagePositionAnimationStep < 8)
				{
					slaveMessagePositionAnimationStep++;
					changeNotification.Post();
				}
				else
				{
					slaveMessagePositionAnimationStep = 0;
					changeNotification.Post();
					break;
				}
			}
		}

		Func<Func<IMessage, MessageDisplayTextInfo>> MakeDisplayTextGetterSelector()
		{
			Func<IMessage, MessageDisplayTextInfo> createSearchResultsTextGetter(MessageTextGetter inner, IFiltersList filters)
			{
				var cache = new LRUCache<IMessage, MessageDisplayTextInfo>(128);
				return msg =>
				{
					var key = msg;
					if (cache.TryGetValue(key, out var ret))
						return ret;
					ret = highlightingManager.GetSearchResultMessageText(msg, inner, filters);
					cache.Set(key, ret);
					return ret;
				};
			}

			return Selectors.Create(
				() => viewModeStrategy.IsRawMessagesMode,
				() => searchResultModel?.SearchFiltersList,
				(rawMode, searchResultFilters) =>
				{
					var directGetter = MessageTextGetters.Get(rawMode);
					if (searchResultFilters == null)
						return msg => new MessageDisplayTextInfo()
						{
							DisplayText = directGetter(msg),
							LinesMapper = identityTextLinesMapper,
							ReverseLinesMapper = identityTextLinesMapper
						};
					return createSearchResultsTextGetter(directGetter, searchResultFilters);
				}
			);
		}

		private void HandleSettingsChange(object sender, Settings.SettingsChangeEvent e)
		{
			if ((e.ChangedPieces & Settings.SettingsPiece.Appearance) != 0)
			{
				ReadGlobalSettings();
			}
		}

		private IPresenterInternal ThisIntf { get { return this; } }
		private int DisplayLinesPerPage { get { return (int)screenBuffer.ViewSize; }}

		readonly IModel model;
		readonly IChangeNotification changeNotification;
		readonly ISubscription subscription;
		readonly ISearchResultModel searchResultModel;
		readonly IView view;
		readonly IPresentersFacade presentationFacade;
		readonly LJTraceSource tracer;
		readonly IBookmarks bookmarks;
		readonly IBookmarksFactory bookmarksFactory;
		readonly Settings.IGlobalSettingsAccessor settings;
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly IScreenBufferFactory screenBufferFactory;
		readonly LazyUpdateFlag pendingIncrementalUpdateFlag = new LazyUpdateFlag();
		readonly LazyUpdateFlag pendingFullUpdateFlag = new LazyUpdateFlag();
		readonly IScreenBuffer screenBuffer;
		readonly INavigationManager navigationManager;
		readonly ISelectionManager selectionManager;
		readonly IFiltersList highlightFilters;
		readonly IHighlightingManager highlightingManager;
		readonly IColorTheme theme;
		readonly IViewModeStrategy viewModeStrategy;
		readonly IColoringModeStrategy coloringModeStrategy;

		IBookmark slaveModeFocusedMessage;
		string defaultFocusedMessageActionCaption;
		FontData font = new FontData();
		bool showTime;
		bool showMilliseconds = true;
		UserInteraction disabledUserInteractions = UserInteraction.None;
		FocusedMessageDisplayModes focusedMessageDisplayMode;
		int slaveMessagePositionAnimationStep;
		CancellationTokenSource slaveMessageAnimationThreadCancellation;
		bool drawingErrorReported;
		bool viewTailMode;
		int logSourceColorsRevision;

		readonly Func<int> timeMaxLength;
		readonly Func<int[]> focusedMessageMark;
		readonly Func<ImmutableArray<ViewLine>> viewLines;
		readonly Func<string> viewLinesText;
		readonly Func<Func<IMessage, MessageDisplayTextInfo>> displayTextGetterSelector;
		static readonly MessageTextLinesMapper identityTextLinesMapper = i => i;
		readonly Func<IBookmark> focusedMessageBookmark;
		readonly Func<ImmutableArray<VisibleLine>> visibleLines;
	};
};

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.Postprocessing.Timeline;
using LogJoint.Postprocessing;
using System.Collections.Immutable;
using LogJoint.Drawing;

namespace LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer
{
	public class TimelineVisualizerPresenter : IPresenter, IViewModel
	{
		public TimelineVisualizerPresenter(
			ITimelineVisualizerModel model,
			ILogSourcesManager sources,
			IView view,
			StateInspectorVisualizer.IPresenterInternal stateInspectorVisualizer,
			Common.IPresentationObjectsFactory presentationObjectsFactory,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IBookmarks bookmarks,
			Persistence.IStorageManager storageManager,
			IPresentersFacade presentersFacade,
			IUserNamesProvider userNamesProvider,
			IChangeNotification parentChangeNotification,
			IColorTheme theme,
			ToolsContainer.IPresenter toolsContainerPresenter
		)
		{
			this.model = model;
			this.view = view;
			this.changeNotification = parentChangeNotification.CreateChainedChangeNotification(initiallyActive: false);
			this.quickSearchTextBoxPresenter = presentationObjectsFactory.CreateQuickSearch(view.QuickSearchTextBox, changeNotification);
			this.stateInspectorVisualizer = stateInspectorVisualizer;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.presentersFacade = presentersFacade;
			this.bookmarks = bookmarks;
			this.userNamesProvider = userNamesProvider;
			this.theme = theme;
			this.toolsContainerPresenter = toolsContainerPresenter;
			this.unfinishedActivitiesFolded = true;

			var getAvailableTags = Selectors.Create(() => model.Activities, activities => ImmutableHashSet.CreateRange(activities.SelectMany(a => a.Tags)));
			var sourcesSelector = Selectors.Create(() => model.Outputs, outputs => outputs.Select(output => output.LogSource));
			this.persistentState = new Common.PresenterPersistentState(
				storageManager, "postproc.timeline", "postproc.timeline.view-state.xml", changeNotification, getAvailableTags, sourcesSelector);
			this.tagsListPresenter = presentationObjectsFactory.CreateTagsList(persistentState, view.TagsListView, changeNotification);

			model.EverythingChanged += (sender, args) =>
			{
				PerformFullUpdate();
			};

			model.SequenceDiagramNamesChanged += (sender, args) =>
			{
				++activityDrawAdditionalInputRevision;
				changeNotification.Post();
			};

			sources.OnLogSourceColorChanged += (sender, args) =>
			{
				++activityDrawAdditionalInputRevision;
				changeNotification.Post();
			};

			quickSearchTextBoxPresenter.OnSearchNow += (sender, args) =>
			{
				view.ReceiveInputFocus();
			};

			quickSearchTextBoxPresenter.OnCancelled += (sender, args) =>
			{
				view.ReceiveInputFocus();
			};

			toastNotificationsPresenter = presentationObjectsFactory.CreateToastNotifications(view.ToastNotificationsView, changeNotification);
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateCorrelatorToastNotificationItem());
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateUnprocessedLogsToastNotification(PostprocessorKind.Timeline));

			getFilteringPredicate = Selectors.Create(() => quickSearchTextBoxPresenter.Text, () => persistentState.TagsPredicate,
				MakeFilteringPredicate);
			getVisibleActivitiesInfo = Selectors.Create(() => model.Activities, () => visibleRange,
				getFilteringPredicate, () => unfinishedActivitiesFolded,
				MakeVisibleActivitiesInfo);
			getVisibleActivities = Selectors.Create(getVisibleActivitiesInfo, i => i.activities);
			getSelectedActivityIndex = Selectors.Create(() => selectedActivity, getVisibleActivities, (sa, visibleActivities) =>
			{
				return visibleActivities.IndexOf(va => sa != null && va.Type == sa.Value.Type && va.Activity == sa.Value.Activity);
			});
			getSelectedActivityInfo = Selectors.Create(getSelectedActivityIndex, idx =>
			{
				TryGetVisibleActivity(idx, out var ret);
				return ret;
			});
			isSelectedActivityPresentInStateInspector = Selectors.Create(getSelectedActivityInfo,
				a => IsActivitySelectedInStateInspector(a?.Activity, stateInspectorVisualizer));
			getCurrentActivityDrawInfo = Selectors.Create(getSelectedActivityInfo, isSelectedActivityPresentInStateInspector,
				(a, visInSI) => GetCurrentActivityDrawInfo(a?.Activity, visInSI));
			getActivityDrawInfos = Selectors.Create(() => visibleRange, getVisibleActivitiesInfo, 
				getSelectedActivityIndex, () => unfinishedActivitiesFolded, () => quickSearchTextBoxPresenter.Text,
				() => (theme.ThreadColors, activityDrawAdditionalInputRevision),
				MakeActivityDrawInfos);
			getAvailableRangeRulerMarksDrawInfo = Selectors.Create(() => availableRange, () => view.AvailableRangeRulerMetrics,
				MakeRulerMarkDrawInfos);
			getVisibleRangeRulerMarksDrawInfo = Selectors.Create(() => visibleRange, () => view.VisibleRangeRulerMetrics,
				MakeRulerMarkDrawInfos);
			getNavigationPanelDrawInfo = Selectors.Create(() => availableRange, () => visibleRange, MakeNavigationPanelDrawInfo);
			getAvailableRangeEventsDrawInfo = Selectors.Create(() => availableRange, () => model.Events, MakeEventsDrawInfo);
			getVisibleRangeEventsDrawInfo = Selectors.Create(() => visibleRange, () => model.Events, MakeEventsDrawInfo);
			getAvailableRangeBookmarksDrawInfo = Selectors.Create(() => availableRange, () => bookmarks.Items, MakeBookmarksDrawInfo);
			getVisibleRangeBookmarksDrawInfo = Selectors.Create(() => visibleRange, () => bookmarks.Items, MakeBookmarksDrawInfo);
			var logViewerPresenter = loadedMessagesPresenter.LogViewerPresenter;
			getAvailableRangeFocusedMessageDrawInfo = Selectors.Create(() => availableRange, () => logViewerPresenter.FocusedMessage,
				MakeFocusedMessageDrawInfo);
			getVisibleRangeFocusedMessageDrawInfo = Selectors.Create(() => visibleRange, () => logViewerPresenter.FocusedMessage,
				MakeFocusedMessageDrawInfo);
			getMeasurerDrawInfo = Selectors.Create(() => measurer.Version, () => visibleRange,
				(_, visibleRange) => MakeMeasurerDrawInfo(measurer, visibleRange));

			view.SetViewModel(this);

			PerformFullUpdate();
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		QuickSearchTextBox.IViewModel IViewModel.QuickSearchTextBox => quickSearchTextBoxPresenter.ViewModel;

		ColorThemeMode IViewModel.ColorTheme => theme.Mode;

		void IViewModel.OnWindowShown()
		{
			changeNotification.Active = true;
		}

		void IViewModel.OnWindowHidden()
		{
			changeNotification.Active = false;
		}

		IReadOnlyList<RulerMarkDrawInfo> IViewModel.RulerMarksDrawInfo(DrawScope scope)
		{
			if (scope == DrawScope.AvailableRange)
				return getAvailableRangeRulerMarksDrawInfo();
			if (scope == DrawScope.VisibleRange)
				return getVisibleRangeRulerMarksDrawInfo();
			return ImmutableArray<RulerMarkDrawInfo>.Empty;
		}

		IReadOnlyList<RulerMarkDrawInfo> MakeRulerMarkDrawInfos(SpanRange range, RulerMetrics metrics)
		{
			if (metrics.Width <= 0)
			{
				return ImmutableArray<RulerMarkDrawInfo>.Empty;
			}
			DateTime origin = model.Origin;
			long timelineRangeTicks = range.Length.Ticks;
			if (timelineRangeTicks == 0)
			{
				return ImmutableArray<RulerMarkDrawInfo>.Empty;
			}
			TimeSpan minTimespan = new TimeSpan(NumUtils.MulDiv(timelineRangeTicks, metrics.MinAllowedDistanceBetweenMarks, metrics.Width));
			var intervals = RulerUtils.FindTimeRulerIntervals(minTimespan);
			if (intervals != null)
			{
				return ImmutableArray.CreateRange(RulerUtils.GenerateTimeRulerMarks(intervals.Value,
					new DateRange(origin + range.Begin, origin + range.End)
				).Select(r => new RulerMarkDrawInfo()
				{
					X = (double)(r.Time - origin - range.Begin).Ticks / (double)timelineRangeTicks,
					Label = r.ToString(),
					IsMajor = r.IsMajor
				}));
			}
			else
			{
				return ImmutableArray<RulerMarkDrawInfo>.Empty;
			}
		}


		IReadOnlyList<ActivityDrawInfo> IViewModel.ActivitiesDrawInfo => getActivityDrawInfos();

		IReadOnlyList<ActivityDrawInfo> MakeActivityDrawInfos(
			SpanRange range, VisibileActivitiesInfo visibileActivitiesInfo, int? selectedActivityIdx, bool unfinishedActivitiesFolded,
			string filter, (ImmutableArray<Color> themeColors, int revision) additionalData)
		{
			var result = ImmutableArray.CreateBuilder<ActivityDrawInfo>();
			bool displaySequenceDiagramTexts = model.Outputs.Count >= 2;

			var (visibleActivities, unfinishedActivities) = visibileActivitiesInfo;
			// This code assumes UnfinishedActivities are followed only by entries of type Activity.
			int activitiesStartIdx = visibleActivities.TakeWhile(a => a.Type == VisibileActivityType.UnfinishedActivities).Count();

			foreach (var a in visibleActivities.Select((va, i) =>
			{
				if (va.Type == VisibileActivityType.Activity)
				{
					var a = va.Activity;
					var pairedActivities = model.GetPairedActivities(a);
					int? pairedActivityIndex = null;
					if (pairedActivities != null && pairedActivities.Item2 == a)
					{
						var idx1 = visibleActivities.BinarySearch(activitiesStartIdx, visibleActivities.Count, (predicateArg) =>
						{
							return model.Comparer.Compare(pairedActivities.Item1, predicateArg.Activity) > 0;
						});
						var idx2 = visibleActivities.BinarySearch(activitiesStartIdx, visibleActivities.Count, (predicateArg) =>
						{
							return model.Comparer.Compare(pairedActivities.Item1, predicateArg.Activity) >= 0;
						});
						for (int idx = idx1; idx != idx2; ++idx)
							if (visibleActivities[idx].Activity == pairedActivities.Item1)
								pairedActivityIndex = idx;
					}
					return new ActivityDrawInfo()
					{
						Index = i,
						X1 = GetTimeX(range, a.GetTimelineBegin()),
						X2 = GetTimeX(range, a.GetTimelineEnd()),
						Caption = userNamesProvider.ResolveShortNamesMurkup(a.DisplayName),
						CaptionSelectionBegin = GetActivityMatchIdx(a, filter),
						CaptionSelectionLength = filter.Length,
						IsSelected = i == selectedActivityIdx,
						Type =
							a.Type == ActivityType.Lifespan ? ActivityDrawType.Lifespan :
							a.Type == ActivityType.Procedure ? ActivityDrawType.Procedure :
							a.Type == ActivityType.IncomingNetworking || a.Type == ActivityType.OutgoingNetworking ? ActivityDrawType.Networking :
							ActivityDrawType.Unknown,
						Color = !a.BeginOwner.LogSource.IsDisposed ? additionalData.themeColors.GetByIndex(a.BeginOwner.LogSource.ColorIndex) : new Color?(),
						BeginTrigger = new TriggerData(a, a.BeginOwner, a.BeginTrigger),
						EndTrigger = new TriggerData(a, a.EndOwner, a.EndTrigger),
						MilestonesCount = a.Milestones.Count,
						Milestones = a.Milestones.Select(m => new ActivityMilestoneDrawInfo()
						{
							X = GetTimeX(range, m.GetTimelineTime()),
							Caption = m.DisplayName,
							Trigger = new TriggerData(a, m.Owner, m.Trigger, m.DisplayName),
						}),
						PhasesCount = a.Phases.Count,
						Phases = a.Phases.Where(ph => ph.Begin >= a.Begin && ph.End <= a.End).Select(ph => new ActivityPhaseDrawInfo()
						{
							X1 = GetTimeX(range, ph.GetTimelineBegin()),
							X2 = GetTimeX(range, ph.GetTimelineEnd()),
							Type = ph.Type,
						}),
						PairedActivityIndex = pairedActivityIndex,
						SequenceDiagramText = displaySequenceDiagramTexts ? GetSequenceDiagramText(a, pairedActivities) : null,
						IsError = a.IsError
					};
				}
				else if (va.Type == VisibileActivityType.UnfinishedActivities)
				{
					return new ActivityDrawInfo()
					{
						Index = i,
						X1 = 0,
						X2 = 0,
						Caption = string.Format("started and never finished ({0})", unfinishedActivities.Count),
						IsSelected = i == selectedActivityIdx,
						Type = ActivityDrawType.Group,
						Color = new Color(0xffffffff),
						Milestones = Enumerable.Empty<ActivityMilestoneDrawInfo>(),
						Phases = Enumerable.Empty<ActivityPhaseDrawInfo>(),
						IsFolded = unfinishedActivitiesFolded
					};
				}
				else
				{
					throw new InvalidCastException();
				}
			}))
			{
				result.Add(a);
			}
			return result.ToImmutable();
		}

		bool IViewModel.NotificationsIconVisibile
		{
			get { return toastNotificationsPresenter.HasSuppressedNotifications; }
		}

		static private string GetSequenceDiagramText(IActivity a, Tuple<IActivity, IActivity> activitiesPair)
		{
			IActivity pairedActivity = null;
			if (activitiesPair != null)
				pairedActivity = activitiesPair.Item1 == a ? activitiesPair.Item2 : activitiesPair.Item1;
			var sequenceDiagramText = new StringBuilder();
			if (a.Type == ActivityType.OutgoingNetworking)
			{
				sequenceDiagramText.Append(a.BeginOwner.SequenceDiagramName);
				sequenceDiagramText.Append(" -> ");
				if (pairedActivity != null)
				{
					sequenceDiagramText.Append(pairedActivity.BeginOwner.SequenceDiagramName);
				}
			}
			else if (a.Type == ActivityType.IncomingNetworking)
			{
				if (pairedActivity != null)
				{
					sequenceDiagramText.Append(pairedActivity.BeginOwner.SequenceDiagramName);
				}
				else
				{
					sequenceDiagramText.Append("   ");
				}
				sequenceDiagramText.Append(" -> ");
				sequenceDiagramText.Append(a.BeginOwner.SequenceDiagramName);
			}
			else
			{
				sequenceDiagramText.AppendFormat("{0}:", a.BeginOwner.SequenceDiagramName);
			}
			return sequenceDiagramText.ToString();
		}

		IReadOnlyList<EventDrawInfo> IViewModel.EventsDrawInfo(DrawScope scope)
		{
			if (scope == DrawScope.AvailableRange)
				return getAvailableRangeEventsDrawInfo();
			if (scope == DrawScope.VisibleRange)
				return getVisibleRangeEventsDrawInfo();
			return ImmutableArray<EventDrawInfo>.Empty;
		}

		static IReadOnlyList<EventDrawInfo> MakeEventsDrawInfo(SpanRange range, IReadOnlyList<IEvent> events)
		{
			return ImmutableArray.CreateRange(events.Select(e => new EventDrawInfo
			{
				X = GetTimeX(range, e.GetTimelineTime()),
				Type = e.Type,
				Caption = e.DisplayName,
				Trigger = new TriggerData(e, string.Format("{0}\n{1}\n{2}", e.DisplayName,
					e.Owner.SequenceDiagramName,
					e.Owner.LogSource.GetShortDisplayNameWithAnnotation()))
			}));
		}

		IReadOnlyList<BookmarkDrawInfo> IViewModel.BookmarksDrawInfo(DrawScope scope)
		{
			if (scope == DrawScope.AvailableRange)
				return getAvailableRangeBookmarksDrawInfo();
			if (scope == DrawScope.VisibleRange)
				return getVisibleRangeBookmarksDrawInfo();
			return ImmutableArray<BookmarkDrawInfo>.Empty;
		}

		IReadOnlyList<BookmarkDrawInfo> MakeBookmarksDrawInfo(SpanRange range, IReadOnlyList<IBookmark> bookmarks)
		{
			return ImmutableArray.CreateRange(bookmarks.Select(bmk =>
			{
				var ls = bmk.GetLogSource();
				if (ls == null || ls.IsDisposed)
					return new BookmarkDrawInfo();
				return new BookmarkDrawInfo
				{
					X = GetTimeX(range, bmk.GetTimelineTime(model)),
					Caption = bmk.DisplayName,
					Trigger = new TriggerData(bmk)
				};
			}).Where(i => i.Trigger != null));
		}

		FocusedMessageDrawInfo IViewModel.FocusedMessageDrawInfo(DrawScope scope)
		{
			if (scope == DrawScope.AvailableRange)
				return getAvailableRangeFocusedMessageDrawInfo();
			if (scope == DrawScope.VisibleRange)
				return getVisibleRangeFocusedMessageDrawInfo();
			return null;
		}

		FocusedMessageDrawInfo MakeFocusedMessageDrawInfo(SpanRange range, IMessage msg)
		{
			if (msg == null)
				return null;
			var msgSource = msg.GetLogSource();
			if (msgSource == null)
				return null;
			return new FocusedMessageDrawInfo
			{
				x = GetTimeX(range, msg.Time.ToLocalDateTime() - model.Origin)
			};
		}

		void IViewModel.OnKeyPressed(char keyChar)
		{
			if (!char.IsWhiteSpace(keyChar))
				quickSearchTextBoxPresenter.Focus(new string(keyChar, 1));
		}

		void IViewModel.OnKeyDown(KeyCode code)
		{
			if (code == KeyCode.Down || code == KeyCode.Up)
			{
				var newSelectedActivity = getSelectedActivityIndex() + (code == KeyCode.Down ? 1 : -1);
				if (newSelectedActivity == -1)
					quickSearchTextBoxPresenter.Focus(null);
				else
					TrySetSelectedActivity(newSelectedActivity);
			}
			else if (code == KeyCode.Left || code == KeyCode.Right)
			{
				var delta = visibleRange.Length.Multiply(0.05);
				if (code == KeyCode.Left)
					delta = delta.Negate();
				SetVisibleRange(visibleRange.Add(delta));
			}
			else if ((code & (KeyCode.Plus | KeyCode.Minus)) != 0 && (code & KeyCode.Ctrl) != 0)
			{
				ZoomInternal(0.5, (code & KeyCode.Plus) != 0 ? 1 : -1);
			}
			else if ((code & KeyCode.Enter) != 0)
			{
				PerformDefaultActionForSelectedActivity();
			}
			else if ((code & KeyCode.Find) != 0)
			{
				quickSearchTextBoxPresenter.Focus(null);
			}
			else if ((code & KeyCode.FindCurrentTimeShortcut) != 0)
			{
				FindCurrentTime();
			}
			else if ((code & KeyCode.NextBookmarkShortcut) != 0)
			{
				FindNextBookmark(+1);
			}
			else if ((code & KeyCode.PrevBookmarkShortcut) != 0)
			{
				FindNextBookmark(-1);
			}
			else if ((code & KeyCode.Escape) != 0)
			{
				HandleEscape();
			}
		}

		void IViewModel.OnMouseZoom(double mousePosX, int delta)
		{
			ZoomInternal(mousePosX, delta, null);
		}

		void IViewModel.OnGestureZoom(double mousePosX, double delta)
		{
			ZoomInternal(mousePosX, null, delta);
		}

		NavigationPanelDrawInfo IViewModel.NavigationPanelDrawInfo => getNavigationPanelDrawInfo();

		static NavigationPanelDrawInfo MakeNavigationPanelDrawInfo(SpanRange availableRange, SpanRange visibleRange)
		{
			var availLen = availableRange.Length;
			var drawInfo = new NavigationPanelDrawInfo();
			if (availLen.Ticks > 0)
			{
				drawInfo.VisibleRangeX1 = (double)(visibleRange.Begin - availableRange.Begin).Ticks / (double)availLen.Ticks;
				drawInfo.VisibleRangeX2 = (double)(visibleRange.End - availableRange.Begin).Ticks / (double)availLen.Ticks;
			}
			return drawInfo;
		}

		void IViewModel.OnActivityTriggerClicked(object trigger)
		{
			ShowTrigger(trigger);
		}

		void IViewModel.OnEventTriggerClicked(object trigger)
		{
			ShowTrigger(trigger);
		}

		void IViewModel.OnActiveNotificationButtonClicked()
		{
			toastNotificationsPresenter.UnsuppressNotifications();
		}

		void IViewModel.OnActivitySourceLinkClicked(object trigger)
		{
			ILogSource ls = trigger as ILogSource;
			if (ls != null)
				presentersFacade.ShowLogSource(ls);
		}

		void IViewModel.OnNoContentLinkClicked(bool searchLeft)
		{
			var filteringPredicate = getFilteringPredicate();
			IActivity activityToShow = null;
			if (searchLeft)
			{
				activityToShow = model.Activities
					.TakeWhile(a => a.GetTimelineBegin() < visibleRange.Begin)
					.Where(filteringPredicate)
					.MaxByKey(a => a.GetTimelineEnd());
			}
			else
			{
				var idx = model.Activities.BinarySearch(0, model.Activities.Count,
					a => a.GetTimelineBegin() <= visibleRange.End);
				activityToShow = model.Activities
					.Skip(idx)
					.Where(filteringPredicate)
					.FirstOrDefault();
			}
			if (activityToShow != null)
			{
				var delta =
					  (searchLeft ? activityToShow.GetTimelineEnd() : activityToShow.GetTimelineBegin())
					- visibleRange.Mid;
				SetVisibleRange(visibleRange.Add(delta));
			}
		}

		void IViewModel.OnMouseDown(object hitTestToken, KeyCode keys, bool doubleClick)
		{
			var htResult = view.HitTest(hitTestToken);

			bool selectActivity = false;
			bool startPan = false;
			bool waitPan = false;
			bool startMeasure = false;
			bool startNavigation1 = false;
			bool startNavigation2 = false;
			bool navigate = false;
			bool navigateToViewAll = false;
			if (htResult.Area == HitTestResult.AreaCode.RulersPanel)
			{
				startMeasure = true;
			}
			else if (htResult.Area == HitTestResult.AreaCode.ActivityTrigger
				  || htResult.Area == HitTestResult.AreaCode.EventTrigger
				  || htResult.Area == HitTestResult.AreaCode.BookmarkTrigger)
			{
				if ((keys & KeyCode.Ctrl) != 0)
					startMeasure = true;
				else
					ShowTrigger(htResult.Trigger);
			}
			else if (htResult.Area == HitTestResult.AreaCode.ActivitiesPanel
				  || htResult.Area == HitTestResult.AreaCode.Activity
				  || htResult.Area == HitTestResult.AreaCode.ActivityPhase)
			{
				if ((keys & KeyCode.Ctrl) != 0)
				{
					startMeasure = true;
				}
				else
				{
					if (doubleClick)
						selectActivity = true;
					waitPan = true;
				}
			}
			else if (htResult.Area == HitTestResult.AreaCode.CaptionsPanel)
			{
				selectActivity = true;
			}
			else if (htResult.Area == HitTestResult.AreaCode.NavigationPanelResizer1)
			{
				if (doubleClick)
					navigateToViewAll = true;
				else
					startNavigation1 = true;
			}
			else if (htResult.Area == HitTestResult.AreaCode.NavigationPanelResizer2)
			{
				if (doubleClick)
					navigateToViewAll = true;
				else
					startNavigation2 = true;
			}
			else if (htResult.Area == HitTestResult.AreaCode.NavigationPanelThumb)
			{
				if (doubleClick)
					navigateToViewAll = true;
				else
				{
					startNavigation1 = true;
					startNavigation2 = true;
				}
			}
			else if (htResult.Area == HitTestResult.AreaCode.NavigationPanel)
			{
				if (doubleClick)
					navigateToViewAll = true;
				else
					navigate = true;
			}
			else if (htResult.Area == HitTestResult.AreaCode.FoldingSign)
			{
				if (htResult.ActivityIndex == getVisibleActivitiesInfo().unfinishedActivities.VisibleActivityIndex)
				{
					unfinishedActivitiesFolded = !unfinishedActivitiesFolded;
					changeNotification.Post();
				}
			}

			if (startPan || waitPan)
			{
				mouseCaptureState = waitPan ? MouseCaptureState.WaitingActivitiesPan : MouseCaptureState.ActivitiesPan;
				pan.StartPosition = htResult.RelativeX;
				pan.OriginalBegin = visibleRange.Begin;
				pan.OriginalEnd = visibleRange.End;
			}
			if (startMeasure)
			{
				mouseCaptureState = MouseCaptureState.Measuring;
				measurer.RangeBegin = VisibleRangeRelativePositionToAbsolute(htResult.RelativeX);
				if ((keys & KeyCode.Shift) == 0)
					measurer.RangeBegin = SnapToMilestone(measurer.RangeBegin, null).Key;
				measurer.RangeEnd = measurer.RangeBegin;
				measurer.State = MeasurerState.WaitingFirstMove;
				measurer.ExtraComment = null;
				measurer.Version++;
				changeNotification.Post();
			}
			if (selectActivity)
			{
				SelectActivity(doubleClick, htResult);
			}
			if (startNavigation1 || startNavigation2)
			{
				navigationInfo = new NavigationInfo()
				{
					StartPosition = htResult.RelativeX,
					OriginalBegin = startNavigation1 ? visibleRange.Begin : new TimeSpan?(),
					OriginalEnd = startNavigation2 ? visibleRange.End : new TimeSpan?(),
				};
				mouseCaptureState = MouseCaptureState.Navigation;
			}
			if (navigate)
			{
				var halfLen = visibleRange.Length.Multiply(0.5);
				var pos = availableRange.Length.Multiply(htResult.RelativeX);
				pos = SnapToMilestone(pos, null,
					EnumMilestonesOptions.AvailableRangeScale | EnumMilestonesOptions.IncludeBookmarks | EnumMilestonesOptions.IncludeEvents).Key;
				SetVisibleRange(new SpanRange(pos - halfLen, pos + halfLen), realTimePanMode: true);
			}
			if (navigateToViewAll)
			{
				SetVisibleRange(availableRange);
			}
		}

		void IViewModel.OnMouseMove(object hitTestToken, KeyCode keys)
		{
			if (mouseCaptureState == MouseCaptureState.Measuring)
			{
				var htResult = view.HitTest(hitTestToken);
				if (measurer.State == MeasurerState.WaitingFirstMove)
					measurer.State = MeasurerState.Measuring;
				if (measurer.State != MeasurerState.Measuring)
					return;
				measurer.RangeEnd = VisibleRangeRelativePositionToAbsolute(htResult.RelativeX);
				if ((keys & KeyCode.Shift) == 0)
				{
					var sticky = SnapToMilestone(measurer.RangeEnd, htResult.ActivityIndex);
					measurer.RangeEnd = sticky.Key;
					measurer.ExtraComment = sticky.Value;
				}
				else
				{
					measurer.ExtraComment = null;
				}
				measurer.Version++;
				changeNotification.Post();
			}
			else if (mouseCaptureState == MouseCaptureState.WaitingActivitiesPan)
			{
				var htResult = view.HitTest(hitTestToken);
				if (Math.Abs(pan.StartPosition - htResult.RelativeX) > 0.01)
				{
					mouseCaptureState = MouseCaptureState.ActivitiesPan;
				}
			}
			else if (mouseCaptureState == MouseCaptureState.Navigation)
			{
				var htResult = view.HitTest(hitTestToken);
				var delta = availableRange.Length.Multiply(htResult.RelativeX - navigationInfo.StartPosition);
				SetVisibleRange(
					new SpanRange(
						(navigationInfo.OriginalBegin + delta).GetValueOrDefault(visibleRange.Begin),
						(navigationInfo.OriginalEnd + delta).GetValueOrDefault(visibleRange.End)),
					realTimePanMode: false
				);
			}
			
			if (mouseCaptureState == MouseCaptureState.ActivitiesPan)
			{
				var htResult = view.HitTest(hitTestToken);
				var delta = (pan.OriginalEnd - pan.OriginalBegin).Multiply(pan.StartPosition - htResult.RelativeX);
				SetVisibleRange(new SpanRange(pan.OriginalBegin + delta, pan.OriginalEnd + delta), realTimePanMode: true);
			}
		}

		void IViewModel.OnMouseUp(object hitTestToken)
		{
			if (mouseCaptureState == MouseCaptureState.Measuring)
			{
				mouseCaptureState = MouseCaptureState.NoCapture;
				switch (measurer.State)
				{
					case MeasurerState.WaitingFirstMove:
						measurer.State = MeasurerState.Unset;
						measurer.Version++;
						changeNotification.Post();
						break;
					case MeasurerState.Measuring:
						// below floating point numbers are compared strictly. 
						// it's ok because those numbers are calculated 
						// from discrete screen coordinates. numbers will be 
						// strictly equal if calculated from same discrete input.
						if (measurer.RangeEnd != measurer.RangeBegin)
							measurer.State = MeasurerState.Set;
						else
							measurer.State = MeasurerState.Unset;
						measurer.Version++;
						changeNotification.Post();
						break;
				}
				measurer.ExtraComment = null;
			}
			else if (mouseCaptureState == MouseCaptureState.ActivitiesPan)
			{
				mouseCaptureState = MouseCaptureState.NoCapture;
			}
			else if (mouseCaptureState == MouseCaptureState.Navigation)
			{
				mouseCaptureState = MouseCaptureState.NoCapture;
				view.ReceiveInputFocus();
			}
			if (mouseCaptureState == MouseCaptureState.WaitingActivitiesPan)
			{
				var htResult = view.HitTest(hitTestToken);
				SelectActivity(false, htResult);
				mouseCaptureState = MouseCaptureState.NoCapture;
			}
		}

		void IViewModel.OnScrollWheel(double deltaX)
		{
			var delta = visibleRange.Length.Multiply(deltaX);
			SetVisibleRange(visibleRange.Add(delta), realTimePanMode: true);
		}

		MeasurerDrawInfo IViewModel.MeasurerDrawInfo => getMeasurerDrawInfo();

		static MeasurerDrawInfo MakeMeasurerDrawInfo(MeasurerInfo measurer, SpanRange visibleRange)
		{
			var text = measurer.State == MeasurerState.WaitingFirstMove ? 
				"move to measure time interval" : 
				TimeUtils.TimeDeltaToString((measurer.RangeEnd - measurer.RangeBegin).Abs(), false);
			if (measurer.ExtraComment != null)
				text += "\n" + measurer.ExtraComment;
			var ret = new MeasurerDrawInfo()
			{
				MeasurerVisible = measurer.State != MeasurerState.Unset,
				X1 = VisibleRangeAbsolutePositionToRelative(visibleRange, measurer.RangeBegin),
				X2 = VisibleRangeAbsolutePositionToRelative(visibleRange, measurer.RangeEnd),
				Text = text
			};
			if (ret.X1 > ret.X2)
			{
				var tmp = ret.X1;
				ret.X1 = ret.X2;
				ret.X2 = tmp;
			}
			return ret;
		}

		string IViewModel.OnToolTip(object hitTestToken)
		{
			if (mouseCaptureState != MouseCaptureState.NoCapture)
				return null;
			var htResult = view.HitTest(hitTestToken);
			if (htResult.Area == HitTestResult.AreaCode.ActivityTrigger
			 || htResult.Area == HitTestResult.AreaCode.EventTrigger
			 || htResult.Area == HitTestResult.AreaCode.BookmarkTrigger)
			{
				var trigger = htResult.Trigger as TriggerData;
				if (trigger == null)
					return null;
				if (trigger.ToolTip == null)
					return null;
				return trigger.ToolTip;
			}
			if (htResult.Area == HitTestResult.AreaCode.Activity
			 || htResult.Area == HitTestResult.AreaCode.ActivityPhase)
			{
				if (TryGetVisibleActivity(htResult.ActivityIndex, out var a) && a.Value.Activity?.Phases.Count > 0)
				{
					return string.Join(Environment.NewLine,
						a.Value.Activity.Phases.Select(ph => string.Format("{0} {1}ms", ph.DisplayName, (ph.End - ph.Begin).TotalMilliseconds)));
				}
			}
			return null;
		}

		void IViewModel.OnPrevUserEventButtonClicked()
		{
			FindNextUserAction(-1, 0.0);
		}

		void IViewModel.OnNextUserEventButtonClicked()
		{
			FindNextUserAction(+1, 1.0);
		}

		void IViewModel.OnPrevBookmarkButtonClicked()
		{
			FindNextBookmark(-1);
		}

		void IViewModel.OnNextBookmarkButtonClicked()
		{
			FindNextBookmark(+1);
		}

		void IViewModel.OnFindCurrentTimeButtonClicked()
		{
			FindCurrentTime();
		}

		void IViewModel.OnZoomInButtonClicked()
		{
			ZoomInternal(0.5, 1);
		}

		void IViewModel.OnZoomOutButtonClicked()
		{
			ZoomInternal(0.5, -1);
		}

		bool IViewModel.OnEscapeCmdKey()
		{
			return HandleEscape();
		}

		void IViewModel.OnQuickSearchExitBoxKeyDown(KeyCode code)
		{
			if (code == KeyCode.Down)
			{
				view.ReceiveInputFocus();
			}
		}

		void IPostprocessorVisualizerPresenter.Show()
		{
			if (IsBrowser.Value && toolsContainerPresenter != null)
				toolsContainerPresenter.ShowTool(ToolsContainer.ToolKind.Timeline);
			else
				view.Show();
		}

		void IPresenter.Navigate(TimeSpan t1, TimeSpan t2)
		{
			SetVisibleRange(new SpanRange(t1, t2), realTimePanMode: false);
		}

		bool IViewModel.NoContentMessageVisibile
		{
			get { return getVisibleActivities().Count == 0 && model.Activities.Count > 0; }
		}

		CurrentActivityDrawInfo IViewModel.CurrentActivity
		{
			get { return getCurrentActivityDrawInfo(); }
		}

		bool TrySetSelectedActivity(int? value)
		{
			if (!TryGetVisibleActivity(value, out var _))
				return false;
			SetSelectedActivity(value.Value);
			return true;
		}

		void SetSelectedActivity(int? value, bool ensureVisible = true)
		{
			if (TryGetVisibleActivity(value, out var info))
				selectedActivity = new Ref<VisibileActivityInfo>(info.Value);
			else
				selectedActivity = null;
			if (value.HasValue && ensureVisible)
				view.EnsureActivityVisible(value.Value);
			changeNotification.Post();
		}

		void PerformDefaultActionForSelectedActivity()
		{
			var sa = getSelectedActivityInfo();
			if (sa?.Type == VisibileActivityType.Activity)
			{
				var a = sa.Value.Activity;
				if (a.BeginTrigger != null)
					ShowTrigger(new TriggerData(a, a.BeginOwner, a.BeginTrigger));
				else if (a.EndTrigger != null)
					ShowTrigger(new TriggerData(a, a.EndOwner, a.EndTrigger));
				else
					return;
				loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
			}
			else if (sa?.Type == VisibileActivityType.UnfinishedActivities)
			{
				unfinishedActivitiesFolded = !unfinishedActivitiesFolded;
				changeNotification.Post();
			}
		}

		static bool IsActivitySelectedInStateInspector(IActivity a, StateInspectorVisualizer.IPresenterInternal stateInspectorVisualizer)
		{
			return
				   a != null
				&& a.Type == ActivityType.Lifespan
				&& stateInspectorVisualizer != null
				&& stateInspectorVisualizer.IsObjectEventPresented(a.BeginOwner.LogSource, a.BeginTrigger as TextLogEventTrigger);
		}

		static CurrentActivityDrawInfo GetCurrentActivityDrawInfo(IActivity a, bool isCurrentActivityVisibleInStateInspector)
		{
			if (a == null)
			{
				return new CurrentActivityDrawInfo()
				{
					Caption = "",
					DescriptionText = "",
 					DescriptionLinks = null,
					SourceLink = null,
					SourceText = null
				};
			}
			else
			{
				var captionBuilder = new StringBuilder();
				captionBuilder.AppendFormat("{0}: {1}", a.Type.ToString(), a.DisplayName);

				var descriptionBuilder = new StringBuilder();

				descriptionBuilder.AppendFormat("total duration: {0}", TimeUtils.TimeDeltaToString(a.GetDuration(), false));

				descriptionBuilder.Append("    started at: ");
				int beginLinkIdx = descriptionBuilder.Length;
				descriptionBuilder.Append(TimeUtils.TimeDeltaToString(a.GetTimelineBegin(), false));
				int beginLinkLen = descriptionBuilder.Length - beginLinkIdx;

				descriptionBuilder.Append("    ended at: ");
				int endLinkIdx = descriptionBuilder.Length;
				descriptionBuilder.Append(TimeUtils.TimeDeltaToString(a.GetTimelineEnd(), false));
				int endLinkLen = descriptionBuilder.Length - endLinkIdx;

				int? stateInspectorLinkIdx = null;
				int? stateInspectorLinkLen = null;
				if (isCurrentActivityVisibleInStateInspector)
				{
					descriptionBuilder.Append("    ");
					stateInspectorLinkIdx = descriptionBuilder.Length;
					descriptionBuilder.Append("show");
					stateInspectorLinkLen = descriptionBuilder.Length - stateInspectorLinkIdx.Value;
					descriptionBuilder.Append(" on StateInspector");
				}

				descriptionBuilder.Append("    tags: ");
				var tagsLinks = new List<Tuple<object, int, int>>();
				if (a.Tags.Count != 0)
				{
					foreach (var tag in a.Tags)
					{
						if (tagsLinks.Count > 0)
							descriptionBuilder.Append(", ");
						var linkBegin = descriptionBuilder.Length;
						descriptionBuilder.Append(tag);
						tagsLinks.Add(Tuple.Create((object)new TriggerData(tag), linkBegin, tag.Length));
					}
				}
				else
				{
					descriptionBuilder.Append("<none>");
				}

				var links = new[]
				{
					a.BeginTrigger != null ? Tuple.Create((object)new TriggerData(a, a.BeginOwner, a.BeginTrigger), beginLinkIdx, beginLinkLen) : null,
					a.EndTrigger != null ? Tuple.Create((object)new TriggerData(a, a.EndOwner, a.EndTrigger), endLinkIdx, endLinkLen) : null,
					stateInspectorLinkIdx != null ? Tuple.Create(
						(object)new TriggerData(a, a.BeginOwner, a.BeginTrigger, isStateInspectorLink: true),
						stateInspectorLinkIdx.Value,
						stateInspectorLinkLen.Value) : null
				}.Where(l => l != null).Union(tagsLinks);

				StringBuilder logSourceLinkBuilder = null;
				Tuple<object, int, int> sourceLink = null;
				if (true)
				{
					var ls = a.BeginOwner.LogSource;
					logSourceLinkBuilder = new StringBuilder("log source: ");
					int sourceLinkIdx = logSourceLinkBuilder.Length;
					logSourceLinkBuilder.Append(ls.GetShortDisplayNameWithAnnotation());
					int sourceLinkLen = logSourceLinkBuilder.Length - sourceLinkIdx;
					sourceLink = Tuple.Create((object)ls, sourceLinkIdx, sourceLinkLen);
				}

				return new CurrentActivityDrawInfo()
				{
					Caption = captionBuilder.ToString(),
					DescriptionText = descriptionBuilder.ToString(),
					DescriptionLinks = links,
					SourceText = logSourceLinkBuilder?.ToString(),
					SourceLink = sourceLink
				};
			}
		}

		bool TryGetVisibleActivity(int? index, out VisibileActivityInfo? activity)
		{
			if (index != null && index >= 0 && index < getVisibleActivities().Count)
				activity = getVisibleActivities()[index.Value];
			else
				activity = null;
			return activity != null;
		}

		static double GetTimeX(SpanRange range, TimeSpan ts)
		{
			var total = range.Length;
			if (total.Ticks == 0)
				return 0;
			var x = (ts - range.Begin);
			return x.TotalMilliseconds / total.TotalMilliseconds;
		}

		static Func<IActivity, bool> MakeFilteringPredicate(string filter, TagsPredicate tagsPredicate)
		{
			return a =>
				   (a.Tags.Count == 0 || tagsPredicate.IsMatch(a.Tags))
				&& GetActivityMatchIdx(a, filter) >= 0;
		}

		static IEnumerable<IActivity> GetActivitiesOverlappingWithRange(IReadOnlyList<IActivity> allActivities,
			SpanRange range, Func<IActivity, bool> filteringPredicate)
		{
			return allActivities
				.TakeWhile(a => a.GetTimelineBegin() < range.End)
				.Where(a => a.GetTimelineEnd() >= range.Begin)
				.Where(filteringPredicate);
		}

		static VisibileActivitiesInfo MakeVisibleActivitiesInfo(IReadOnlyList<IActivity> allActivities, SpanRange visibleRange,
			Func<IActivity, bool> filteringPredicate, bool unfinishedActivitiesFolded)
		{
			var visibleActivities = new List<VisibileActivityInfo>();
			visibleActivities.AddRange(GetActivitiesOverlappingWithRange(allActivities, visibleRange, filteringPredicate).Select(a => new VisibileActivityInfo()
			{
				Type = VisibileActivityType.Activity,
				Activity = a
			}));

			ActivitiesGroupInfo unfinishedActivities;
			unfinishedActivities.Count = visibleActivities.TakeWhile(a => a.Activity.IsEndedForcefully && a.Activity.Begin <= visibleRange.Begin).Count();
			bool unfinishedActivitiesVisible = unfinishedActivities.Count > 1;
			unfinishedActivities.VisibleActivityIndex = unfinishedActivitiesVisible ? 0 : new int?();
			if (unfinishedActivitiesVisible)
			{
				visibleActivities.Insert(0, new VisibileActivityInfo()
				{
					Type = VisibileActivityType.UnfinishedActivities
				});
				if (unfinishedActivitiesFolded)
				{
					visibleActivities.RemoveRange(1, unfinishedActivities.Count);
				}
			}
			return new VisibileActivitiesInfo
			{
				activities = visibleActivities,
				unfinishedActivities = unfinishedActivities
			};
		}

		static int GetActivityMatchIdx(IActivity activity, string filter)
		{
			if (string.IsNullOrEmpty(filter))
				return 0;
			var idx = activity.DisplayName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase);
			if (idx >= 0)
				return idx;
			return -1;
		}

		private void ReadAvailableRange()
		{
			var r = model.AvailableRange;
			var delta = origin - model.Origin;

			origin = model.Origin;
			availableRange = new SpanRange(r.Item1, r.Item2);

			if (!visibleRange.IsEmpty)
			{
				visibleRange = visibleRange.Add(delta);
			}

			ResetMeasurerIfItIsOutsideOfAvailableRange();
			SetSelectedActivity(null);
		}

		private void ResetMeasurerIfItIsOutsideOfAvailableRange()
		{
			if (measurer.State == MeasurerState.Set)
			{
				if (availableRange.Begin < measurer.RangeBegin || availableRange.End > measurer.RangeEnd)
				{
					measurer.State = MeasurerState.Unset;
					measurer.Version++;
					changeNotification.Post();
				}
			}
		}

		void SetVisibleRange(SpanRange newValue, bool realTimePanMode = false)
		{
			if (newValue.End <= newValue.Begin)
				return;
			visibleRange = newValue;
			changeNotification.Post();
		}

		TimeSpan VisibleRangeRelativePositionToAbsolute(double relativeLocationX)
		{
			var currentRange = visibleRange.Length;
			return visibleRange.Begin + currentRange.Multiply(relativeLocationX);
		}

		static double VisibleRangeAbsolutePositionToRelative(SpanRange range, TimeSpan position)
		{
			var currentRange = range.Length;
			return (double)((position - range.Begin).Ticks) / (double)(currentRange.Ticks);
		}

		KeyValuePair<TimeSpan, string> SnapToMilestone(TimeSpan ts, int? preferredActivityIndex, EnumMilestonesOptions options = EnumMilestonesOptions.Default)
		{
			TimeSpan minimumDistance = TimeSpan.MaxValue;
			TimeSpan ret = ts;
			string comment = null;
			bool searchOnlyInPreferredActivity = false;
			foreach (var ms in EnumMilestones(ts, options))
			{
				bool isPreferredActivity = ms.ActivityIndex != null && preferredActivityIndex != null
					&& ms.ActivityIndex.Value == preferredActivityIndex.Value;
				if (isPreferredActivity)
					searchOnlyInPreferredActivity = true;
				if (searchOnlyInPreferredActivity && !isPreferredActivity)
					continue;
				if (ms.Distance < minimumDistance)
				{
					minimumDistance = ms.Distance;
					ret = ms.MilestoneTime;
					comment = GetMilestoneComment(ms);
				}
			}
			return new KeyValuePair<TimeSpan,string>(ret, comment);
		}

		private static string GetMilestoneComment(MilestoneInfo ms)
		{
			string comment = null;
			if (!string.IsNullOrEmpty(ms.MilestoneDisplayName))
			{
				comment = "snap to: " + ms.MilestoneDisplayName;
				int maxCommentLen = 50;
				if (comment.Length > maxCommentLen)
					comment = comment.Substring(0, maxCommentLen - 3) + "...";
			}
			return comment;
		}

		IEnumerable<MilestoneInfo> EnumMilestones(TimeSpan ts, EnumMilestonesOptions options)
		{
			TimeSpan acceptableDistanceFromMilestone;
			if ((options & EnumMilestonesOptions.VisibleRangeScale) != 0)
				acceptableDistanceFromMilestone = visibleRange.Length.Multiply(0.01);
			else if ((options & EnumMilestonesOptions.AvailableRangeScale) != 0)
				acceptableDistanceFromMilestone = availableRange.Length.Multiply(0.01);
			else
				yield break;
			TimeSpan dist;
			if ((options & EnumMilestonesOptions.IncludeActivities) != 0)
			{
				var visibleActivities = getVisibleActivities();
				for (int activityIdx = 0; activityIdx < visibleActivities.Count; ++activityIdx)
				{
					var a = visibleActivities[activityIdx].Activity;
					if (a == null)
						continue;
					if ((dist = (a.GetTimelineBegin() - ts).Abs()) < acceptableDistanceFromMilestone)
						yield return new MilestoneInfo(dist, a.GetTimelineBegin(), a.DisplayName, activityIdx);
					if ((dist = (a.GetTimelineEnd() - ts).Abs()) < acceptableDistanceFromMilestone)
						yield return new MilestoneInfo(dist, a.GetTimelineEnd(), a.DisplayName, activityIdx);
					foreach (var ms in a.Milestones)
					{
						if ((dist = (ms.GetTimelineTime() - ts).Abs()) < acceptableDistanceFromMilestone)
							yield return new MilestoneInfo(dist, ms.GetTimelineTime(), ms.DisplayName, activityIdx);
					}
				}
			}
			if ((options & EnumMilestonesOptions.IncludeEvents) != 0)
			{
				foreach (var e in model.Events)
				{
					if ((dist = (e.GetTimelineTime() - ts).Abs()) < acceptableDistanceFromMilestone)
						yield return new MilestoneInfo(dist, e.GetTimelineTime(), e.DisplayName, null);
				}
			}
			if ((options & EnumMilestonesOptions.IncludeBookmarks) != 0)
			{
				foreach (var b in bookmarks.Items)
				{
					if ((dist = (b.GetTimelineTime(model) - ts).Abs()) < acceptableDistanceFromMilestone)
						yield return new MilestoneInfo(dist, b.GetTimelineTime(model), b.DisplayName, null);
				}
			}
		}

		void ShowTrigger(object trigger)
		{
			if (!(trigger is TriggerData triggerData))
				return;
			if (triggerData.Type == TriggerType.General || triggerData.Type == TriggerType.StateInspector)
			{
				if (triggerData.Trigger is TextLogEventTrigger slTrigger && triggerData.Source != null && !triggerData.Source.IsDisposed)
				{
					if (triggerData.Type != TriggerType.StateInspector)
					{
						presentersFacade.ShowMessage(
							bookmarks.Factory.CreateBookmark(
								slTrigger.Timestamp.Adjust(triggerData.Source.TimeOffsets),
								triggerData.Source.GetSafeConnectionId(),
								slTrigger.StreamPosition,
								0
							),
							BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet
						);
					}
					else
					{
						Func<StateInspectorVisualizer.IVisualizerNode, int> disambiguationFunction = io =>
							triggerData.Activity != null && triggerData.Activity.DisplayName.Contains(io.Id) ? 1 : 0;
						if (stateInspectorVisualizer != null && stateInspectorVisualizer.TrySelectObject(triggerData.Source, slTrigger, disambiguationFunction))
							stateInspectorVisualizer.Show();
					}
				}
			}
			else if (triggerData.Type == TriggerType.Tag)
			{
				tagsListPresenter.Edit(triggerData.Tag);
			}
		}

		bool HandleEscape()
		{
			if (quickSearchTextBoxPresenter.Text != "")
			{
				quickSearchTextBoxPresenter.Reset();
				return true;
			}
			return false;
		}

		private void ZoomInternal(double mousePosX, int? delta, double? ddelta = null)
		{
			var currentRange = visibleRange.Length;
			long deltaTicks = 0;
			if (delta != null)
				deltaTicks = -Math.Sign(delta.Value) * currentRange.Ticks / 5;
			else if (ddelta != null)
				deltaTicks = (long)(-ddelta.Value * (double)currentRange.Ticks / 1.2d);
			var newBegin = visibleRange.Begin - TimeSpan.FromTicks((long)(mousePosX * (double)deltaTicks));
			var newEnd = visibleRange.End + TimeSpan.FromTicks((long)((1 - mousePosX) * (double)deltaTicks));
			SetVisibleRange(new SpanRange(newBegin, newEnd));
		}

		private void SetInitialVisibleRange()
		{
			var firstActivities = GetActivitiesOverlappingWithRange(model.Activities, availableRange, getFilteringPredicate()).Take(2).ToArray();
			if (firstActivities.Length > 0)
			{
				TimeSpan b = firstActivities[0].Begin;
				var viewSize = firstActivities[0].GetDuration();
				if (viewSize.Ticks == 0 && firstActivities.Length > 1)
					viewSize = firstActivities[1].End - firstActivities[0].Begin;
				TimeSpan e;
				if (viewSize.Ticks == 0)
					e = b + TimeSpan.FromSeconds(1);
				else
					e = b + TimeSpan.FromSeconds(viewSize.TotalSeconds * 2.5d);
				visibleRange = new SpanRange(b, e);
			}
			else
			{
				visibleRange = availableRange;
			}
		}

		private void EnsureVisibleRangeIsAvailable()
		{
			var visibleLen = visibleRange.Length;
			if (visibleRange.End < availableRange.Begin)
			{
				visibleRange = new SpanRange(availableRange.Begin, availableRange.Begin + visibleLen);
			}
			else if (visibleRange.Begin >= availableRange.End)
			{
				visibleRange = new SpanRange(availableRange.End - visibleLen, availableRange.End);
			}
		}

		void ScrollToTime(TimeSpan pos)
		{
			var halfLen = visibleRange.Length.Multiply(0.5);
			SetVisibleRange(new SpanRange(pos - halfLen, pos + halfLen), realTimePanMode: false);
		}

		void FindNextUserAction(int direction, double pivot)
		{
			var currentPos = visibleRange.Begin + visibleRange.Length.Multiply(pivot);
			var evt = model
				.Events
				.Where(e => e.Type == EventType.UserAction)
				.Aggregate(
					Tuple.Create((IEvent)null, TimeSpan.MaxValue), 
					(bestEvtSoFar, candidateEvt) =>
					{
						var delta = candidateEvt.GetTimelineTime() - currentPos;
						if (Math.Sign(delta.Ticks) != direction)
							return bestEvtSoFar;
						var deltaAbs = delta.Abs();
						if (deltaAbs > bestEvtSoFar.Item2)
							return bestEvtSoFar;
						return Tuple.Create(candidateEvt, deltaAbs);
					}
				);
			if (evt.Item1 != null)
				ScrollToTime(evt.Item1.GetTimelineTime());
		}

		void FindNextBookmark(int direction)
		{
			var currentPos = visibleRange.Mid;
			var bmk = 
				bookmarks
				.Items
				.Aggregate(
					Tuple.Create((IBookmark)null, TimeSpan.MaxValue),
					(bestBmkSoFar, candidateBmk) =>
					{
						var delta = candidateBmk.GetTimelineTime(model) - currentPos;
						if (Math.Sign(delta.Ticks) != direction)
							return bestBmkSoFar;
						var deltaAbs = delta.Abs();
						if (deltaAbs > bestBmkSoFar.Item2)
							return bestBmkSoFar;
						return Tuple.Create(candidateBmk, deltaAbs);
					}
				);
			if (bmk.Item1 != null)
				ScrollToTime(bmk.Item1.GetTimelineTime(model));
		}

		private void FindCurrentTime()
		{
			var msg = loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;
			if (msg != null)
				ScrollToTime(msg.Time.ToLocalDateTime() - model.Origin);
		}


		private void SelectActivity(bool doubleClick, HitTestResult htResult)
		{
			if (TryGetVisibleActivity(htResult.ActivityIndex, out var _) && htResult.ActivityIndex != getSelectedActivityIndex())
				SetSelectedActivity(htResult.ActivityIndex.Value);
			else if (!TryGetVisibleActivity(htResult.ActivityIndex, out var _) && TryGetVisibleActivity(getSelectedActivityIndex(), out var _))
				SetSelectedActivity(null);
			if (doubleClick)
				PerformDefaultActionForSelectedActivity();
		}

		private void PerformFullUpdate()
		{
			ReadAvailableRange();
			if (visibleRange.End == visibleRange.Begin || availableRange.Begin == availableRange.End)
				SetInitialVisibleRange();
			else
				EnsureVisibleRangeIsAvailable();
		}

		enum MeasurerState
		{
			Unset,
			WaitingFirstMove,
			Measuring,
			Set
		};

		enum MouseCaptureState
		{
			NoCapture,
			Measuring,
			ActivitiesPan,
			WaitingActivitiesPan,
			Navigation
		};

		public enum TriggerType
		{
			General,
			StateInspector,
			Tag
 		};
		class TriggerData
		{
			public TriggerType Type;
			public IActivity Activity;
			public ILogSource Source;
			public object Trigger;
			public string ToolTip;
			public string Tag;

			public TriggerData(IActivity a, ITimelinePostprocessorOutput triggerOwner, object trigger, string toolTip = null, bool isStateInspectorLink = false)
			{
				Type = TriggerType.General;
				Activity = a;
				Source = triggerOwner.LogSource;
				Trigger = trigger;
				ToolTip = toolTip;
				Type = isStateInspectorLink ? TriggerType.StateInspector : TriggerType.General;
			}
			public TriggerData(IEvent e, string toolTip = null)
			{
				Type = TriggerType.General;
				Source = e.Owner.LogSource;
				Trigger = e.Trigger;
				ToolTip = toolTip;
			}
			public TriggerData(IBookmark bmk)
			{
				Type = TriggerType.General;
				Source = bmk.GetLogSource();
				Trigger = new TextLogEventTrigger(bmk);
				ToolTip = bmk.DisplayName;
			}
			public TriggerData(string tag)
			{
				Type = TriggerType.Tag;
				Tag = tag;
			}
		};

		struct MilestoneInfo
		{
			public readonly TimeSpan Distance;
			public readonly TimeSpan MilestoneTime;
			public readonly string MilestoneDisplayName;
			public readonly int? ActivityIndex;
			public MilestoneInfo(TimeSpan dist, TimeSpan msTime, string displayName, int? activityIndex)
			{
				Distance = dist;
				MilestoneTime = msTime;
				MilestoneDisplayName = displayName;
				ActivityIndex = activityIndex;
			}
		};

		struct MeasurerInfo
		{
			public MeasurerState State;
			public TimeSpan RangeBegin, RangeEnd;
			public string ExtraComment;
			public int Version;
		};

		struct PanInfo
		{
			public double StartPosition;
			public TimeSpan OriginalBegin;
			public TimeSpan OriginalEnd;
		};

		struct NavigationInfo
		{
			public TimeSpan? OriginalBegin;
			public TimeSpan? OriginalEnd;
			public double StartPosition;
		};

		enum EnumMilestonesOptions
		{
			AvailableRangeScale = 1,
			VisibleRangeScale = 2,
			IncludeActivities = 4,
			IncludeEvents = 8,
			IncludeBookmarks = 16,
			Default = VisibleRangeScale | IncludeActivities | IncludeEvents | IncludeBookmarks
		};

		struct ActivitiesGroupInfo
		{
			public int? VisibleActivityIndex;
			public int Count;
		};

		enum VisibileActivityType
		{
			Activity,
			UnfinishedActivities
		};

		struct VisibileActivityInfo
		{
			public VisibileActivityType Type;
			public IActivity Activity;
		};

		class VisibileActivitiesInfo
		{
			public IReadOnlyList<VisibileActivityInfo> activities;
			public ActivitiesGroupInfo unfinishedActivities;
			public void Deconstruct(out IReadOnlyList<VisibileActivityInfo> activities, out ActivitiesGroupInfo unfinishedActivities)
			{
				activities = this.activities;
				unfinishedActivities = this.unfinishedActivities;
			}
		};

		class SpanRange
		{
			public TimeSpan Begin { get; private set; }
			public TimeSpan End { get; private set; }
			public SpanRange(TimeSpan b, TimeSpan e)
			{
				Begin = b;
				End = e;
			}
			public TimeSpan Length => End - Begin;
			public TimeSpan Mid => (End + Begin).Multiply(0.5);
			public bool IsEmpty => Begin == End;
			public SpanRange Add(TimeSpan delta) => new SpanRange(Begin + delta, End + delta);
			public bool Eq(SpanRange r) => Begin == r.Begin && End == r.End;
		};

		readonly IChainedChangeNotification changeNotification;
		readonly ITimelineVisualizerModel model;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly IBookmarks bookmarks;
		readonly IPresentersFacade presentersFacade;
		readonly LogJoint.UI.Presenters.QuickSearchTextBox.IPresenter quickSearchTextBoxPresenter;
		readonly TagsList.IPresenter tagsListPresenter;
		readonly StateInspectorVisualizer.IPresenterInternal stateInspectorVisualizer;
		readonly Common.PresenterPersistentState persistentState;
		readonly ToastNotificationPresenter.IPresenter toastNotificationsPresenter;
		readonly IUserNamesProvider userNamesProvider;
		readonly Func<int?> getSelectedActivityIndex;
		readonly Func<VisibileActivityInfo?> getSelectedActivityInfo;
		readonly Func<CurrentActivityDrawInfo> getCurrentActivityDrawInfo;
		readonly Func<bool> isSelectedActivityPresentInStateInspector;
		readonly Func<Func<IActivity, bool>> getFilteringPredicate;
		readonly Func<VisibileActivitiesInfo> getVisibleActivitiesInfo;
		readonly Func<IReadOnlyList<VisibileActivityInfo>> getVisibleActivities;
		readonly Func<IReadOnlyList<ActivityDrawInfo>> getActivityDrawInfos;
		readonly Func<IReadOnlyList<RulerMarkDrawInfo>> getVisibleRangeRulerMarksDrawInfo;
		readonly Func<IReadOnlyList<RulerMarkDrawInfo>> getAvailableRangeRulerMarksDrawInfo;
		readonly Func<NavigationPanelDrawInfo> getNavigationPanelDrawInfo;
		readonly Func<IReadOnlyList<EventDrawInfo>> getAvailableRangeEventsDrawInfo;
		readonly Func<IReadOnlyList<EventDrawInfo>> getVisibleRangeEventsDrawInfo;
		readonly Func<IReadOnlyList<BookmarkDrawInfo>> getAvailableRangeBookmarksDrawInfo;
		readonly Func<IReadOnlyList<BookmarkDrawInfo>> getVisibleRangeBookmarksDrawInfo;
		readonly Func<FocusedMessageDrawInfo> getAvailableRangeFocusedMessageDrawInfo;
		readonly Func<FocusedMessageDrawInfo> getVisibleRangeFocusedMessageDrawInfo;
		readonly Func<MeasurerDrawInfo> getMeasurerDrawInfo;
		readonly IColorTheme theme;
		readonly ToolsContainer.IPresenter toolsContainerPresenter;
		DateTime origin;
		SpanRange availableRange = new SpanRange(TimeSpan.Zero, TimeSpan.Zero);
		SpanRange visibleRange = new SpanRange(TimeSpan.Zero, TimeSpan.Zero);
		Ref<VisibileActivityInfo> selectedActivity;
		bool unfinishedActivitiesFolded;
		int activityDrawAdditionalInputRevision;
		MouseCaptureState mouseCaptureState;
		MeasurerInfo measurer;
		PanInfo pan;
		NavigationInfo navigationInfo;
	}
}

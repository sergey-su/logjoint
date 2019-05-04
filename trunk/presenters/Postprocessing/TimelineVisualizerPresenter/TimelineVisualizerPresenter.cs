using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.Postprocessing.Timeline;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer
{
	public class TimelineVisualizerPresenter : IPresenter, IViewModel
	{
		public TimelineVisualizerPresenter(
			ITimelineVisualizerModel model,
			IView view,
			StateInspectorVisualizer.IPresenter stateInspectorVisualizer,
			Common.IPresentationObjectsFactory presentationObjectsFactory,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IBookmarks bookmarks,
			Persistence.IStorageManager storageManager,
			IPresentersFacade presentersFacade,
			IUserNamesProvider userNamesProvider,
			IChangeNotification parentChangeNotification,
			IColorTheme theme
		)
		{
			this.model = model;
			this.view = view;
			this.changeNotification = parentChangeNotification.CreateChainedChangeNotification(initiallyActive: false);
			this.quickSearchTextBoxPresenter = presentationObjectsFactory.CreateQuickSearch(view.QuickSearchTextBox);
			this.stateInspectorVisualizer = stateInspectorVisualizer;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.presentersFacade = presentersFacade;
			this.bookmarks = bookmarks;
			this.userNamesProvider = userNamesProvider;
			this.theme = theme;
			this.unfinishedActivities.IsFolded = true;

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
				view.UpdateSequenceDiagramAreaMetrics();
				view.Invalidate(ViewAreaFlag.ActivitiesCaptionsView);
			};

			loadedMessagesPresenter.LogViewerPresenter.FocusedMessageChanged += (sender, args) =>
			{
				view.Invalidate();
			};

			bookmarks.OnBookmarksChanged += (sender, args) =>
			{
				view.Invalidate();
			};

			quickSearchTextBoxPresenter.OnRealtimeSearch += (sender, args) =>
			{
				UpdateVisibleActivities();
				view.Invalidate();
			};

			quickSearchTextBoxPresenter.OnSearchNow += (sender, args) =>
			{
				UpdateVisibleActivities();
				view.ReceiveInputFocus();
				view.Invalidate();
			};

			quickSearchTextBoxPresenter.OnCancelled += (sender, args) =>
			{
				UpdateVisibleActivities();
				view.ReceiveInputFocus();
				view.Invalidate();
			};

			toastNotificationsPresenter = presentationObjectsFactory.CreateToastNotifications(view.ToastNotificationsView, changeNotification);
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateCorrelatorToastNotificationItem());
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateUnprocessedLogsToastNotification(PostprocessorIds.Timeline));

			getSelectedActivity = Selectors.Create(() => selectedActivity, idx =>
			{
				TryGetVisibleActivity(idx, out var ret);
				return ret;
			});
			isSelectedActivityPresentInStateInspector = Selectors.Create(getSelectedActivity,
				a => IsActivitySelectedInStateInspector(a?.Activity, stateInspectorVisualizer));
			getCurrentActivityDrawInfo = Selectors.Create(getSelectedActivity, isSelectedActivityPresentInStateInspector,
				(a, visInSI) => GetCurrentActivityDrawInfo(a?.Activity, visInSI));

			view.SetViewModel(this);

			PerformFullUpdate();

			// todo: get rid of this updater - make rendering reactive
			this.changeNotification.CreateSubscription(Updaters.Create(() => persistentState.TagsPredicate, _ =>
			{
				UpdateVisibleActivities();
				view.Invalidate();
			}));
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		ColorThemeMode IViewModel.ColorTheme => theme.Mode;

		void IViewModel.OnWindowShown()
		{
			changeNotification.Active = true;
		}

		void IViewModel.OnWindowHidden()
		{
			changeNotification.Active = false;
		}

		IEnumerable<RulerMark> IViewModel.OnDrawRulers(DrawScope scope, int totalRulerSize, int minAllowedDistanceBetweenMarks)
		{
			if (totalRulerSize <= 0)
			{
				return Enumerable.Empty<RulerMark>();
			}
			DateTime origin = model.Origin;
			var range = GetScopeRange(scope);
			long timelineRangeTicks = (range.Item2 - range.Item1).Ticks;
			if (timelineRangeTicks == 0)
			{
				return Enumerable.Empty<RulerMark>();
			}
			TimeSpan minTimespan = new TimeSpan(NumUtils.MulDiv(timelineRangeTicks, minAllowedDistanceBetweenMarks, totalRulerSize));
			var intervals = RulerUtils.FindTimeRulerIntervals(minTimespan);
			if (intervals != null)
			{
				return RulerUtils.GenerateTimeRulerMarks(intervals.Value,
					new DateRange(origin + range.Item1, origin + range.Item2)
				).Select(r => new RulerMark()
				{
					X = (double)(r.Time - origin - range.Item1).Ticks / (double)timelineRangeTicks,
					Label = r.ToString(),
					IsMajor = r.IsMajor
				});
			}
			else
			{
				return Enumerable.Empty<RulerMark>();
			}
		}

		IEnumerable<ActivityDrawInfo> IViewModel.OnDrawActivities()
		{
			var range = GetScopeRange(DrawScope.VisibleRange);
			bool displaySequenceDiagramTexts = model.Outputs.Count >= 2;
			var filter = quickSearchTextBoxPresenter.Text;
			
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
						IsSelected = i == selectedActivity,
						Type =
							a.Type == ActivityType.Lifespan ? ActivityDrawType.Lifespan :
							a.Type == ActivityType.Procedure ? ActivityDrawType.Procedure :
							a.Type == ActivityType.IncomingNetworking || a.Type == ActivityType.OutgoingNetworking ? ActivityDrawType.Networking :
							ActivityDrawType.Unknown,
						Color = !a.BeginOwner.LogSource.IsDisposed ? theme.ThreadColors.GetByIndex(a.BeginOwner.LogSource.ColorIndex) : new ModelColor?(),
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
						IsSelected = i == selectedActivity,
						Type = ActivityDrawType.Group,
						Color = new ModelColor(0xffffffff),
						Milestones = Enumerable.Empty<ActivityMilestoneDrawInfo>(),
						Phases = Enumerable.Empty<ActivityPhaseDrawInfo>(),
						IsFolded = unfinishedActivities.IsFolded
					};
				}
				else
				{
					throw new InvalidCastException();
				}
			}))
			{
				yield return a;
			}
		}

		int IViewModel.ActivitiesCount
		{
			get { return visibleActivities.Count; }
		}

		bool IViewModel.NotificationsIconVisibile
		{
			get { return toastNotificationsPresenter.HasSuppressedNotifications; }
		}

		private string GetSequenceDiagramText(IActivity a, Tuple<IActivity, IActivity> activitiesPair)
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

		IEnumerable<EventDrawInfo> IViewModel.OnDrawEvents(DrawScope scope)
		{
			var range = GetScopeRange(scope);
			return model.Events.Select(e => new EventDrawInfo
			{
				X = GetTimeX(range, e.GetTimelineTime()),
				Type = e.Type,
				Caption = e.DisplayName,
				Trigger = new TriggerData(e, string.Format("{0}\n{1}\n{2}", e.DisplayName,
					e.Owner.SequenceDiagramName,
					e.Owner.LogSource.GetShortDisplayNameWithAnnotation()))
			});
		}

		IEnumerable<BookmarkDrawInfo> IViewModel.OnDrawBookmarks(DrawScope scope)
		{
			var range = GetScopeRange(scope);
			return bookmarks.Items.Select(bmk =>
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
			}).Where(i => i.Trigger != null);
		}

		double? IViewModel.OnDrawFocusedMessage(DrawScope scope)
		{
			var msg = loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;
			if (msg == null)
				return null;
			var msgSource = msg.GetLogSource();
			if (msgSource == null)
				return null;
			return GetTimeX(GetScopeRange(scope), msg.Time.ToLocalDateTime() - model.Origin);
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
				var newSelectedActivity = selectedActivity + (code == KeyCode.Down ? 1 : -1);
				if (newSelectedActivity == -1)
					quickSearchTextBoxPresenter.Focus(null);
				else
					TrySetSelectedActivity(newSelectedActivity);
			}
			else if (code == KeyCode.Left || code == KeyCode.Right)
			{
				var delta = (visibleRangeEnd - visibleRangeBegin).Multiply(0.05);
				if (code == KeyCode.Left)
					delta = delta.Negate();
				SetVisibleRange(visibleRangeBegin + delta, visibleRangeEnd + delta);
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
		}

		void IViewModel.OnMouseZoom(double mousePosX, int delta)
		{
			ZoomInternal(mousePosX, delta, null);
		}

		void IViewModel.OnGestureZoom(double mousePosX, double delta)
		{
			ZoomInternal(mousePosX, null, delta);
		}

		NavigationPanelDrawInfo IViewModel.OnDrawNavigationPanel()
		{
			var availLen = availableRangeEnd - availableRangeBegin;
			var drawInfo = new NavigationPanelDrawInfo();
			if (availLen.Ticks > 0)
			{
				drawInfo.VisibleRangeX1 = (double)(visibleRangeBegin - availableRangeBegin).Ticks / (double)availLen.Ticks;
				drawInfo.VisibleRangeX2 = (double)(visibleRangeEnd - availableRangeBegin).Ticks / (double)availLen.Ticks;
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
			var filteringPredicate = MakeFilteringPredicate();
			IActivity activityToShow = null;
			if (searchLeft)
			{
				activityToShow = model.Activities
					.TakeWhile(a => a.GetTimelineBegin() < visibleRangeBegin)
					.Where(filteringPredicate)
					.MaxByKey(a => a.GetTimelineEnd());
			}
			else
			{
				var idx = model.Activities.BinarySearch(0, model.Activities.Count,
					a => a.GetTimelineBegin() <= visibleRangeEnd);
				activityToShow = model.Activities
					.Skip(idx)
					.Where(filteringPredicate)
					.FirstOrDefault();
			}
			if (activityToShow != null)
			{
				var delta =
					  (searchLeft ? activityToShow.GetTimelineEnd() : activityToShow.GetTimelineBegin())
					- (visibleRangeBegin + visibleRangeEnd).Multiply(0.5);
				SetVisibleRange(visibleRangeBegin + delta, visibleRangeEnd + delta);
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
				if (htResult.ActivityIndex == unfinishedActivities.VisibleActivityIndex)
				{
					unfinishedActivities.IsFolded = !unfinishedActivities.IsFolded;
					UpdateVisibleActivities();
					view.Invalidate();
				}
			}

			if (startPan || waitPan)
			{
				mouseCaptureState = waitPan ? MouseCaptureState.WaitingActivitiesPan : MouseCaptureState.ActivitiesPan;
				pan.StartPosition = htResult.RelativeX;
				pan.OriginalBegin = visibleRangeBegin;
				pan.OriginalEnd = visibleRangeEnd;
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
				view.Invalidate(ViewAreaFlag.ActivitiesBarsView);
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
					OriginalBegin = startNavigation1 ? visibleRangeBegin : new TimeSpan?(),
					OriginalEnd = startNavigation2 ? visibleRangeEnd : new TimeSpan?(),
				};
				mouseCaptureState = MouseCaptureState.Navigation;
			}
			if (navigate)
			{
				var halfLen = (visibleRangeEnd - visibleRangeBegin).Multiply(0.5);
				var pos = (availableRangeEnd - availableRangeBegin).Multiply(htResult.RelativeX);
				pos = SnapToMilestone(pos, null,
					EnumMilestonesOptions.AvailableRangeScale | EnumMilestonesOptions.IncludeBookmarks | EnumMilestonesOptions.IncludeEvents).Key;
				SetVisibleRange(pos - halfLen, pos + halfLen, realTimePanMode: true);
			}
			if (navigateToViewAll)
			{
				SetVisibleRange(availableRangeBegin, availableRangeEnd);
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
				view.Invalidate(ViewAreaFlag.ActivitiesBarsView);
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
				var delta = (availableRangeEnd - availableRangeBegin).Multiply(htResult.RelativeX - navigationInfo.StartPosition);
				SetVisibleRange(
					(navigationInfo.OriginalBegin + delta).GetValueOrDefault(visibleRangeBegin),
					(navigationInfo.OriginalEnd + delta).GetValueOrDefault(visibleRangeEnd),
					realTimePanMode: false
				);
			}
			
			if (mouseCaptureState == MouseCaptureState.ActivitiesPan)
			{
				var htResult = view.HitTest(hitTestToken);
				var delta = (pan.OriginalEnd - pan.OriginalBegin).Multiply(pan.StartPosition - htResult.RelativeX);
				SetVisibleRange(pan.OriginalBegin + delta, pan.OriginalEnd + delta, realTimePanMode: true);
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
						view.Invalidate(ViewAreaFlag.ActivitiesBarsView);
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
						view.Invalidate(ViewAreaFlag.ActivitiesBarsView);
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
			var delta = (visibleRangeEnd - visibleRangeBegin).Multiply(deltaX);
			SetVisibleRange(visibleRangeBegin + delta, visibleRangeEnd + delta, realTimePanMode: true);
		}

		MeasurerDrawInfo IViewModel.OnDrawMeasurer()
		{
			var text = measurer.State == MeasurerState.WaitingFirstMove ? 
				"move to measure time interval" : 
				TimeUtils.TimeDeltaToString((measurer.RangeEnd - measurer.RangeBegin).Abs(), false);
			if (measurer.ExtraComment != null)
				text += "\n" + measurer.ExtraComment;
			var ret = new MeasurerDrawInfo()
			{
				MeasurerVisible = measurer.State != MeasurerState.Unset,
				X1 = VisibleRangeAbsolutePositionToRelative(measurer.RangeBegin),
				X2 = VisibleRangeAbsolutePositionToRelative(measurer.RangeEnd),
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
			if (quickSearchTextBoxPresenter.Text != "")
			{
				quickSearchTextBoxPresenter.Reset();
				return true;
			}
			return false;
		}

		void IViewModel.OnQuickSearchExitBoxKeyDown(KeyCode code)
		{
			if (code == KeyCode.Down)
			{
				view.ReceiveInputFocus();
				view.Invalidate();
			}
		}

		void IPresenter.Navigate(TimeSpan t1, TimeSpan t2)
		{
			SetVisibleRange(t1, t2, realTimePanMode: false);
		}

		bool IViewModel.NoContentMessageVisibile
		{
			get { return visibleActivities.Count == 0 && model.Activities.Count > 0; }
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
			selectedActivity = value;
			view.Invalidate(ViewAreaFlag.ActivitiesCaptionsView | ViewAreaFlag.ActivitiesBarsView);
			if (value.HasValue && ensureVisible)
				view.EnsureActivityVisible(value.Value);
			changeNotification.Post();
		}

		void PerformDefaultActionForSelectedActivity()
		{
			var sa = GetSelectedActivity();
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
				unfinishedActivities.IsFolded = !unfinishedActivities.IsFolded;
				UpdateVisibleActivities();
				view.Invalidate();
			}
		}

		static bool IsActivitySelectedInStateInspector(IActivity a, StateInspectorVisualizer.IPresenter stateInspectorVisualizer)
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
			if (index != null && index >= 0 && index < visibleActivities.Count)
				activity = visibleActivities[index.Value];
			else
				activity = null;
			return activity != null;
		}

		VisibileActivityInfo? GetSelectedActivity()
		{
			TryGetVisibleActivity(selectedActivity, out var activity);
			return activity;
		}

		double GetTimeX(Tuple<TimeSpan, TimeSpan> range, TimeSpan ts)
		{
			var total = range.Item2 - range.Item1;
			if (total.Ticks == 0)
				return 0;
			var x = (ts - range.Item1);
			return x.TotalMilliseconds / total.TotalMilliseconds;
		}

		Func<IActivity, bool> MakeFilteringPredicate()
		{
			string filter = quickSearchTextBoxPresenter.Text;
			return a =>
				   (a.Tags.Count == 0 || persistentState.TagsPredicate.IsMatch(a.Tags))
				&& GetActivityMatchIdx(a, filter) >= 0;
		}

		IEnumerable<IActivity> GetActivitiesOverlappingWithRange(TimeSpan rangeBegin, TimeSpan rangeEnd)
		{
			var filteringPredicate = MakeFilteringPredicate();
			return model.Activities
				.TakeWhile(a => a.GetTimelineBegin() < rangeEnd)
				.Where(a => a.GetTimelineEnd() >= rangeBegin)
				.Where(filteringPredicate);
		}

		void UpdateVisibleActivities()
		{
			var savedSelection = GetSelectedActivity();
			selectedActivity = null;
			string filter = quickSearchTextBoxPresenter.Text;
			visibleActivities.Clear();
			visibleActivities.AddRange(GetActivitiesOverlappingWithRange(visibleRangeBegin, visibleRangeEnd).Select(a => new VisibileActivityInfo()
			{
				Type = VisibileActivityType.Activity,
				Activity = a
			}));
			GroupActiviteis(visibleActivities, visibleRangeBegin, ref unfinishedActivities);
			if (savedSelection != null)
				SetSelectedActivity(
					visibleActivities.IndexOf(va => savedSelection != null && va.Type == savedSelection.Value.Type && va.Activity == savedSelection.Value.Activity),
					ensureVisible: false
				);
			view.UpdateSequenceDiagramAreaMetrics();
			changeNotification.Post();
		}

		private static void GroupActiviteis(List<VisibileActivityInfo> visibleActivities, TimeSpan visibleRangeBegin, ref ActivitiesGroupInfo unfinishedActivities)
		{
			unfinishedActivities.Count = visibleActivities.TakeWhile(a => a.Activity.IsEndedForcefully && a.Activity.Begin <= visibleRangeBegin).Count();
			bool visible = unfinishedActivities.Count > 1;
			unfinishedActivities.VisibleActivityIndex = visible ? 0 : new int?();
			if (visible)
			{
				visibleActivities.Insert(0, new VisibileActivityInfo()
				{
					Type = VisibileActivityType.UnfinishedActivities
				});
				if (unfinishedActivities.IsFolded)
				{
					visibleActivities.RemoveRange(1, unfinishedActivities.Count);
				}
			}
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
			availableRangeBegin = r.Item1;
			availableRangeEnd = r.Item2;

			if (visibleRangeBegin != visibleRangeEnd)
			{
				visibleRangeBegin += delta;
				visibleRangeEnd += delta;
			}

			ResetMeasurerIfItIsOutsideOfAvailableRange();
			SetSelectedActivity(null);
		}

		private void ResetMeasurerIfItIsOutsideOfAvailableRange()
		{
			if (measurer.State == MeasurerState.Set)
			{
				if (availableRangeBegin < measurer.RangeBegin || availableRangeEnd > measurer.RangeEnd)
				{
					measurer.State = MeasurerState.Unset;
				}
			}
		}

		Tuple<TimeSpan, TimeSpan> GetScopeRange(DrawScope scope)
		{
			if (scope == DrawScope.AvailableRange)
				return Tuple.Create(availableRangeBegin, availableRangeEnd);
			else if (scope == DrawScope.VisibleRange)
				return Tuple.Create(visibleRangeBegin, visibleRangeEnd);
			throw new ArgumentException();
		}

		void SetVisibleRange(TimeSpan newBegin, TimeSpan newEnd, bool realTimePanMode = false)
		{
			if (newEnd <= newBegin)
				return;
			visibleRangeBegin = newBegin;
			visibleRangeEnd = newEnd;
			UpdateVisibleActivities();
			if (realTimePanMode)
			{
				view.Invalidate(ViewAreaFlag.ActivitiesCaptionsView | ViewAreaFlag.NavigationPanelView);
				view.Refresh(ViewAreaFlag.ActivitiesBarsView);
			}
			else
			{
				view.Invalidate();
			}
		}

		TimeSpan VisibleRangeRelativePositionToAbsolute(double relativeLocationX)
		{
			var currentRange = visibleRangeEnd - visibleRangeBegin;
			return visibleRangeBegin + currentRange.Multiply(relativeLocationX);
		}

		double VisibleRangeAbsolutePositionToRelative(TimeSpan position)
		{
			var currentRange = visibleRangeEnd - visibleRangeBegin;
			return (double)((position - visibleRangeBegin).Ticks) / (double)(currentRange.Ticks);
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
				acceptableDistanceFromMilestone = (visibleRangeEnd - visibleRangeBegin).Multiply(0.01);
			else if ((options & EnumMilestonesOptions.AvailableRangeScale) != 0)
				acceptableDistanceFromMilestone = (availableRangeEnd - availableRangeBegin).Multiply(0.01);
			else
				yield break;
			TimeSpan dist;
			if ((options & EnumMilestonesOptions.IncludeActivities) != 0)
			{
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
						Func<LogJoint.Postprocessing.StateInspector.IInspectedObject, int> disambiguationFunction = io =>
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

		private void ZoomInternal(double mousePosX, int? delta, double? ddelta = null)
		{
			var currentRange = visibleRangeEnd - visibleRangeBegin;
			long deltaTicks = 0;
			if (delta != null)
				deltaTicks = -Math.Sign(delta.Value) * currentRange.Ticks / 5;
			else if (ddelta != null)
				deltaTicks = (long)(-ddelta.Value * (double)currentRange.Ticks / 1.2d);
			var newBegin = visibleRangeBegin - TimeSpan.FromTicks((long)(mousePosX * (double)deltaTicks));
			var newEnd = visibleRangeEnd + TimeSpan.FromTicks((long)((1 - mousePosX) * (double)deltaTicks));
			SetVisibleRange(newBegin, newEnd);
		}

		private void SetInitialVisibleRange()
		{
			var firstActivities = GetActivitiesOverlappingWithRange(availableRangeBegin, availableRangeEnd).Take(2).ToArray();
			if (firstActivities.Length > 0)
			{
				visibleRangeBegin = firstActivities[0].Begin;
				var viewSize = firstActivities[0].GetDuration();
				if (viewSize.Ticks == 0 && firstActivities.Length > 1)
					viewSize = firstActivities[1].End - firstActivities[0].Begin;
				if (viewSize.Ticks == 0)
					visibleRangeEnd = visibleRangeBegin + TimeSpan.FromSeconds(1);
				else
					visibleRangeEnd = visibleRangeBegin + TimeSpan.FromSeconds(viewSize.TotalSeconds * 2.5d);
			}
			else
			{
				visibleRangeBegin = availableRangeBegin;
				visibleRangeEnd = availableRangeEnd;
			}
		}

		private void EnsureVisibleRangeIsAvailable()
		{
			var visibleLen = visibleRangeEnd - visibleRangeBegin;
			if (visibleRangeEnd < availableRangeBegin)
			{
				visibleRangeBegin = availableRangeBegin;
				visibleRangeEnd = visibleRangeBegin + visibleLen;
			}
			else if (visibleRangeBegin >= availableRangeEnd)
			{
				visibleRangeEnd = availableRangeEnd;
				visibleRangeBegin = visibleRangeEnd - visibleLen;
			}
		}

		void ScrollToTime(TimeSpan pos)
		{
			var halfLen = (visibleRangeEnd - visibleRangeBegin).Multiply(0.5);
			SetVisibleRange(pos - halfLen, pos + halfLen, realTimePanMode: false);
		}

		void FindNextUserAction(int direction, double pivot)
		{
			var currentPos = visibleRangeBegin + (visibleRangeEnd - visibleRangeBegin).Multiply(pivot);
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
			var currentPos = (visibleRangeEnd + visibleRangeBegin).Multiply(0.5);
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
			if (TryGetVisibleActivity(htResult.ActivityIndex, out var _) && htResult.ActivityIndex != selectedActivity)
				SetSelectedActivity(htResult.ActivityIndex.Value);
			else if (!TryGetVisibleActivity(htResult.ActivityIndex, out var _) && TryGetVisibleActivity(selectedActivity, out var _))
				SetSelectedActivity(null);
			if (doubleClick)
				PerformDefaultActionForSelectedActivity();
		}

		private void PerformFullUpdate()
		{
			ReadAvailableRange();
			if (visibleRangeEnd == visibleRangeBegin || availableRangeBegin == availableRangeEnd)
				SetInitialVisibleRange();
			else
				EnsureVisibleRangeIsAvailable();
			UpdateVisibleActivities();
			view.Invalidate();
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
			public bool IsFolded;
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

		readonly IChainedChangeNotification changeNotification;
		readonly ITimelineVisualizerModel model;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly IBookmarks bookmarks;
		readonly IPresentersFacade presentersFacade;
		readonly LogJoint.UI.Presenters.QuickSearchTextBox.IPresenter quickSearchTextBoxPresenter;
		readonly TagsList.IPresenter tagsListPresenter;
		readonly StateInspectorVisualizer.IPresenter stateInspectorVisualizer;
		readonly List<VisibileActivityInfo> visibleActivities = new List<VisibileActivityInfo>();
		ActivitiesGroupInfo unfinishedActivities;
		readonly Common.PresenterPersistentState persistentState;
		readonly ToastNotificationPresenter.IPresenter toastNotificationsPresenter;
		readonly IUserNamesProvider userNamesProvider;
		readonly Func<VisibileActivityInfo?> getSelectedActivity;
		readonly Func<CurrentActivityDrawInfo> getCurrentActivityDrawInfo;
		readonly Func<bool> isSelectedActivityPresentInStateInspector;
		readonly IColorTheme theme;
		DateTime origin;
		TimeSpan availableRangeBegin, availableRangeEnd;
		TimeSpan visibleRangeBegin, visibleRangeEnd;
		int? selectedActivity;
		MouseCaptureState mouseCaptureState;
		MeasurerInfo measurer;
		PanInfo pan;
		NavigationInfo navigationInfo;
	}
}

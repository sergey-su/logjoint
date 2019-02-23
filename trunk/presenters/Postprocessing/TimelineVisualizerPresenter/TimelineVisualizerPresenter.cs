using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using LJRulerMark = LogJoint.TimeRulerMark;
using LogJoint.UI.Presenters;
using LogJoint.Postprocessing.Timeline;
using LogJoint.Postprocessing;

namespace LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer
{
	public class TimelineVisualizerPresenter : IPresenter, IViewEvents
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
			IUserNamesProvider userNamesProvider
		)
		{
			this.model = model;
			this.view = view;
			this.quickSearchTextBoxPresenter = presentationObjectsFactory.CreateQuickSearch(view.QuickSearchTextBox);
			this.tagsListPresenter = presentationObjectsFactory.CreateTagsList(view.TagsListView);
			this.stateInspectorVisualizer = stateInspectorVisualizer;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.presentersFacade = presentersFacade;
			this.bookmarks = bookmarks;
			this.userNamesProvider = userNamesProvider;

			view.SetEventsHandler(this);

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

			tagsListPresenter.SelectedTagsChanged += (sender, e) => 
			{
				OnSelectedTagsChanged();
			};


			toastNotificationsPresenter = presentationObjectsFactory.CreateToastNotifications(view.ToastNotificationsView);
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateCorrelatorToastNotificationItem());
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateUnprocessedLogsToastNotification(PostprocessorIds.Timeline));
			toastNotificationsPresenter.SuppressedNotificationsChanged += (sender, args) =>
			{
				UpdateNotificationsIcon();
			};

			persistentState = new Common.PresentaterPersistentState(
				storageManager, "postproc.timeline", "postproc.timeline.view-state.xml");

			PerformFullUpdate();
		}

		IEnumerable<RulerMark> IViewEvents.OnDrawRulers(DrawScope scope, int totalRulerSize, int minAllowedDistanceBetweenMarks)
		{
			if (totalRulerSize <= 0)
			{
				return Enumerable.Empty<RulerMark>();
			}
			DateTime arbitraryOrigin = new DateTime(2000, 1, 1);
			var range = GetScopeRange(scope);
			long timelineRangeTicks = (range.Item2 - range.Item1).Ticks;
			TimeSpan minTimespan = new TimeSpan(NumUtils.MulDiv(timelineRangeTicks, minAllowedDistanceBetweenMarks, totalRulerSize));
			var intervals = RulerUtils.FindTimeRulerIntervals(minTimespan);
			if (intervals != null)
			{
				return RulerUtils.GenerateTimeRulerMarks(intervals.Value,
					new DateRange(arbitraryOrigin + range.Item1, arbitraryOrigin + range.Item2)
				).Select(r => new RulerMark()
				{
					X = (double)(r.Time - arbitraryOrigin - range.Item1).Ticks / (double)timelineRangeTicks,
					Label = GetRulerLabel(r.Time - arbitraryOrigin, r),
					IsMajor = r.IsMajor
				});
			}
			else
			{
				return Enumerable.Empty<RulerMark>();
			}
		}

		IEnumerable<ActivityDrawInfo> IViewEvents.OnDrawActivities()
		{
			var range = GetScopeRange(DrawScope.VisibleRange);
			bool displaySequenceDiagramTexts = model.Outputs.Count >= 2;
			var filter = quickSearchTextBoxPresenter.Text;
			return visibleActivities.Select((a, i) =>
			{
				var pairedActivities = model.GetPairedActivities(a);
				int? pairedActivityIndex = null;
				if (pairedActivities != null && pairedActivities.Item2 == a)
				{
					var idx1 = visibleActivities.LowerBound(pairedActivities.Item1, model.Comparer);
					var idx2 = visibleActivities.UpperBound(pairedActivities.Item1, model.Comparer);
					for (int idx = idx1; idx != idx2; ++idx)
						if (visibleActivities[idx] == pairedActivities.Item1)
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
					Type = a.Type,
					Color = !a.BeginOwner.LogSource.IsDisposed ? a.BeginOwner.LogSource.Color : new ModelColor?(),
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
					IsError = a.IsError,
				};
			});
		}

		int IViewEvents.ActivitiesCount
		{
			get { return visibleActivities.Count; }
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

		IEnumerable<EventDrawInfo> IViewEvents.OnDrawEvents(DrawScope scope)
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

		IEnumerable<BookmarkDrawInfo> IViewEvents.OnDrawBookmarks(DrawScope scope)
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

		double? IViewEvents.OnDrawFocusedMessage(DrawScope scope)
		{
			var msg = loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;
			if (msg == null)
				return null;
			var msgSource = msg.GetLogSource();
			if (msgSource == null)
				return null;
			return GetTimeX(GetScopeRange(scope), msg.Time.ToLocalDateTime() - model.Origin);
		}

		void IViewEvents.OnKeyPressed(char keyChar)
		{
			if (!char.IsWhiteSpace(keyChar))
				quickSearchTextBoxPresenter.Focus(new string(keyChar, 1));
		}

		void IViewEvents.OnKeyDown(KeyCode code)
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
				ShowSelectedActivity();
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

		void IViewEvents.OnMouseZoom(double mousePosX, int delta)
		{
			ZoomInternal(mousePosX, delta, null);
		}

		void IViewEvents.OnGestureZoom(double mousePosX, double delta)
		{
			ZoomInternal(mousePosX, null, delta);
		}

		NavigationPanelDrawInfo IViewEvents.OnDrawNavigationPanel()
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

		void IViewEvents.OnActivityTriggerClicked(object trigger)
		{
			ShowTrigger(trigger);
		}

		void IViewEvents.OnEventTriggerClicked(object trigger)
		{
			ShowTrigger(trigger);
		}

		void IViewEvents.OnActiveNotificationButtonClicked()
		{
			toastNotificationsPresenter.UnsuppressNotifications();
		}

		void IViewEvents.OnActivitySourceLinkClicked(object trigger)
		{
			ILogSource ls = trigger as ILogSource;
			if (ls != null)
				presentersFacade.ShowLogSource(ls);
		}

		void IViewEvents.OnMouseDown(object hitTestToken, KeyCode keys, bool doubleClick)
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

		void IViewEvents.OnMouseMove(object hitTestToken, KeyCode keys)
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

		void IViewEvents.OnMouseUp(object hitTestToken)
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

		void IViewEvents.OnScrollWheel(double deltaX)
		{
			var delta = (visibleRangeEnd - visibleRangeBegin).Multiply(deltaX);
			SetVisibleRange(visibleRangeBegin + delta, visibleRangeEnd + delta, realTimePanMode: true);
		}

		MeasurerDrawInfo IViewEvents.OnDrawMeasurer()
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

		string IViewEvents.OnToolTip(object hitTestToken)
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
				if (TryGetActivity(htResult.ActivityIndex, out var a) && a.Phases.Count > 0)
				{
					return string.Join(Environment.NewLine,
						a.Phases.Select(ph => string.Format("{0} {1}ms", ph.DisplayName, (ph.End - ph.Begin).TotalMilliseconds)));
				}
			}
			return null;
		}

		void IViewEvents.OnPrevUserEventButtonClicked()
		{
			FindNextUserAction(-1, 0.0);
		}

		void IViewEvents.OnNextUserEventButtonClicked()
		{
			FindNextUserAction(+1, 1.0);
		}

		void IViewEvents.OnPrevBookmarkButtonClicked()
		{
			FindNextBookmark(-1);
		}

		void IViewEvents.OnNextBookmarkButtonClicked()
		{
			FindNextBookmark(+1);
		}

		void IViewEvents.OnFindCurrentTimeButtonClicked()
		{
			FindCurrentTime();
		}

		void IViewEvents.OnZoomInButtonClicked()
		{
			ZoomInternal(0.5, 1);
		}

		void IViewEvents.OnZoomOutButtonClicked()
		{
			ZoomInternal(0.5, -1);
		}

		bool IViewEvents.OnEscapeCmdKey()
		{
			if (quickSearchTextBoxPresenter.Text != "")
			{
				quickSearchTextBoxPresenter.Reset();
				return true;
			}
			return false;
		}

		void IViewEvents.OnQuickSearchExitBoxKeyDown(KeyCode code)
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

		void OnSelectedTagsChanged()
		{
			visibleTags = new HashSet<string>(tagsListPresenter.SelectedTags);
			visibleTags.IntersectWith(availableTags);
			UpdateVisibleActivities();
			SaveTags();
			view.Invalidate();
		}

		void UpdateNotificationsIcon()
		{
			view.SetNotificationsIconVisibility(toastNotificationsPresenter.HasSuppressedNotifications);
		}

		bool TrySetSelectedActivity(int? value)
		{
			if (!TryGetActivity(value, out var _))
				return false;
			SetSelectedActivity(value.Value);
			return true;
		}

		void SetSelectedActivity(int? value)
		{
			selectedActivity = value;
			view.Invalidate(ViewAreaFlag.ActivitiesCaptionsView | ViewAreaFlag.ActivitiesBarsView);
			if (value.HasValue)
				view.EnsureActivityVisible(value.Value);
			UpdateCurrentActivityControls();
		}

		void ShowSelectedActivity()
		{
			var a = GetSelectedActivity();
			if (a != null)
			{
				if (a.BeginTrigger != null)
					ShowTrigger(new TriggerData(a, a.BeginOwner, a.BeginTrigger));
				else if (a.EndTrigger != null)
					ShowTrigger(new TriggerData(a, a.EndOwner, a.EndTrigger));
				else
					return;
				loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
			}
		}

		void UpdateCurrentActivityControls()
		{
			var a = GetSelectedActivity();
			if (a == null)
			{
				view.UpdateCurrentActivityControls("", "", null, null, null);
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
				if (a.Type == ActivityType.Lifespan 
				 && stateInspectorVisualizer != null
				 && stateInspectorVisualizer.IsObjectEventPresented(a.BeginOwner.LogSource, a.BeginTrigger as TextLogEventTrigger))
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

				view.UpdateCurrentActivityControls(
					captionBuilder.ToString(),
					descriptionBuilder.ToString(),
					links,
					logSourceLinkBuilder != null ? logSourceLinkBuilder.ToString() : null,
					sourceLink
				);
			}
		}

		bool TryGetActivity(int? index, out IActivity activity)
		{
			if (index != null && index >= 0 && index < visibleActivities.Count)
				activity = visibleActivities[index.Value];
			else
				activity = null;
			return activity != null;
		}

		IActivity GetSelectedActivity()
		{
			TryGetActivity(selectedActivity, out var activity);
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

		IEnumerable<IActivity> GetActivitiesOverlappingWithRange(TimeSpan rangeBegin, TimeSpan rangeEnd)
		{
			string filter = quickSearchTextBoxPresenter.Text;
			return model.Activities
				.TakeWhile(a => a.GetTimelineBegin() < rangeEnd)
				.Where(a => a.GetTimelineEnd() >= rangeBegin)
				.Where(a => a.Tags.Count == 0 || a.Tags.Overlaps(visibleTags))
				.Where(a => GetActivityMatchIdx(a, filter) >= 0);
		}

		void UpdateVisibleActivities()
		{
			var saveSelection = GetSelectedActivity();
			selectedActivity = null;
			string filter = quickSearchTextBoxPresenter.Text;
			visibleActivities.Clear();
			visibleActivities.AddRange(GetActivitiesOverlappingWithRange(visibleRangeBegin, visibleRangeEnd));
			if (saveSelection != null)
			{
				var idx = visibleActivities.IndexOf(saveSelection);
				SetSelectedActivity(TryGetActivity(idx, out var _) ? idx : new int?());
			}
			view.UpdateActivitiesScroller(visibleActivities.Count);
			view.UpdateSequenceDiagramAreaMetrics();
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

		static string GetRulerLabel(TimeSpan ts, LJRulerMark rm)
		{
			if (ts == TimeSpan.Zero)
				return "0";
			var s = ts.ToString(GetRulerLabelFormat(rm));// + string.Format("{0} {1}", rm.Component, rm.IsMajor);
			if (ts < TimeSpan.Zero)
				s = "-" + s;
			return s;
		}

		static string GetRulerLabelFormat(LJRulerMark rm)
		{
			switch (rm.Component)
			{
				case DateComponent.Year:
				case DateComponent.Month:
				case DateComponent.Day:
					return @"d\d";
				case DateComponent.Hour:
					return rm.IsMajor ? @"d\d\ h\h" : @"h\h";
				case DateComponent.Minute:
					return rm.IsMajor ? @"h\h\:m\m" : @"m\m";
				case DateComponent.Seconds:
					return rm.IsMajor ? @"m\m\:s\s" : @"s\s";
				case DateComponent.Milliseconds:
					return rm.IsMajor ? @"s\.fff\s" : @"fff\m\s";
				default:
					return null;
			}
		}

		private void ReadAvailableRange()
		{
			var r = model.AvailableRange;
			availableRangeBegin = r.Item1;
			availableRangeEnd = r.Item2;

			ResetMeasurerIfItIsOutsideOfAvailableRange();
			SetSelectedActivity(null);
		}

		private void UpdateTags()
		{
			var newAvailableTags = new HashSet<string>(model.Activities.SelectMany(a => a.Tags));

			visibleTags = persistentState.GetVisibleTags(model.Outputs.Select(output => output.LogSource), newAvailableTags, new [] { "calling" });

			availableTags = newAvailableTags;

			tagsListPresenter.SetTags(availableTags, visibleTags);
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
					var a = visibleActivities[activityIdx];
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
			if (TryGetActivity(htResult.ActivityIndex, out var _) && htResult.ActivityIndex != selectedActivity)
				SetSelectedActivity(htResult.ActivityIndex.Value);
			else if (!TryGetActivity(htResult.ActivityIndex, out var _) && TryGetActivity(selectedActivity, out var _))
				SetSelectedActivity(null);
			if (doubleClick)
				ShowSelectedActivity();
		}

		private void PerformFullUpdate()
		{
			ReadAvailableRange();
			UpdateTags();
			if (visibleRangeEnd == visibleRangeBegin || availableRangeBegin == availableRangeEnd)
				SetInitialVisibleRange();
			else
				EnsureVisibleRangeIsAvailable();
			UpdateVisibleActivities();
			view.Invalidate();
		}

		void SaveTags()
		{
			persistentState.SaveVisibleTags(visibleTags, model.Outputs.Select(output => output.LogSource));
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

		readonly ITimelineVisualizerModel model;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly IBookmarks bookmarks;
		readonly IPresentersFacade presentersFacade;
		readonly LogJoint.UI.Presenters.QuickSearchTextBox.IPresenter quickSearchTextBoxPresenter;
		readonly TagsList.IPresenter tagsListPresenter;
		readonly StateInspectorVisualizer.IPresenter stateInspectorVisualizer;
		readonly List<IActivity> visibleActivities = new List<IActivity>();
		readonly Common.PresentaterPersistentState persistentState;
		readonly ToastNotificationPresenter.IPresenter toastNotificationsPresenter;
		readonly IUserNamesProvider userNamesProvider;
		HashSet<string> availableTags = new HashSet<string>();
		HashSet<string> visibleTags = new HashSet<string>();
		TimeSpan availableRangeBegin, availableRangeEnd;
		TimeSpan visibleRangeBegin, visibleRangeEnd;
		int? selectedActivity;
		MouseCaptureState mouseCaptureState;
		MeasurerInfo measurer;
		PanInfo pan;
		NavigationInfo navigationInfo;
	}
}

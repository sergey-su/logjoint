using M = LogJoint.Postprocessing.Messaging;
using TL = LogJoint.Postprocessing.Timeline;
using SI = LogJoint.Postprocessing.StateInspector;
using SD = LogJoint.Postprocessing.SequenceDiagram;
using System;
using System.Collections.Generic;
using LogJoint.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using LogJoint.Postprocessing.SequenceDiagram;
using LogJoint.Postprocessing;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer
{
	public class SequenceDiagramVisualizerPresenter: IPresenter, IViewModel
	{
		readonly IView view;
		readonly ISequenceDiagramVisualizerModel model;
		readonly IChainedChangeNotification changeNotification;
		readonly StateInspectorVisualizer.IPresenter stateInspectorPresenter;
		readonly TagsList.IPresenter tagsListPresenter;
		readonly QuickSearchTextBox.IPresenter quickSearchPresenter;
		ImmutableDictionary<string, Role> roles = ImmutableDictionary.Create<string, Role>();
		ImmutableArray<Arrow> arrows = ImmutableArray.Create<Arrow>();
		ImmutableDictionary<string, Arrow> hiddenLinkableResponses = ImmutableDictionary.Create<string, Arrow>();
		readonly static IComparer<Arrow> arrowComparer = new ArrowComparer();
		readonly Common.PresenterPersistentState persistentState;
		readonly ToastNotificationPresenter.IPresenter toastNotificationsPresenter;
		readonly IBookmarks bookmarks;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly IPresentersFacade presentersFacade;
		readonly IUserNamesProvider userNamesProvider;
		ImmutableHashSet<string> availableTags = ImmutableHashSet.Create<string>();
		SortedList<int, bool> selectedArrows = new SortedList<int, bool>();
		int focusedSelectedArrow;
		readonly Func<CurrentArrowInfo> currentArrowInfo;
		bool hideResponses = false;
		bool collapseRoleInstances = false;
		readonly MetricsCache metrics;

		Point? capturedMousePt;
		Matrix capturedMouseTransform;
		bool viewMovedAfterCapturing;

		Matrix transform = new Matrix();

		public SequenceDiagramVisualizerPresenter(
			ISequenceDiagramVisualizerModel model, 
			IView view, 
			StateInspectorVisualizer.IPresenter stateInspectorPresenter,
			Common.IPresentationObjectsFactory presentationObjectsFactory,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IBookmarks bookmarks,
			Persistence.IStorageManager storageManager,
			IPresentersFacade presentersFacade,
			IUserNamesProvider userNamesProvider,
			IChangeNotification parentChangeNotification
		)
		{
			this.model = model;
			this.view = view;
			this.changeNotification = parentChangeNotification.CreateChainedChangeNotification(initiallyActive: false);
			this.stateInspectorPresenter = stateInspectorPresenter;
			this.quickSearchPresenter = presentationObjectsFactory.CreateQuickSearch(view.QuickSearchTextBox);
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.bookmarks = bookmarks;
			this.presentersFacade = presentersFacade;
			this.userNamesProvider = userNamesProvider;

			view.SetViewModel(this);

			this.metrics = new MetricsCache(view.GetMetrics());

			transform.Translate(metrics.nodeWidth, metrics.messageHeight);

			model.Changed += (s, e) => Update();
			loadedMessagesPresenter.LogViewerPresenter.FocusedMessageChanged += (s, e) => view.Invalidate();
			bookmarks.OnBookmarksChanged += (s, e) => Update();

			var sourcesSelector = Selectors.Create(() => model.Outputs, outputs => outputs.Select(output => output.LogSource));
			this.persistentState = new Common.PresenterPersistentState(
				storageManager, "postproc.sequence-diagram", "postproc.sequence.view-state.xml",
				changeNotification, () => availableTags, sourcesSelector);
			this.tagsListPresenter = presentationObjectsFactory.CreateTagsList(persistentState, view.TagsListView, changeNotification);
			this.tagsListPresenter.SetIsSingleLine(false);

			// todo: get rid of this updater - make rendering reactive
			changeNotification.CreateSubscription(Updaters.Create(() => persistentState.TagsPredicate, _ => Update()));

			collapseRoleInstances = true;

			quickSearchPresenter.OnRealtimeSearch += (sender, args) =>
			{
				Update();
			};

			quickSearchPresenter.OnSearchNow += (sender, args) =>
			{
				Update();
				view.PutInputFocusToArrowsArea();
			};

			quickSearchPresenter.OnCancelled += (sender, args) =>
			{
				Update();
				view.PutInputFocusToArrowsArea();
			};

			toastNotificationsPresenter = presentationObjectsFactory.CreateToastNotifications(view.ToastNotificationsView, changeNotification);
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateCorrelatorToastNotificationItem());
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateUnprocessedLogsToastNotification(PostprocessorKind.SequenceDiagram));

			currentArrowInfo = Selectors.Create(
				() => focusedSelectedArrow,
				arrowIdx => MakeCurrentArrowInfo(GetSelectedArrow(), stateInspectorPresenter)
			);

			Update();
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		bool IViewModel.IsNotificationsIconVisibile => toastNotificationsPresenter.HasSuppressedNotifications;

		CurrentArrowInfo IViewModel.CurrentArrowInfo => currentArrowInfo();

		void IViewModel.OnWindowShown()
		{
			changeNotification.Active = true;
		}

		void IViewModel.OnWindowHidden()
		{
			changeNotification.Active = false;
		}

		IEnumerable<RoleDrawInfo> IViewModel.OnDrawRoles()
		{
			var msg = loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;
			var focusedLogSource = msg != null ? msg.GetLogSource() : null;
			int w = Math.Max(0, Transform(metrics.nodeWidth, 0, transformVector: true).X - 10);
			int h = view.RolesCaptionsAreaHeight - 3;
			int firstArrowIdx = Math.Max(GetMessageIndex(0), 0);
			int lastArrowIdx = GetMessageIndex(view.ArrowsAreaHeight) + 1;
			foreach (var role in roles.Values)
			{
				var x = GetRoleX(role.DisplayIndex);
				yield return new RoleDrawInfo()
				{
					X = x,
					DisplayName = role.DisplayName,
					Bounds = new Rectangle(x - w / 2, 2, w, h),
					LogSourceTrigger = role.LogSources.Count == 0 ? null : new TriggerData(role.LogSources.First()),
					ExecutionOccurrences = role.ExecutionOccurrences
						.Where(eo => Math.Max(firstArrowIdx, eo.Begin.Index) <= Math.Min(lastArrowIdx, eo.End.Index))
						.Select(eo => 
					{
						var beginY = GetArrowY(eo.Begin.Index);
						var endY = GetArrowY(eo.End.Index);
						var yDelta = 0;
						var drawMode = ExecutionOccurrenceDrawMode.Normal;
						if (eo.Begin.Type == ArrowType.ActivityBegin)
						{
							// move activity box up to hide its ends under activity begin/end labels
							yDelta = -metrics.messageHeight / 2;
				 			drawMode = ExecutionOccurrenceDrawMode.Activity;
				 		}
						return new ExecutionOccurrenceDrawInfo()
						{
							Bounds = new Rectangle(
								x - metrics.executionOccurrenceWidth / 2 + eo.Level * metrics.executionOccurrenceLevelOffset,
								beginY + yDelta,
								metrics.executionOccurrenceWidth,
								endY - beginY
							),
							IsHighlighted = IsHighlightedArrow(eo.Begin),
							DrawMode = drawMode,
						};
					}),
					ContainsFocusedMessage = focusedLogSource != null && role.LogSources.Contains(focusedLogSource)
				};
			}
		}

		static Point Transform(Matrix m, int x, int y, bool transformVector = false)
		{
			var pts = new[] { new PointF(x, y) };
			if (transformVector)
				m.TransformVectors(pts);
			else
				m.TransformPoints(pts);
			return new Point((int)pts[0].X, (int)pts[0].Y);
		}

		Point Transform(int x, int y, bool transformVector = false)
		{
			return Transform(transform, x, y, transformVector);
		}

		IEnumerable<ArrowDrawInfo> IViewModel.OnDrawArrows()
		{
			int maxY = view.ArrowsAreaHeight + metrics.messageHeight;
			int w = Transform(metrics.nodeWidth, 0, true).X;
			var fmr = GetFocusedMessageRange();
			int minX = GetRoleX(0);
			int maxX = GetRoleX(Math.Max(roles.Count - 1, 0));

			Func<Arrow, bool, ArrowDrawInfo> makeDrawInfo = (arrow, isFullyVisible) =>
			{
				var ret = new ArrowDrawInfo()
				{
					Mode = ToDrawMode(arrow.Type),
					Y = GetArrowY(arrow.Index),
					DisplayName = arrow.ShortDisplayName,
					FromX = GetRoleX(arrow.From.DisplayIndex) + arrow.FromOffset,
					ToX = GetRoleX(arrow.To.DisplayIndex) + arrow.ToOffset,
					IsBookmarked = arrow.IsBookmarked,
					Height = metrics.messageHeight,
					Width = w,
					TextPadding = metrics.arrowTextPadding,
					Delta = "",
					MinX = minX,
					MaxX = maxX,
					Color = arrow.Color,
					IsFullyVisible = isFullyVisible
				};

				bool isSelected = selectedArrows.ContainsKey(arrow.Index);
				if (arrow.Index == focusedSelectedArrow)
					ret.SelectionState = ArrowSelectionState.FocusedSelectedArrow;
				else if (isSelected)
					ret.SelectionState = ArrowSelectionState.SelectedArrow;
				else
					ret.SelectionState = ArrowSelectionState.NotSelected;

				ret.IsHighlighted = IsHighlightedArrow(arrow);

				if (selectedArrows.Count > 1)
				{
					if (isSelected)
					{
						var prevSelectedArrowIdxIdx = ListUtils.GetBound(selectedArrows.Keys.AsReadOnly(), arrow.Index, ValueBound.UpperReversed, Comparer<int>.Default);
						if (prevSelectedArrowIdxIdx >= 0)
						{
							var prevSelectedArrowIdx = selectedArrows.Keys[prevSelectedArrowIdxIdx];
							if (IsValidArrowIndex(prevSelectedArrowIdx))
							{
								ret.Delta = TimeUtils.TimeDeltaToString(arrow.FromTimestamp - arrows[prevSelectedArrowIdx].FromTimestamp);
							}
						}
					}
				}
				else
				{
					ret.Delta = TimeUtils.TimeDeltaToString(arrow.Delta);
				}

				if (fmr != null)
				{
					if (fmr.Item1 == arrow.Index)
						if (fmr.Item2 == fmr.Item1)
							ret.CurrentTimePosition = -1;
						else
							ret.CurrentTimePosition = 0;
					else if (fmr.Item1 > arrow.Index && arrow.Index == arrows.Length - 1)
						ret.CurrentTimePosition = 1;
				}

				var nonHorizontalArrowRole = GetNonHorizontalArrowRole(arrow);
				if (nonHorizontalArrowRole == NonHorizontalArrowRole.Receiver)
				{
					ret.Mode = ArrowDrawMode.NoArrow;
				}
				else if (nonHorizontalArrowRole == NonHorizontalArrowRole.Sender)
				{
					ret.NonHorizontalDrawingData = new NonHorizontalArrowDrawInfo()
					{
						ToX = GetRoleX(arrow.To.DisplayIndex) + arrow.NonHorizontalConnectedArrow.ToOffset,
						Y = GetArrowY(arrow.NonHorizontalConnectedArrow.Index),
						VerticalLineX = GetRoleX(arrow.To.DisplayIndex) + arrow.NonHorizontalConnectorOffset
					};
				}

				if (hideResponses && arrow.LinkedArrowId != null)
				{
					Arrow hiddenResponse;
					if (hiddenLinkableResponses.TryGetValue(arrow.LinkedArrowId, out hiddenResponse) && hiddenResponse.Color != ArrowColor.Normal)
					{
						// highlight abnormal responses: take color and display text (with status code inside) from the response
						ret.DisplayName = hiddenResponse.ShortDisplayName;
						ret.Color = hiddenResponse.Color;
					}
				}

				return ret;
			};

			int firstArrowIdx = Math.Max(GetMessageIndex(0), 0);
			foreach (var arrow in arrows.Skip(firstArrowIdx))
			{
				int y = GetArrowY(arrow.Index);
				if (y < 0)
					continue;
				if (y > maxY)
					break;
				if (arrow.Index == firstArrowIdx && arrow.OverlappingNonHorizontalArrows != null)
				{
					foreach (var overlappingNonHorizontalArrow in arrow.OverlappingNonHorizontalArrows)
						yield return makeDrawInfo(overlappingNonHorizontalArrow, false);
				}
				yield return makeDrawInfo(arrow, true);
			}
		}

		void IViewModel.OnArrowsAreaMouseDown(Point pt, bool doubleClick)
		{
			if (doubleClick)
			{
				ShowSelectedArrow();
			}
			else
			{
				capturedMousePt = pt;
				capturedMouseTransform = transform.Clone();
				viewMovedAfterCapturing = false;
			}
		}

		void IViewModel.OnArrowsAreaMouseMove(Point pt)
		{
			if (capturedMousePt != null)
			{
				int deltaX = pt.X - capturedMousePt.Value.X;
				int deltaY = pt.Y - capturedMousePt.Value.Y;
				if (!viewMovedAfterCapturing)
				{
					if (Math.Abs(deltaX) < 3 && Math.Abs(deltaY) < 3)
						return;
					viewMovedAfterCapturing = true;
				}
				transform = capturedMouseTransform.Clone();
				ModifyTransform(m => m.Translate(deltaX, deltaY, MatrixOrder.Append));
			}
		}

		void IViewModel.OnArrowsAreaMouseUp(Point pt, Key modifiers)
		{
			bool selectArrow = false;
			if (capturedMousePt != null)
			{
				capturedMousePt = null;
				capturedMouseTransform = null;
				if (!viewMovedAfterCapturing)
					selectArrow = true;
			}
			if (selectArrow)
			{
				SetSelectedArrowIndex(GetMessageIndex(pt.Y), multiselectionMode: (modifiers & Key.MultipleSelectionModifier) != 0);
			}
		}

		void IViewModel.OnArrowsAreaMouseWheel(Point pt, int delta, Key modifiers)
		{
			if ((modifiers & Key.WheelZoomModifier) != 0)
			{
				ScaleTransform(pt.X, delta > 0 ? 1.1f : 0.9f);
			}
			else
			{
				ModifyTransform(m => m.Translate(0, delta > 0 ? metrics.vScrollOffset : -metrics.vScrollOffset, MatrixOrder.Append));
			}
		}


		void IViewModel.OnLeftPanelMouseDown(Point pt, bool doubleClick, Key modifiers)
		{
			SetSelectedArrowIndex(GetMessageIndex(pt.Y), multiselectionMode: (modifiers & Key.MultipleSelectionModifier) != 0);
			if (doubleClick)
				ShowSelectedArrow();
		}

		void IViewModel.OnKeyDown(Key code)
		{
			if (code == Key.Left)
			{
				ModifyTransform(m => m.Translate(100f, 0, MatrixOrder.Append));
			}
			else if (code == Key.Right)
			{
				ModifyTransform(m => m.Translate(-100f, 0, MatrixOrder.Append));
			}
			else if (code == Key.MoveSelectionUp)
			{
				SetSelectedArrowIndex(focusedSelectedArrow - 1);
			}
			else if (code == Key.ScrollLineUp)
			{
				ModifyTransform(m => m.Translate(0, 30f, MatrixOrder.Append));
			}
			else if (code == Key.MoveSelectionDown)
			{
				SetSelectedArrowIndex(focusedSelectedArrow + 1);
			}
			else if (code == Key.ScrollLineDown)
			{
				ModifyTransform(m => m.Translate(0, -30f, MatrixOrder.Append));
			}
			else if (code == Key.Plus)
			{
				ScaleTransform(view.ArrowsAreaWidth / 2, 1.1f);
			}
			else if (code == Key.Minus)
			{
				ScaleTransform(view.ArrowsAreaWidth / 2, 0.9f);
			}
			else if (code == Key.PageDown)
			{
				SetSelectedArrowIndex(focusedSelectedArrow + 10);
			}
			else if (code == Key.PageUp)
			{
				SetSelectedArrowIndex(focusedSelectedArrow - 10);
			}
			else if (code == Key.Home)
			{
				SetSelectedArrowIndex(0);
			}
			else if (code == Key.End)
			{
				SetSelectedArrowIndex(arrows.Length - 1);
			}
			else if (code == Key.Enter)
			{
				ShowSelectedArrow();
			}
			else if (code == Key.Find)
			{
				quickSearchPresenter.Focus("");
			}
			else if (code == Key.Bookmark)
			{
				ToggleBookmark().IgnoreCancellation();
			}
			else if (code == Key.FindCurrentTimeShortcut)
			{
				FindCurrentTime();
			}
			else if (code == Key.NextBookmarkShortcut)
			{
				SelectNextArrowByPredicate(a => a.IsBookmarked);
			}
			else if (code == Key.PrevNextBookmarkShortcut)
			{
				SelectPrevArrowByPredicate(a => a.IsBookmarked);
			}
		}

		void IViewModel.OnTriggerClicked(object trigger)
		{
			ShowTrigger(trigger);
		}

		void IViewModel.OnResized()
		{
			AdjustTransformToFitViewFrame();
			UpdateViewScrollBars();
		}

		void IViewModel.OnPrevUserEventButtonClicked()
		{
			SelectPrevArrowByPredicate(a => a.Type == ArrowType.UserAction);
		}

		void IViewModel.OnNextUserEventButtonClicked()
		{
			SelectNextArrowByPredicate(a => a.Type == ArrowType.UserAction);
		}

		void IViewModel.OnNextBookmarkButtonClicked()
		{
			SelectNextArrowByPredicate(a => a.IsBookmarked);
		}

		void IViewModel.OnPrevBookmarkButtonClicked()
		{
			SelectPrevArrowByPredicate(a => a.IsBookmarked);
		}

		void IViewModel.OnFindCurrentTimeButtonClicked()
		{
			FindCurrentTime();
		}

		void IViewModel.OnZoomInButtonClicked()
		{
			ScaleTransform(view.ArrowsAreaWidth / 2, 1.2f);
		}

		void IViewModel.OnZoomOutButtonClicked()
		{
			ScaleTransform(view.ArrowsAreaWidth / 2, 0.83f);
		}

		void IViewModel.OnScrolled(int? hScrollValue, int? vScrollValue)
		{
			var sceneRect = GetSceneRect();
			ModifyTransform(m => m.Translate(
				-(hScrollValue.GetValueOrDefault(-sceneRect.X) + sceneRect.X),
				-(vScrollValue.GetValueOrDefault(-sceneRect.Y) + sceneRect.Y),
				MatrixOrder.Append
			));
		}

		void IViewModel.OnGestureZoom(Point pt, float magnification)
		{
			ScaleTransform(pt.X, 1f + magnification);
		}

		bool IViewModel.OnEscapeCmdKey()
		{
			if (quickSearchPresenter.Text != "")
			{
				quickSearchPresenter.Reset();
				return true;
			}
			return false;
		}

		bool IViewModel.IsCollapseResponsesChecked => hideResponses;

		void IViewModel.OnCollapseResponsesChange(bool value)
		{
			if (value != hideResponses)
			{
				hideResponses = value;
				Update();
				changeNotification.Post();
			}
		}

		bool IViewModel.IsCollapseRoleInstancesChecked => collapseRoleInstances;

		void IViewModel.OnCollapseRoleInstancesChange(bool value)
		{
			if (value != collapseRoleInstances)
			{
				collapseRoleInstances = value;
				Update();
				changeNotification.Post();
			}
		}

		void IViewModel.OnActiveNotificationButtonClicked()
		{
			toastNotificationsPresenter.UnsuppressNotifications();
		}

		int GetMessageIndex(int y)
		{
			var logicalY = Transform(InvertTransform(), 0, y).Y + metrics.messageHeight;
			return logicalY / metrics.messageHeight;
		}

		void SetSelectedArrowIndex(int value, bool multiselectionMode = false)
		{
			value = Math.Max(0, Math.Min(value, arrows.Length - 1));
			if (multiselectionMode)
			{
				if (selectedArrows.ContainsKey(value))
				{
					if (selectedArrows.Count == 1)
					{
						return; // preserve the last selected line
					}
					else
					{
						selectedArrows.Remove(value);
						if (focusedSelectedArrow == value)
							focusedSelectedArrow = selectedArrows.Keys.FirstOrDefault(-1);
					}
				}
				else
				{
					selectedArrows.Add(value, true);
					focusedSelectedArrow = value;
				}
			}
			else
			{
				focusedSelectedArrow = value;
				selectedArrows.Clear();
				selectedArrows.Add(value, true);
			}
			EnsureArrowVisible(focusedSelectedArrow);
			changeNotification.Post();
			view.Invalidate();
		}

		private static CurrentArrowInfo MakeCurrentArrowInfo(
			Arrow arrow, StateInspectorVisualizer.IPresenter stateInspectorPresenter)
		{
			var links = new List<Tuple<object, int, int>>();
			if (arrow != null)
			{
				int linkBegin;
				var txt = new StringBuilder();
				var arrowTypeStr = "";
				if (arrow.Type == ArrowType.Request || arrow.Type == ArrowType.Response)
				{
					if (arrow.Type == ArrowType.Response)
						arrowTypeStr = "Response: ";

					txt.Append("Sent from ");
					linkBegin = txt.Length;
					txt.Append(arrow.From.DisplayName);
					if (arrow.FromLogSource != null)
						links.Add(Tuple.Create((object)new TriggerData(arrow.FromLogSource), linkBegin, txt.Length - linkBegin));

					if (arrow.FromLogSource != null)
					{
						txt.Append(" at ");
						linkBegin = txt.Length;
						txt.Append(TimeUtils.TimeDeltaToString(arrow.FromRelativeTimestamp, false));
						if (arrow.FromTrigger != null)
							links.Add(Tuple.Create((object)arrow.FromTrigger, linkBegin, txt.Length - linkBegin));
					}

					txt.Append("    received by ");
					linkBegin = txt.Length;
					txt.Append(arrow.To.DisplayName);
					if (arrow.ToLogSource != null)
						links.Add(Tuple.Create((object)new TriggerData(arrow.ToLogSource), linkBegin, txt.Length - linkBegin));

					if (arrow.ToTrigger != null)
					{
						txt.Append(" at ");
						linkBegin = txt.Length;
						txt.Append(TimeUtils.TimeDeltaToString(arrow.ToRelativeTimestamp, false));
						links.Add(Tuple.Create((object)arrow.ToTrigger, linkBegin, txt.Length - linkBegin));
					}

					if (Debugger.IsAttached)
					{
						if (arrow.TargetIdHint != null)
							txt.AppendFormat(" trgt.id={0}", arrow.TargetIdHint);
					}
				}
				else
				{
					if (arrow.Type == ArrowType.UserAction)
						arrowTypeStr = "User action: ";
					if (arrow.Type == ArrowType.APICall)
						arrowTypeStr = "API: ";
					else if (arrow.Type == ArrowType.Bookmark)
						arrowTypeStr = "Bookmark: ";
					else if (arrow.Type == ArrowType.ActivityBegin || arrow.Type == ArrowType.ActivityEnd)
						arrowTypeStr = "Activity: ";

					if (arrow.From != null)
					{
						txt.Append("Happened in ");
						linkBegin = txt.Length;
						txt.Append(arrow.From.DisplayName);
						if (arrow.FromLogSource != null)
							links.Add(Tuple.Create((object)new TriggerData(arrow.FromLogSource), linkBegin, txt.Length - linkBegin));
					}

					txt.Append(" at ");
					linkBegin = txt.Length;
					txt.Append(TimeUtils.TimeDeltaToString(arrow.FromRelativeTimestamp, false));
					if (arrow.FromTrigger != null)
						links.Add(Tuple.Create((object)arrow.FromTrigger, linkBegin, txt.Length - linkBegin));

					if (arrow.StateInspectorPropChange != null
					 && arrow.FromLogSource != null)
					{
						var changeTrigger = arrow.StateInspectorPropChange.Trigger as TextLogEventTrigger;
						if (changeTrigger != null && stateInspectorPresenter.IsObjectEventPresented(arrow.FromLogSource, changeTrigger))
						{
							txt.Append("      ");
							linkBegin = txt.Length;
							txt.Append("show");
							links.Add(Tuple.Create(
								(object)new TriggerData(arrow.FromLogSource, changeTrigger) { StateInspectorChange = arrow.StateInspectorPropChange },
								linkBegin, txt.Length - linkBegin));
							txt.Append(" on StateInspector");
						}
					}
				}

				if (arrow.Tags != null && arrow.Tags.Count > 0)
				{
					txt.Append("    tags: ");
					int tagIndex = 0;
					foreach (var tag in arrow.Tags)
					{
						if (tagIndex > 0)
							txt.Append(", ");
						var tagLinkBegin = txt.Length;
						txt.Append(tag);
						links.Add(Tuple.Create((object)new TriggerData(tag), tagLinkBegin, tag.Length));
						++tagIndex;
					}
				}

				string durationStr = GetResponseLatencyString(arrow);

				return new CurrentArrowInfo()
				{
					Caption = arrowTypeStr + arrow.FullDisplayName + durationStr,
					DescriptionText = txt.ToString(),
					DescriptionLinks = links
				};
			}
			else
			{
				return new CurrentArrowInfo()
				{
					Caption = "",
					DescriptionText = "",
					DescriptionLinks = links
				};
			}
		}

		private static string GetResponseLatencyString(Arrow arrow)
		{
			string durationStr = "";
			if (arrow.LinkedArrow != null && arrow.Type == ArrowType.Response)
			{
				var duration = arrow.ToRelativeTimestamp - arrow.LinkedArrow.FromRelativeTimestamp;
				if (duration.Ticks >= 0)
				{
					durationStr = string.Format(" ({0})",
						TimeUtils.TimeDeltaToString(duration, addPlusSign: false));
				}
			}
			return durationStr;
		}

		void EnsureArrowVisible(int arrowIdx)
		{
			var y = GetArrowY(arrowIdx);
			if (y <= 0)
			{
				ModifyTransform(m => m.Translate(0, -y + view.ArrowsAreaHeight / 3));
			}
			else if (y >= view.ArrowsAreaHeight)
			{
				ModifyTransform(m => m.Translate(0, view.ArrowsAreaHeight - y - view.ArrowsAreaHeight / 3));
			}
		}

		int GetRoleX(int roleIndex)
		{
			return Transform(roleIndex * metrics.nodeWidth, 0).X;
		}

		int GetArrowY(int arrowIndex)
		{
			return Transform(0, arrowIndex * metrics.messageHeight).Y;
		}


		void Update()
		{
			var savedSelection = SaveSelectedArrows();

			var rolesBuilder = ImmutableDictionary.CreateBuilder<string, Role>();
			var arrowsBuilder = ImmutableArray.CreateBuilder<Arrow>();

			var externalRolesProperties = GetExternalRolesProperties(model.MetadataEntries);
			Role getRole(Node ni) => GetRole(ni, collapseRoleInstances, rolesBuilder, model.MetadataEntries);

			AddRoles(getRole, model.InternodeMessages, model.UnpairedMessages, model.StateComments);
			AddInternodeMessages(getRole, model.InternodeMessages, arrowsBuilder);
			AddComments(model.TimelineComments, model.StateComments, getRole, arrowsBuilder);

			// it should follow comments because comments can add new roles used by unpaired messages matcher
			AddUnpairedMessages(model.UnpairedMessages, arrowsBuilder,
				externalRolesProperties, getRole, rolesBuilder);

			this.hiddenLinkableResponses = CollectHiddenLinkableResponceArrows(arrowsBuilder, hideResponses);

			UpdateTags(arrowsBuilder);
			RemoveHiddenArrows(arrowsBuilder, 
				quickSearchPresenter.Text, persistentState.TagsPredicate, hideResponses, collapseRoleInstances);

			SortArrows(arrowsBuilder, finalSort: false);
			if (AddBookmarks(bookmarks.Items, rolesBuilder.Values, arrowsBuilder) > 0)
				SortArrows(arrowsBuilder, finalSort: false);
			AddNonhorizontalArrows(arrowsBuilder); // note: bookmarks must be added before this step

			SortArrows(arrowsBuilder, finalSort: false);
			FindLinkedArrows(arrowsBuilder);
			FixUnpairedLinkedArrows(arrowsBuilder);

			RemoveHiddenRoles(rolesBuilder, arrowsBuilder);

			SortArrows(arrowsBuilder, finalSort: true);
			SortRoles(rolesBuilder, arrowsBuilder);

			FindOverlappingNonHorizonalArrows(arrowsBuilder);
			InitializeOffsets(arrowsBuilder, rolesBuilder.Values, metrics);

			this.arrows = arrowsBuilder.ToImmutable();
			this.roles = rolesBuilder.ToImmutable();

			AdjustTransformToFitViewFrame();
			UpdateViewScrollBars();

			RestoreSelectedArrows(savedSelection);

			view.Invalidate();
		}

		List<Arrow> SaveSelectedArrows()
		{
			return selectedArrows.Keys.Where(i => i >= 0 && i < arrows.Length).Select(i => arrows[i]).ToList();
		}

		void RestoreSelectedArrows(List<Arrow> oldSelection)
		{
			selectedArrows.Clear();
			foreach (var idx in oldSelection.Select(a => arrows.LowerBound(a, arrowComparer)).Where(i => i < arrows.Length))
				selectedArrows.Add(idx, true);
			if (selectedArrows.Count == 0 && arrows.Length > 0)
				selectedArrows.Add(0, true);
			focusedSelectedArrow = selectedArrows.FirstOrDefault().Key;
			EnsureArrowVisible(focusedSelectedArrow);
			changeNotification.Post();
		}

		static ImmutableDictionary<string, Arrow> CollectHiddenLinkableResponceArrows(IEnumerable<Arrow> arrows, bool hideResponses)
		{
			var result = ImmutableDictionary.CreateBuilder<string, Arrow>();
			if (hideResponses)
			{
				foreach (var rsp in arrows.Where(a => a.Type == ArrowType.Response && a.LinkedArrowId != null))
					result[rsp.LinkedArrowId] = rsp;
			}
			return result.ToImmutable();
		}

		static void SortRoles(ImmutableDictionary<string, Role>.Builder roles, IEnumerable<Arrow> arrows)
		{
			int realRoleMaxIndex = 0;
			int virtualRoleMaxIndex = roles.Values.Count(r => r.LogSources.Count != 0);

			Action<Role> initIndex = role =>
			{
				if (role != null && role.DisplayIndex < 0)
					role.DisplayIndex = role.LogSources.Count != 0 ? realRoleMaxIndex++ : virtualRoleMaxIndex++;
			};

			foreach (var a in arrows)
			{
				initIndex(a.From);
				initIndex(a.To);
			}

			foreach (var r in roles.Values)
				initIndex(r);
		}

		static void SortArrows(ImmutableArray<Arrow>.Builder arrows, bool finalSort)
		{
			arrows.Sort(arrowComparer);
			if (!finalSort)
				return;
			int arrowIdx = 0;
			DateTime prevTime = new DateTime();
			DateTime timeOrigin = new DateTime();
			foreach (var arrow in arrows)
			{
				if (arrowIdx == 0)
					timeOrigin = arrow.FromTimestamp;
				arrow.Index = arrowIdx;
				arrow.Delta = arrowIdx > 0 ? (arrow.FromTimestamp - prevTime) : new TimeSpan?();
				arrow.FromRelativeTimestamp = arrow.FromTimestamp - timeOrigin;
				arrow.ToRelativeTimestamp = arrow.ToTimestamp - timeOrigin;
				++arrowIdx;
				prevTime = arrow.FromTimestamp;
			}
		}

		static void FindLinkedArrows(IEnumerable<Arrow> arrows)
		{
			var linkedArrowsMap = new Dictionary<string, Arrow>();
			foreach (var arrow in arrows.Where(a => a.LinkedArrowId != null))
			{
				if (arrow.Type == ArrowType.Request || arrow.Type == ArrowType.ActivityBegin)
				{
					linkedArrowsMap[arrow.LinkedArrowId] = arrow;
				}
				else if (arrow.Type == ArrowType.Response || arrow.Type == ArrowType.ActivityEnd)
				{
					Arrow linkedArrow;
					if (linkedArrowsMap.TryGetValue(arrow.LinkedArrowId, out linkedArrow))
					{
						arrow.LinkedArrow = linkedArrow;
						linkedArrow.LinkedArrow = arrow;
						linkedArrowsMap.Remove(arrow.LinkedArrowId);
					}
				}
			}
		}

		static void InitializeOffsets(
			IEnumerable<Arrow> arrows,
			IEnumerable<Role> roles,
			MetricsCache metrics)
		{
			int GetArrowEndOffset(int x)
			{
				if (x == 0)
					return 0;
				return Math.Sign(x) * (metrics.executionOccurrenceWidth / 2 + (Math.Abs(x) - 1) * metrics.executionOccurrenceLevelOffset);
			}

			foreach (var arrow in arrows)
			{
				if (arrow.From == null || arrow.To == null)
					continue;
				int arrowDirection = 0;
				if (arrow.Type == ArrowType.Request || arrow.Type == ArrowType.Response)
					arrowDirection = Math.Sign(arrow.To.DisplayIndex - arrow.From.DisplayIndex);
				else if (arrow.Type == ArrowType.ActivityBegin)
					arrowDirection = 1;
				else if (arrow.Type == ArrowType.ActivityEnd)
					arrowDirection = -1;
				if (arrowDirection != 0)
				{
					if (arrow.LinkedArrow != null && (arrow.Type == ArrowType.Request || arrow.Type == ArrowType.ActivityBegin))
						BeginOffset(arrow, arrowDirection, arrow.To.CurrentExecutionOccurencesOffsets, true);
					if (arrow.NonHorizontalConnectedArrow != null && arrow.Index < arrow.NonHorizontalConnectedArrow.Index)
						BeginOffset(arrow, arrowDirection, arrow.To.CurrentNonHorizontalArrowsOffsets, false);
				}
				bool debugOffsets = false;
				if (debugOffsets)
				{
					Func<Dictionary<Arrow, Offset>, string> dumpCurrentOffsets = offsets =>
						offsets.Values.Aggregate(new StringBuilder(), (s, off) => s.AppendFormat("{0},", off.Level), sb => sb.ToString());
					arrow.FullDisplayName = string.Format(
						"From.EO: {0}; From.NH:{1}; To.EO:{2}; To.NH:{3}",
						dumpCurrentOffsets(arrow.From.CurrentExecutionOccurencesOffsets),
						dumpCurrentOffsets(arrow.From.CurrentNonHorizontalArrowsOffsets),
						dumpCurrentOffsets(arrow.To.CurrentExecutionOccurencesOffsets),
						dumpCurrentOffsets(arrow.To.CurrentNonHorizontalArrowsOffsets)
					);
				}
				if (arrowDirection != 0)
				{
					arrow.FromOffset = GetArrowEndOffset(arrowDirection * arrow.From.CurrentExecutionOccurencesOffsets.Values.Where(
						eo => eo.TestLevelSign(arrowDirection)).Select(eo => Math.Abs(eo.Level) + 1).DefaultIfEmpty().Max());
					arrow.ToOffset = GetArrowEndOffset(-arrowDirection * arrow.To.CurrentExecutionOccurencesOffsets.Values.Where(
						eo => eo.TestLevelSign(-arrowDirection)).Select(eo => Math.Abs(eo.Level) + 1).DefaultIfEmpty().Max());
					if (GetNonHorizontalArrowRole(arrow) == NonHorizontalArrowRole.Sender)
					{
						var x = arrow.To.CurrentNonHorizontalArrowsOffsets.Values.Count(eo => eo.TestLevelSign(-arrowDirection));
						arrow.NonHorizontalConnectorOffset = -arrowDirection * (metrics.executionOccurrenceWidth * 3 + x * metrics.parallelNonHorizontalArrowsOffset);
					}
				}
				else
				{
					var loopDirection = 1;
					arrow.FromOffset = arrow.ToOffset = GetArrowEndOffset(loopDirection * arrow.From.CurrentExecutionOccurencesOffsets.Values.Count(
						eo => eo.TestLevelSign(loopDirection)));
				}
				if (arrowDirection != 0)
				{
					if (arrow.NonHorizontalConnectedArrow != null && arrow.Index > arrow.NonHorizontalConnectedArrow.Index)
						EndOffset(arrow, arrow.To.CurrentNonHorizontalArrowsOffsets, arrow.NonHorizontalConnectedArrow, null);
					if (arrow.LinkedArrow != null && (arrow.Type == ArrowType.Response || arrow.Type == ArrowType.ActivityEnd))
						EndOffset(arrow, arrow.From.CurrentExecutionOccurencesOffsets, arrow.LinkedArrow, arrow.From.ExecutionOccurrences);
				}
			}
			foreach (var role in roles)
			{
				role.ExecutionOccurrences.Sort((eo1, eo2) => Math.Abs(eo1.Level) - Math.Abs(eo2.Level));
			}
		}

		private static void EndOffset(Arrow arrow, Dictionary<Arrow, Offset> offsetsContainer, Arrow endOffsetKey, List<Offset> finalizedOffsetsContainer)
		{
			Offset eo;
			if (offsetsContainer.TryGetValue(endOffsetKey, out eo))
			{
				eo.End = arrow;
				offsetsContainer.Remove(endOffsetKey);
				if (finalizedOffsetsContainer != null)
				{
					finalizedOffsetsContainer.Add(eo);
				}
			}
		}

		private static void BeginOffset(Arrow arrow, int arrowDirection, Dictionary<Arrow, Offset> offsetsContainer, bool allowZeroLevel)
		{
			int newOffsetLevel = allowZeroLevel ? 0 : -arrowDirection;
			while (offsetsContainer.Values.FirstOrDefault(eo => eo.Level == newOffsetLevel) != null)
				newOffsetLevel -= arrowDirection;
			offsetsContainer.Add(arrow, new Offset()
			{
				Level = newOffsetLevel,
				Begin = arrow
			});
		}

		static private Role GetUnpairedMessageRemoteRole(
			M.NetworkMessageEvent messageEvent,
			ImmutableDictionary<string, ExternalRolesProperties> externalRolesProperties,
			ImmutableDictionary<string, Role>.Builder roles
		)
		{
			string remoteRoleKey = null;
			string remoteRoleName = null;

			var http = messageEvent as M.HttpMessage;
			if (http != null)
			{
				if (http.TargetIdHint != null)
				{
					// take the only entry in order not to bind to ambiguous role. 
					// Example is when target hint is UI version and there are several roles with same UI Version.
					var existingRoles = roles.Values.Where(r => r.UnpairedMessagesTargetIds.Contains(http.TargetIdHint)).ToArray();
					if (existingRoles.Length == 1)
						return existingRoles[0];

					var externalRole = MakeExternalRoleFromProperties(http.TargetIdHint, externalRolesProperties, roles);
					if (externalRole != null)
						return externalRole;
				}

				if ((http.MessageDirection == M.MessageDirection.Outgoing && http.MessageType == M.MessageType.Request)
				 || (http.MessageDirection == M.MessageDirection.Incoming && http.MessageType == M.MessageType.Response))
				{
					Uri targetUri;
					if (!Uri.TryCreate(http.Url, UriKind.Absolute, out targetUri))
						return null;
					remoteRoleKey = "external." + targetUri.Host;
					remoteRoleName = targetUri.Host;
				}
			}
			else
			{
				var net = messageEvent as M.NetworkMessageEvent;
				if (remoteRoleKey == null && net != null && net.RemoteSideId != null && net.EventType != null)
				{
					remoteRoleKey = string.Format("{0}.{1}", net.EventType, net.RemoteSideId);
					remoteRoleName = string.Format("{0} {1}", net.EventType, net.RemoteSideId);
				}
			}

			remoteRoleKey = remoteRoleKey ?? "unknown";
			remoteRoleName = remoteRoleName ?? "unknown";

			Role remoteRole;
			if (!roles.TryGetValue(remoteRoleKey, out remoteRole))
				roles.Add(remoteRoleKey, remoteRole = new Role(remoteRoleName, null));

			return remoteRole;
		}

		static Role MakeExternalRoleFromProperties (
			string externalRoleId,
			ImmutableDictionary<string, ExternalRolesProperties> externalRolesProperties,
			ImmutableDictionary<string, Role>.Builder roles)
		{
			ExternalRolesProperties externalRoleProps;
			if (!externalRolesProperties.TryGetValue (externalRoleId, out externalRoleProps))
				return null;
			var roleId = "external.id." + externalRoleId;
			Role externalRole;
			if (roles.TryGetValue(roleId, out externalRole))
				return externalRole;
			externalRole = new Role(externalRoleProps.DisplayName, externalRoleProps.Ids);
			roles.Add(roleId, externalRole);
			return externalRole;
		}

		static private void AddUnpairedMessages(
			IEnumerable<SD.Message> unpairedMessages,
			ImmutableArray<Arrow>.Builder arrows,
			ImmutableDictionary<string, ExternalRolesProperties> externalRolesProperties,
			Func<Node, Role> getRole,
			ImmutableDictionary<string, Role>.Builder roles
		)
		{
			foreach (var unpairedMessageInfo in unpairedMessages)
			{
				var messageEvent = unpairedMessageInfo.Event;
				var networkEvt = messageEvent as M.NetworkMessageEvent;
				if (networkEvt == null)
					continue;

				Role messageRole = getRole(unpairedMessageInfo.Node);
				Role remoteRole = GetUnpairedMessageRemoteRole(networkEvt, externalRolesProperties, roles);
				if (remoteRole == null)
					continue;

				string shortDisplayName = GetUnpairedMessageShortDisplayName(messageEvent);

				var ts = unpairedMessageInfo.Timestamp;
				var arrow = new Arrow()
				{
					Type = networkEvt.MessageType == M.MessageType.Response ?
						ArrowType.Response : ArrowType.Request,
					ShortDisplayName = shortDisplayName,
					FullDisplayName = networkEvt.DisplayName,
					FromTimestamp = ts,
					ToTimestamp = ts,
					LinkedArrowId = networkEvt.MessageId,
					Tags = networkEvt.Tags,
					TargetIdHint = networkEvt.TargetIdHint
				};
				if (unpairedMessageInfo.Direction == M.MessageDirection.Outgoing)
				{
					arrow.From = messageRole;
					arrow.FromTrigger = MakeTriggerData(unpairedMessageInfo);
					arrow.FromLogSource = unpairedMessageInfo.LogSource;
					arrow.To = remoteRole;
					arrow.Color = GetArrowColor(unpairedMessageInfo);
				}
				else
				{
					arrow.From = remoteRole;
					arrow.To = messageRole;
					arrow.ToTrigger = MakeTriggerData(unpairedMessageInfo);
					arrow.ToLogSource = unpairedMessageInfo.LogSource;
					arrow.Color = GetArrowColor(unpairedMessageInfo);
				}
				arrows.Add(arrow);
			}
		}

		private static string GetUnpairedMessageShortDisplayName(M.Event messageEvent)
		{
			string shortDisplayName = messageEvent.DisplayName;
			var http = messageEvent as M.HttpMessage;
			if (http != null)
			{
				shortDisplayName = MakeDisplayName(http);
			}
			return shortDisplayName;
		}

		static void AddComments(
			IEnumerable<TimelineComment> timelineComments,
			IEnumerable<StateComment> stateComments,
			Func<Node, Role> getRole,
			ImmutableArray<Arrow>.Builder arrows
		)
		{
			foreach (var comment in timelineComments)
			{
				var role = getRole(comment.Node);
				var trigger = MakeTriggerData(comment);
				var ts = comment.Timestamp;
				ArrowType arrowType;
				string linkedArrowId = null;
				TL.ProcedureEvent prc;
				if (comment.Event is TL.UserActionEvent)
					arrowType = ArrowType.UserAction;
				else if (comment.Event is TL.APICallEvent)
					arrowType = ArrowType.APICall;
				else if ((prc = comment.Event as TL.ProcedureEvent) != null)
				{
					if (prc.Type == TL.ActivityEventType.Begin)
						arrowType = ArrowType.ActivityBegin;
					else
						arrowType = ArrowType.ActivityEnd;
					linkedArrowId = "timeline_activity." + prc.ActivityId;
				}
				else
					continue;
				var arrow = new Arrow()
				{
					Type = arrowType,
					ShortDisplayName = comment.Event.DisplayName,
					FullDisplayName = comment.Event.DisplayName,
					FromTimestamp = ts,
					ToTimestamp = ts,
					From = role,
					To = role,
					FromTrigger = trigger,
					FromLogSource = comment.LogSource,
					ToLogSource = comment.LogSource,
					Tags = comment.Event.Tags,
					LinkedArrowId = linkedArrowId
				};
				arrows.Add(arrow);
			}
			foreach (var comment in stateComments)
			{
				var propChange = comment.Event as SI.PropertyChange;
				if (propChange == null)
					continue;
				var role = getRole(comment.Node);
				var trigger = MakeTriggerData(comment);
				var ts = comment.Timestamp;
				var arrow = new Arrow()
				{
					Type = ArrowType.StateChange,
					ShortDisplayName = string.Format("{0}.{1}->{2}", propChange.ObjectId, propChange.PropertyName, propChange.Value),
					FullDisplayName = string.Format(
						propChange.OldValue != null ? "{0}.{1} changed from {2} to {3}" : "{0}.{1} changed to {3}",
						propChange.ObjectId, propChange.PropertyName, propChange.OldValue, propChange.Value),
					FromTimestamp = ts,
					ToTimestamp = ts,
					From = role,
					To = role,
					FromTrigger = trigger,
					FromLogSource = comment.LogSource,
					ToLogSource = comment.LogSource,
					Tags = comment.Event.Tags,
					StateInspectorPropChange = propChange
				};
				arrows.Add(arrow);
			}
		}

		static int FixUnpairedLinkedArrows(IEnumerable<Arrow> arrows)
		{
			int fixedCount = 0;
			foreach (var a in arrows.Where(a => a.LinkedArrow != null && a.LinkedArrow.FromLogSource != null && a.ToLogSource != null))
			{
				if (a.FromLogSource == null)
				{
					a.From = a.LinkedArrow.To;
					++fixedCount;
				}
				else if (a.ToLogSource == null)
				{
					a.To = a.LinkedArrow.From;
					++fixedCount;
				}
			}
			return fixedCount;
		}

		static int AddBookmarks(
			IEnumerable<IBookmark> bookmarks,
			IEnumerable<Role> roles,
			ImmutableArray<Arrow>.Builder arrows
		)
		{
			int bookmarksAdded = 0;
			var lsToRole =
				roles
				.SelectMany(r => r.LogSources.Select(ls => new { Role = r, LogSource = ls }))
				.ToLookup(x => x.LogSource, x => x.Role);
			foreach (var bmk in bookmarks)
			{
				if (bmk.Thread.IsDisposed)
					continue;
				var ts = bmk.Time.ToUnspecifiedTime();
				var ls = bmk.GetLogSource();
				var role = lsToRole[ls].FirstOrDefault();
				var pos = bmk.Position;
				if (ls != null && role != null)
				{
					var p1 = ListUtils.BinarySearch(arrows, 0, arrows.Count,
						a => ArrowComparer.Compare(a, ts, ls, pos) < 0);
					var p2 = ListUtils.BinarySearch(arrows, 0, arrows.Count,
						a => ArrowComparer.Compare(a, ts, ls, pos) <= 0);
					if (p2 == p1 + 1)
					{
						arrows[p1].IsBookmarked = true;
						continue;
					}
					var trigger = MakeTriggerData(bmk);
					var arrow = new Arrow()
					{
						Type = ArrowType.Bookmark,
						ShortDisplayName = bmk.DisplayName,
						FullDisplayName = bmk.DisplayName,
						FromTimestamp = ts,
						ToTimestamp = ts,
						From = role,
						To = role,
						FromTrigger = trigger,
						FromLogSource = ls,
						ToLogSource = ls,
						IsBookmarked = true,
						Visible = true
					};
					arrows.Add(arrow);
					++bookmarksAdded;
				}
			}
			return bookmarksAdded;
		}

		static TriggerData MakeTriggerData(SD.Message msgInfo)
		{
			TextLogEventTrigger textLogTrigger = msgInfo.Event.Trigger as TextLogEventTrigger;
			if (textLogTrigger == null)
				return null;
			var logSource = msgInfo.LogSource;
			if (logSource == null)
				return null;
			return new TriggerData(logSource, textLogTrigger);
		}

		static TriggerData MakeTriggerData(TimelineComment comment)
		{
			TextLogEventTrigger textLogTrigger = comment.Event.Trigger as TextLogEventTrigger;
			if (textLogTrigger == null)
				return null;
			var logSource = comment.LogSource;
			if (logSource == null)
				return null;
			return new TriggerData(logSource, textLogTrigger);
		}

		static TriggerData MakeTriggerData(StateComment comment)
		{
			TextLogEventTrigger textLogTrigger = comment.Event.Trigger as TextLogEventTrigger;
			if (textLogTrigger == null)
				return null;
			var logSource = comment.LogSource;
			if (logSource == null)
				return null;
			return new TriggerData(logSource, textLogTrigger);
		}

		static TriggerData MakeTriggerData(IBookmark bmk)
		{
			TextLogEventTrigger textLogTrigger = new TextLogEventTrigger(bmk);
			return new TriggerData(bmk.GetLogSource(), textLogTrigger);
		}

		static void AddRoles(
			Func<Node, Role> getRole,
			IEnumerable<InternodeMessage> internodeMessages,
			IEnumerable<SD.Message> unpairedMessages,
			IEnumerable<StateComment> stateComments)
		{
			// make sure roles are pre-created to make searching by TargetIdHint possible

			foreach (var internodeMsg in internodeMessages)
			{
				getRole(internodeMsg.OutgoingMessage.Node);
				getRole(internodeMsg.IncomingMessage.Node);
			}
			foreach (var unpairedMessageInfo in unpairedMessages)
			{
				getRole(unpairedMessageInfo.Node); 
			}
			foreach (var comment in stateComments)
			{
				getRole(comment.Node);
			}
		}

		static ImmutableDictionary<string, ExternalRolesProperties> GetExternalRolesProperties(IReadOnlyCollection<MetadataEntry> metadataEntries)
		{
			return ImmutableDictionary.CreateRange(
				metadataEntries
				.Select(e => new { e = e, match = Regex.Match(e.Event.Key, "^" + M.MetadataKeys.ExternalRolePropertyPrefix + @"\/(?<k>([^\/]|\/\/)+)\/(?<val>\w+)$", RegexOptions.ExplicitCapture) })
				.Where(x => x.match.Success)
				.Select(x => new { roleId = x.match.Groups[1].Value.Replace("//", "/"), prop = x.match.Groups[2].Value, value = x.e.Event.Value })
				.GroupBy(x => x.roleId)
				.Select(x => new ExternalRolesProperties(x.Select(p => new KeyValuePair<string, string>(p.prop, p.value))))
				.Where(p => p.IsGood)
				.SelectMany(p => p.Ids.Select(id => new { id = id, value = p}))
				.ToDictionarySafe(x => x.id, x => x.value, (p1, p2) => { p1.Ids.UnionWith(p2.Ids); return p1; })
			);
		}

		static private void AddInternodeMessages(
			Func<Node, Role> getRole,
			IEnumerable<InternodeMessage> internodeMessages,
			ImmutableArray<Arrow>.Builder arrows
		)
		{
			foreach (var internodeMsg in internodeMessages)
			{
				arrows.Add(new Arrow()
				{
					Type = internodeMsg.OutgoingMessageType == M.MessageType.Response ? ArrowType.Response : ArrowType.Request,
					FromTimestamp = internodeMsg.OutgoingMessage.Timestamp,
					ToTimestamp = internodeMsg.IncomingMessage.Timestamp,
					From = getRole(internodeMsg.OutgoingMessage.Node),
					To = getRole(internodeMsg.IncomingMessage.Node),
					FromTrigger = MakeTriggerData(internodeMsg.OutgoingMessage),
					ToTrigger = MakeTriggerData(internodeMsg.IncomingMessage),
					FromLogSource = internodeMsg.OutgoingMessage.LogSource,
					ToLogSource = internodeMsg.IncomingMessage.LogSource,
					ShortDisplayName = MakeDisplayName(internodeMsg),
					FullDisplayName = internodeMsg.OutgoingMessage.Event.DisplayName,
					LinkedArrowId = internodeMsg.OutgoingMessageId,
					Tags = internodeMsg.OutgoingMessage.Event.Tags,
					Color = GetArrowColor(internodeMsg.OutgoingMessage, internodeMsg.IncomingMessage)
				});
			}
		}

		static ArrowColor GetArrowColor(params SD.Message[] msgs)
		{
			foreach (var m in msgs)
			{
				var http = m.Event as M.HttpMessage;
				if (http != null && http.StatusCode.GetValueOrDefault(200) >= 400)
					return ArrowColor.Error;
			}
			return ArrowColor.Normal;
		}

		static int AddNonhorizontalArrows(ImmutableArray<Arrow>.Builder arrows)
		{
			var nonhorizontalArrows = new List<Arrow>();
			var arrowsToRemove = new List<int>();
			for (var arrowIdx = 0; arrowIdx < arrows.Count; ++arrowIdx)
			{
				var arrow = arrows[arrowIdx];
				if (arrow.Type != ArrowType.Request && arrow.Type != ArrowType.Response)
					continue;
				var recdTimeIndex = ListUtils.BinarySearch(arrows, 0, arrows.Count,
					a => ArrowComparer.Compare(a, arrow.ToTimestamp, arrow.ToTrigger) <= 0);
				if (recdTimeIndex - arrowIdx > 1)
				{
					var recvArrow = new Arrow()
					{
						Type = arrow.Type,
						FromTimestamp = arrow.ToTimestamp,
						ToTimestamp = arrow.ToTimestamp,
						From = arrow.From,
						To = arrow.To,
						FromTrigger = null,
						ToTrigger = arrow.ToTrigger,
						FromLogSource = arrow.FromLogSource,
						ToLogSource = arrow.ToLogSource,
						ShortDisplayName = "",
						FullDisplayName = arrow.FullDisplayName,
						LinkedArrowId = arrow.LinkedArrowId,
						Tags = arrow.Tags,
						NonHorizontalConnectedArrow = arrow
					};
					nonhorizontalArrows.Add(recvArrow);
					arrow.NonHorizontalConnectedArrow = recvArrow;
					recvArrow.NonHorizontalConnectedArrow = arrow;

					var recdTimeArrow = arrows[recdTimeIndex - 1];
					if (recdTimeArrow.IsBookmarked
					 && arrowComparer.Compare(arrows[recdTimeIndex - 1], recvArrow) == 0)
					{
						recvArrow.IsBookmarked = true;
						arrowsToRemove.Add(recdTimeIndex - 1);
					}
				}
			}
			foreach (var i in arrowsToRemove.OrderByDescending(x => x))
				arrows.RemoveAt(i);
			arrows.AddRange(nonhorizontalArrows);

			return nonhorizontalArrows.Count + arrowsToRemove.Count;
		}

		static private void FindOverlappingNonHorizonalArrows(IReadOnlyList<Arrow> arrows)
		{
			foreach (var arrow in arrows)
			{
				if (arrow.NonHorizontalConnectedArrow != null)
				{
					for (int i = arrow.Index + 1; i <= arrow.NonHorizontalConnectedArrow.Index; ++i)
					{
						var a = arrows[i];
						if (a.OverlappingNonHorizontalArrows == null)
							a.OverlappingNonHorizontalArrows = new List<Arrow>();
						a.OverlappingNonHorizontalArrows.Add(arrow);
					}
				}
			}
		}

		static string MakeDisplayName(M.HttpMessage http)
		{
			string status = "";
			if (http.StatusCode != null)
			{
				var sb = new StringBuilder();
				sb.AppendFormat("{0} ", http.StatusCode.Value);
				if (http.StatusComment != null)
					sb.AppendFormat("({0}) ", http.StatusComment);
				status = sb.ToString();
			}
			return string.Format("{2}{0} {1}", http.Method, ShortenUri(http.Url), status);
		}

		static string MakeDisplayName(InternodeMessage msg)
		{
			var http = msg.OutgoingMessage.Event as M.HttpMessage;
			if (http != null)
			{
				return MakeDisplayName(http);
			}
			return msg.OutgoingMessage.Event.DisplayName;
		}

		static string ShortenUri(string uriStr)
		{
			Uri uri;
			if (!Uri.TryCreate(uriStr, UriKind.RelativeOrAbsolute, out uri))
				return uriStr;
			if (!uri.IsAbsoluteUri)
				return uriStr;
			return string.Format("{0}...{1}", uri.Host, uri.Segments.LastOrDefault());
		}

		static Role GetRole(
			Node ni,
			bool collapseRoleInstances,
			ImmutableDictionary<string, Role>.Builder roles,
			IEnumerable<MetadataEntry> metadataEntries
		)
		{
			string key;
			string displayName;
			if (collapseRoleInstances && !string.IsNullOrEmpty(ni.RoleName))
			{
				key = ni.RoleName;
				displayName = ni.RoleName;
			}
			else
			{
				key = ni.Id;
				displayName = ni.RoleInstanceName;
			}
			Role role, existingRole;
			if (!roles.TryGetValue(key, out existingRole))
			{
				var unpairedMessagesTargetIds = new HashSet<string>(
					metadataEntries
					.Where(m => m.Node == ni && m.Event.Key == M.MetadataKeys.TargetRoleIdHint && !string.IsNullOrEmpty(m.Event.Value))
					.Select(m => m.Event.Value)
				);
				role = new Role(displayName, unpairedMessagesTargetIds);
				roles.Add(key, role);
			}
			else
			{
				role = existingRole;
			}
			if (existingRole == null    // initing brand new object
			 || collapseRoleInstances)  // OR role represents many nodes. it should aggregate log sources from all its nodes.
			{
				foreach (var ls in ni.LogSources)
					role.LogSources.Add(ls);
			}
			return role;
		}

		private void ScaleTransform(int centerX, float factor)
		{
			var pt = Transform(InvertTransform(), centerX, 0);
			ModifyTransform(m =>
			{
				m.Translate(pt.X, 0);
				m.Scale(factor, 1.0f);
				m.Translate(-pt.X, 0);
			});
		}

		Matrix InvertTransform()
		{
			var tmp = transform.Clone();
			tmp.Invert();
			return tmp;
		}

		bool IsValidArrowIndex(int value)
		{
			return value >= 0 && value < arrows.Length;
		}

		Arrow GetSelectedArrow()
		{
			return IsValidArrowIndex(focusedSelectedArrow) ? arrows[focusedSelectedArrow] : null;
		}

		void ShowSelectedArrow()
		{
			Arrow a = GetSelectedArrow();
			if (a != null)
			{
				ShowTrigger(a.FromTrigger ?? a.ToTrigger);
				loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
			}
		}

		async Task ToggleBookmark()
		{
			Arrow a = GetSelectedArrow();
			if (a != null)
			{
				var trigger = a.FromTrigger ?? a.ToTrigger;
				if (trigger != null)
				{
					var bmk = await trigger.Source.CreateTogglableBookmark(
						bookmarks.Factory,
						CreateTriggerBookmark(trigger),
						CancellationToken.None);
					if (bmk != null)
					{
						bookmarks.ToggleBookmark(bmk);
					}
				}
			}
		}

		void ShowTrigger(object triggerData)
		{
			var data = triggerData as TriggerData;
			if (data != null)
				ShowTrigger(data);
		}

		IBookmark CreateTriggerBookmark(TriggerData triggerData)
		{
			return bookmarks.Factory.CreateBookmark(
				triggerData.Trigger.Timestamp.Adjust(triggerData.Source.TimeOffsets),
				triggerData.Source.GetSafeConnectionId(),
				triggerData.Trigger.StreamPosition,
				0
			);
		}

		void ShowTrigger(TriggerData triggerData)
		{
			if (triggerData.Tag != null)
			{
				tagsListPresenter.Edit(triggerData.Tag);
				return;
			}
			if (triggerData.Source == null || triggerData.Source.IsDisposed)
				return;
			if (triggerData.Trigger != null)
			{
				if (triggerData.StateInspectorChange == null)
				{
					presentersFacade.ShowMessage(
						CreateTriggerBookmark(triggerData),
						BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet
					);
				}
				else
				{
					Func<StateInspectorVisualizer.IVisualizerNode, int> disambiguationFunction = io =>
						triggerData.StateInspectorChange.ObjectId.Contains(io.Id) ? 1 : 0;
					if (stateInspectorPresenter.TrySelectObject(triggerData.Source, triggerData.Trigger, disambiguationFunction))
						stateInspectorPresenter.Show();
				}
			}
			else
			{
				presentersFacade.ShowLogSource(triggerData.Source);
			}
		}

		private Tuple<int, int> GetFocusedMessageRange()
		{
			var msg = loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;
			if (msg != null)
			{
				var ls = msg.GetLogSource();
				if (ls != null)
				{
					var t = msg.Time.ToUnspecifiedTime();
					var p1 = ListUtils.BinarySearch(arrows, 0, arrows.Length,
						a => ArrowComparer.Compare(a, t, ls, msg.Position) < 0);
					var p2 = ListUtils.BinarySearch(arrows, 0, arrows.Length,
						a => ArrowComparer.Compare(a, t, ls, msg.Position) <= 0);
					return Tuple.Create(p1, p2);
				}
			}
			return null;
		}

		private void UpdateTags(IEnumerable<Arrow> arrows)
		{
			var tmp = ImmutableHashSet.CreateRange(arrows.SelectMany(a => a.Tags));
			if (!availableTags.SetEquals(tmp))
			{
				availableTags = tmp;
			}
		}

		static private void RemoveHiddenArrows(ImmutableArray<Arrow>.Builder arrows,
			string filter,
			TagsPredicate tags,
			bool hideResponses,
			bool collapseRoleInstances)
		{
			foreach (var a in arrows)
			{
				a.Visible = a.Tags.Count == 0 || tags.IsMatch(a.Tags);
				if (a.Visible && !string.IsNullOrEmpty(filter))
				{
					a.Visible = 
						a.FullDisplayName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 || 
						a.ShortDisplayName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
				}
				if (a.Visible && hideResponses && a.Type == ArrowType.Response)
				{
					a.Visible = false;
				}
				if (a.Visible && collapseRoleInstances && a.To == a.From && a.FromLogSource != a.ToLogSource)
				{
					a.Visible = false; // skip inter-instance messages within one collapsed role
				}
			}
			arrows.RemoveAll(a => !a.Visible);
		}

		private static void RemoveHiddenRoles(ImmutableDictionary<string, Role>.Builder roles, IEnumerable<Arrow> arrows)
		{
			foreach (var a in arrows)
			{
				if (a.Visible)
				{
					if (a.From != null)
						a.From.Visible = true;
					if (a.To != null)
						a.To.Visible = true;
				}
			}
			foreach (var hiddenRoleKey in roles.Where(r => !r.Value.Visible).Select(r => r.Key).ToArray())
			{
				roles.Remove(hiddenRoleKey);
			}
		}

		static ArrowDrawMode ToDrawMode(ArrowType type)
		{
			switch (type)
			{
				case ArrowType.Request: return ArrowDrawMode.Arrow;
				case ArrowType.Response: return ArrowDrawMode.DottedArrow;
				case ArrowType.StateChange: return ArrowDrawMode.StateChange;
				case ArrowType.UserAction: return ArrowDrawMode.UserAction;
				case ArrowType.APICall: return ArrowDrawMode.APICall;
				case ArrowType.Bookmark: return ArrowDrawMode.Bookmark;
				case ArrowType.ActivityBegin: return ArrowDrawMode.ActivityLabel;
				case ArrowType.ActivityEnd: return ArrowDrawMode.ActivityLabel;
				default: return ArrowDrawMode.Arrow;
			}
		}

		void ModifyTransform(Action<Matrix> action)
		{
			action(transform);
			AdjustTransformToFitViewFrame();
			UpdateViewScrollBars();
			view.Invalidate();
		}

		void AdjustTransformToFitViewFrame()
		{
			var viewRect = GetViewRect();
			var sceneRect = GetSceneRect();

			Func<int, int, int, int, int> getDelta = (viewX1, viewX2, sceneX1, sceneX2) =>
			{
				if ((sceneX2 - sceneX1) > (viewX2 - viewX1))
				{
					if (sceneX1 > viewX1)
						return viewX1 - sceneX1;
					else if (sceneX2 < viewX2)
						return viewX2 - sceneX2;
				}
				else
				{
					if (sceneX1 < viewX1)
						return viewX1 - sceneX1;
					else if (sceneX2 > viewX2)
						return viewX2 - sceneX2;
				}
				return 0;
			};

			int deltaX = getDelta(viewRect.X, viewRect.Right, sceneRect.X, sceneRect.Right);
			int deltaY = getDelta(viewRect.Y, viewRect.Bottom, sceneRect.Y, sceneRect.Bottom);

			if (deltaX != 0 || deltaY != 0)
				transform.Translate(deltaX, deltaY, MatrixOrder.Append);
		}

		private void FindCurrentTime()
		{
			var fmr = GetFocusedMessageRange();
			if (fmr == null)
				return;
			var i = fmr.Item1;
			if (i == arrows.Length)
				--i;
			if (!IsValidArrowIndex(i))
				return;
			SetSelectedArrowIndex(i);
			EnsureArrowVisible(i);
		}

		private void UpdateViewScrollBars()
		{
			var sceneRect = GetSceneRect();
			var viewRect = GetViewRect();
			view.UpdateScrollBars(
				sceneRect.Height, viewRect.Height, -sceneRect.Y,
				sceneRect.Width, viewRect.Width, -sceneRect.X
			);
		}

		private Rectangle GetViewRect()
		{
			var viewRect = new Rectangle(0, 0, view.ArrowsAreaWidth, view.ArrowsAreaHeight);
			return viewRect;
		}

		private Rectangle GetSceneRect()
		{
			var sceneRect = new Rectangle(
				Transform(-metrics.nodeWidth, -metrics.messageHeight),
				new Size(Transform(metrics.nodeWidth * (roles.Count + 1), metrics.messageHeight * arrows.Length, transformVector: true))
			);
			sceneRect.Inflate(0, 10);
			return sceneRect;
		}

		private void SelectNextArrowByPredicate(Predicate<Arrow> test)
		{
			int startIndex = IsValidArrowIndex(focusedSelectedArrow) ? (focusedSelectedArrow + 1) : 0;
			for (int i = startIndex; i < arrows.Length; ++i)
			{
				if (test(arrows[i]))
				{
					SetSelectedArrowIndex(i);
					EnsureArrowVisible(i);
					break;
				}
			}
		}

		private void SelectPrevArrowByPredicate(Predicate<Arrow> test)
		{
			int startIndex = IsValidArrowIndex(focusedSelectedArrow) ? (focusedSelectedArrow - 1) : arrows.Length - 1;
			for (int i = startIndex; i >= 0; --i)
			{
				if (test(arrows[i]))
				{
					SetSelectedArrowIndex(i);
					EnsureArrowVisible(i);
					break;
				}
			}
		}

		private static NonHorizontalArrowRole GetNonHorizontalArrowRole(Arrow a)
		{
			if (a.NonHorizontalConnectedArrow == null)
				return NonHorizontalArrowRole.None;
			if (a.Index < a.NonHorizontalConnectedArrow.Index)
				return NonHorizontalArrowRole.Sender;
			return NonHorizontalArrowRole.Receiver;
		}

		bool IsHighlightedArrow(Arrow arrow)
		{
			Arrow a1, nh1, a2, nh2;

			var nhca = arrow.NonHorizontalConnectedArrow;
			if (arrow.LinkedArrow != null)
			{
				a1 = arrow;
				nh1 = nhca;
			}
			else if (nhca != null && nhca.LinkedArrow != null)
			{
				a1 = nhca;
				nh1 = arrow;
			}
			else
			{
				return false;
			}

			a2 = a1.LinkedArrow;

			if (a2 == null)
			{
				return false;
			}

			nh2 = a2.NonHorizontalConnectedArrow;

			if ((selectedArrows.ContainsKey(a1.Index))
			||  (nh1 != null && selectedArrows.ContainsKey(nh1.Index))
			||  (selectedArrows.ContainsKey(a2.Index))
			||  (nh2 != null && selectedArrows.ContainsKey(nh2.Index)))
			{
				return true;
			}

			return false;
		}

		class Role
		{
			public readonly string DisplayName;
			public readonly HashSet<ILogSource> LogSources;
			public readonly HashSet<string> UnpairedMessagesTargetIds;
			public readonly List<Offset> ExecutionOccurrences = new List<Offset>();
			public readonly Dictionary<Arrow, Offset> CurrentExecutionOccurencesOffsets = new Dictionary<Arrow, Offset>();
			public readonly Dictionary<Arrow, Offset> CurrentNonHorizontalArrowsOffsets = new Dictionary<Arrow, Offset>();

			public int DisplayIndex = -1;
			public bool Visible;

			public Role(string displayName, HashSet<string> unpairedMessagesTargetIds)
			{
				this.DisplayName = displayName;
				this.LogSources = new HashSet<ILogSource>();
				this.UnpairedMessagesTargetIds = unpairedMessagesTargetIds ?? new HashSet<string>();

				if (Debugger.IsAttached)
					foreach (var id in this.UnpairedMessagesTargetIds)
						this.DisplayName += ", " + id;
			}
		};

		class Offset
		{
			public int Level;
			public Arrow Begin, End;

			public bool TestLevelSign(int sign)
			{
				return Level == 0 || Math.Sign(Level) == sign;
			}
		};

		class Arrow
		{
			public ArrowType Type;
			public DateTime FromTimestamp, ToTimestamp;
			public string ShortDisplayName;
			public string FullDisplayName;
			public Role From, To;
			public TriggerData FromTrigger, ToTrigger;
			public ILogSource FromLogSource, ToLogSource;
			public int Index;
			public TimeSpan? Delta;
			public TimeSpan FromRelativeTimestamp, ToRelativeTimestamp;
			public string LinkedArrowId;
			public Arrow LinkedArrow; // response arrow for request arrow and vise versa
			public int FromOffset, ToOffset;
			public bool IsBookmarked;
			public ISet<string> Tags;
			public bool Visible;
			public SI.PropertyChange StateInspectorPropChange;
			public Arrow NonHorizontalConnectedArrow;
			public int NonHorizontalConnectorOffset;
			public List<Arrow> OverlappingNonHorizontalArrows;
			public ArrowColor Color;
			public string TargetIdHint;
		};

		public enum NonHorizontalArrowRole
		{
			None,
			Sender,
			Receiver
		};

		public enum ArrowType
		{
			Request,
			Response,
			UserAction,
			APICall,
			StateChange,
			Bookmark,
			ActivityBegin,
			ActivityEnd,
		};

		class TriggerData
		{
			public ILogSource Source;
			public TextLogEventTrigger Trigger;
			public SI.PropertyChange StateInspectorChange;
			public string Tag;

			public TriggerData(ILogSource source, TextLogEventTrigger trigger = null)
			{
				Source = source;
				Trigger = trigger;
			}

			public TriggerData(IBookmark bmk)
			{
				Source = bmk.GetLogSource();
				Trigger = new TextLogEventTrigger(bmk);
			}

			public TriggerData(string tag)
			{
				Tag = tag;
			}
		};

		class ArrowComparer : IComparer<Arrow>
		{
			public int Compare(Arrow a1, Arrow a2)
			{
				var ret = DateTime.Compare(a1.FromTimestamp, a2.FromTimestamp);
				if (ret != 0)
					return ret;
				ret = string.Compare(GetSourceId(a1), GetSourceId(a2));
				if (ret != 0)
					return ret;
				ret = Math.Sign(GetStreamPosition(a1) - GetStreamPosition(a2));
				return ret;
			}

			public static int Compare(Arrow a, DateTime ts, ILogSource ls, long streamPos)
			{
				var ret = DateTime.Compare(a.FromTimestamp, ts);
				if (ret != 0)
					return ret;
				ret = string.Compare(GetSourceId(a), ls.GetSafeConnectionId());
				if (ret != 0)
					return ret;
				ret = Math.Sign(GetStreamPosition(a) - streamPos);
				return ret;
			}

			public static int Compare(Arrow a, DateTime ts, TriggerData trigger)
			{
				var ret = DateTime.Compare(a.FromTimestamp, ts);
				if (ret != 0)
					return ret;
				ret = string.Compare(GetSourceId(a), GetSourceId(trigger));
				if (ret != 0)
					return ret;
				ret = Math.Sign(GetStreamPosition(a) - GetStreamPosition(trigger));
				return ret;
			}

			static string GetSourceId(Arrow a)
			{
				return GetSourceId(a.FromTrigger ?? a.ToTrigger);
			}

			static string GetSourceId(TriggerData trigger)
			{
				if (trigger == null)
					return "";
				return trigger.Source.GetSafeConnectionId();
			}

			static long GetStreamPosition(Arrow a)
			{
				return GetStreamPosition(a.FromTrigger ?? a.ToTrigger);
			}

			static long GetStreamPosition(TriggerData trigger)
			{
				if (trigger == null)
					return 0;
				var t = trigger.Trigger;
				if (t == null)
					return 0;
				return t.StreamPosition;
			}
		}

		[DebuggerDisplay("{DisplayName}s")]
		class ExternalRolesProperties
		{
			public HashSet<string> Ids = new HashSet<string>();
			public string DisplayName;

			public ExternalRolesProperties(IEnumerable<KeyValuePair<string, string>> props)
			{
				foreach (var p in props)
				{
					if (p.Key == M.MetadataKeys.RoleInstanceName)
						DisplayName = p.Value;
					else if (p.Key == M.MetadataKeys.TargetRoleIdHint)
						Ids.Add(p.Value);
				}
			}


			public bool IsGood { get { return !string.IsNullOrEmpty(DisplayName) && Ids.Count > 0; } }
		};

		class MetricsCache
		{
			public readonly int nodeWidth;
			public readonly int messageHeight;
			public readonly int executionOccurrenceWidth;
			public readonly int executionOccurrenceLevelOffset = 6;
			public readonly int parallelNonHorizontalArrowsOffset = 4;
			public readonly int vScrollOffset;
			public readonly int arrowTextPadding;

			public MetricsCache(ViewMetrics viewMetrics)
			{
				this.messageHeight = viewMetrics.MessageHeight;
				this.nodeWidth = viewMetrics.NodeWidth;
				this.executionOccurrenceWidth = viewMetrics.ExecutionOccurrenceWidth;
				this.executionOccurrenceLevelOffset = viewMetrics.ExecutionOccurrenceLevelOffset;
				this.parallelNonHorizontalArrowsOffset = viewMetrics.ParallelNonHorizontalArrowsOffset;
				this.vScrollOffset = viewMetrics.VScrollOffset;
				this.arrowTextPadding = this.executionOccurrenceWidth / 2 + 1;
			}
		};
	}
}

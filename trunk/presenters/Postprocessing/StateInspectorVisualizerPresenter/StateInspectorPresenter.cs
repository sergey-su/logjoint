using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SI = LogJoint.Postprocessing.StateInspector;
using LogJoint.Postprocessing.StateInspector;
using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.Reactive;
using System.Collections.Immutable;
using System.Diagnostics;
using LogJoint.UI.Presenters.LogViewer;

namespace LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer
{
	public class StateInspectorPresenter: IPresenter, IViewModel, IPresenterInternal
	{
		public StateInspectorPresenter(
			IView view,
			IStateInspectorVisualizerModel model,
			IUserNamesProvider shortNames,
			ILogSourcesManager logSources,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IBookmarks bookmarks,
			IModelThreads threads,
			IPresentersFacade presentersFacade,
			IClipboardAccess clipboardAccess,
			SourcesManager.IPresenter sourcesManagerPresenter,
			IColorTheme theme,
			IChangeNotification changeNotification,
			ToolsContainer.IPresenter toolsContainerPresenter,
			Common.IPresentationObjectsFactory presentationObjectsFactory,
			IShellOpen shellOpen
		)
		{
			this.view = view;
			this.model = model;
			this.shortNames = shortNames;
			this.threads = threads;
			this.presentersFacade = presentersFacade;
			this.bookmarks = bookmarks;
			this.clipboardAccess = clipboardAccess;
			this.sourcesManagerPresenter = sourcesManagerPresenter;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.theme = theme;
			this.toolsContainerPresenter = toolsContainerPresenter;
			this.shellOpen = shellOpen;
			this.changeNotification = changeNotification.CreateChainedChangeNotification(initiallyActive: false);
			this.inlineSearch = new InlineSearch.Presenter(changeNotification);

			toastNotification = presentationObjectsFactory.CreateToastNotifications(changeNotification);
			toastNotification.Register(presentationObjectsFactory.CreateUnprocessedLogsToastNotification(PostprocessorKind.StateInspector));

			var annotationsVersion = 0;
			logSources.OnLogSourceAnnotationChanged += (sender, e) =>
			{
				annotationsVersion++;
				changeNotification.Post();
			};

			var getAnnotationsMap = Selectors.Create(
				() => logSources.Items,
				() => annotationsVersion,
				(sources, _) =>
					sources
					.Where(s => !s.IsDisposed && !string.IsNullOrEmpty(s.Annotation))
					.ToImmutableDictionary(s => s, s => s.Annotation)
			);

			VisualizerNode rootNode = new VisualizerNode(null, ImmutableList<VisualizerNode>.Empty, true, false, 0, ImmutableDictionary<ILogSource, string>.Empty);
			var updateRoot = Updaters.Create(
				() => model.Groups,
				getAnnotationsMap,
				(groups, annotationsMap) => rootNode = MakeRootNode(groups, OnNodeCreated, annotationsMap, rootNode)
			);
			this.getRootNode = () =>
			{
				updateRoot();
				return rootNode;
			};
			this.updateRootNode = reducer =>
			{
				var oldRoot = getRootNode();
				var newRoot = reducer(oldRoot);
				if (oldRoot != newRoot)
				{
					rootNode = newRoot;
					changeNotification.Post();
				}
			};

			this.getSelectedNodes = Selectors.Create(
				getRootNode,
				(root) => 
				{
					var result = ImmutableArray.CreateBuilder<VisualizerNode>();

					void traverse(VisualizerNode n)
					{
						if (!n.HasSelectedNodes)
							return;
						if (n.IsSelected)
							result.Add(n);
						foreach (var c in n.Children)
							traverse(c);
					}

					traverse(root);

					return result.ToImmutable();
				}
			);

			this.getSelectedInspectedObjects = Selectors.Create(
				getSelectedNodes,
				nodes => ImmutableArray.CreateRange(nodes.Select(n => n.InspectedObject))
			);

			this.getStateHistoryItems = Selectors.Create(
				getSelectedInspectedObjects,
				() => selectedHistoryEvents,
				MakeSelectedObjectHistory
			);

			this.getIsHistoryItemBookmarked = Selectors.Create(
				() => bookmarks.Items,
				boormarksItems =>
				{
					bool result(IStateHistoryItem item)
					{
						var change = (item as StateHistoryItem)?.Event;
						if (change == null || change.Output.LogSource.IsDisposed)
							return false;
						var bmk = bookmarks.Factory.CreateBookmark(
							change.Trigger.Timestamp.Adjust(change.Output.LogSource.TimeOffsets),
								change.Output.LogSource.GetSafeConnectionId(), change.Trigger.StreamPosition, 0);
						var pos = boormarksItems.FindBookmark(bmk);
						return pos.Item2 > pos.Item1;
					}
					return (Predicate<IStateHistoryItem>)result;
				}
			);

			this.getFocusedMessageInfo = () => loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;

			this.getFocusedMessageEqualRange = Selectors.Create(
				getFocusedMessageInfo,
				focusedMessageInfo =>
				{
					var cache = new Dictionary<IStateInspectorOutputsGroup, FocusedMessageEventsRange>();
					FocusedMessageEventsRange result(IStateInspectorOutputsGroup forGroup)
					{
						if (!cache.TryGetValue(forGroup, out FocusedMessageEventsRange eventsRange))
						{
							eventsRange = new FocusedMessageEventsRange(focusedMessageInfo,
								forGroup.Events.CalcFocusedMessageEqualRange(focusedMessageInfo));
							cache.Add(forGroup, eventsRange);
						}
						return eventsRange;
					}
					return (Func<IStateInspectorOutputsGroup, FocusedMessageEventsRange>)result;
				}
			);

			this.getPaintNode = Selectors.Create(
				getFocusedMessageEqualRange,
				r => MakePaintNodeDelegate(r, theme)
			);

			this.getFocusedMessagePositionInHistory = Selectors.Create(
				getStateHistoryItems,
				getFocusedMessageInfo,
				(changes, focusedMessage) =>
				{
					return
						focusedMessage == null ? null :
						new ListUtils.VirtualList<StateInspectorEvent>(changes.Length,
							i => changes[i].Event).CalcFocusedMessageEqualRange(focusedMessage);
				}
			);

			this.getCurrentTimeLabelText = Selectors.Create(
				getFocusedMessageInfo,
				focusedMsg => focusedMsg != null ? $"at {focusedMsg.Time}" : ""
			);

			this.getCurrentProperties = Selectors.Create(
				getSelectedInspectedObjects,
				getFocusedMessageEqualRange,
				() => selectedProperty,
				MakeCurrentProperties
			);

			this.getPropertyItems = Selectors.Create(
				getCurrentProperties,
				props => (IReadOnlyList<IPropertyListItem>)props.Cast<IPropertyListItem>().ToImmutableArray()
			);

			this.getObjectsProperties = Selectors.Create(
				getCurrentProperties,
				props => (IReadOnlyList<KeyValuePair<string, object>>)props.Select(p => p.ToDataSourceItem()).ToImmutableArray()
			);

			this.getDescription = Selectors.Create(
				getSelectedInspectedObjects,
				objs => objs.Length == 1 ? objs[0].Description : null
			);

			inlineSearch.OnSearch += (s, e) => PerformInlineSearch(e.Query, e.Reverse);

			view.SetViewModel(this);
		}

		public event EventHandler<MenuData> OnMenu;
		public event EventHandler<NodeCreatedEventArgs> OnNodeCreated;

		bool IPresenterInternal.IsObjectEventPresented(ILogSource source, TextLogEventTrigger objectEvent)
		{
			return model
				.Groups
				.Where(g => g.Outputs.Any(o => o.LogSource == source))
				.SelectMany(o => o.Events)
				.Any(e => objectEvent.CompareTo(e.Trigger) == 0);
		}

		IVisualizerNode IPresenter.SelectedObject => getSelectedNodes().FirstOrDefault();

		IEnumerableAsync<IVisualizerNode> IPresenter.Roots
		{
			get
			{
				return getRootNode().Children.Cast<IVisualizerNode>().ToAsync();
			}
		}

		bool IPresenterInternal.TrySelectObject(ILogSource source, TextLogEventTrigger creationTrigger, Func<IVisualizerNode, int> disambiguationFunction)
		{
			bool predecate(IInspectedObject obj)
			{
				return obj.Owner.Outputs.Any(o => o.LogSource == source)
					&& obj.StateChangeHistory.Any(change => change.Trigger.CompareTo(creationTrigger) == 0);
			}

			var candidates = EnumRoots().SelectMany(EnumTree).Where(predecate).ToList();

			if (candidates.Count > 0)
			{
				VisualizerNode node;
				if (candidates.Count == 1 || disambiguationFunction == null)
					node = FindOrCreateNode(candidates[0]);
				else
					node = candidates
						.Select(c => FindOrCreateNode(c))
						.Select(n => new { Node = n, Rating = disambiguationFunction(n) })
						.OrderByDescending(x => x.Rating)
						.First().Node;
				if (node != null)
				{
					SetSelection(new[] { node });
					return true;
				}
			}
			return false;
		}

		void IPostprocessorVisualizerPresenter.Show()
		{
			if (IsBrowser.Value && toolsContainerPresenter != null)
				toolsContainerPresenter.ShowTool(ToolsContainer.ToolKind.StateInspector);
			else
				view.Show();
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IObjectsTreeNode IViewModel.ObjectsTreeRoot => getRootNode();

		IReadOnlyList<IStateHistoryItem> IViewModel.ChangeHistoryItems => getStateHistoryItems();

		Predicate<IStateHistoryItem> IViewModel.IsChangeHistoryItemBookmarked => getIsHistoryItemBookmarked();

		Tuple<int, int> IViewModel.FocusedMessagePositionInChangeHistory => getFocusedMessagePositionInHistory();

		string IViewModel.CurrentTimeLabelText => getCurrentTimeLabelText();

		ColorThemeMode IViewModel.ColorTheme => theme.Mode;

		double? IViewModel.HistorySize => historySize;

		double? IViewModel.ObjectsTreeSize => objectsTreeSize;

		void IViewModel.OnVisibleChanged(bool value)
		{
			changeNotification.Active = value;
		}

		void IViewModel.OnPropertiesRowDoubleClicked(int rowIndex)
		{
			HandlePropertyDoubleClick(getCurrentProperties().ElementAtOrDefault(rowIndex));
		}

		void IViewModel.OnResizeHistory(double value)
		{
			historySize = value;
			changeNotification.Post();
		}

		void IViewModel.OnResizeObjectsTree(double value)
		{
			objectsTreeSize = value;
			changeNotification.Post();
		}

		PropertyCellPaintInfo IViewModel.OnPropertyCellPaint(int rowIndex)
		{
			var ret = new PropertyCellPaintInfo();
			var p = getCurrentProperties().ElementAtOrDefault(rowIndex);
			if (p.PropertyView != null)
			{
				ret.PaintAsLink = p.PropertyView.IsLink();
			}
			ret.AddLeftPadding = p.IsChildProperty;
			return ret;
		}


		void IViewModel.OnPropertyCellClicked(int rowIndex)
		{
			HandlePropertyCellClick(getCurrentProperties().ElementAtOrDefault(rowIndex));
		}

		PaintNodeDelegate IViewModel.PaintNode => getPaintNode();

		void IViewModel.OnExpandNode(IObjectsTreeNode node) => ExpandNode(node, true);

		void IViewModel.OnCollapseNode(IObjectsTreeNode node) => ExpandNode(node, false);

		void IViewModel.OnSelect(IReadOnlyCollection<IObjectsTreeNode> proposedSelection)
		{
			SetSelection(proposedSelection.OfType<VisualizerNode>());
		}

		void IViewModel.OnChangeHistoryItemDoubleClicked(IStateHistoryItem item)
		{
			if (item is StateHistoryItem historyItem)
				ShowPropertyChange(historyItem.Event, retainFocus: false);
		}

		async void IViewModel.OnChangeHistoryItemKeyEvent(IStateHistoryItem item, Key key)
		{
			if (item is StateHistoryItem historyItem)
			{
				if (key == Key.Enter)
					ShowPropertyChange(historyItem.Event, retainFocus: true);
				else if (key == Key.BookmarkShortcut)
					await ToggleBookmark(historyItem.Event);
				else if (key == Key.CopyShortcut)
				{
					clipboardAccess.SetClipboard(
						getStateHistoryItems()
							.Where(item => item.IsSelected)
							.Aggregate(
								new StringBuilder(),
								(sb, item) => sb.AppendLine($"{FormatTimestampt(item.Event)} {item.Message}"),
								sb => sb.ToString()
							)
					);
				}
			}
		}

		void IViewModel.OnChangeHistoryChangeSelection(IEnumerable<IStateHistoryItem> items)
		{
			this.selectedHistoryEvents = ImmutableArray.CreateRange(items.OfType<StateHistoryItem>().Select(i => i.Event));
			this.changeNotification.Post();
		}

		void IViewModel.OnFindCurrentPositionInChangeHistory()
		{
			var pos = getFocusedMessagePositionInHistory();
			if (pos == null)
				return;
			view.ScrollStateHistoryItemIntoView(pos.Item1);
		}

		MenuData IViewModel.OnNodeMenuOpening()
		{
			var menuData = new MenuData()
			{
				Items = new List<MenuData.Item>()
			};
			OnMenu?.Invoke(this, menuData);
			return menuData;
		}

		IReadOnlyList<KeyValuePair<string, object>> IViewModel.ObjectsProperties => getObjectsProperties();

		IReadOnlyList<IPropertyListItem> IViewModel.PropertyItems => getPropertyItems();

		void IViewModel.OnSelectProperty(IPropertyListItem property)
		{
			selectedProperty = property is PropertyInfo prop ? new SelectedProperty(prop.Object, prop.PropertyKey) : null;
			changeNotification.Post();
		}

		void IViewModel.OnPropertyDoubleClicked(IPropertyListItem property)
		{
			HandlePropertyDoubleClick(property as PropertyInfo);
		}

		void IViewModel.OnPropertyCellClicked(IPropertyListItem property)
		{
			HandlePropertyCellClick(property as PropertyInfo);
		}

		void IViewModel.OnPropertyCellCopyShortcutPressed()
		{
			var prop = getCurrentProperties().FirstOrDefault(p => p.IsSelected);
			if (prop != null)
				CopyPropertyToClipboard(prop);
		}

		void IViewModel.OnNodeDeleteKeyPressed()
		{
			var objs = getSelectedInspectedObjects();
			if (objs.All(x => x.Parent == null))
			{
				var logSources = objs.Select(obj => obj.GetPrimarySource()).Distinct().ToArray();
				if (logSources.Length > 0)
				{
					sourcesManagerPresenter.StartDeletionInteraction(logSources);
				}
			}
		}

		InlineSearch.IViewModel IViewModel.InlineSearch => inlineSearch.ViewModel;

		ToastNotificationPresenter.IViewModel IViewModel.ToastNotification => toastNotification.ViewModel;

		bool IViewModel.IsNotificationsIconVisibile => toastNotification.HasSuppressedNotifications;

		void IViewModel.OnActiveNotificationButtonClicked()
		{
			toastNotification.UnsuppressNotifications();
		}

		void IViewModel.OnSearchShortcutPressed()
		{
			inlineSearch.Show("");
		}

		string IViewModel.ObjectDescription
		{
			get { return getDescription(); }
		}

		static VisualizerNode MakeRootNode(
			IReadOnlyList<IStateInspectorOutputsGroup> groups,
			EventHandler<NodeCreatedEventArgs> nodeCreationHandler,
			ImmutableDictionary<ILogSource, string> annotationsMap,
			VisualizerNode existingRoot
		)
		{
			var existingGroups = existingRoot.Children.ToLookup(c => c.InspectedObject);

			var children = ImmutableList.CreateRange(
				groups.Select(group =>
				{
					var existingNode = existingGroups[group].FirstOrDefault();
					if (existingNode != null)
						return existingNode.SetAnnotationsMap(annotationsMap);
					var newNode = MakeVisualizerNode(group, 1, annotationsMap);
					newNode.SetInitialProps(nodeCreationHandler); // call handler on second phase when all children and parents are initiated
					return newNode;
				})
			);

			children = children.Sort((n1, n2) => MessageTimestamp.Compare(GetNodeTimestamp(n1), GetNodeTimestamp(n2)));

			var result = new VisualizerNode(null, children, expanded: true, selected: false, level: 0, annotationsMap);

			if (!result.HasSelectedNodes &&  result.Children.Count > 0)
				result = result.Children[0].Select(true);

			return result;
		}

		void ExpandNode(IObjectsTreeNode node, bool expand)
		{
			updateRootNode(rootNode =>
			{
				var vn = new InspectedObjectPath(node as VisualizerNode).Follow(rootNode);
				return vn?.Expand(expand) ?? rootNode;
			});
		}

		static PaintNodeDelegate MakePaintNodeDelegate(
			Func<IStateInspectorOutputsGroup, FocusedMessageEventsRange> getFocusedMessageEqualRange,
			IColorTheme theme
		)
		{
			NodePaintInfo result(IObjectsTreeNode node, bool getPrimaryPropValue)
			{
				var ret = new NodePaintInfo();

				var visualizerNode = node as VisualizerNode;
				IInspectedObject obj = visualizerNode?.InspectedObject;
				if (obj == null)
					return ret;

				ret.DrawingEnabled = true;

				ret.Annotation = visualizerNode.Annotation;

				var focusedMessageEventsRange = getFocusedMessageEqualRange(obj.Owner);
				if (obj.Parent == null) // Log source group node
				{
					var logSources = obj.EnumInvolvedLogSources();
					var focusedLs = focusedMessageEventsRange?.FocusedMessage?.GetLogSource();
					if (focusedLs != null)
					{
						ret.DrawFocusedMsgMark = logSources.Any(ls => ls == focusedLs);
					}
					ret.Coloring = NodeColoring.LogSource;
					ret.LogSourceColor = theme.ThreadColors.GetByIndex(logSources.FirstOrDefault().ColorIndex);
				}
				else
				{
					var liveStatus = obj.GetLiveStatus(focusedMessageEventsRange);
					var coloring = GetLiveStatusColoring(liveStatus);
					ret.Coloring = coloring;

					if (liveStatus == InspectedObjectLiveStatus.Alive || liveStatus == InspectedObjectLiveStatus.Deleted || obj.IsTimeless)
					{
						if (getPrimaryPropValue)
						{
							ret.PrimaryPropValue = obj.GetCurrentPrimaryPropertyValue(focusedMessageEventsRange);
						}
					}
				}

				return ret;
			}

			return result;
		}

		void SetSelection(IEnumerable<VisualizerNode> proposedSelection)
		{
			var newSelection = ImmutableHashSet.CreateRange(proposedSelection.Select(n => new InspectedObjectPath(n).Follow(getRootNode())).Where(n => n != null));
			var selectedNodes = getSelectedNodes();
			var pathsToDeselect = selectedNodes.Except(newSelection).Select(n => new InspectedObjectPath(n));
			var pathsToSelect = newSelection.Except(selectedNodes).Select(n => new InspectedObjectPath(n));

			updateRootNode(rootNode =>
			{
				var newRoot = rootNode;
				void select(InspectedObjectPath p, bool value) => newRoot = p.Follow(newRoot)?.Select(value) ?? newRoot;
				foreach (var p in pathsToDeselect)
					select(p, false);
				foreach (var p in pathsToSelect)
					select(p, true);
				return newRoot;
			});

			changeNotification.Post();
		}

		static NodeColoring GetLiveStatusColoring(InspectedObjectLiveStatus liveStatus)
		{
			return liveStatus switch
			{
				InspectedObjectLiveStatus.Alive => NodeColoring.Alive,
				InspectedObjectLiveStatus.Deleted => NodeColoring.Deleted,
				_ => NodeColoring.NotCreatedYet,
			};
		}

		static MessageTimestamp GetNodeTimestamp(VisualizerNode node)
		{
			var obj = (node as VisualizerNode)?.InspectedObject;
			StateInspectorEvent referenceEvt = null;
			if (obj != null)
				referenceEvt = obj.StateChangeHistory.FirstOrDefault();
			if (referenceEvt != null)
				return referenceEvt.Trigger.Timestamp.Adjust(referenceEvt.Output.LogSource.TimeOffsets);
			return MessageTimestamp.MaxValue;
		}

		static VisualizerNode MakeVisualizerNode(IInspectedObject modelNode, int level, ImmutableDictionary<ILogSource, string> annotationsMap)
		{
			var children = ImmutableList.CreateRange(modelNode.Children.Select(child => MakeVisualizerNode(child, level + 1, annotationsMap)));
			return new VisualizerNode(modelNode, children, false, false, level, annotationsMap);
		}

		StateHistoryItem MakeStateHistoryItem(StateInspectorEventInfo evtInfo,
			bool isSelected, bool showTimeDeltas, StateInspectorEvent prevSelectedEvent,
			StateHistoryMessageFormatter messageFormatter, int index)
		{
			var evt = evtInfo.Event;
			string time;
			if (showTimeDeltas)
				if (isSelected && prevSelectedEvent != null)
					time = TimeUtils.TimeDeltaToString(
						evt.Trigger.Timestamp.ToUnspecifiedTime() - prevSelectedEvent.Trigger.Timestamp.ToUnspecifiedTime(),
						addPlusSign: true);
				else
					time = "";
			else
				time = FormatTimestampt(evt);
			string message;
			messageFormatter.Reset();
			evt.OriginalEvent.Visit(messageFormatter);
			if (evtInfo.InspectedObjectNr != 0)
				message = string.Format("#{0}: {1}", evtInfo.InspectedObjectNr, messageFormatter.message);
			else
				message = messageFormatter.message;
			return new StateHistoryItem(evt, time, message, isSelected, index);
		}

		static IEnumerable<StateInspectorEventInfo> GetHistoryEventInfos(IReadOnlyList<IInspectedObject> objects)
		{
			return
				objects
				.ZipWithIndex()
				.Where(obj => !obj.Value.IsTimeless)
				.Select(obj => obj.Value.StateChangeHistory.Select((e, idx) => new StateInspectorEventInfo()
				{
					Object = obj.Value,
					InspectedObjectNr = objects.Count >= 2 ? obj.Key + 1 : 0,
					Event = e,
					EventIndex = idx,
				}))
				.ToArray()
				.MergeSortedSequences(new EventsComparer());
		}

		ImmutableArray<StateHistoryItem> MakeSelectedObjectHistory(
			ImmutableArray<IInspectedObject> selectedObjects,
			ImmutableArray<StateInspectorEvent> selectedEvents
		)
		{
			var result = ImmutableArray.CreateBuilder<StateHistoryItem>();
			var changes =
				GetHistoryEventInfos(selectedObjects)
				.ToList();

			var selectedEventsSet = selectedEvents.Select(e => (e.Output, e.Index)).ToHashSet();
			bool isEventSelected(StateInspectorEvent e) => selectedEventsSet.Contains((e.Output, e.Index));

			var messageFormatter = new StateHistoryMessageFormatter { shortNames = this.shortNames };
			bool showTimeDeltas = changes.Where(c => isEventSelected(c.Event)).Take(2).Count() > 1;
			StateInspectorEvent prevSelectedEvent = null;
			foreach (var change in changes.ZipWithIndex())
			{
				messageFormatter.currentObject = change.Value.Object;
				bool isSelected = isEventSelected(change.Value.Event);
				result.Add(MakeStateHistoryItem(change.Value, isSelected,
					showTimeDeltas, prevSelectedEvent, messageFormatter, change.Key));
				if (isSelected)
					prevSelectedEvent = change.Value.Event;
			}

			return result.ToImmutable();
		}

		static ImmutableArray<PropertyInfo> MakeCurrentProperties(
			ImmutableArray<IInspectedObject> objs,
			Func<IStateInspectorOutputsGroup, FocusedMessageEventsRange> getFocusedMessageEqualRange,
			SelectedProperty selectedProperty
		)
		{
			var result = ImmutableArray.CreateBuilder<PropertyInfo>();
			bool isMultiObjectMode = objs.Length >= 2;
			int objectIndex = 0;
			foreach (var obj in objs)
			{
				foreach (var dynamicProperty in obj.GetCurrentProperties(getFocusedMessageEqualRange(obj.Owner)))
				{
					var idProperty = dynamicProperty.Value as IdPropertyView;
					result.Add(new PropertyInfo(
						obj,
						dynamicProperty.Key,
						dynamicProperty.Value,
						isChildProperty: isMultiObjectMode && idProperty == null,
						isSelected: selectedProperty != null 
							&& selectedProperty.Object == obj && selectedProperty.PropertyName == dynamicProperty.Key
					));
					if (idProperty != null)
					{
						idProperty.ObjectNr = isMultiObjectMode ? objectIndex + 1 : 0;
					}
				}
				++objectIndex;
			}
			return result.ToImmutable();
		}

		IEnumerable<IInspectedObject> EnumTree(IInspectedObject obj)
		{
			return Enumerable.Repeat(obj, 1).Concat(obj.Children.SelectMany(EnumTree));
		}

		IEnumerable<IInspectedObject> EnumRoots()
		{
			return model.Groups.SelectMany(g => g.Children);
		}

		VisualizerNode FindOrCreateNode(IInspectedObject obj)
		{
			return new InspectedObjectPath(obj).Follow(getRootNode());
		}

		IBookmark CreateBookmark(StateInspectorEvent change)
		{
			return bookmarks.Factory.CreateBookmark(
				change.Trigger.Timestamp.Adjust(change.Output.LogSource.TimeOffsets),
				change.Output.LogSource.GetSafeConnectionId(),
				change.Trigger.StreamPosition,
				0
			);
		}

		void ShowPropertyChange(StateInspectorEvent change, bool retainFocus)
		{
			presentersFacade.ShowMessage(
				CreateBookmark(change),
				BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet
			);
			if (!retainFocus)
				loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
		}

		async Task ToggleBookmark(StateInspectorEvent change)
		{
			var togglableBmk = await change.Output.LogSource.CreateTogglableBookmark(
				bookmarks.Factory,
				CreateBookmark(change),
				CancellationToken.None
			);
			if (togglableBmk != null)
			{
				bookmarks.ToggleBookmark(togglableBmk);
			}
		}

		static string FormatTimestampt(StateInspectorEvent evt)
		{
			return evt.Trigger.Timestamp.ToUserFrendlyString(showMilliseconds: true, showDate: false);
		}

		void CopyPropertyToClipboard(PropertyInfo property)
		{
			var str = property?.PropertyView?.ToClipboardString();
			if (!string.IsNullOrEmpty(str))
				clipboardAccess.SetClipboard(str);
		}

		void HandlePropertyDoubleClick(PropertyInfo property)
		{
			if (property?.PropertyView == null)
				return;
			if (!(property.PropertyView.GetTrigger() is StateInspectorEvent evt))
				return;
			ShowPropertyChange(evt, false);
		}

		static bool TryGetExternalLink(string maybeUri, out Uri uri)
		{
			return Uri.TryCreate(maybeUri, UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https");
		}

		void HandlePropertyCellClick(PropertyInfo property)
		{
			var pcView = property?.PropertyView;
			if (pcView == null)
				return;
			if (pcView.GetTrigger() is StateInspectorEvent evt)
			{
				if (!(evt.OriginalEvent is PropertyChange pc))
					return;
				if (pc.ValueType == SI.ValueType.Reference)
				{
					var preferredRoot = pcView.InspectedObject.GetRoot();
					var query =
						from obj in EnumRoots().OrderBy(root => root == preferredRoot ? 0 : 1).SelectMany(EnumTree)
						where obj.Id == pc.Value
						select FindOrCreateNode(obj);
					var nodeToSelect = query.FirstOrDefault();
					if (nodeToSelect != null)
					{
						SetSelection(new[] { nodeToSelect });
					}
					else if (TryGetExternalLink(pc.Value, out var externalUri))
					{
						shellOpen.OpenInWebBrowser(externalUri);
					}
				}
				else if (pc.ValueType == SI.ValueType.ThreadReference)
				{
					var thread = threads.Items.FirstOrDefault(t => t.ID == pc.Value);
					if (thread != null)
						presentersFacade.ShowThread(thread);
				}
				return;
			}
			if (pcView.GetTrigger() is ILogSource ls)
			{
				presentersFacade.ShowLogSource(ls);
				return;
			}
		}

		void PerformInlineSearch(string searchText, bool reverse)
		{
			VisualizerNode root = getRootNode();
			VisualizerNode originNode = getSelectedNodes().FirstOrDefault() ?? root;
			if (originNode == null)
				return;
			StateHistoryItem selectedHistoryItem = getStateHistoryItems().FirstOrDefault(i => i.IsSelected);
			(VisualizerNode node, int? historyItemEventIndex) origin = (originNode, selectedHistoryItem?.Index);
			IEnumerable<(VisualizerNode node, StateInspectorEventInfo? historyItem)> traverse(VisualizerNode node)
			{
				yield return (node, null);
				if (node.InspectedObject != null)
					foreach (var e in GetHistoryEventInfos(new[] { node.InspectedObject }))
						yield return (node, e);
				foreach (var c in node.Children)
					foreach (var n in traverse(c))
						yield return n;
			};
			IEnumerable<(VisualizerNode node, StateInspectorEventInfo? historyItem)> traverseBackwards(VisualizerNode node)
			{
				for (int i = node.Children.Count - 1; i >= 0; --i)
					foreach (var n in traverseBackwards(node.Children[i]))
						yield return n;
				if (node.InspectedObject != null)
					foreach (var e in GetHistoryEventInfos(new[] { node.InspectedObject }).Reverse())
						yield return (node, e);
				yield return (node, null);
			};
			var messageFormatter = new StateHistoryMessageFormatter { shortNames = this.shortNames };
			(VisualizerNode node, StateInspectorEventInfo? historyItem) candidateBeforeOrigin = (null, null);
			(VisualizerNode node, StateInspectorEventInfo? historyItem) candidateAfterOrigin = (null, null);
			bool foundOrigin = false;
			foreach (var n in reverse ? traverseBackwards(root) : traverse(root))
			{
				string textToMatch;
				if (n.historyItem.HasValue)
				{
					messageFormatter.currentObject = n.node.InspectedObject;
					messageFormatter.Reset();
					n.historyItem.Value.Event.OriginalEvent.Visit(messageFormatter);
					textToMatch = messageFormatter.message;
				}
				else
				{
					textToMatch = n.ToString();
				}
				if (textToMatch.IndexOf(searchText, 0, StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					if (foundOrigin)
					{
						candidateAfterOrigin = n;
						break;
					}
					else if (candidateBeforeOrigin.node == null)
					{
						candidateBeforeOrigin = n;
					}
				}
				if (n.node == origin.node && n.historyItem?.EventIndex == origin.historyItemEventIndex)
				{
					foundOrigin = true;
				}
			}
			var newSelection = candidateAfterOrigin.node != null ? candidateAfterOrigin : candidateBeforeOrigin;
			if (newSelection.node != null)
			{
				SetSelection(new[] { newSelection.node });

				ImmutableArray<StateInspectorEvent> newSelectedHistoryItems;
				if (newSelection.historyItem.HasValue)
				{
					newSelectedHistoryItems = GetHistoryEventInfos(new[] { newSelection.node.InspectedObject })
						.Skip(newSelection.historyItem.Value.EventIndex).Take(1)
						.Select(e => e.Event)
						.ToImmutableArray();
				}
				else
				{
					newSelectedHistoryItems = ImmutableArray<StateInspectorEvent>.Empty;
				}
				selectedHistoryEvents = newSelectedHistoryItems;
				changeNotification.Post();
			}
		}

		class PropertyInfo : IPropertyListItem
		{
			public readonly IInspectedObject Object;
			public readonly string PropertyKey;
			public readonly PropertyViewBase PropertyView;
			public readonly bool IsChildProperty;
			public readonly bool IsSelected;

			public PropertyInfo(IInspectedObject obj, string propKey, PropertyViewBase propView, bool isChildProperty, bool isSelected)
			{
				this.Object = obj;
				this.PropertyKey = propKey;
				this.PropertyView = propView;
				this.IsChildProperty = isChildProperty;
				this.IsSelected = isSelected;
			}

			public static bool Equal(PropertyInfo p1, PropertyInfo p2)
			{
				return p1.Object == p2.Object && p1.PropertyKey == p2.PropertyKey;
			}

			public KeyValuePair<string, object> ToDataSourceItem()
			{
				return new KeyValuePair<string, object>(PropertyKey, PropertyView);
			}

			string IPropertyListItem.Name => PropertyKey;
			string IPropertyListItem.Value => PropertyView.ToString();
			PropertyLinkType IPropertyListItem.LinkType =>
				!PropertyView.IsLink() ? PropertyLinkType.None
				: TryGetExternalLink(PropertyView.ToString(), out var _) ? PropertyLinkType.External
				: PropertyLinkType.Internal;

			bool IPropertyListItem.IsLeftPadded => IsChildProperty;

			string IListItem.Key => $"{Object.GetHashCode():x}.{PropertyKey}";
			bool IListItem.IsSelected => IsSelected;
		};

		class InspectedObjectPath
		{
			readonly string outputsGroupKey;
			readonly List<IInspectedObject> path;

			public InspectedObjectPath(VisualizerNode node): this(node?.InspectedObject)
			{
			}

			public InspectedObjectPath(IInspectedObject obj)
			{
				this.path = new List<IInspectedObject>();
				this.outputsGroupKey = obj?.Owner?.Key;
				for (IInspectedObject i = obj; i != null; i = i.Parent)
					path.Add(i);
				path.Reverse();
			}

			public VisualizerNode Follow(VisualizerNode rootNode)
			{
				VisualizerNode ret = null;
				bool inspectingRoots = true;
				foreach (var segment in path)
				{
					VisualizerNode found = (
						from n in inspectingRoots ? rootNode.Children : ret.Children
						let obj = n.InspectedObject
						where obj != null
						where inspectingRoots ? (obj == segment && obj.Owner.Key == outputsGroupKey) : (obj == segment)
						select n
					).FirstOrDefault();
					if (found == null)
						return null;
					ret = found;
					inspectingRoots = false;
				}
				return ret;
			}
		};

		class StateHistoryMessageFormatter : IEventsVisitor
		{
			public string message = "";
			public IUserNamesProvider shortNames;
			public IInspectedObject currentObject;

			public void Reset()
			{
				message = "";
			}

			void IEventsVisitor.Visit (ObjectCreation objectCreation)
			{
				message = "created";
			}
			void IEventsVisitor.Visit (ObjectDeletion objectDeletion)
			{
				message = "deleted";
			}
			void IEventsVisitor.Visit (PropertyChange change)
			{
				message = string.Format("'{0}'->'{1}'", change.PropertyName,
					change.ValueType == SI.ValueType.UserHash ? shortNames.AddShortNameToUserHash(change.Value) :
					change.ValueType == SI.ValueType.Reference && currentObject.Owner.TryGetDisplayName(change.Value, out var displayName) ? displayName :
					change.Value);
			}
			void IEventsVisitor.Visit (ParentChildRelationChange parentChildRelationChange)
			{
			}
		};

		[DebuggerDisplay("{key} {text}")]
		class VisualizerNode : IVisualizerNode, IObjectsTreeNode
		{
			private readonly IInspectedObject obj;
			private readonly string key;
			private bool expanded;
			private readonly bool selected;
			private readonly string text;
			private readonly ImmutableList<VisualizerNode> children;
			private readonly int level;
			private readonly bool hasSelectedNodes;
			private readonly ImmutableDictionary<ILogSource, string> annotationsMap;
			private readonly string annotation;

			// Parent pointers lead to currently visible root node.
			// If null - the node is not reachable from ViewModel root.
			// This mutable state does not break Reactive.ITreeNode immutability requirement
			// because it's not visible via ITreeNode interface.
			private VisualizerNode parent;

			public VisualizerNode(
				IInspectedObject obj,
				ImmutableList<VisualizerNode> children,
				bool expanded,
				bool selected,
				int level,
				ImmutableDictionary<ILogSource, string> annotationsMap
			)
			{
				this.obj = obj;
				this.key = $"{obj?.GetHashCode():x08}";
				this.children = children;
				this.annotationsMap = annotationsMap;
				this.text = GetNodeText(obj, level);
				this.annotation = GetNodeAnnotation(obj, level, annotationsMap);
				this.expanded = expanded;
				this.selected = selected;
				this.level = level;
				children.ForEach(c => c.parent = this);
				this.hasSelectedNodes = selected || children.Any(c => c.HasSelectedNodes);
			}


			public IInspectedObject InspectedObject => obj;
			public bool IsSelected => selected;
			public bool HasSelectedNodes => hasSelectedNodes;
			public IReadOnlyList<VisualizerNode> Children => children;
			public bool IsExpandable => true;
			public string Annotation => annotation;

			public void SetInitialProps(EventHandler<NodeCreatedEventArgs> nodeCreationHandler)
			{
				bool createCollapsed = false;
				if (level < 7) // todo: why 7?
				{
					if (nodeCreationHandler != null)
					{
						var args = new NodeCreatedEventArgs() { NodeObject = this };
						nodeCreationHandler(this, args);
						createCollapsed = args.CreateCollapsed.GetValueOrDefault(createCollapsed);
					}
				}
				expanded = !createCollapsed;
				children.ForEach(c => c.SetInitialProps(nodeCreationHandler));
			}

			public VisualizerNode ReplaceChild(VisualizerNode old, VisualizerNode newChild, bool ensureExpanded)
			{
				var copy = new VisualizerNode(obj, ImmutableList.CreateRange(children.Select(c => c == old ? newChild : c)),
						ensureExpanded || expanded, selected, level, annotationsMap);
				old.parent = null;
				return parent == null ? copy : parent.ReplaceChild(this, copy, ensureExpanded);
			}

			public VisualizerNode Expand(bool value)
			{
				var copy = new VisualizerNode(obj, children, value, selected, level, annotationsMap);
				return parent.ReplaceChild(this, copy, ensureExpanded: false);
			}

			public VisualizerNode Select(bool value)
			{
				var copy = new VisualizerNode(obj, children, expanded, value, level, annotationsMap);
				return parent.ReplaceChild(this, copy, ensureExpanded: value);
			}

			public VisualizerNode SetAnnotationsMap(ImmutableDictionary<ILogSource, string> annotationsMap)
			{
				if (level != 1 || this.annotationsMap == annotationsMap)
					return this;
				return new VisualizerNode(obj, children, expanded, selected, level, annotationsMap);
			}

			string IVisualizerNode.Id => obj.Id;

			Event IVisualizerNode.CreationEvent => obj.CreationEvent?.OriginalEvent;

			IVisualizerNode IVisualizerNode.Parent => parent?.obj != null ? parent : null;
			IEnumerableAsync<IVisualizerNode> IVisualizerNode.Children => children.Cast<IVisualizerNode>().ToAsync();

			bool IVisualizerNode.BelongsToSource(ILogSource logSource)
			{
				return obj.Owner.Outputs.Any(x => x.LogSource == logSource);
			}

			IEnumerable<PropertyChange> IVisualizerNode.ChangeHistory
			{
				get
				{
					return obj.StateChangeHistory.Select(i => i.OriginalEvent).OfType<PropertyChange>();
				}
			}

			string ITreeNode.Key => key;

			IReadOnlyList<ITreeNode> ITreeNode.Children => children;

			bool ITreeNode.IsExpanded => expanded;

			bool ITreeNode.IsSelected => selected;

			public override string ToString() => text;

			static private string GetNodeText(IInspectedObject node, int level)
			{
				switch (level)
				{
					case 0: return "";
					case 1:
						return node.EnumInvolvedLogSources().FirstOrDefault().DisplayName;
					default:
						string nodeText = node.DisplayName;
						if (node.Comment != "")
							nodeText += " (" + node.Comment + ")";
						return nodeText;
				}
			}

			static private string GetNodeAnnotation(IInspectedObject node, int level, 
				ImmutableDictionary<ILogSource, string> annotationsMap)
			{
				if (level == 1)
				{
					var ls = node.EnumInvolvedLogSources().FirstOrDefault();
					if (annotationsMap.TryGetValue(ls, out var logSourceAnnotation))
						return logSourceAnnotation;
				}
				return null;
			}
		};

		class StateHistoryItem : IStateHistoryItem
		{
			readonly string time;
			readonly string message;
			readonly bool isSelected;
			readonly StateInspectorEvent @event;
			readonly string key;
			readonly int index;

			public StateHistoryItem(StateInspectorEvent @event, string time, string message, bool isSelected, int index)
			{
				this.time = time;
				this.message = message;
				this.isSelected = isSelected;
				this.@event = @event;
				this.key = (@event, message).GetHashCode().ToString("x");
				this.index = index;
			}

			public StateInspectorEvent Event => @event;

			public bool IsSelected => isSelected;

			public int Index => index;

			string IStateHistoryItem.Time => time;

			int IStateHistoryItem.Index => index;

			public string Message => message;

			string IListItem.Key => key;

			bool IListItem.IsSelected => isSelected;
		};

		class SelectedProperty
		{
			public readonly IInspectedObject Object;
			public readonly string PropertyName;
			public SelectedProperty(IInspectedObject obj, string propertyName)
			{
				Object = obj;
				PropertyName = propertyName;
			}
		};

		readonly IView view;
		readonly IStateInspectorVisualizerModel model;
		readonly IUserNamesProvider shortNames;
		readonly IModelThreads threads;
		readonly IBookmarks bookmarks;
		readonly IPresentersFacade presentersFacade;
		readonly IClipboardAccess clipboardAccess;
		readonly SourcesManager.IPresenter sourcesManagerPresenter;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly IColorTheme theme;
		readonly IChainedChangeNotification changeNotification;
		readonly IShellOpen shellOpen;
		readonly Func<VisualizerNode> getRootNode;
		readonly Action<Func<VisualizerNode, VisualizerNode>> updateRootNode;
		readonly Func<ImmutableArray<VisualizerNode>> getSelectedNodes;
		readonly Func<PaintNodeDelegate> getPaintNode;
		readonly Func<ImmutableArray<IInspectedObject>> getSelectedInspectedObjects;
		readonly Func<ImmutableArray<StateHistoryItem>> getStateHistoryItems;
		readonly Func<Predicate<IStateHistoryItem>> getIsHistoryItemBookmarked;
		readonly Func<Func<IStateInspectorOutputsGroup, FocusedMessageEventsRange>> getFocusedMessageEqualRange;
		readonly Func<IMessage> getFocusedMessageInfo;
		readonly Func<Tuple<int, int>> getFocusedMessagePositionInHistory;
		readonly Func<string> getCurrentTimeLabelText;
		ImmutableArray<StateInspectorEvent> selectedHistoryEvents = ImmutableArray<StateInspectorEvent>.Empty;
		readonly Func<ImmutableArray<PropertyInfo>> getCurrentProperties;
		readonly Func<IReadOnlyList<KeyValuePair<string, object>>> getObjectsProperties;
		readonly Func<IReadOnlyList<IPropertyListItem>> getPropertyItems;
		SelectedProperty selectedProperty;
		double? objectsTreeSize, historySize;
		readonly InlineSearch.IPresenter inlineSearch;
		readonly ToolsContainer.IPresenter toolsContainerPresenter;
		readonly ToastNotificationPresenter.IPresenter toastNotification;
		readonly Func<string> getDescription;
	}
}

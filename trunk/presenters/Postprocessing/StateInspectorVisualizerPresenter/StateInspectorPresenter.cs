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
			IChangeNotification changeNotification
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
			this.changeNotification = changeNotification.CreateChainedChangeNotification(initiallyActive: false);

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
					Predicate<IStateHistoryItem> result = (item) =>
					{
						var change = (item as StateHistoryItem)?.Event;
						if (change == null || change.Output.LogSource.IsDisposed)
							return false;
						var bmk = bookmarks.Factory.CreateBookmark(
							change.Trigger.Timestamp.Adjust(change.Output.LogSource.TimeOffsets),
								change.Output.LogSource.GetSafeConnectionId(), change.Trigger.StreamPosition, 0);
						var pos = boormarksItems.FindBookmark(bmk);
						return pos.Item2 > pos.Item1;
					};
					return result;
				}
			);

			this.getFocusedMessageInfo = () => loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;

			this.getFocusedMessageEqualRange = Selectors.Create(
				getFocusedMessageInfo,
				focusedMessageInfo =>
				{
					var cache = new Dictionary<IStateInspectorOutputsGroup, FocusedMessageEventsRange>();
					Func<IStateInspectorOutputsGroup, FocusedMessageEventsRange> result = forGroup =>
					{
						if (!cache.TryGetValue(forGroup, out FocusedMessageEventsRange eventsRange))
						{
							eventsRange = new FocusedMessageEventsRange(focusedMessageInfo,
								forGroup.Events.CalcFocusedMessageEqualRange(focusedMessageInfo));
							cache.Add(forGroup, eventsRange);
						}
						return eventsRange;
					};
					return result;
				}
			);

			this.getPaintNode = Selectors.Create(
				getFocusedMessageEqualRange,
				MakePaintNodeDelegate
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
				MakeCurrentProperties
			);

			this.getObjectsProperties = Selectors.Create(
				getCurrentProperties,
				props => (IReadOnlyList<KeyValuePair<string, object>>)props.Select(p => p.ToDataSourceItem()).ToImmutableArray()
			);

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
			Func<IInspectedObject, bool> predecate = obj =>
			{
				return obj.Owner.Outputs.Any(o => o.LogSource == source)
					&& obj.StateChangeHistory.Any(change => change.Trigger.CompareTo(creationTrigger) == 0);
			};

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
			view.Show();
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IObjectsTreeNode IViewModel.ObjectsTreeRoot => getRootNode();

		IReadOnlyList<IStateHistoryItem> IViewModel.ChangeHistoryItems => getStateHistoryItems();

		Predicate<IStateHistoryItem> IViewModel.IsChangeHistoryItemBookmarked => getIsHistoryItemBookmarked();

		Tuple<int, int> IViewModel.FocusedMessagePositionInChangeHistory => getFocusedMessagePositionInHistory();

		string IViewModel.CurrentTimeLabelText => getCurrentTimeLabelText();

		ColorThemeMode IViewModel.ColorTheme => theme.Mode;

		void IViewModel.OnVisibleChanged(bool value)
		{
			changeNotification.Active = value;
		}

		void IViewModel.OnPropertiesRowDoubleClicked(int rowIndex)
		{
			var selectedProp = getCurrentProperties().ElementAtOrDefault(rowIndex);
			if (selectedProp.PropertyView == null)
				return;
			var evt = selectedProp.PropertyView.GetTrigger() as StateInspectorEvent;
			if (evt == null)
				return;
			ShowPropertyChange(evt, false);
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
			var currentProperties = getCurrentProperties();
			if (rowIndex >= 0 && rowIndex < currentProperties.Length)
			{
				var pcView = currentProperties[rowIndex].PropertyView;
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
						if (nodeToSelect == null)
							return;
						SetSelection(new[] { nodeToSelect });
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

		void IViewModel.OnChangeHistoryItemKeyEvent(IStateHistoryItem item, Key key)
		{
			if (item is StateHistoryItem historyItem)
			{
				if (key == Key.Enter)
					ShowPropertyChange(historyItem.Event, retainFocus: true);
				else if (key == Key.BookmarkShortcut)
					ToggleBookmark(historyItem.Event);
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

		void IViewModel.OnPropertyCellCopyShortcutPressed(int propertyIndex)
		{
			var sel = getCurrentProperties().ElementAtOrDefault(propertyIndex);
			if (sel.PropertyView == null)
				return;
			var str = sel.PropertyView.ToClipboardString();
			if (!string.IsNullOrEmpty(str))
				clipboardAccess.SetClipboard(str);
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

		static VisualizerNode MakeRootNode(
			IReadOnlyList<IStateInspectorOutputsGroup> groups,
			EventHandler<NodeCreatedEventArgs> nodeCreationHandler,
			ImmutableDictionary<ILogSource, string> annotationsMap,
			VisualizerNode existingRoot
		)
		{
			var existingRoots = existingRoot.Children.ToLookup(c => c.InspectedObject);

			var children = ImmutableList.CreateRange(
				groups.SelectMany(
					group => group.Roots.Select(rootObj =>
					{
						var existingNode = existingRoots[rootObj].FirstOrDefault();
						if (existingNode != null)
							return existingNode.SetAnnotationsMap(annotationsMap);
						var newNode = MakeVisualizerNode(rootObj, 1, annotationsMap);
						newNode.SetInitialProps(nodeCreationHandler); // call handler on second phase when all children and parents are initiated
						return newNode;
					})
				)
			);

			children = children.Sort((n1, n2) => MessageTimestamp.Compare(GetNodeTimestamp(n1), GetNodeTimestamp(n2)));

			var result = new VisualizerNode(null, children, true, false, 0, annotationsMap);

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
			Func<IStateInspectorOutputsGroup, FocusedMessageEventsRange> getFocusedMessageEqualRange
		)
		{
			NodePaintInfo result(IObjectsTreeNode node, bool getPrimaryPropValue)
			{
				var ret = new NodePaintInfo();

				IInspectedObject obj = (node as VisualizerNode)?.InspectedObject;
				if (obj == null)
					return ret;

				ret.DrawingEnabled = true;

				var focusedMessageEventsRange = getFocusedMessageEqualRange(obj.Owner);
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

				if (obj.Parent == null)
				{
					var focusedLs = focusedMessageEventsRange?.FocusedMessage?.GetLogSource();
					if (focusedLs != null)
					{
						ret.DrawFocusedMsgMark = obj.EnumInvolvedLogSources().Any(ls => ls == focusedLs);
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
			switch (liveStatus)
			{
				case InspectedObjectLiveStatus.Alive:
					return NodeColoring.Alive;
				case InspectedObjectLiveStatus.Deleted:
					return NodeColoring.Deleted;
				default:
					return NodeColoring.NotCreatedYet;
			}
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
			StateHistoryMessageFormatter messageFormatter)
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
			return new StateHistoryItem(evt, time, message, isSelected);
		}

		ImmutableArray<StateHistoryItem> MakeSelectedObjectHistory(
			ImmutableArray<IInspectedObject> selectedObjects,
			ImmutableArray<StateInspectorEvent> selectedEvents
		)
		{
			var result = ImmutableArray.CreateBuilder<StateHistoryItem>();
			var changes =
				selectedObjects
				.ZipWithIndex()
				.Where(obj => !obj.Value.IsTimeless)
				.Select(obj => obj.Value.StateChangeHistory.Select(e => new StateInspectorEventInfo()
				{
					Object = obj.Value,
					InspectedObjectNr = selectedObjects.Length >= 2 ? obj.Key + 1 : 0,
					Event = e
				}))
				.ToArray()
				.MergeSortedSequences(new EventsComparer())
				.ToList();

			var selectedEventsSet = selectedEvents.Select(e => (e.Output, e.Index)).ToHashSet();
			bool isEventSelected(StateInspectorEvent e) => selectedEventsSet.Contains((e.Output, e.Index));

			var messageFormatter = new StateHistoryMessageFormatter { shortNames = this.shortNames };
			bool showTimeDeltas = changes.Where(c => isEventSelected(c.Event)).Take(2).Count() > 1;
			StateInspectorEvent prevSelectedEvent = null;
			foreach (var change in changes.ZipWithIndex())
			{
				bool isSelected = isEventSelected(change.Value.Event);
				result.Add(MakeStateHistoryItem(change.Value, isSelected,
					showTimeDeltas, prevSelectedEvent, messageFormatter));
				if (isSelected)
					prevSelectedEvent = change.Value.Event;
			}

			return result.ToImmutable();
		}

		static ImmutableArray<PropertyInfo> MakeCurrentProperties(
			ImmutableArray<IInspectedObject> objs,
			Func<IStateInspectorOutputsGroup, FocusedMessageEventsRange> getFocusedMessageEqualRange
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
						isChildProperty: isMultiObjectMode && idProperty == null
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

		FocusedMessageEventsRange GetFocusedMessageEqualRange(IInspectedObject obj)
		{
			return obj == null ? null : getFocusedMessageEqualRange()(obj.Owner);
		}


		IEnumerable<IInspectedObject> EnumTree(IInspectedObject obj)
		{
			return Enumerable.Repeat(obj, 1).Concat(obj.Children.SelectMany(EnumTree));
		}

		IEnumerable<IInspectedObject> EnumRoots()
		{
			return model.Groups.SelectMany(g => g.Roots);
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

		static string GetObjectTypeName(IInspectedObject obj)
		{
			if (obj.CreationEvent == null)
				return null;
			var createEvt = obj.CreationEvent.OriginalEvent;
			if (createEvt == null || createEvt.ObjectType == null)
				return null;
			return createEvt.ObjectType.TypeName;
		}

		static string FormatTimestampt(StateInspectorEvent evt)
		{
			return evt.Trigger.Timestamp.ToUserFrendlyString(showMilliseconds: true, showDate: false);
		}

		struct PropertyInfo
		{
			public IInspectedObject Object;
			public string PropertyKey;
			public PropertyViewBase PropertyView;
			public bool IsChildProperty;

			public PropertyInfo(IInspectedObject obj, string propKey, PropertyViewBase propView, bool isChildProperty)
			{
				this.Object = obj;
				this.PropertyKey = propKey;
				this.PropertyView = propView;
				this.IsChildProperty = isChildProperty;
			}

			public static bool Equal(PropertyInfo p1, PropertyInfo p2)
			{
				return p1.Object == p2.Object && p1.PropertyKey == p2.PropertyKey;
			}

			public KeyValuePair<string, object> ToDataSourceItem()
			{
				return new KeyValuePair<string, object>(PropertyKey, PropertyView);
			}
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
					change.ValueType == SI.ValueType.UserHash ? shortNames.AddShortNameToUserHash(change.Value) : change.Value);
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
				this.text = GetNodeText(obj, level, annotationsMap);
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

			public void SetInitialProps(EventHandler<NodeCreatedEventArgs> nodeCreationHandler)
			{
				bool createCollapsed = false;
				if (level < 5) // todo: why 5?
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
						ensureExpanded ? true : expanded, selected, level, annotationsMap);
				old.parent = null;
				return parent == null ? copy : parent.ReplaceChild(this, copy, ensureExpanded);
			}

			public VisualizerNode Expand(bool value)
			{
				var copy = new VisualizerNode(obj, children, value, selected, level, annotationsMap);
				return parent.ReplaceChild(this, copy, false);
			}

			public VisualizerNode Select(bool value)
			{
				var copy = new VisualizerNode(obj, children, expanded, value, level, annotationsMap);
				return parent.ReplaceChild(this, copy, true);
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

			static private string GetNodeText(IInspectedObject node, int level, ImmutableDictionary<ILogSource, string> annotationsMap)
			{
				switch (level)
				{
					case 0: return "";
					case 1:
						var rootNodeText = new StringBuilder(node.Id);
						var ls = node.EnumInvolvedLogSources().FirstOrDefault();
						if (ls != null && annotationsMap.TryGetValue(ls, out var logSourceAnnotation))
							rootNodeText.AppendFormat(" ({0})", logSourceAnnotation);
						if (node.Comment != "")
							rootNodeText.AppendFormat(" ({0})", node.Comment);
						return rootNodeText.ToString();
					default:
						string nodeText = node.DisplayName;
						if (node.Comment != "")
							nodeText += " (" + node.Comment + ")";
						return nodeText;
				}
			}
		};

		class StateHistoryItem : IStateHistoryItem
		{
			readonly string time;
			readonly string message;
			readonly bool isSelected;
			readonly StateInspectorEvent @event;
			readonly string key;

			public StateHistoryItem(StateInspectorEvent @event, string time, string message, bool isSelected)
			{
				this.time = time;
				this.message = message;
				this.isSelected = isSelected;
				this.@event = @event;
				this.key = @event.GetHashCode().ToString();
			}

			public StateInspectorEvent Event => @event;

			string IStateHistoryItem.Time => time;

			string IStateHistoryItem.Message => message;

			string IListItem.Key => key;

			bool IListItem.IsSelected => isSelected;
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
	}
}

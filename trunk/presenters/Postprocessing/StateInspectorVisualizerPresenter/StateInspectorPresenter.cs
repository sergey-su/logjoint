using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SI = LogJoint.Analytics.StateInspector;
using LogJoint.Postprocessing.StateInspector;
using LogJoint.Analytics;
using LogJoint.Postprocessing;
using LogJoint.Analytics.StateInspector;

namespace LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer
{
	public class StateInspectorPresenter: IPresenter, IViewEvents
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
			SourcesManager.IPresenter sourcesManagerPresenter
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

			view.SetEventsHandler(this);

			logSources.OnLogSourceAnnotationChanged += (sender, e) =>
			{
				InvalidateTree();
			};

			loadedMessagesPresenter.LogViewerPresenter.FocusedMessageChanged += (sender, args) =>
			{
				HandleFocusedMessageChange();
			};

			bookmarks.OnBookmarksChanged += (sender, args) =>
			{
				view.BeginUpdateStateHistoryList(false, false);
				view.EndUpdateStateHistoryList(null, false, redrawFocusedMessageMark: true);
			};

			model.Changed += (sender, args) =>
			{
				InvalidateTree();
				HandleFocusedMessageChange();
				RemoveMissingGroupsFromCache();
			};

			InvalidateTree();
			HandleFocusedMessageChange();
		}

		public event EventHandler<MenuData> OnMenu;
		public event EventHandler<NodeCreatedEventArgs> OnNodeCreated;

		bool IPresenter.IsObjectEventPresented(ILogSource source, TextLogEventTrigger objectEvent)
		{
			return model
				.Groups
				.Where(g => g.Outputs.Any(o => o.LogSource == source))
				.SelectMany(o => o.Events)
				.Any(e => objectEvent.CompareTo(e.Trigger) == 0);
		}

		IInspectedObject IPresenter.SelectedObject
		{
			get 
			{
				return GetSelectedInspectedObjects().FirstOrDefault();
			}
		}

		bool IPresenter.TrySelectObject(ILogSource source, TextLogEventTrigger creationTrigger, Func<IInspectedObject, int> disambiguationFunction)
		{
			EnsureTreeView();

			Func<IInspectedObject, bool> predecate = obj =>
			{
				return obj.Owner.Outputs.Any(o => o.LogSource == source)
					&& obj.StateChangeHistory.Any(change => change.Trigger.CompareTo(creationTrigger) == 0);
			};

			var candidates = EnumRoots().SelectMany(EnumTree).Where(predecate).ToList();

			if (candidates.Count > 0)
			{
				IInspectedObject obj;
				if (candidates.Count == 1 || disambiguationFunction == null)
					obj = candidates[0];
				else
					obj = candidates
						.Select(c => new { Obj = c, Rating = disambiguationFunction(c) })
						.OrderByDescending(x => x.Rating)
						.First().Obj;
				var n = FindOrCreateNode(obj);
				if (n != null)
				{
					view.SelectedNodes = new[] { n.Value };
					view.ScrollSelectedNodesInView();
					return true;
				}
			}
			return false;
		}

		void IPresenter.Show()
		{
			view.Show();
		}

		void IViewEvents.OnVisibleChanged()
		{
			if (view.Visible)
			{
				EnsureTreeView();
				EnsureAliveObjectsView();
			}
		}

		void IViewEvents.OnSelectedNodesChanged()
		{
			UpdateSelectedObjectPropertiesAndHistory();
		}

		void IViewEvents.OnPropertiesRowDoubleClicked()
		{
			var selectedProp = GetSelectedProperty();
			if (selectedProp == null)
				return;
			var evt = selectedProp.Value.PropertyView.GetTrigger() as StateInspectorEvent;
			if (evt == null)
				return;
			ShowPropertyChange(evt, false);
		}

		PropertyCellPaintInfo IViewEvents.OnPropertyCellPaint(int rowIndex)
		{
			var ret = new PropertyCellPaintInfo();
			var p = currentProperties[rowIndex];
			if (p.PropertyView != null)
			{
				ret.PaintAsLink = p.PropertyView.IsLink();
			}
			ret.AddLeftPadding = p.IsChildProperty;
			return ret;
		}

		void IViewEvents.OnPropertyCellClicked(int rowIndex)
		{
			if (rowIndex >= 0 && rowIndex < currentProperties.Count)
			{
				var pcView = currentProperties[rowIndex].PropertyView;
				if (pcView == null)
					return;
				var evt = pcView.GetTrigger() as StateInspectorEvent;
				if (evt != null)
				{
					var pc = evt.OriginalEvent as PropertyChange;
					if (pc == null)
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
						view.SelectedNodes = new[] { nodeToSelect.Value };
					}
					else if (pc.ValueType == SI.ValueType.ThreadReference)
					{
						var thread = threads.Items.FirstOrDefault(t => t.ID == pc.Value);
						if (thread != null)
							presentersFacade.ShowThread(thread);
					}
					return;
				}
				var ls = pcView.GetTrigger() as ILogSource;
				if (ls != null)
				{
					presentersFacade.ShowLogSource(ls);
					return;
				}
			}
		}

		NodePaintInfo IViewEvents.OnPaintNode(NodeInfo node, bool getPrimaryPropValue)
		{
			var ret = new NodePaintInfo();
			
			IInspectedObject obj = GetInspectedObject(node);
			if (obj == null)
				return ret;

			ret.DrawingEnabled = true;

			var focusedMessageInfo = GetFocusedMessageEqualRange(obj);
			var liveStatus = obj.GetLiveStatus(focusedMessageInfo);
			var coloring = GetLiveStatusColoring(liveStatus);
			ret.Coloring = coloring;

			if (liveStatus == InspectedObjectLiveStatus.Alive || liveStatus == InspectedObjectLiveStatus.Deleted || obj.IsTimeless)
			{
				if (getPrimaryPropValue)
				{
					ret.PrimaryPropValue = obj.GetCurrentPrimaryPropertyValue(focusedMessageInfo);
				}
			}

			if (obj.Parent == null && focusedMessageInfo != null)
			{
				var m = focusedMessageInfo.FocusedMessage.FocusedMessage;
				if (m != null)
				{
					var focusedLs = m.GetLogSource();
					if (focusedLs != null)
					{
						ret.DrawFocusedMsgMark = obj.EnumInvolvedLogSources().Any(ls => ls == focusedLs);
					}
				}
			}

			return ret;
		}

		void IViewEvents.OnNodeExpanding(NodeInfo node)
		{
			if (!view.TreeSupportsLoadingOnExpansion)
				return;
			EnsureLazyLoadedChildrenCollectionAndEnum(node).FirstOrDefault();
		}

		Tuple<int, int> IViewEvents.OnDrawFocusedMessageMark()
		{
			return stateHistoryFocusedMessage;
		}

		bool IViewEvents.OnGetHistoryItemBookmarked(StateHistoryItem item)
		{
			var change = item.Data as StateInspectorEvent;
			if (change == null || change.Output.LogSource.IsDisposed)
				return false;
			var bmk = bookmarks.Factory.CreateBookmark(
				change.Trigger.Timestamp.Adjust(change.Output.LogSource.TimeOffsets), 
					change.Output.LogSource.GetSafeConnectionId(), change.Trigger.StreamPosition, 0);
			var pos = bookmarks.FindBookmark(bmk);
			return pos.Item2 > pos.Item1;
		}

		void IViewEvents.OnChangeHistoryItemClicked(StateHistoryItem item)
		{
			var change = item != null ? item.Data as StateInspectorEvent : null;
			if (change != null)
				ShowPropertyChange(change, retainFocus: false);
		}

		void IViewEvents.OnChangeHistoryItemKeyEvent(StateHistoryItem item, Key key)
		{
			var change = item != null ? item.Data as StateInspectorEvent : null;
			if (change != null)
			{
				if (key == Key.Enter)
					ShowPropertyChange(change, retainFocus: true);
				else if (key == Key.BookmarkShortcut)
					ToggleBookmark(change);
			}
		}

		void IViewEvents.OnChangeHistorySelectionChanged()
		{
			UpdateSelectedObjectHistory(GetSelectedInspectedObjects());
		}

		void IViewEvents.OnFindCurrentPositionInStateHistory()
		{
			if (stateHistoryFocusedMessage == null)
				return;
			view.ScrollStateHistoryItemIntoView(stateHistoryFocusedMessage.Item1);
		}

		MenuData IViewEvents.OnMenuOpening()
		{
			var menuData = new MenuData()
			{
				Items = new List<MenuData.Item>()
			};
			OnMenu?.Invoke(this, menuData);
			return menuData;
		}

		void IViewEvents.OnCopyShortcutPressed()
		{
			var sel = view.SelectedPropertiesRow;
			if (sel == null || sel.Value >= currentProperties.Count)
				return;
			var str = currentProperties[sel.Value].PropertyView.ToClipboardString();
			if (!string.IsNullOrEmpty(str))
				clipboardAccess.SetClipboard(str);
		}

		void IViewEvents.OnDeleteKeyPressed()
		{
			var objs = GetSelectedInspectedObjects();
			if (objs.All(x => x.Parent == null))
			{
				var logSources = objs.Select(obj => obj.GetPrimarySource()).Distinct().ToArray();
				if (logSources.Length > 0)
				{
					sourcesManagerPresenter.StartDeletionInteraction(logSources);
				}
			}
		}

		void InvalidateTree()
		{
			treeViewInvalidated = true;
			if (!view.Visible)
				return;
			EnsureTreeView();
		}

		void EnsureTreeView()
		{
			if (!treeViewInvalidated)
				return;
			treeViewInvalidated = false;

			var oldRoots = view.EnumCollection(view.RootNodesCollection).ToList();
			var newRoots = new List<NodeInfo>();
			bool updateStarted = false;

			MarkAllViewPartsAsDead();
			foreach (var group in model.Groups)
			{
				TreeViewPart part;
				if (!viewPartsCache.TryGetValue(group.Key, out part))
				{
					viewPartsCache.Add(group.Key, part = new TreeViewPart() { Key = group.Key });

					foreach (var rootObj in group.Roots)
					{
						if (!updateStarted) 
						{
							view.BeginTreeUpdate ();
							updateStarted = true;
						}
						var nodesToCollapse = new List<NodeInfo>();
						var rootNode = CreateViewNode(view, new NodesCollectionInfo(), rootObj, 0, nodesToCollapse);
						part.RootNodes.Add(rootNode);
						view.ExpandAll(rootNode);
						nodesToCollapse.ForEach(view.Collapse);
					}
				}
				newRoots.AddRange(part.RootNodes);
				part.OutputIsAlive = true;
			}
			RemoveDeadViewPartsFromCache();

			newRoots.Sort((n1, n2) => MessageTimestamp.Compare(GetNodeTimestamp(n1), GetNodeTimestamp(n2)));

			bool propsNeedUpdating = false;
			if (!newRoots.SequenceEqual(oldRoots))
			{
				if (!updateStarted) 
					view.BeginTreeUpdate ();

				var oldSelectedNodes = view.SelectedNodes;
				var newSelectedNodes = (
					from selectedNode in oldSelectedNodes
					let selectedObj = GetInspectedObject(selectedNode)
					where selectedObj != null
					let selectedObjRoot = selectedObj.GetRoot()
					where newRoots.Any(r => r.Tag == selectedObjRoot)
					select selectedNode
				).ToArray();

				view.Clear(view.RootNodesCollection);
				foreach (var rootNode in newRoots)
					view.AddNode(view.RootNodesCollection, rootNode);

				if (newSelectedNodes.Length > 0)
				{
					view.SelectedNodes = newSelectedNodes;
					selectedObjectsPathsBeforeSelectionLoss.Clear();
				}
				else if (oldSelectedNodes.Length > 0)
				{
					selectedObjectsPathsBeforeSelectionLoss.Clear();
					selectedObjectsPathsBeforeSelectionLoss.AddRange(
						from selectedNode in oldSelectedNodes
						let selectedObj = GetInspectedObject(selectedNode)
						where selectedObj != null
						select new InspectedObjectPath(selectedObj)
					);
				}
				else if (selectedObjectsPathsBeforeSelectionLoss.Count > 0)
				{
					newSelectedNodes = (
						from p in selectedObjectsPathsBeforeSelectionLoss
						let n = p.Follow(this)
						where n != null
						select n.Value
					).ToArray();
					if (newSelectedNodes.Length > 0)
					{
						view.SelectedNodes = newSelectedNodes;
						view.ScrollSelectedNodesInView();
						selectedObjectsPathsBeforeSelectionLoss.Clear();
					}
				}

				view.EndTreeUpdate();
			}
			newRoots.ForEach(UpdateRootNodeText);

			if (propsNeedUpdating)
			{
				UpdateSelectedObjectPropertiesAndHistory();
			}
		}

		private void RemoveDeadViewPartsFromCache()
		{
			foreach (TreeViewPart rec in new List<TreeViewPart>(viewPartsCache.Values))
			{
				if (rec.OutputIsAlive)
					continue;
				viewPartsCache.Remove(rec.Key);
			}
		}

		void RemoveMissingGroupsFromCache()
		{
			MarkAllViewPartsAsDead();
			TreeViewPart part;
			foreach (var group in model.Groups)
				if (viewPartsCache.TryGetValue(group.Key, out part))
					part.OutputIsAlive = true;
			RemoveDeadViewPartsFromCache();
		}

		private void MarkAllViewPartsAsDead()
		{
			foreach (TreeViewPart rec in viewPartsCache.Values)
				rec.OutputIsAlive = false;
		}

		void EnsureAliveObjectsView()
		{
			if (!aliveObjectsViewInvalidated)
				return;
			aliveObjectsViewInvalidated = false;

			UpdateLiveObjectsColoring(view.RootNodesCollection);

			view.InvalidateTree();

			UpdateSelectedObjectPropertiesAndHistory();

			FocusedMessageInfo focusedMsgInfo = GetFocusedMessageInfo();
			
			view.SetCurrentTimeLabelText(focusedMsgInfo.FocusedMessage != null ? "at " + focusedMsgInfo.FocusedMessage.Time.ToString() : "");
		}

		private void UpdateLiveObjectsColoring( NodesCollectionInfo collection)
		{
			foreach (NodeInfo node in EnumViewTree(collection))
			{
				var obj = GetInspectedObject(node);
				if (obj == null)
					continue;
				var liveStatus = obj.GetLiveStatus(GetFocusedMessageEqualRange(obj));
				var coloring = GetLiveStatusColoring(liveStatus);
				if (node.Coloring != coloring)
				{
					view.SetNodeColoring(node, coloring);
				}
			}
		}

		NodeColoring GetLiveStatusColoring(InspectedObjectLiveStatus liveStatus)
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

		private void UpdateRootNodeText(NodeInfo node)
		{
			IInspectedObject root = GetInspectedObject(node);
			if (root == null)
				return;
			var ls = root.EnumInvolvedLogSources().FirstOrDefault();
			if (ls == null)
				return;
			var nodeText = new StringBuilder(root.Id);
			if (!string.IsNullOrEmpty(ls.Annotation))
				nodeText.AppendFormat(" ({0})", ls.Annotation);
			if (root.Comment != "")
				nodeText.AppendFormat(" ({0})", root.Comment);
			view.SetNodeText(node, nodeText.ToString());
		}

		static IInspectedObject GetInspectedObject(NodeInfo node)
		{
			var inspectedObject = node.Tag as IInspectedObject;
			if (inspectedObject == null)
				return null;
			if (inspectedObject.EnumInvolvedLogSources().Any(ls => ls.IsDisposed)) // todo: get rid of this check
				return null;
			return inspectedObject;
		}

		MessageTimestamp GetNodeTimestamp(NodeInfo node)
		{
			var obj = GetInspectedObject(node);
			StateInspectorEvent referenceEvt = null;
			if (obj != null)
				referenceEvt = obj.StateChangeHistory.FirstOrDefault();
			if (referenceEvt != null)
				return referenceEvt.Trigger.Timestamp.Adjust(referenceEvt.Output.LogSource.TimeOffsets);
			return MessageTimestamp.MaxValue;
		}

		NodeInfo CreateViewNode(IView view, NodesCollectionInfo collectionToAddViewNode, IInspectedObject modelNode, int level, List<NodeInfo> nodesToCollapse)
		{
			string nodeText = modelNode.DisplayName;
			if (modelNode.Comment != "")
				nodeText += " (" + modelNode.Comment + ")";

			bool createCollapsed = false;
			bool createLazilyLoaded = false;
			if (level == 1 || modelNode.IsTimeless)
			{
				if (OnNodeCreated != null)
				{
					var args = new NodeCreatedEventArgs() { NodeObject = modelNode };
					OnNodeCreated(this, args);
					createCollapsed = args.CreateCollapsed.GetValueOrDefault(createCollapsed);
					createLazilyLoaded = args.CreateLazilyLoaded.GetValueOrDefault(createLazilyLoaded);
				}
				if (createLazilyLoaded && !view.TreeSupportsLoadingOnExpansion)
				{
					createLazilyLoaded = false;
				}
			}

			var viewNode = view.CreateNode(nodeText, modelNode, collectionToAddViewNode);
			if (createLazilyLoaded)
			{
				view.CreateNode(lazyLoadTag, lazyLoadTag, viewNode.ChildrenNodesCollection);
			}
			else
			{
				foreach (var child in modelNode.Children)
					CreateViewNode(view, viewNode.ChildrenNodesCollection, child, level + 1, nodesToCollapse);
			}
			if (createCollapsed)
			{
				nodesToCollapse.Add(viewNode);
			}
			return viewNode;
		}

		StateHistoryItem MakeStateHistoryItem(StateInspectorEventInfo evtInfo, bool isSelected, bool showTimeDeltas, StateInspectorEvent prevSelectedEvent)
		{
			var evt = evtInfo.Event;
			StateHistoryItem ret = new StateHistoryItem() { Data = evt };
			if (showTimeDeltas)
				if (isSelected && prevSelectedEvent != null)
					ret.Time = TimeUtils.TimeDeltaToString(
						evt.Trigger.Timestamp.ToUnspecifiedTime() - prevSelectedEvent.Trigger.Timestamp.ToUnspecifiedTime(),
						addPlusSign: true);
				else
					ret.Time = "";
			else
				ret.Time = FormatTimestampt(evt);
			var messageFormatter = new StateHistoryMessageFormatter() { shortNames = this.shortNames };
			evt.OriginalEvent.Visit(messageFormatter);
			if (evtInfo.InspectedObjectNr != 0)
				ret.Message = string.Format("#{0}: {1}", evtInfo.InspectedObjectNr, messageFormatter.message);
			else
				ret.Message = messageFormatter.message;
			return ret;
		}

		IInspectedObject[] GetSelectedInspectedObjects()
		{
			return view.SelectedNodes.Select(n => GetInspectedObject(n)).Where(obj => obj != null).ToArray();
		}

		StateInspectorEvent[] GetSelectedStateHistoryEvents()
		{
			return view
				.SelectedStateHistoryEvents
				.Select(item => item.Data as StateInspectorEvent)
				.Where(x => x != null)
				.ToArray();
		}

		PropertyInfo? GetSelectedProperty()
		{
			int? row = view.SelectedPropertiesRow;
			if (row != null)
				return currentProperties[row.Value];
			return null;
		}

		void UpdateSelectedObjectPropertiesAndHistory()
		{
			var objs = GetSelectedInspectedObjects();

			PropertyInfo? savedSelectedProperty = GetSelectedProperty();
			UpdateCurrentPropertiesCollection(objs);
			UpdatePropertiesDataGrid(savedSelectedProperty);

			UpdateSelectedObjectHistory(objs);
		}

		void UpdateSelectedObjectHistory(IInspectedObject[] objs)
		{
			var olsSelection = GetSelectedStateHistoryEvents();
			bool updateItemsFlag = // if false - skip some steps to reduce flickering
				!currentObjects.SetEquals(objs) ||
				olsSelection.Length != stateHistorySelectedRowsCount;
			currentObjects.Clear();
			foreach (var obj in objs)
				currentObjects.Add(obj);
			var newSelectedIndexes = new HashSet<int>();
			view.BeginUpdateStateHistoryList(updateItemsFlag, updateItemsFlag);
			try
			{
				stateHistoryFocusedMessage = null;
				var changes =
					objs
					.ZipWithIndex()
					.Where(obj => !obj.Value.IsTimeless)
					.Select(obj => obj.Value.StateChangeHistory.Select(e => new StateInspectorEventInfo()
					{
						Object = obj.Value,
						InspectedObjectNr = objs.Length >= 2 ? obj.Key + 1 : 0,
						Event = e
					}))
					.ToArray()
					.MergeSortedSequences(new EventsComparer())
					.ToList();
				if (updateItemsFlag)
				{
					foreach (var change in changes.ZipWithIndex())
					{
						if (olsSelection.Length > 0 
						 && olsSelection.Any(i => i.Output == change.Value.Event.Output && i.Index == change.Value.Event.Index))
						{
							newSelectedIndexes.Add(change.Key);
						}
					}
					stateHistorySelectedRowsCount = newSelectedIndexes.Count;
					StateInspectorEvent prevSelectedEvent = null;
					foreach (var change in changes.ZipWithIndex())
					{
						bool isSelected = newSelectedIndexes.Contains(change.Key);
						view.AddStateHistoryItem(MakeStateHistoryItem(change.Value, 
							isSelected, stateHistorySelectedRowsCount > 1, prevSelectedEvent));
						if (isSelected)
							prevSelectedEvent = change.Value.Event;
					}
				}
				var focusedMessageInfo = GetFocusedMessageEqualRange(objs.FirstOrDefault());
				if (focusedMessageInfo != null)
				{
					stateHistoryFocusedMessage = new ListUtils.VirtualList<StateInspectorEvent>(changes.Count, 
						i => changes[i].Event).CalcFocusedMessageEqualRange(focusedMessageInfo.FocusedMessage);
				}
			}
			finally
			{
				view.EndUpdateStateHistoryList(
					updateItemsFlag ? newSelectedIndexes.ToArray() : null,
					updateItemsFlag,
					!updateItemsFlag);
			}
		}

		void UpdatePropertiesDataGrid(PropertyInfo? savedSelectedProperty)
		{
			view.SetPropertiesDataSource(currentProperties.Select(p => p.ToDataSourceItem()).ToArray());

			if (savedSelectedProperty != null)
				for (int rowIdx = 0; rowIdx < currentProperties.Count; ++rowIdx)
					if (PropertyInfo.Equal(currentProperties[rowIdx], savedSelectedProperty.Value))
					{
						view.SelectedPropertiesRow = rowIdx;
						break;
					}
		}

		void UpdateCurrentPropertiesCollection(IInspectedObject[] objs)
		{
			bool isMultiObjectMode = objs.Length >= 2;
			int objectIndex = 0;
			currentProperties.Clear();
			foreach (var obj in objs)
			{
				foreach (var dynamicProperty in obj.GetCurrentProperties(GetFocusedMessageEqualRange(obj)))
				{
					var idProperty = dynamicProperty.Value as IdPropertyView;
					currentProperties.Add(new PropertyInfo(
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
		}

		FocusedMessageEventsRange GetFocusedMessageEqualRange(IInspectedObject obj)
		{
			if (obj == null)
				return null;
			return GetFocusedMessageEqualRange(obj.Owner);
		}

		FocusedMessageEventsRange GetFocusedMessageEqualRange(IStateInspectorOutputsGroup forGroup)
		{
			FocusedMessageEventsRange ret;
			if (focusedMessagePositionsCache.TryGetValue(forGroup, out ret))
				return ret;
			var equalRange = forGroup.Events.CalcFocusedMessageEqualRange(GetFocusedMessageInfo());
			ret = new FocusedMessageEventsRange(GetFocusedMessageInfo(), equalRange);
			focusedMessagePositionsCache.Add(forGroup, ret);
			return ret;
		}

		FocusedMessageInfo GetFocusedMessageInfo()
		{
			if (focusedMessageInfoCache == null)
				focusedMessageInfoCache = new FocusedMessageInfo(
					loadedMessagesPresenter.LogViewerPresenter.FocusedMessage);
			return focusedMessageInfoCache;
		}

		IEnumerable<NodeInfo> EnumViewTree(NodesCollectionInfo rootCollection)
		{
			foreach (NodeInfo node in view.EnumCollection(rootCollection))
			{
				yield return node;
				foreach (NodeInfo child in EnumViewTree(node.ChildrenNodesCollection))
					yield return child;
			}
		}

		IEnumerable<IInspectedObject> EnumTree(IInspectedObject obj)
		{
			return Enumerable.Repeat(obj, 1).Concat(obj.Children.SelectMany(EnumTree));
		}

		IEnumerable<IInspectedObject> EnumRoots()
		{
			return model.Groups.SelectMany(g => g.Roots);
		}

		NodeInfo? FindOrCreateNode(IInspectedObject obj)
		{
			return new InspectedObjectPath(obj).Follow(this);
		}

		void HandleFocusedMessageChange()
		{
			// invalidate caches that depend on focused message
			focusedMessageInfoCache = null;
			focusedMessagePositionsCache.Clear();

			aliveObjectsViewInvalidated = true;
			if (!view.Visible)
				return;

			EnsureAliveObjectsView();
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

		IEnumerable<NodeInfo> EnsureLazyLoadedChildrenCollectionAndEnum(NodeInfo node)
		{
			var viewNodes = view.EnumCollection(node.ChildrenNodesCollection);
			bool lazyLoadingNeedsTesting = view.TreeSupportsLoadingOnExpansion;
			bool needsLazyLoading = false;
			foreach (var child in viewNodes)
			{
				if (lazyLoadingNeedsTesting)
				{
					needsLazyLoading = child.Tag == (object)lazyLoadTag;
					if (needsLazyLoading)
						break;
					lazyLoadingNeedsTesting = false;
				}
				yield return child;
			}
			if (needsLazyLoading)
			{
				view.Clear(node.ChildrenNodesCollection);
				var nodesToCollapse = new List<NodeInfo>();
				var ret = new List<NodeInfo>();
				var obj = GetInspectedObject(node);
				if (obj == null)
					yield break;
				foreach (var child in obj.Children)
					ret.Add(CreateViewNode(view, node.ChildrenNodesCollection, child, 2, nodesToCollapse));
				UpdateLiveObjectsColoring(node.ChildrenNodesCollection);
				foreach (var child in ret)
					yield return child;
			}
		}

		class TreeViewPart
		{
			public string Key;
			public List<NodeInfo> RootNodes = new List<NodeInfo>();
			public bool OutputIsAlive;
		};

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
			public readonly string OutputsGroupKey;
			public readonly List<string> Path;

			public InspectedObjectPath(IInspectedObject obj)
			{
				this.Path = new List<string>();
				this.OutputsGroupKey = obj.Owner.Key;
				for (IInspectedObject i = obj; i != null; i = i.Parent)
					Path.Add(i.Id);
				Path.Reverse();
			}

			public NodeInfo? Follow(StateInspectorPresenter owner)
			{
				NodeInfo? ret = null;
				bool inspectingRoots = true;
				foreach (var segment in Path)
				{
					NodeInfo? found = (
						from n in inspectingRoots ?
							  owner.view.EnumCollection(owner.view.RootNodesCollection)
							: owner.EnsureLazyLoadedChildrenCollectionAndEnum(ret.Value)
						let obj = GetInspectedObject(n)
						where obj != null
						where inspectingRoots ? (obj.Id == segment && obj.Owner.Key == OutputsGroupKey) : (obj.Id == segment)
						select new NodeInfo?(n)
					).FirstOrDefault();
					if (found == null)
						return null;
					ret = found;
					inspectingRoots = false;
				}
				return ret;
			}
		};

		class StateHistoryMessageFormatter: IEventsVisitor
		{
			public string message = "???";
			public IUserNamesProvider shortNames;

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

		readonly IView view;
		readonly IStateInspectorVisualizerModel model;
		readonly IUserNamesProvider shortNames;
		readonly IModelThreads threads;
		readonly IBookmarks bookmarks;
		readonly IPresentersFacade presentersFacade;
		readonly IClipboardAccess clipboardAccess;
		readonly SourcesManager.IPresenter sourcesManagerPresenter;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly Dictionary<string, TreeViewPart> viewPartsCache = new Dictionary<string, TreeViewPart>();
		readonly List<PropertyInfo> currentProperties = new List<PropertyInfo>();
		readonly HashSet<IInspectedObject> currentObjects = new HashSet<IInspectedObject>();
		readonly Dictionary<IStateInspectorOutputsGroup, FocusedMessageEventsRange> focusedMessagePositionsCache = new Dictionary<IStateInspectorOutputsGroup, FocusedMessageEventsRange>();
		readonly List<InspectedObjectPath> selectedObjectsPathsBeforeSelectionLoss = new List<InspectedObjectPath>();
		bool treeViewInvalidated;
		bool aliveObjectsViewInvalidated;
		FocusedMessageInfo focusedMessageInfoCache;
		Tuple<int, int> stateHistoryFocusedMessage;
		int stateHistorySelectedRowsCount;
		static readonly string lazyLoadTag = "(load-me)";
	}
}

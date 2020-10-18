using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Collections.Immutable;

namespace LogJoint.Postprocessing.StateInspector
{
	public class StateInspectorVisualizerModel : IStateInspectorVisualizerModel
	{
		public StateInspectorVisualizerModel(
			IManagerInternal postprocessorsManager,
			ILogSourcesManager logSourcesManager,
			IChangeNotification changeNotification,
			IUserNamesProvider shortNamesManager)
		{
			this.postprocessorsManager = postprocessorsManager;
			this.shortNamesManager = shortNamesManager;

			int postprocessorsVersion = 0;
			postprocessorsManager.Changed += (sender, args) =>
			{
				postprocessorsVersion++;
				changeNotification.Post();
			};
			var updateGroups = Updaters.Create(
				() => logSourcesManager.VisibleItems,
				() => postprocessorsVersion,
				(visibleSources, _) =>
				{
					UpdateOutputs();
					UpdateOutputGroups();
				}
			);
			this.getGroupsList = Selectors.Create(
				() =>
				{
					updateGroups();
					return groups;
				},
				dict => (IReadOnlyList<IStateInspectorOutputsGroup>)dict.Values.ToImmutableArray()
			);
		}

		IReadOnlyList<IStateInspectorOutputsGroup> IStateInspectorVisualizerModel.Groups => getGroupsList();

		void UpdateOutputs()
		{
			var newOutputs = new HashSet<IStateInspectorOutput>(
				postprocessorsManager.LogSourcePostprocessors
					.Where(output => output.OutputStatus == LogSourcePostprocessorState.Status.Finished || output.OutputStatus == LogSourcePostprocessorState.Status.Outdated)
					.Select(output => output.OutputData)
					.OfType<IStateInspectorOutput>()
					.Where(output => !output.LogSource.IsDisposed)
					.Where(output => output.LogSource.Visible)
				);
			if (!newOutputs.SetEquals(outputs))
			{
				outputs = newOutputs;
				UpdateOutputGroups();
			}
		}

		void UpdateOutputGroups()
		{
			var newGroups =
				outputs
				.GroupBy(output => output.RotatedLogPartToken, new PartsOfSameLogEqualityComparer())
				.Select(group => new RotatedLogGroup()
				{
					key = string.Join("#", group.Select(output => output.LogSource.GetSafeConnectionId().GetHashCode())),
					parts = group.ToList()
				});

			var oldGroups = groups;
			var builder = ImmutableDictionary.CreateBuilder<string, RotatedLogGroup>();

			foreach (var newGroup in newGroups)
			{
				RotatedLogGroup existingGroup;
				if (oldGroups.TryGetValue(newGroup.key, out existingGroup))
					builder.Add(newGroup.key, existingGroup);
				else
					builder.Add(newGroup.key, newGroup);
			}
			groups = builder.ToImmutable();

			foreach (var group in groups.Values)
			{
				if (!group.isInitialized)
				{
					group.parts.Sort((x, y) => x.RotatedLogPartToken.CompareTo(y.RotatedLogPartToken));

					int evtIdx = 0;
					group.events = new List<StateInspectorEvent>();
					foreach (var part in group.parts)
						foreach (var e in part.Events)
							group.events.Add(new StateInspectorEvent(group, part, (TextLogEventTrigger)e.Trigger, e, evtIdx++));

					TreeBuilder treeBuilder = new TreeBuilder(group, shortNamesManager);
					treeBuilder.AddEventsFrom(group);
					group.roots = treeBuilder.Build();
					group.displayNames = MakeDisplayNamesMap(group.events);

					group.isInitialized = true;
				}
			}
		}

		Dictionary<string, string> MakeDisplayNamesMap(IEnumerable<StateInspectorEvent> events)
		{
			var result = new Dictionary<string, string>();
			foreach (var e in events)
			{
				if (e.OriginalEvent is ObjectCreation creation && !string.IsNullOrEmpty(creation.DisplayName))
				{
					result[creation.ObjectId] = creation.DisplayName;
				}
			}
			return result;
		}

		class RotatedLogGroup : IStateInspectorOutputsGroup
		{
			public string key;
			public bool isInitialized;
			public List<IStateInspectorOutput> parts;
			public List<StateInspectorEvent> events;
			public IReadOnlyList<IInspectedObject> roots;
			public Dictionary<string, string> displayNames;

			string IStateInspectorOutputsGroup.Key
			{
				get { return key; }
			}

			IReadOnlyList<IInspectedObject> IStateInspectorOutputsGroup.Roots
			{
				get { return roots; }
			}

			IReadOnlyList<StateInspectorEvent> IStateInspectorOutputsGroup.Events
			{
				get { return events; }
			}

			IReadOnlyList<IStateInspectorOutput> IStateInspectorOutputsGroup.Outputs
			{
				get { return parts; }
			}

			bool IStateInspectorOutputsGroup.TryGetDisplayName(string objectId, out string displayName)
			{
				displayName = null;
				return objectId != null && displayNames.TryGetValue(objectId, out displayName);
			}
		};

		readonly IManagerInternal postprocessorsManager;
		readonly IUserNamesProvider shortNamesManager;
		HashSet<IStateInspectorOutput> outputs = new HashSet<IStateInspectorOutput>();
		ImmutableDictionary<string, RotatedLogGroup> groups = ImmutableDictionary<string, RotatedLogGroup>.Empty;
		readonly AsyncInvokeHelper outputsUpdateInvocation;
		readonly Func<IReadOnlyList<IStateInspectorOutputsGroup>> getGroupsList;
	};
}

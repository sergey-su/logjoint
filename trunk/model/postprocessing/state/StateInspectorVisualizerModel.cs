using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using LogJoint.Analytics;

namespace LogJoint.Postprocessing.StateInspector
{
	public class StateInspectorVisualizerModel : IStateInspectorVisualizerModel
	{
		public StateInspectorVisualizerModel(
			IPostprocessorsManager postprocessorsManager,
			ILogSourcesManager logSourcesManager,
			ISynchronizationContext invokeSync,
			IUserNamesProvider shortNamesManager)
		{
			this.postprocessorsManager = postprocessorsManager;
			this.outputsUpdateInvocation = new AsyncInvokeHelper(invokeSync, (Action)UpdateOutputs) { ForceAsyncInvocation = true };
			this.shortNamesManager = shortNamesManager;

			postprocessorsManager.Changed += (sender, args) =>
			{
				outputsUpdateInvocation.Invoke();
			};
			logSourcesManager.OnLogSourceVisiblityChanged += (sender, args) =>
			{
				outputsUpdateInvocation.Invoke();
			};

			UpdateOutputs();
		}

		IEnumerable<IStateInspectorOutputsGroup> IStateInspectorVisualizerModel.Groups
		{
			get { return groups.Values; }
		}

		public event EventHandler Changed;


		void UpdateOutputs()
		{
			var newOutputs = new HashSet<IStateInspectorOutput>(
				postprocessorsManager.LogSourcePostprocessorsOutputs
					.Where(output => output.OutputStatus == LogSourcePostprocessorOutput.Status.Finished || output.OutputStatus == LogSourcePostprocessorOutput.Status.Outdated)
					.Select(output => output.OutputData)
					.OfType<IStateInspectorOutput>()
					.Where(output => !output.LogSource.IsDisposed)
					.Where(output => output.LogSource.Visible)
				);
			if (!newOutputs.SetEquals(outputs))
			{
				outputs = newOutputs;
				UpdateOutputGroups();
				if (Changed != null)
					Changed(this, EventArgs.Empty);
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
			groups = new Dictionary<string, RotatedLogGroup>();

			foreach (var newGroup in newGroups)
			{
				RotatedLogGroup existingGroup;
				if (oldGroups.TryGetValue(newGroup.key, out existingGroup))
					groups.Add(newGroup.key, existingGroup);
				else
					groups.Add(newGroup.key, newGroup);
			}

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

					group.isInitialized = true;
				}
			}
		}

		class RotatedLogGroup : IStateInspectorOutputsGroup
		{
			public string key;
			public bool isInitialized;
			public List<IStateInspectorOutput> parts;
			public List<StateInspectorEvent> events;
			public List<IInspectedObject> roots;

			string IStateInspectorOutputsGroup.Key
			{
				get { return key; }
			}

			IEnumerable<IInspectedObject> IStateInspectorOutputsGroup.Roots
			{
				get { return roots; }
			}

			IList<StateInspectorEvent> IStateInspectorOutputsGroup.Events
			{
				get { return events; }
			}

			IEnumerable<IStateInspectorOutput> IStateInspectorOutputsGroup.Outputs
			{
				get { return parts; }
			}
		};

		readonly IPostprocessorsManager postprocessorsManager;
		readonly IUserNamesProvider shortNamesManager;
		HashSet<IStateInspectorOutput> outputs = new HashSet<IStateInspectorOutput>();
		Dictionary<string, RotatedLogGroup> groups = new Dictionary<string, RotatedLogGroup>();
		readonly AsyncInvokeHelper outputsUpdateInvocation;
	};
}

using System;
using System.Linq;
using System.Collections.Generic;
using LogJoint.Analytics;

namespace LogJoint.Postprocessing.Timeline
{
	public class TimelineVisualizerModel : ITimelineVisualizerModel
	{
		public TimelineVisualizerModel(
			IPostprocessorsManager postprocessorsManager,
			ILogSourcesManager logSourcesManager,
			IUserNamesProvider shortNames,
			ILogSourceNamesProvider logSourceNamesProvider)
		{
			this.postprocessorsManager = postprocessorsManager;
			this.entitiesComparer = TimelineEntitiesComparer.Instance;
			this.shortNames = shortNames;
			this.logSourceNamesProvider = logSourceNamesProvider;

			postprocessorsManager.Changed += (sender, args) => UpdateOutputs(invalidateGroupContents: true);
			logSourcesManager.OnLogSourceTimeOffsetChanged += (logSource, args) => UpdateAll();
			logSourcesManager.OnLogSourceAnnotationChanged += (logSource, args) => UpdateOutputsSequenceDiagramNames(true);
			logSourcesManager.OnLogSourceVisiblityChanged += (logSource, args) => UpdateOutputs(invalidateGroupContents: false);

			UpdateOutputs(invalidateGroupContents: false);
		}

		public event EventHandler EverythingChanged;
		public event EventHandler SequenceDiagramNamesChanged;
		
		ICollection<ITimelinePostprocessorOutput> ITimelineVisualizerModel.Outputs
		{
			get { return outputs; }
		}

		DateTime ITimelineVisualizerModel.Origin { get { return origin; } }

		IList<IActivity> ITimelineVisualizerModel.Activities { get { return activities; } }

		IList<IEvent> ITimelineVisualizerModel.Events { get { return events; } }

		Tuple<TimeSpan, TimeSpan> ITimelineVisualizerModel.AvailableRange { get { return Tuple.Create(availableRangeBegin, availableRangeEnd); } }

		Tuple<IActivity, IActivity> ITimelineVisualizerModel.GetPairedActivities(IActivity a)
		{
			if (a.ActivityMatchingId == null)
				return null;
			MatchedActivitiesPair pair;
			if (outgoingNetworkingActivities.TryGetValue(a.ActivityMatchingId, out pair))
				return Tuple.Create(pair.OutgoingActivity, pair.IncomingActivity);
			return null;
		}

		IEntitiesComparer ITimelineVisualizerModel.Comparer { get { return entitiesComparer; } }


		void UpdateOutputs(bool invalidateGroupContents)
		{
			if (invalidateGroupContents)
				foreach (var g in outputsGroups.Values)
					g.IsInitialized = false;

			var newOutputs = new HashSet<ITimelinePostprocessorOutput>(
				postprocessorsManager.LogSourcePostprocessorsOutputs
					.Where(output => output.OutputStatus == LogSourcePostprocessorOutput.Status.Finished || output.OutputStatus == LogSourcePostprocessorOutput.Status.Outdated)
					.Select(output => output.OutputData)
					.OfType<ITimelinePostprocessorOutput>()
					.Where(output => !output.LogSource.IsDisposed)
					.Where(output => output.LogSource.Visible)
				);
			if (!newOutputs.SetEquals(outputs))
			{
				outputs = newOutputs;
				UpdateAll();
			}
		}

		private void UpdateAll()
		{
			UpdateRotatedLogGroups();
			UpdateOriginAndPostprocessorOutputsBases();
			UpdateOutputsSequenceDiagramNames(false);
			UpdateActivitiesAndEvents();
			UpdateAvailableRange();
			FireChanged();
		}

		private void FireChanged()
		{
			if (EverythingChanged != null)
				EverythingChanged(this, EventArgs.Empty);
		}

		void UpdateOriginAndPostprocessorOutputsBases()
		{
			var tmp = outputsGroups.Values.Select(
				group => new { group = group, originWithTimeOffset = group.Origin }).ToArray();
			DateTime earliestGroupTime = DateTime.MaxValue;
			RotatedLogGroup earliestGroup = null;
			foreach (var i in tmp)
			{
				if (i.originWithTimeOffset.HasValue && i.originWithTimeOffset < earliestGroupTime)
				{
					earliestGroupTime = i.originWithTimeOffset.Value;
					earliestGroup = i.group;
				}
			}
			origin = earliestGroupTime;
			foreach (var i in tmp)
				if (i.originWithTimeOffset.HasValue)
					foreach (var j in i.group.Outputs)
						j.SetTimelineOffset(i.originWithTimeOffset.Value - origin);
		}

		void UpdateOutputsSequenceDiagramNames(bool raiseChangeEvent)
		{
			var names = logSourceNamesProvider.GetSourcesSequenceDiagramNames(
				outputs.Select(output => output.LogSource),
				outputsGroups.Values
					.Where(g => !string.IsNullOrEmpty(g.GroupDisplayName))
					.SelectMany(g => g.Outputs.Select(
						output => new { LogSource = output.LogSource, SuggestedName = g.GroupDisplayName }
					))
					.ToDictionary(x => x.LogSource, x => new LogSourceNames() { 
						RoleInstanceName = x.SuggestedName 
					})
			);

			bool changed = false;
			foreach (var output in outputs)
			{
				var name = names[output.LogSource];
				if (name.RoleInstanceName != output.SequenceDiagramName)
				{
					output.SetSequenceDiagramName(name.RoleInstanceName);
					changed = true;
				}
			}
			if (changed && raiseChangeEvent && SequenceDiagramNamesChanged != null)
				SequenceDiagramNamesChanged(this, EventArgs.Empty);
		}

		void UpdateRotatedLogGroups()
		{
			var newGroups =
				outputs
				.GroupBy(output => output.RotatedLogPartToken, new PartsOfSameLogEqualityComparer())
				.Select(group => new RotatedLogGroup()
				{
					Key = string.Join("#", group.Select(output => output.GetHashCode())),
					Outputs = group.ToList()
				});

			var oldGroups = outputsGroups;
			outputsGroups = new Dictionary<string, RotatedLogGroup>();

			foreach (var newGroup in newGroups)
			{
				RotatedLogGroup existingGroup;
				if (oldGroups.TryGetValue(newGroup.Key, out existingGroup))
					outputsGroups.Add(newGroup.Key, existingGroup);
				else
					outputsGroups.Add(newGroup.Key, newGroup);
			}

			foreach (var group in outputsGroups.Values)
			{
				if (!group.IsInitialized)
				{
					group.Outputs.Sort((x, y) => x.RotatedLogPartToken.CompareTo(y.RotatedLogPartToken));

					var builder = new TimelineBuilder(entitiesComparer, shortNames);
					var last = group.Outputs.Last();
					foreach (var output in group.Outputs)
						builder.AddEvents(output, output.TimelineEvents, isLastEventsSet: output == last);
					var timelineData = builder.FinalizeAndGetTimelineData();
					group.Activities = timelineData.Activities;
					group.Events = timelineData.Events;
					group.Origin = timelineData.Origin;
					group.GroupDisplayName = timelineData.TimelineDisplayName;

					group.IsInitialized = true;
				}
			}
		}

		void UpdateActivitiesAndEvents()
		{
			activities.Clear();
			activities.AddRange(
				outputsGroups
				.Select(group => (IEnumerable<IActivity>)(group.Value.Activities))
				.ToArray()
				.MergeSortedSequences(entitiesComparer)
			);

			DetectMatchingActivities();

			events.Clear();
			events.AddRange(
				outputsGroups
				.Select(output => (IEnumerable<IEvent>)(output.Value.Events))
				.ToArray()
				.MergeSortedSequences(entitiesComparer)
			);
		}

		private void DetectMatchingActivities()
		{
			outgoingNetworkingActivities.Clear();
			foreach (var a in activities.Where(a => a.ActivityMatchingId != null))
			{
				if (a.Type == ActivityType.OutgoingNetworking)
				{
					outgoingNetworkingActivities[a.ActivityMatchingId] = new MatchedActivitiesPair()
					{
						OutgoingActivity = a
					};
				}
				else if (a.Type == ActivityType.IncomingNetworking)
				{
					MatchedActivitiesPair pair;
					if (outgoingNetworkingActivities.TryGetValue(a.ActivityMatchingId, out pair))
					{
						if (pair.IncomingActivity == null)
						{
							pair.IncomingActivity = a;
						}
						else
						{
							a.ActivityMatchingId.ToString(); // todo: log that
						}
					}
				}
			}
			var unpairedActivitiesIds = outgoingNetworkingActivities
				.Where(a => a.Value.IncomingActivity == null)
				.Select(a => a.Key)
				.ToArray();
			foreach (var id in unpairedActivitiesIds)
				outgoingNetworkingActivities.Remove(id);
		}

		void UpdateAvailableRange()
		{
			availableRangeBegin = new TimeSpan();
			availableRangeEnd = new TimeSpan();
			if (activities.Count > 0)
			{
				ExpandAvailbaleRangeBegin(activities[0].GetTimelineBegin());
				foreach (var t in activities.Select(a => a.GetTimelineEnd()))
					ExpandAvailbaleRangeEnd(t);
			}
			if (events.Count > 0)
			{
				ExpandAvailbaleRangeBegin(events[0].GetTimelineTime());
				foreach (var t in events.Select(e => e.GetTimelineTime()))
					ExpandAvailbaleRangeEnd(t);
			}
		}

		void ExpandAvailbaleRangeBegin(TimeSpan valueToInclude)
		{
			if (valueToInclude < availableRangeBegin)
				availableRangeBegin = valueToInclude;
		}

		void ExpandAvailbaleRangeEnd(TimeSpan valueToInclude)
		{
			if (valueToInclude > availableRangeEnd)
				availableRangeEnd = valueToInclude;
		}


		class MatchedActivitiesPair
		{
			public IActivity OutgoingActivity, IncomingActivity;
		};

		class RotatedLogGroup
		{
			public string Key;
			public List<ITimelinePostprocessorOutput> Outputs;
			public bool IsInitialized;
			public IList<IActivity> Activities;
			public IList<IEvent> Events;
			public DateTime? Origin;
			public string GroupDisplayName;
		};

		readonly IPostprocessorsManager postprocessorsManager;
		readonly IEntitiesComparer entitiesComparer;
		readonly List<IActivity> activities = new List<IActivity>();
		readonly List<IEvent> events = new List<IEvent>();
		readonly IUserNamesProvider shortNames;
		readonly ILogSourceNamesProvider logSourceNamesProvider;
		Dictionary<string, RotatedLogGroup> outputsGroups = new Dictionary<string, RotatedLogGroup>();
		DateTime origin;
		TimeSpan availableRangeBegin, availableRangeEnd;
		HashSet<ITimelinePostprocessorOutput> outputs = new HashSet<ITimelinePostprocessorOutput>();
		readonly Dictionary<string, MatchedActivitiesPair> outgoingNetworkingActivities = new Dictionary<string, MatchedActivitiesPair>();
	};
}

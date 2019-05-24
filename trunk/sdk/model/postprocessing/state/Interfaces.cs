using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.StateInspector
{
	public interface IStateInspectorOutput: IPostprocessorOutputETag
	{
		ILogSource LogSource { get; }
		IList<Event> Events { get; }
		ILogPartToken RotatedLogPartToken { get; }
	};

	public interface IInspectedObject
	{
		IStateInspectorOutputsGroup Owner { get; }
		string Id { get; }
		string DisplayName { get; }
		string Comment { get; }
		IEnumerable<IInspectedObject> Children { get; }
		IInspectedObject Parent { get; }
		IEnumerable<StateInspectorEvent> StateChangeHistory { get; }
		StateInspectorEvent CreationEvent { get; }
		IEnumerable<KeyValuePair<string, PropertyViewBase>> GetCurrentProperties(FocusedMessageEventsRange focusedMessage);
		string GetCurrentPrimaryPropertyValue(FocusedMessageEventsRange focusedMessage);
		InspectedObjectLiveStatus GetLiveStatus(FocusedMessageEventsRange focusedMessage);
		IEnumerable<ILogSource> EnumInvolvedLogSources();
		bool IsTimeless { get; }

		void SetParent(IInspectedObject value);
		void RemoveChild(IInspectedObject child);
		void AddChild(IInspectedObject child);
		void SetCreationEvent(StateInspectorEvent evt);
		void SetDeletionEvent(StateInspectorEvent evt);
		void AddStateChangeEvent(StateInspectorEvent evt);
	};

	public enum InspectedObjectLiveStatus
	{
		NotCreatedYet,
		Alive,
		Deleted
	};

	public interface IStateInspectorVisualizerModel
	{
		IEnumerable<IStateInspectorOutputsGroup> Groups { get; }

		event EventHandler Changed;
	};

	public interface IStateInspectorOutputsGroup
	{
		string Key { get; }
		IEnumerable<IInspectedObject> Roots { get; }
		IReadOnlyList<StateInspectorEvent> Events { get; }
		IEnumerable<IStateInspectorOutput> Outputs { get; }
	};

	public interface IModel
	{
		Task SavePostprocessorOutput(
			IEnumerableAsync<Event[]> events,
			Task<ILogPartToken> rotatedLogPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		);
	};
}

using System;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Timeline
{
	public class Model : IModel
	{
		readonly ITempFilesManager tempFiles;

		public Model(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		Task IModel.SavePostprocessorOutput(
			IEnumerableAsync<Event[]> events,
			Task<ILogPartToken> rotatedLogPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			return TimelinePostprocessorOutput.SerializePostprocessorOutput(
				events,
				rotatedLogPartToken,
				triggersConverter,
				postprocessorInput.InputContentsEtag,
				postprocessorInput.OutputFileName,
				tempFiles,
				postprocessorInput.CancellationToken
			);
		}

		IEndOfTimelineEventSource<Message> IModel.CreateEndOfTimelineEventSource<Message>(
			Func<Message, object> triggetSelector)
		{
			return new GenericEndOfTimelineEventSource<Message>(triggetSelector);
		}

		IInspectedObjectsLifetimeEventsSource IModel.CreateInspectedObjectsLifetimeEventsSource(
			Predicate<StateInspector.Event> inspectedObjectsFilter)
		{
			return new InspectedObjectsLifetimeEventsSource(inspectedObjectsFilter);
		}
	};
}

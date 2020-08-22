using System;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Timeline
{
	class Model : IModel
	{
		readonly ITempFilesManager tempFiles;
		readonly ILogPartTokenFactories logPartTokenFactories;

		public Model(ITempFilesManager tempFiles, ILogPartTokenFactories logPartTokenFactories)
		{
			this.tempFiles = tempFiles;
			this.logPartTokenFactories = logPartTokenFactories;
		}

		PostprocessorOutputBuilder IModel.CreatePostprocessorOutputBuilder()
		{
			return new PostprocessorOutputBuilder
			{
				build = (postprocessorInput, builder) => TimelinePostprocessorOutput.SerializePostprocessorOutput(
					builder.events,
					builder.rotatedLogPartToken,
					logPartTokenFactories,
					builder.triggersConverter,
					postprocessorInput.InputContentsEtag,
					postprocessorInput.openOutputFile,
					tempFiles,
					postprocessorInput.CancellationToken
				)
			};
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
				logPartTokenFactories,
				triggersConverter,
				postprocessorInput.InputContentsEtag,
				postprocessorInput.openOutputFile,
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

		IMessagingEventsSource IModel.CreateMessagingEventsSource()
		{
			return new MessagingTimelineEventsSource();
		}
	};
}

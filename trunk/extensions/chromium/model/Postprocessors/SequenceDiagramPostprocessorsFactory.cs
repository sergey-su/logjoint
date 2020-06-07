using System.Threading.Tasks;
using System.Linq;
using LogJoint.Postprocessing;
using HAR = LogJoint.Chromium.HttpArchive;
using GA = LogJoint.Google.Analog;
using System.Collections.Generic;

namespace LogJoint.Chromium.SequenceDiagram
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateHttpArchivePostprocessor();
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateAnalogPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;
		readonly PluginModel pluginModel;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing, PluginModel pluginModel)
		{
			this.postprocessing = postprocessing;
			this.pluginModel = pluginModel;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateHttpArchivePostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.SequenceDiagram,
				i => RunForHttpArchive(new HAR.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.SequenceDiagram,
				i => RunForChromeDebug(new ChromeDebugLog.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateAnalogPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.SequenceDiagram,
				i => RunForAnalog(new GA.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		async Task RunForHttpArchive(
			IEnumerableAsync<HAR.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			HAR.IMessagingEvents messagingEvents = new HAR.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(input)
			);

			await postprocessing.SequenceDiagram.CreatePostprocessorOutputBuilder()
				.SetMessagingEvents(events)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((HAR.Message)evtTrigger))
				.Build(postprocessorInput);
		}

		async Task RunForChromeDebug(
			IEnumerableAsync<ChromeDebugLog.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var multiplexedInput = input.Multiplex();
			var matcher = postprocessing.CreatePrefixMatcher();

			var extensionSources = pluginModel.ChromeDebugLogMessagingEventSources.Select(src => src(
				matcher, multiplexedInput, postprocessorInput.TemplatesTracker)).ToArray();

			var events = EnumerableAsync.Merge(extensionSources.Select(s => s.Events).ToArray());

			var serialize = postprocessing.SequenceDiagram.CreatePostprocessorOutputBuilder()
				.SetMessagingEvents(events)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.FromUnknownTrigger(evtTrigger))
				.Build(postprocessorInput);

			var tasks = new List<Task>();
			tasks.Add(serialize);
			tasks.AddRange(extensionSources.SelectMany(s => s.MultiplexingEnumerables.Select(e => e.Open())));
			tasks.Add(multiplexedInput.Open());
			await Task.WhenAll(tasks);
		}

		async Task RunForAnalog(
			IEnumerableAsync<GA.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			GA.MediaRouter.IMessagingEvents messagingEvents = new GA.MediaRouter.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(input)
			);

			await postprocessing.SequenceDiagram.CreatePostprocessorOutputBuilder()
				.SetMessagingEvents(events)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((GA.Message)evtTrigger))
				.Build(postprocessorInput);
		}
	};
}

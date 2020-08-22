using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using CD = LogJoint.Chromium.ChromeDriver;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using HAR = LogJoint.Chromium.HttpArchive;
using LogJoint.Postprocessing.Timeline;
using System.Collections.Generic;

namespace LogJoint.Chromium.Timeline
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDriverPostprocessor();
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateHttpArchivePostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;
		readonly PluginModel pluginModel;

		public PostprocessorsFactory(
			Postprocessing.IModel postprocessing,
			PluginModel pluginModel
		)
		{
			this.postprocessing = postprocessing;
			this.pluginModel = pluginModel;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDriverPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.Timeline,
				i => RunForChromeDriver(new CD.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.Timeline,
				i => RunForChromeDebug(new CDL.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.OpenLogFile, s => s.Dispose(), i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateHttpArchivePostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.Timeline,
				i => RunForHttpArchive(new HAR.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		async Task RunForChromeDriver(
			IEnumerableAsync<CD.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var logMessages = input.MatchTextPrefixes(matcher).Multiplex();

			CD.ITimelineEvents networkEvents = new CD.TimelineEvents(matcher);
			var endOfTimelineEventSource = postprocessing.Timeline.CreateEndOfTimelineEventSource<MessagePrefixesPair<CD.Message>> (m => m.Message);

			var extensionSources = pluginModel.ChromeDriverTimeLineEventSources.Select(src => src(
				matcher, logMessages, postprocessorInput.TemplatesTracker)).ToArray();

			var networkEvts = networkEvents.GetEvents(logMessages);
			var eofEvts = endOfTimelineEventSource.GetEvents(logMessages);

			matcher.Freeze();

			var events = extensionSources.Select(s => s.Events).ToList();
			events.Add(networkEvts);
			events.Add(eofEvts);

			var serialize = postprocessing.Timeline.CreatePostprocessorOutputBuilder()
				.SetEvents(EnumerableAsync.Merge(events.ToArray()))
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((CD.Message)evtTrigger))
				.Build(postprocessorInput);

			var tasks = new List<Task>();
			tasks.Add(serialize);
			tasks.AddRange(extensionSources.SelectMany(s => s.MultiplexingEnumerables.Select(e => e.Open())));
			tasks.Add(logMessages.Open());

			await Task.WhenAll(tasks);
		}

		async Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var multiplexedInput = input.Multiplex();
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();

			var extensionSources = pluginModel.ChromeDebugLogTimeLineEventSources.Select(src => src(
				matcher, multiplexedInput, postprocessorInput.TemplatesTracker)).ToArray();

			var events = postprocessorInput.TemplatesTracker.TrackTemplates(EnumerableAsync.Merge(extensionSources.Select(s => s.Events).ToArray()));

			matcher.Freeze();

			var serialize = postprocessing.Timeline.CreatePostprocessorOutputBuilder()
				.SetEvents(events)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.FromUnknownTrigger(evtTrigger))
				.Build(postprocessorInput);

			var tasks = new List<Task>();
			tasks.Add(serialize);
			tasks.AddRange(extensionSources.SelectMany(s => s.MultiplexingEnumerables.Select(e => e.Open())));
			tasks.Add(multiplexedInput.Open());
			await Task.WhenAll(tasks);
		}

		async Task RunForHttpArchive(
			IEnumerableAsync<HAR.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			HAR.ITimelineEvents timelineEvents = new HAR.TimelineEvents();

			var events = EnumerableAsync.Merge(
				postprocessorInput.TemplatesTracker.TrackTemplates(timelineEvents.GetEvents(input))
			);

			await postprocessing.Timeline.CreatePostprocessorOutputBuilder()
				.SetEvents(events)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((HAR.Message)evtTrigger))
				.Build(postprocessorInput);
		}
	};
}

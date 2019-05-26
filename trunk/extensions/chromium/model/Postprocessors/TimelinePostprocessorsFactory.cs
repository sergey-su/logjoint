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
				i => RunForChromeDebug(new CDL.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateHttpArchivePostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.Timeline,
				i => RunForHttpArchive(new HAR.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		static IEnumerableAsync<Event[]> TrackTemplates(IEnumerableAsync<Event[]> events, ICodepathTracker codepathTracker)
		{
			return events.Select(batch =>
			{
				if (codepathTracker != null)
					foreach (var e in batch)
						codepathTracker.RegisterUsage(e.TemplateId);
				return batch;
			});
		}

		async Task RunForChromeDriver(
			IEnumerableAsync<CD.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var logMessages = CD.Helpers.MatchPrefixes(input, matcher).Multiplex();

			CD.ITimelineEvents networkEvents = new CD.TimelineEvents(matcher);
			// Sym.ICITimelineEvents symCIEvents = new Sym.CITimelineEvents(matcher); todo
			var endOfTimelineEventSource = postprocessing.Timeline.CreateEndOfTimelineEventSource<CD.MessagePrefixesPair>(m => m.Message);

			var networkEvts = networkEvents.GetEvents(logMessages);
			var eofEvts = endOfTimelineEventSource.GetEvents(logMessages);
			// var symCIEvts = symCIEvents.GetEvents(logMessages); todo

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				networkEvts,
				eofEvts
				// , symCIEvts todo
			), postprocessorInput.TemplatesTracker);

			var serialize = postprocessing.Timeline.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((CD.Message)evtTrigger),
				postprocessorInput
			);

			await Task.WhenAll(serialize, logMessages.Open());
		}

		async Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var multiplexedInput = input.Multiplex();
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();

			var extensionSources = pluginModel.ChromeDebugLogTimeLineEventSources.Select(src => src(
				matcher, multiplexedInput)).ToArray();
			// Sym.ICITimelineEvents symCI = new Sym.CITimelineEvents(matcher); todo

			/*var symEvents = RunForSymMessages(
				matcher,
				(new Sym.Reader(postprocessing.TextLogParser, CancellationToken.None)).FromChromeDebugLog(multiplexedInput),
				postprocessorInput.TemplatesTracker,
				out var symLog
			);
			var ciEvents = symCI.GetEvents(multiplexedInput);

			var events = EnumerableAsync.Merge(
				symEvents,
				ciEvents
			);*/

			var events = EnumerableAsync.Merge(extensionSources.Select(s => s.Events).ToArray());

			matcher.Freeze();

			var serialize = postprocessing.Timeline.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.FromUnknownTrigger(evtTrigger),
				postprocessorInput
			);

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

			var events = TrackTemplates(EnumerableAsync.Merge(
				timelineEvents.GetEvents(input)
			), postprocessorInput.TemplatesTracker);

			await postprocessing.Timeline.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((HAR.Message)evtTrigger),
				postprocessorInput
			);
		}

	};
}

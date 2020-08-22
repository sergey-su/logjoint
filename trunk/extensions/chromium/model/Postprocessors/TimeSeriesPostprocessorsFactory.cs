using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.TimeSeries;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using DMP = LogJoint.Chromium.WebrtcInternalsDump;

namespace LogJoint.Chromium.TimeSeries
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateWebRtcInternalsDumpPostprocessor();
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

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.TimeSeries,
				i => RunForChromeDebug(new CDL.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.OpenLogFile, s => s.Dispose(), i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.TimeSeries,
				i => RunForWebRtcInternalsDump(new DMP.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		async Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var inputMultiplexed = input.Multiplex();

			ICombinedParser parser = postprocessing.TimeSeries.CreateParser();

			var feedNativeEvents = parser.FeedLogMessages(inputMultiplexed);

			var extensionSources = pluginModel.ChromeDebugTimeSeriesSources.Select(
				src => src(inputMultiplexed, parser)).ToList();
			var tasks = extensionSources.Select(s => s.Task).ToList();
			tasks.Add(feedNativeEvents);
			tasks.AddRange(extensionSources.SelectMany(s => s.MultiplexingEnumerables.Select(e => e.Open())));
			tasks.Add(inputMultiplexed.Open());

			await Task.WhenAll(tasks);

			foreach (var ts in parser.GetParsedTimeSeries())
			{
				ts.DataPoints = Postprocessing.TimeSeries.Filters.RemoveRepeatedValues.Filter(ts.DataPoints).ToList();
			}

			await postprocessing.TimeSeries.SavePostprocessorOutput(parser, postprocessorInput);
		}

		async Task RunForWebRtcInternalsDump(
			IEnumerableAsync<DMP.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			ICombinedParser parser = postprocessing.TimeSeries.CreateParser();

			await parser.FeedLogMessages(input, m => m.ObjectId, m => m.Text);

			await postprocessing.TimeSeries.SavePostprocessorOutput(parser, postprocessorInput);
		}
	};
}

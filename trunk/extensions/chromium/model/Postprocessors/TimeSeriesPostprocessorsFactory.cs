using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.TimeSeries;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using DMP = LogJoint.Chromium.WebrtcInternalsDump;
using Sym = LogJoint.Symphony.Rtc;

namespace LogJoint.Chromium.TimeSeries
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateWebRtcInternalsDumpPostprocessor();
		ILogSourcePostprocessor CreateSymphonyRtcPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing)
		{
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.TimeSeries,
				i => RunForWebRtcNativeLogMessages(new CDL.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.TimeSeries,
				i => RunForWebRtcInternalsDump(new DMP.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSymphonyRtcPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.TimeSeries,
				i => RunForSymphonyRtc(new Sym.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		async Task RunForWebRtcNativeLogMessages(
			IEnumerableAsync<CDL.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var inputMultiplexed = input.Multiplex();
			var symMessages = (new Sym.Reader()).FromChromeDebugLog(inputMultiplexed);

			ICombinedParser parser = postprocessing.TimeSeries.CreateParser();

			var feedNativeEvents = parser.FeedLogMessages(inputMultiplexed);
			var feedSymEvents = parser.FeedLogMessages(symMessages, m => m.Logger, m => string.Format("{0}.{1}", m.Logger, m.Text));

			await Task.WhenAll(feedNativeEvents, feedSymEvents, inputMultiplexed.Open());

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

		async Task RunForSymphonyRtc(
			IEnumerableAsync<Sym.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			ICombinedParser parser = postprocessing.TimeSeries.CreateParser();

			await parser.FeedLogMessages(input, m => m.Logger, m => string.Format("{0}.{1}", m.Logger, m.Text));

			await postprocessing.TimeSeries.SavePostprocessorOutput(parser, postprocessorInput);
		}
	};
}

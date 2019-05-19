using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.TimeSeries;
using System.Xml;
using Sym = LogJoint.Symphony.Rtc;

namespace LogJoint.Symphony.TimeSeries
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateSymphonyRtcPostprocessor();
		Chromium.TimeSeriesDataSource<Chromium.ChromeDebugLog.Message>.Factory CreateChromeDebugSourceFactory();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing)
		{
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSymphonyRtcPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.TimeSeries,
				i => RunForSymphonyRtc(new Sym.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		Chromium.TimeSeriesDataSource<Chromium.ChromeDebugLog.Message>.Factory IPostprocessorsFactory.CreateChromeDebugSourceFactory()
		{
			return (messages, parser) =>
			{
				var symMessages = (new Sym.Reader(postprocessing.TextLogParser, CancellationToken.None)).FromChromeDebugLog(messages);
				return new Chromium.TimeSeriesDataSource<Chromium.ChromeDebugLog.Message>(
					parser.FeedLogMessages(symMessages, m => m.Logger, m => string.Format("{0}.{1}", m.Logger, m.Text))
				);
			};
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

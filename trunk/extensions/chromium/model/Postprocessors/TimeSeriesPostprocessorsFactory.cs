using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Analytics.TimeSeries;
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
		readonly static string typeId = PostprocessorIds.TimeSeries;
		readonly static string caption = "Time Series";
		readonly ITimeSeriesTypesAccess timeSeriesTypesAccess;

		public PostprocessorsFactory(ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			this.timeSeriesTypesAccess = timeSeriesTypesAccess;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption, 
				(doc, logSource) => DeserializeOutput(doc, logSource),
				i => RunForWebRtcNativeLogMessages(new CDL.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption,
				(doc, logSource) => DeserializeOutput(doc, logSource),
				i => RunForWebRtcInternalsDump(new DMP.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		TimeSeriesPostprocessorOutput DeserializeOutput(XDocument fromXmlDocument, ILogSource forLogSource)
		{
			return new TimeSeriesPostprocessorOutput(fromXmlDocument, 
				forLogSource, null, timeSeriesTypesAccess);
		}

		async Task RunForWebRtcNativeLogMessages(
			IEnumerableAsync<CDL.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			timeSeriesTypesAccess.CheckForCustomConfigUpdate();

			ICombinedParser parser = new TimeSeriesCombinedParser(timeSeriesTypesAccess.GetMetadataTypes());

			await parser.FeedLogMessages(input);

			foreach (var ts in parser.GetParsedTimeSeries())
			{
				ts.DataPoints = Analytics.TimeSeries.Filters.RemoveRepeatedValues.Filter(ts.DataPoints).ToList();
			}

			TimeSeriesPostprocessorOutput.SerializePostprocessorOutput(
				parser.GetParsedTimeSeries(),
				parser.GetParsedEvents(),
				outputFileName,
				timeSeriesTypesAccess);
		}

		async Task RunForWebRtcInternalsDump(
			IEnumerableAsync<DMP.Message[]> input,
			string outputFileName,
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			timeSeriesTypesAccess.CheckForCustomConfigUpdate();

			ICombinedParser parser = new TimeSeriesCombinedParser(timeSeriesTypesAccess.GetMetadataTypes());

			await parser.FeedLogMessages(input, m => m.ObjectId, m => m.Text);

			TimeSeriesPostprocessorOutput.SerializePostprocessorOutput(
				parser.GetParsedTimeSeries(),
				parser.GetParsedEvents(),
				outputFileName,
				timeSeriesTypesAccess);
		}
	};
}

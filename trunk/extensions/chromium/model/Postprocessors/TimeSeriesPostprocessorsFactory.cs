using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Postprocessing.TimeSeries;
using LogJoint.Chromium.ChromeDebugLog;
using LogJoint.Analytics.TimeSeries;

namespace LogJoint.Chromium.TimeSeries
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
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
				i => RunInternal(new Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		TimeSeriesPostprocessorOutput DeserializeOutput(XDocument fromXmlDocument, ILogSource forLogSource)
		{
			return new TimeSeriesPostprocessorOutput(fromXmlDocument, 
				forLogSource, null, timeSeriesTypesAccess);
		}

		async Task RunInternal(
			IEnumerableAsync<Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			timeSeriesTypesAccess.CheckForCustomConfigUpdate();

			ICombinedParser parser = new CombinedParser(timeSeriesTypesAccess.GetMetadataTypes());

			await parser.FeedLogMessages(input);

			TimeSeriesPostprocessorOutput.SerializePostprocessorOutput(
				parser.GetParsedTimeSeries(),
				parser.GetParsedEvents(),
				outputFileName,
				timeSeriesTypesAccess);
		}
	};
}

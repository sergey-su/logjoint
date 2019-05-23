using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Analytics.TimeSeries;
using LogJoint.Postprocessing.TimeSeries;
using System.Xml;

namespace LogJoint.Symphony.TimeSeries
{
	public interface IPostprocessorsFactory
	{
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
	};
}

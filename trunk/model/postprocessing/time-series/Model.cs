using System.Threading.Tasks;

namespace LogJoint.Postprocessing.TimeSeries
{
	public class Model : IModel
	{
		readonly ITimeSeriesTypesAccess timeSeriesTypesAccess;

		public Model(ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			this.timeSeriesTypesAccess = timeSeriesTypesAccess;
		}

		ICombinedParser IModel.CreateParser()
		{
			timeSeriesTypesAccess.CheckForCustomConfigUpdate();
			return new TimeSeriesCombinedParser(timeSeriesTypesAccess.GetMetadataTypes());
		}

		Task IModel.SavePostprocessorOutput(
			ICombinedParser parser,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			return TimeSeriesPostprocessorOutput.SerializePostprocessorOutput(
				parser.GetParsedTimeSeries(),
				parser.GetParsedEvents(),
				postprocessorInput.openOutputFile,
				timeSeriesTypesAccess);
		}

		void IModel.RegisterTimeSeriesTypesAssembly(System.Reflection.Assembly asm)
		{
			timeSeriesTypesAccess.RegisterTimeSeriesTypesAssembly(asm);
		}
	};
}

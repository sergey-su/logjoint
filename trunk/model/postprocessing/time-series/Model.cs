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
			TimeSeriesPostprocessorOutput.SerializePostprocessorOutput(
				parser.GetParsedTimeSeries(),
				parser.GetParsedEvents(),
				postprocessorInput.OutputFileName,
				timeSeriesTypesAccess);
			return Task.FromResult(0);
		}

		void IModel.RegisterTimeSeriesTypesAssembly(System.Reflection.Assembly asm)
		{
			timeSeriesTypesAccess.RegisterTimeSeriesTypesAssembly(asm);
		}
	};
}

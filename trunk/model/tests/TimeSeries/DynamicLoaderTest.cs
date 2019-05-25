using System.IO;
using System.Linq;
using NUnit.Framework;
using System;

namespace LogJoint.Postprocessing.TimeSeries
{

	[TestFixture]
	public class DynamicScriptLoaderTest
	{
		[Test]
		public void LoadsSampleScript()
		{
			ITimeSeriesTypesAccess tsTypes = new TimeSeriesTypesLoader();

			var tempTimeSeriesConfig = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");
			try
			{
				using (var tempTimeSeriesConfigStream = new FileStream(tempTimeSeriesConfig, FileMode.Create))
					Utils.GetResourceStream("TestParserConfig").CopyTo(tempTimeSeriesConfigStream);
				Environment.SetEnvironmentVariable("TSTEST", tempTimeSeriesConfig);
				tsTypes.CustomConfigEnvVar = "TSTEST";

				Assert.IsTrue(string.IsNullOrEmpty(tsTypes.CustomConfigLoadingError));
				Assert.AreEqual(1, tsTypes.GetMetadataTypes().Count(t => t.Name == "FooBar"));
			}
			finally
			{
				File.Delete(tempTimeSeriesConfig);
			}
		}
	}
}

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LogJoint.Analytics.TimeSeries
{

	[TestClass]
	public class DynamicScriptLoaderTest
	{
		[TestMethod]
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

using LogJoint.Postprocessing.TimeSeries;

namespace TestNS
{
	[TimeSeriesEvent(Type = "Test")]
	[Expression(@"^a\.b\d+\.c\.<id:hex>", Prefix = @"a\.b")]
	[Source(From = "id")]
	[ExampleLine(@"a.b3.c.13dc")]
	public class FooBar
	{
		[TimeSeries(From = "id", Unit = "Kbps", Scale = 0.001, Description = "Test test")]
		public string EstimateForPeer;
	}
}
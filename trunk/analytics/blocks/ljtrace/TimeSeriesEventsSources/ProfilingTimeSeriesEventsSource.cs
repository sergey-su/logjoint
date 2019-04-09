using LogJoint.Analytics.TimeSeries;

namespace LogJoint.Analytics.InternalTrace
{
	[TimeSeriesEvent(Type = "Profiling")]
	[Expression(@"^cntrs (?<id>[\w\#\-\.]+) (?<name>[^\=]+)\=<value:double>( (?<unit>\w+))?$", Prefix = "cntrs")]
	[Source(From = "id")]
	[ExampleLine(@"2019/04/22 13:45:20.720 T#1 I ui.lv: cntrs #drawing paint=6707")]
	public class ProfilingSeries
	{
		[TimeSeries(Name = "name", From = "value", Unit = "<unit>")]
		public double ts;
	}
}

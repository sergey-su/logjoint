using LogJoint.Analytics.TimeSeries;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
	static class Consts
	{
		internal const string objIdRe = @"^(?<id>[^\|]+\|[^\|]+)\|";
	};

	[Source(From = "id")]
	public class WebrtcInternalsDumpBaseTS
	{
		protected const string type = "Streams";
		protected const string pfx = "ssrc_";
	};

	[TimeSeriesEvent(Type = type)]
	[Expression(Consts.objIdRe + @"googFrameRateOutput\|<value:double>", Prefix = pfx)]
	public class Stream_GoogFrameRateOutput: WebrtcInternalsDumpBaseTS
	{
		[TimeSeries(From = "value", Unit = "Fps")]
		public double googFrameRateOutput;
	}

	[TimeSeriesEvent(Type = type)]
	[Expression(Consts.objIdRe + @"bytesReceived\|<value:int>", Prefix = pfx)]
	public class Stream_BytesReceived : WebrtcInternalsDumpBaseTS
	{
		[TimeSeries(From = "value", Unit = "Bytes")]
		public double bytesReceived;
	}

	[TimeSeriesEvent(Type = type)]
	[Expression(Consts.objIdRe + @"bitsReceivedPerSecond\|<value:double>", Prefix = pfx)]
	public class Stream_BitsReceivedPerSecond : WebrtcInternalsDumpBaseTS
	{
		[TimeSeries(From = "value", Unit = "Kbps", Scale = 0.001)]
		public double bitsReceivedPerSecond;
	}
}

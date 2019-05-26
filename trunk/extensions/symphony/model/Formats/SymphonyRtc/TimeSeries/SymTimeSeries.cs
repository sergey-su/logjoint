using LogJoint.Postprocessing.TimeSeries;

namespace LogJoint.Symphony.Rtc
{
	static class Consts
	{
		internal const string objIdRe = @"^(?<id>[\w\-]+\.[^\.]+)\.";
		internal const string type = "WebRTC";
		internal const string prefix = "stats-";
	};

	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>bytesSent|bytesReceived)=<value:int>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Kilobytes
	{
		[TimeSeries(From = "value", Name = "name", Unit = "kB", Scale = 0.001)]
		public double ts;
	}

	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>framesSent|framesReceived|framesEncoded|framesDecoded|framesDropped|framesSent|hugeFramesSent)=<value:int>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Frames
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Frames")]
		public double ts;
	}

	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>messagesReceived|messagesSent|consentRequestsSent|requestsReceived|requestsSent|responsesSent)=<value:int>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Messages
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Messages")]
		public double ts;
	}

	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>packetsSent|packetsLost|packetsReceived|firCount|nackCount|pliCount)=<value:int>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Packets
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Packets")]
		public double ts;
	}

	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>availableIncomingBitrate|availableOutgoingBitrate)=<value:double>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Bitrate
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Kbps", Scale = 0.001)]
		public double ts;
	}

	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>currentRoundTripTime|totalRoundTripTime|jitter|jitterBufferDelay|totalSamplesDuration)=<value:double>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Seconds
	{
		[TimeSeries(From = "value", Name = "name", Unit = "ms", Scale = 1000)]
		public double ts;
	}

	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>frameHeight|frameWidth)=<value:int>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Pixels
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Pixels")]
		public double ts;
	}


	[TimeSeriesEvent(Type = Consts.type)]
	[Expression(Consts.objIdRe + @"(?<name>fractionLost|qpSum|concealedSamples|concealmentEvents|totalAudioEnergy|totalSamplesReceived|audioLevel|dataChannelsClosed|dataChannelsOpened)=<value:double>", Prefix = Consts.prefix)]
	[Source(From = "id")]
	public class Unitless
	{
		[TimeSeries(From = "value", Name = "name", Unit = "")]
		public double ts;
	}
}

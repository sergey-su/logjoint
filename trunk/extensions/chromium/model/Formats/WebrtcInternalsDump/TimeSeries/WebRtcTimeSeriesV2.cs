using LogJoint.Postprocessing.TimeSeries;

namespace LogJoint.Chromium.WebrtcInternalsDumpTSV2
{
	[Source(From = "id")]
	public class BaseStreamsTS
	{
		protected const string objIdRe = @"^(?<id>[^\|]+\|(RTC(Inbound|Outbound)RTP(Audio|Video)Stream|RTCRemoteInboundRtp)[^\|]+)\|";
		protected const string type = "Streams";
		protected const string pfx = "RTC";
	};

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>packetsReceived|packetsSent|packetsLost|retransmittedPacketsSent|firCount|pliCount|nackCount)\|<value:int>", Prefix = pfx)]
	public class RTPStream_PacketsTS: BaseStreamsTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Packets")]
		public double ts;
	}

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>bytesReceived|bytesSent|totalEncodedBytesTarget|retransmittedBytesSent)\|<value:int>", Prefix = pfx)]
	public class RTPStream_BytesTS: BaseStreamsTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Bytes")]
		public double ts;
	}

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>totalDecodeTime|totalEncodeTime|jitter|totalPacketSendDelay|roundTripTime)\|<value:double>", Prefix = pfx)]
	public class RTPStream_SecondsTS: BaseStreamsTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Seconds")]
		public double ts;
	}

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>keyFramesEncoded|framesEncoded|keyFramesEncoded|framesDecoded)\|<value:int>", Prefix = pfx)]
	public class RTPStream_FramesTS: BaseStreamsTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Frames")]
		public double ts;
	}


	[Source(From = "id")]
	public class BaseTransportTS
	{
		protected const string objIdRe = @"^(?<id>[^\|]+\|RTCTransport[^\|]+)\|";
		protected const string type = "Transports";
		protected const string pfx = "RTCTransport";
	};

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>bytesReceived|bytesSent)\|<value:int>", Prefix = pfx)]
	public class RTPTransport_BytesTS: BaseTransportTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Bytes")]
		public double ts;
	}

	[Source(From = "id")]
	public class BaseTracksTS
	{
		protected const string objIdRe = @"^(?<id>[^\|]+\|RTCMediaStreamTrack[^\|]+)\|";
		protected const string type = "Tracks";
		protected const string pfx = "RTCMediaStreamTrack";
	};

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>framesSent|hugeFramesSent|framesReceived|framesDecoded|framesDropped)\|<value:int>", Prefix = pfx)]
	public class RTPTrack_FramesTS: BaseTracksTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Frames")]
		public double ts;
	}

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>interruptionCount|delayedPacketOutageSamples|jitterBufferFlushes|removedSamplesForAcceleration|insertedSamplesForDeceleration|concealmentEvents|silentConcealedSamples|concealedSamples|pauseCount|freezeCount|jitterBufferEmittedCount|totalAudioEnergy|echoReturnLoss|echoReturnLossEnhancement)\|<value:int>", Prefix = pfx)]
	public class RTPTrack_UnitlessTS: BaseTracksTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "")]
		public double ts;
	}

	[TimeSeriesEvent(Type = type)]
	[Expression(objIdRe + @"(?<name>jitterBufferDelay|relativePacketArrivalDelay|sumOfSquaredFramesDuration|totalInterruptionDuration|totalSamplesDuration|totalPausesDuration)\|<value:double>", Prefix = pfx)]
	public class RTPTrack_SecondsTS: BaseTracksTS
	{
		[TimeSeries(From = "value", Name = "name", Unit = "Seconds")]
		public double ts;
	}
}

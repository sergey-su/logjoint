using LogJoint.Postprocessing.TimeSeries;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
    static class Consts
    {
        internal const string objIdRe = @"^(?<id>[^\|]+\|[^\|]+)\|";
    };

    [Source(From = "id")]
    public class BaseStreamsTS
    {
        protected const string type = "Streams";
        protected const string pfx = "ssrc_";
    };

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>googFrameRateDecoded|googFrameRateOutput|googFrameRateReceived|googFrameRateInput|googFrameRateSent)\|<value:int>", Prefix = pfx)]
    public class Stream_FpsTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Fps")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>bytesReceived|bytesSent)\|<value:int>", Prefix = pfx)]
    public class Stream_BytesTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Bytes")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>bitsReceivedPerSecond|bitsSentPerSecond)\|<value:double>", Prefix = pfx)]
    public class Stream_KbpsTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Kbps", Scale = 0.001)]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe +
        @"(?<name>audioOutputLevel|googAccelerateRate|googExpandRate|googSecondaryDecodedRate|googSpeechExpandRate|" +
        @"googPreemptiveExpandRate|googDecodingCNG|googDecodingCTN|googDecodingCTSG|googDecodingMuted|googDecodingNormal|googDecodingPLC|googDecodingPLCCNG|" +
        @"qpSum|aecDivergentFilterFraction|audioInputLevel|googEchoCancellationQualityMin|googEchoCancellationReturnLoss|googEchoCancellationReturnLossEnhancement|" +
        @"googResidualEchoLikelihood|googResidualEchoLikelihoodRecentMax|googAdaptationChanges" +
        @")\|<value:double>",
        Prefix = pfx)]
    public class Stream_UnitlessTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>packetsLost|packetsSent|packetsReceived|googFirsSent|googFirsReceived|googNacksSent|googPlisSent|googNacksReceived|googPlisReceived)\|<value:double>", Prefix = pfx)]
    public class Stream_PacketsTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Packets")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>packetsReceivedPerSecond|packetsSentPerSecond)\|<value:double>", Prefix = pfx)]
    public class Stream_PpsTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Packets/s")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>googCurrentDelayMs|googJitterBufferMs|googJitterReceived|googPreferredJitterBufferMs|" +
        @"googCurrentDelayMs|googDecodeMs|googMaxDecodeMs|googMinPlayoutDelayMs|googRenderDelayMs|googTargetDelayMs|googRtt|googAvgEncodeMs)\|<value:double>", Prefix = pfx)]
    public class Stream_MsTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "ms")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>framesDecoded|framesEncoded)\|<value:int>", Prefix = pfx)]
    public class Stream_FramesTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Frames")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>googFrameHeightReceived|googFrameWidthReceived|googFrameHeightSent|googFrameWidthSent)\|<value:int>", Prefix = pfx)]
    public class Stream_PixelsTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Pixels")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>googEncodeUsagePercent)\|<value:int>", Prefix = pfx)]
    public class Stream_PctTS : BaseStreamsTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "%")]
        public double ts;
    }

    [Source(From = "id")]
    public class BaseBweforvideoTS
    {
        protected const string type = "BWE for video";
        protected const string pfx = "bweforvideo";
    };

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>googActualEncBitrate|googAvailableSendBandwidth|googAvailableReceiveBandwidth|googRetransmitBitrate|googTargetEncBitrate|googTargetEncBitrateCorrected|googTransmitBitrate)\|<value:double>", Prefix = pfx)]
    public class Bweforvideo_BitrateTS : BaseBweforvideoTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Kbps", Scale = 0.001)]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>googBucketDelay)\|<value:int>", Prefix = pfx)]
    public class Bweforvideo_MsTS : BaseBweforvideoTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "ms")]
        public double ts;
    }

    [Source(From = "id")]
    public class BaseConnTS
    {
        protected const string type = "Connections";
        protected const string pfx = "Conn-";
    };

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>bitsReceivedPerSecond|bitsSentPerSecond)\|<value:double>", Prefix = pfx)]
    public class Conn_BptTS : BaseConnTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Kbps", Scale = 0.001)]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>bytesReceived|bytesSent)\|<value:int>", Prefix = pfx)]
    public class Conn_BytesTS : BaseConnTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Bytes")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>packetsSent|requestsSent|consentRequestsSent|responsesSent|requestsReceived|responsesReceived|packetsDiscardedOnSend)\|<value:int>", Prefix = pfx)]
    public class Conn_PacketsTS : BaseConnTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Packets")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>packetsSentPerSecond)\|<value:double>", Prefix = pfx)]
    public class Conn_PpsTS : BaseConnTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "Packets/s")]
        public double ts;
    }

    [TimeSeriesEvent(Type = type)]
    [Expression(Consts.objIdRe + @"(?<name>googRtt)\|<value:double>", Prefix = pfx)]
    public class Conn_MsTS : BaseConnTS
    {
        [TimeSeries(From = "value", Name = "name", Unit = "ms")]
        public double ts;
    }
}

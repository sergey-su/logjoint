using LogJoint.Analytics.TimeSeries;

namespace LogJoint.Chromium.ChromeDebugLog
{
	[TimeSeriesEvent(Type = "VideoSendStream")]
	[Expression(@"^VideoSend(?<id>Stream) stats: <ts:int>, "
		+ @"{input_fps: <input_fps:int>, encode_fps: <encode_fps:int>, encode_ms: <encode_ms:int>, encode_usage_perc: <encode_usage_perc:int>, "
		+ @"target_bps: <target_bps:int>, media_bps: <media_bps:int>, preferred_media_bitrate_bps: <preferred_media_bitrate_bps:int>, " 
		+ @"suspended: \w+, bw_adapted: \w+"
		+ "}", 
		Prefix = "VideoSendStream stats")]
	[Source(From = "id")]
	[ExampleLine(@"[9540:2300:0626/154621.871:INFO:webrtcvideoengine.cc(2027)] VideoSendStream stats: 19468329, {input_fps: 0, encode_fps: 0, encode_ms: 0, encode_usage_perc: 0, target_bps: 300000, media_bps: 0, preferred_media_bitrate_bps: 2500000, suspended: false, bw_adapted: false} {ssrc: 1712343779, width: 1280, height: 720, key: 1, delta: 2, total_bps: 943636, retransmit_bps: 0, avg_delay_ms: 54, max_delay_ms: 72, cum_loss: 0, max_ext_seq: 0, nack: 0, fir: 0, pli: 0}")]
	public class VideoSendStreamStatsSeries
	{
		[TimeSeries(Unit = "Fps", Description = "Input frame rate")]
		public double input_fps;

		[TimeSeries(Unit = "Fps", Description = "Encode frame rate")]
		public double encode_fps;

		[TimeSeries(Unit = "ms", Description = "Avg encode time")]
		public double encode_ms;

		[TimeSeries(Unit = "%", Description = "Avg encode time")]
		public double encode_usage_percent;

		[TimeSeries(Unit = "Kbps", Scale = 0.008, Description = "Target media bitrate")]
		public double target_bps;

		[TimeSeries(Unit = "Kbps", Scale = 0.008, Description = "Media bitrate")]
		public double media_bps;

		[TimeSeries(Unit = "Kbps", Scale = 0.008, Description = "Preferred media bitrate")]
		public double preferred_media_bitrate_bps;
	}

	[Source(From = "ssrc")]
	public class VideoSendStreamSubStreamStatsSeries
	{
		protected const string SubStreamRE =
			@"{ssrc: <ssrc:int>, width: <width:int>, height: <height:int>, key: <key:int>, delta: <delta:int>, total_bps: <total_bps:int>, retransmit_bps: <retransmit_bps:int>, "
		  + @"avg_delay_ms: <avg_delay:int>, max_delay_ms: <max_delay:int>, cum_loss: <cum_loss:int>, max_ext_seq: <max_ext_seq:int>, nack: <nack:int>, fir: <fir:int>, pli: <pli:int>}";

		[TimeSeries(Unit = "Pixels", Description = "Width")]
		public double width;

		[TimeSeries(Unit = "Pixels", Description = "Height")]
		public double height;

		[TimeSeries(From = "key", Unit = "", Description = "Key frames counter")]
		public double key_frames;

		[TimeSeries(From = "delta", Unit = "", Description = "Delta frames counter")]
		public double delta_frames;

		[TimeSeries(Unit = "Kbps", Scale = 0.008, Description = "Total bitrate")]
		public double total_bps;

		[TimeSeries(Unit = "Kbps", Scale = 0.008, Description = "Retransmittion bitrate")]
		public double retransmit_bps;

		[TimeSeries(Unit = "ms", Description = "Average delay")]
		public double avg_delay;

		[TimeSeries(Unit = "ms", Description = "Max delay")]
		public double max_delay;

		[TimeSeries(Unit = "Packets", Description = "Cumulative lost packets")]
		public double cumulative_lost;

		[TimeSeries(Unit = "Packets", Description = "NACK (negative acknowledgement) packets counter")]
		public double nack;

		[TimeSeries(Unit = "Packets", Description = "Full Intra Request packets")]
		public double fir;

		[TimeSeries(Unit = "Packets", Description = "Picture Loss Indication packets")]
		public double pli;
	};

	[TimeSeriesEvent(Type = "VideoSendStream")]
	[Expression(@"^VideoSendStream stats: <ts:int>, {[^}]+} " + SubStreamRE, Prefix = "VideoSendStream stats")]
	[ExampleLine(@"[9540:2300:0626/154621.871:INFO:webrtcvideoengine.cc(2027)] VideoSendStream stats: 19468329, {input_fps: 0, encode_fps: 0, encode_ms: 0, encode_usage_perc: 0, target_bps: 300000, media_bps: 0, preferred_media_bitrate_bps: 2500000, suspended: false, bw_adapted: false} {ssrc: 1712343779, width: 1280, height: 720, key: 1, delta: 2, total_bps: 943636, retransmit_bps: 0, avg_delay_ms: 54, max_delay_ms: 72, cum_loss: 0, max_ext_seq: 0, nack: 0, fir: 0, pli: 0}")]
	public class VideoSendStreamSubStream1StatsSeries: VideoSendStreamSubStreamStatsSeries
	{
	}

	[TimeSeriesEvent(Type = "VideoSendStream")]
	[Expression(@"^VideoSendStream stats: <ts:int>, {[^}]+} {[^}]+} " + SubStreamRE, Prefix = "VideoSendStream stats")]
	public class VideoSendStreamSubStream2StatsSeries : VideoSendStreamSubStreamStatsSeries
	{
	}

	[TimeSeriesEvent(Type = "VideoReceiveStream")]
	[Expression(@"^VideoReceiveStream stats: <ts:int>, {ssrc: <ssrc:int>, "
		+ @"total_bps: <total_bps:int>, width: <width:int>, height: <width:int>, key: <key:int>, delta: <delta:int>, "
		+ @"network_fps: <network_fps:int>, decode_fps: <decode_fps:int>, render_fps: <render_fps:int>, "
		+ @"decode_ms: <decode:int>, max_decode_ms: <max_decode:int>, cur_delay_ms: <cur_delay:int>, targ_delay_ms: <targ_delay:int>, jb_delay_ms: <jb_delay:int>, "
		+ @"min_playout_delay_ms: <min_playout_delay:int>, discarded: <discarded_packets:int>, sync_offset_ms: <sync_offset:int>, "
		+ @"cum_loss: <cumulative_lost:int>, max_ext_seq: <max_ext_seq:int>, nack: <nack:int>, fir: <fir:int>, pli: <pli:int>}",
		Prefix = "VideoReceiveStream stats")]
	[Source(From = "ssrc")]
	[ExampleLine(@"[9620:14688:0626/155043.227:INFO:webrtcvideoengine.cc(2487)] VideoReceiveStream stats: 19729679, {ssrc: 744512964, total_bps: 1740312, width: 640, height: 480, key: 1, delta: 236, network_fps: 30, decode_fps: 29, render_fps: 29, decode_ms: 2, max_decode_ms: 2, cur_delay_ms: 24, targ_delay_ms: 24, jb_delay_ms: 12, min_playout_delay_ms: 0, discarded: 0, sync_offset_ms: 2147483647, cum_loss: 0, max_ext_seq: 30800, nack: 0, fir: 0, pli: 0}")]
	public class VideoReceiveStreamStatsSeries
	{
		[TimeSeries(Unit = "Kbps", Scale = 0.008, Description = "Total bitrate")]
		public double total_bps;

		[TimeSeries(Unit = "Pixels", Description = "")]
		public double width;

		[TimeSeries(Unit = "Pixels", Description = "")]
		public double height;

		[TimeSeries(From = "key", Unit = "", Description = "Key frames counter")]
		public double key_frames;

		[TimeSeries(From = "delta", Unit = "", Description = "Delta frames counter")]
		public double delta_frames;

		[TimeSeries(Unit = "Fps", Description = "Network frame rate")]
		public double network_fps;

		[TimeSeries(Unit = "Fps", Description = "Decode frame rate")]
		public double decode_fps;

		[TimeSeries(Unit = "Fps", Description = "Render frame rate")]
		public double render_fps;

		[TimeSeries(Unit = "ms", Description = "Decodig time")]
		public double decode;

		[TimeSeries(Unit = "ms", Description = "Max decoding time")]
		public double max_decode;

		[TimeSeries(Unit = "ms", Description = "")]
		public double cur_delay;

		[TimeSeries(Unit = "ms", Description = "")]
		public double targ_delay;

		[TimeSeries(Unit = "ms", Description = "Jitter buffer")]
		public double jb_delay;

		[TimeSeries(Unit = "ms", Description = "")]
		public double min_playout_delay;

		[TimeSeries(Unit = "Packets", Description = "Discarded packets counter")]
		public double discarded_packets;

		[TimeSeries(Unit = "ms", Description = "")]
		public double sync_offset;

		[TimeSeries(Unit = "Packets", Description = "Cumulative lost packets counter")]
		public double cumulative_lost;

		[TimeSeries(Unit = "Packets", Description = "NACK (negative acknowledgement) packets counter")]
		public double nack;

		[TimeSeries(Unit = "Packets", Description = "Full Intra Request packets")]
		public double fir;

		[TimeSeries(Unit = "Packets", Description = "Picture Loss Indication packets")]
		public double pli;
	}
}

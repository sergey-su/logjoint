using LogJoint.Analytics.TimeSeries;

namespace LogJoint.Chromium.ChromeDebugLog
{
	[TimeSeriesEvent(Type = "VideoSendStream")]
	[Expression(@"^VideoSendStream stats: <ts:int>, \{input_fps: <input_fps:double>, encode_fps: <encode_fps:double>", 
	            Prefix = "VideoSendStream stats")]
	public class VideoSendStreamStatsSeries
	{
		[TimeSeries(From = "input_fps", Unit = "", Description = "Input FPS")]
		public double input_fps;

		[TimeSeries(From = "encode_fps", Unit = "", Description = "Encode FPS")]
		public double encode_fps;        
	}
}

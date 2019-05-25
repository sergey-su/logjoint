using System.IO;
using NUnit.Framework;
using System.Threading.Tasks;
using LogJoint.Postprocessing;

namespace LogJoint.Chromium.ChromeDebugLog
{
	[TestFixture]
	public class ChromeDebugLogTests
	{
		[Test, Category("SplitAndCompose")]
		public async Task ChromeDebugLog_SplitAndComposeTest()
		{
			var testStream = Utils.GetResourceStream("chrome_debug_2017_06_26");

			var actualContent = new MemoryStream();

			var reader = new Reader();
			var writer = new Writer();

			await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

			Utils.AssertTextsAreEqualLineByLine(
				Utils.SplitTextStream(testStream),
				Utils.SplitTextStream(actualContent)
			);
		}

		[Test]
		public void JsonLikeParserTest()
		{
			var token = JsonLikeStringParser.Parse(@"{decoders: [{decoder: (VideoDecoder), payload_type: 96, payload_name: VP8, codec_params: {}}, {decoder: (VideoDecoder), payload_type: 98, payload_name: VP9, codec_params: {}}, {decoder: (VideoDecoder), payload_type: 100, payload_name: H264, codec_params: {level-asymmetry-allowed: 1packetization-mode: 1profile-level-id: 42e01f}}], rtp: {remote_ssrc: 2518741716, local_ssrc: 3473758211, rtcp_mode: RtcpMode::kReducedSize, rtcp_xr: {receiver_reference_time_report: off}, remb: on, transport_cc: on, nack: {rtp_history_ms: 1000}, ulpfec: {ulpfec_payload_type: 127, red_payload_type: 102, red_rtx_payload_type: 125}, rtx_ssrc: 2508991752, rtx_payload_types: {96 (apt) -> 97 (pt), 98 (apt) -> 99 (pt), 100 (apt) -> 101 (pt), }, extensions: [{uri: http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01, id: 5}, {uri: http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time, id: 3}, {uri: http://www.webrtc.org/experiments/rtp-hdrext/playout-delay, id: 6}, {uri: urn:3gpp:video-orientation, id: 4}, {uri: urn:ietf:params:rtp-hdrext:toffset, id: 2}]}, renderer: (renderer), render_delay_ms: 10, sync_group: h0Gv19oeTn3moIjfzPmh0hmOvCJwDYdVm5E9, pre_decode_callback: nullptr, target_delay_ms: 0}");
			var actual = token.ToString(Newtonsoft.Json.Formatting.Indented);
			var expected = Utils.GetResourceStream("JsonLikeParserTest01");
			Utils.AssertTextsAreEqualLineByLine(
				Utils.SplitTextStream(expected),
				Utils.SplitText(actual)
			);
		}

	}
}

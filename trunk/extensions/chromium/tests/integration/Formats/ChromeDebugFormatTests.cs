using System.Threading.Tasks;
using System.IO;
using System.Threading;
using LogJoint.Chromium.ChromeDebugLog;
using NFluent;
using LogJoint.Postprocessing;

namespace LogJoint.Tests.Integration.Chromium
{
	[IntegrationTestFixture]
	class ChromeDebugTests
	{
		[IntegrationTest]
		public async Task LoadsLogFileAndEnablesPostprocessors(IContext context)
		{
			await context.Utils.EmulateFileDragAndDrop(await context.Samples.GetSampleAsLocalFile("chrome_debug_1.log"));

			await context.Utils.WaitFor(() => context.Presentation.LoadedMessagesLogViewer.VisibleLines.Count > 0);

			Check.That(context.Presentation.LoadedMessagesLogViewer.VisibleLines[0].Value).IsEqualTo(
				"[1:20:0102/101210.009354:INFO:paced_sender.cc(354)] ProcessThreadAttached 0x(nil)");
			var postprocessorsControls = context.Presentation.Postprocessing.SummaryView;
			Check.That(postprocessorsControls.Timeline.Enabled).IsTrue();
			Check.That(postprocessorsControls.StateInspector.Enabled).IsTrue();
			Check.That(postprocessorsControls.TimeSeries.Enabled).IsTrue();
		}

		[IntegrationTest]
		public async Task CanRunStateInspectorPostprocessor(IContext context)
		{
			await context.Utils.EmulateFileDragAndDrop(await context.Samples.GetSampleAsLocalFile("chrome_debug_1.log"));

			var postprocessorsControls = context.Presentation.Postprocessing.SummaryView;
			await context.Utils.WaitFor(() => postprocessorsControls.StateInspector.Run != null);

			postprocessorsControls.StateInspector.Run();

			await context.Utils.WaitFor(() => postprocessorsControls.StateInspector.Show != null);

			var webRtc = await context.Presentation.Postprocessing.StateInspector.Roots.FirstOrDefault(n => n.Id == "WebRTC");
			Check.That(webRtc).IsNotNull();
			var streams = await webRtc.Children.FirstOrDefault(n => n.Id == "Streams");
			Check.That(streams).IsNotNull();
			var ssrc1 = await streams.Children.FirstOrDefault(n => n.Id == "970030813");
			Check.That(ssrc1).IsNotNull();
		}

		[IntegrationTest]
		public async Task ChromeDebugLog_SplitAndComposeTest(IContext context)
		{
			using (var testStream = await context.Samples.GetSampleAsStream("chrome_debug_2017_06_26.log"))
			{
				var actualContent = new MemoryStream();

				var reader = new Reader(context.Model.Postprocessing.TextLogParser, CancellationToken.None);
				var writer = new Writer();

				await writer.Write(() => actualContent, _ => { }, reader.Read(() => Task.FromResult(testStream), _ => { }));

				Check.That(
					Helpers.SplitTextStream(actualContent)
				).ContainsExactly(
					Helpers.SplitTextStream(testStream)
				);
			}
		}

		[IntegrationTest]
		public async Task JsonLikeParserTest(IContext context)
		{
			var token = JsonLikeStringParser.Parse(@"{decoders: [{decoder: (VideoDecoder), payload_type: 96, payload_name: VP8, codec_params: {}}, {decoder: (VideoDecoder), payload_type: 98, payload_name: VP9, codec_params: {}}, {decoder: (VideoDecoder), payload_type: 100, payload_name: H264, codec_params: {level-asymmetry-allowed: 1packetization-mode: 1profile-level-id: 42e01f}}], rtp: {remote_ssrc: 2518741716, local_ssrc: 3473758211, rtcp_mode: RtcpMode::kReducedSize, rtcp_xr: {receiver_reference_time_report: off}, remb: on, transport_cc: on, nack: {rtp_history_ms: 1000}, ulpfec: {ulpfec_payload_type: 127, red_payload_type: 102, red_rtx_payload_type: 125}, rtx_ssrc: 2508991752, rtx_payload_types: {96 (apt) -> 97 (pt), 98 (apt) -> 99 (pt), 100 (apt) -> 101 (pt), }, extensions: [{uri: http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01, id: 5}, {uri: http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time, id: 3}, {uri: http://www.webrtc.org/experiments/rtp-hdrext/playout-delay, id: 6}, {uri: urn:3gpp:video-orientation, id: 4}, {uri: urn:ietf:params:rtp-hdrext:toffset, id: 2}]}, renderer: (renderer), render_delay_ms: 10, sync_group: h0Gv19oeTn3moIjfzPmh0hmOvCJwDYdVm5E9, pre_decode_callback: nullptr, target_delay_ms: 0}");
			var actual = token.ToString(Newtonsoft.Json.Formatting.Indented);
			using (var expected = await context.Samples.GetSampleAsStream("JsonLikeParserTest01.json"))
			{
				Check.That(
					actual
				).AsLines().ContainsExactly(
					Helpers.SplitTextStream(expected)
				);
			}
		}
	}
}

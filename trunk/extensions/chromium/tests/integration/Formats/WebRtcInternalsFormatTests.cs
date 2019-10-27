using System.Threading.Tasks;
using NFluent;

namespace LogJoint.Tests.Integration.Chromium
{
	[IntegrationTestFixture]
	class WebRtcInternalsFormatTests
	{
		[IntegrationTest]
		public async Task LoadsLogFileAndEnablesPostprocessors(IContext context)
		{
			await context.Utils.EmulateFileDragAndDrop(await context.Samples.GetSampleAsLocalFile("webrtc_internals_dump_1.txt"));

			await context.Utils.WaitFor(() => context.Presentation.LoadedMessagesLogViewer.VisibleLines.Count > 0);


			Check.That(context.Presentation.LoadedMessagesLogViewer.VisibleLines[0].Value).IsEqualTo(
				"2017-06-30T18:02:21.000000|C|35286-1|log|addIceCandidate|sdpMid: audio, sdpMLineIndex: 0, candidate: candidate:508100464 1 udp 2122260223 192.168.10.157 57279 typ host generation 0 ufrag yKWx network-id 1 network-cost 10");

			var postprocessorsControls = context.Presentation.Postprocessing.SummaryView;
			Check.That(postprocessorsControls.StateInspector.Enabled).IsTrue();
			Check.That(postprocessorsControls.TimeSeries.Enabled).IsTrue();
		}
	}
}

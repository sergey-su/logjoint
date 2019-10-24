using System.Threading.Tasks;
using LogJoint.Postprocessing;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using WRD = LogJoint.Chromium.WebrtcInternalsDump;
using CD = LogJoint.Chromium.ChromeDriver;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Chromium.Correlator
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateChromeDriverPostprocessor();
		ILogSourcePostprocessor CreateWebRtcInternalsPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly IManager postprocessorsManager;
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(IModel ljModel)
		{
			this.postprocessorsManager = ljModel.Postprocessing.Manager;
			this.postprocessing = ljModel.Postprocessing;

			postprocessorsManager.Register(Correlation.NodeDetectionToken.Factory.Instance);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessor(PostprocessorKind.Correlator, i => RunForChromeDebug(i));
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDriverPostprocessor()
		{
			return new LogSourcePostprocessor(PostprocessorKind.Correlator, i => RunForChromeDriver(i));
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWebRtcInternalsPostprocessor()
		{
			return new LogSourcePostprocessor(PostprocessorKind.Correlator, i => RunForWebRtcInternals(i));
		}

		async Task RunForChromeDebug(LogSourcePostprocessorInput input)
		{
			var reader = new CDL.Reader(postprocessing.TextLogParser, input.CancellationToken).Read(input.LogFileName, input.ProgressHandler);

			IPrefixMatcher prefixMatcher = postprocessing.CreatePrefixMatcher();
			var matchedMessages = reader.MatchTextPrefixes(prefixMatcher).Multiplex();
			var webRtcStateInspector = new CDL.WebRtcStateInspector(prefixMatcher);
			var processIdDetector = new CDL.ProcessIdDetector();
			var nodeDetectionTokenTask = (new CDL.NodeDetectionTokenSource(processIdDetector, webRtcStateInspector)).GetToken(matchedMessages);

			var serialize = postprocessing.Correlation.CreatePostprocessorOutputBuilder()
				.SetSameNodeDetectionToken(nodeDetectionTokenTask)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((CDL.Message)evtTrigger))
				.Build(input);

			await Task.WhenAll(
				matchedMessages.Open(),
				serialize
			);
		}

		async Task RunForChromeDriver(LogSourcePostprocessorInput input)
		{
			var reader = (new CD.Reader(postprocessing.TextLogParser, input.CancellationToken)).Read(input.LogFileName, input.ProgressHandler);
			IPrefixMatcher prefixMatcher = postprocessing.CreatePrefixMatcher();
			var matchedMessages = reader.MatchTextPrefixes(prefixMatcher).Multiplex();
			var nodeDetectionTokenTask = (new CD.NodeDetectionTokenSource(new CD.ProcessIdDetector(prefixMatcher), prefixMatcher)).GetToken(matchedMessages);
			var noMessagingEvents = EnumerableAsync.Empty<M.Event[]>();
			var serialize = postprocessing.Correlation.CreatePostprocessorOutputBuilder()
				.SetSameNodeDetectionToken(nodeDetectionTokenTask)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((CD.Message)evtTrigger))
				.Build(input);
			await Task.WhenAll(
				matchedMessages.Open(),
				serialize
			);
		}

		async Task RunForWebRtcInternals(LogSourcePostprocessorInput input)
		{
			var reader = (new WRD.Reader(postprocessing.TextLogParser, input.CancellationToken)).Read(input.LogFileName, input.ProgressHandler);
			IPrefixMatcher prefixMatcher = postprocessing.CreatePrefixMatcher();
			var matchedMessages = WRD.Helpers.MatchPrefixes(reader, prefixMatcher).Multiplex();
			var webRtcStateInspector = new WRD.WebRtcStateInspector(prefixMatcher);
			var nodeDetectionTokenTask = (new WRD.NodeDetectionTokenSource(webRtcStateInspector)).GetToken(matchedMessages);
			var noMessagingEvents = EnumerableAsync.Empty<M.Event[]>();
			var serialize = postprocessing.Correlation.CreatePostprocessorOutputBuilder()
				.SetSameNodeDetectionToken(nodeDetectionTokenTask)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((WRD.Message)evtTrigger))
				.Build(input);
			await Task.WhenAll(
				matchedMessages.Open(),
				serialize
			);
		}
	}
}

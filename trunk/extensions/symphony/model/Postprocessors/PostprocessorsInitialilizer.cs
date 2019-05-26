using System.Linq;
using LogJoint.Postprocessing;
using System;

namespace LogJoint.Symphony
{
	public interface IPostprocessorsRegistry
	{
		LogSourceMetadata SpringServiceLog { get; }
		LogSourceMetadata RtcLog { get; }
	};

	public class PostprocessorsInitializer : IPostprocessorsRegistry
	{
		private readonly IUserDefinedFactory symRtcLogFormat, springServiceLogFormat;
		private readonly LogSourceMetadata symRtcLogMeta, springServiceLogMeta;


		public PostprocessorsInitializer(
			IPostprocessorsManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			StateInspector.IPostprocessorsFactory stateInspectorPostprocessorsFactory,
			TimeSeries.IPostprocessorsFactory timeSeriesPostprocessorsFactory,
			Correlator.IPostprocessorsFactory correlatorPostprocessorsFactory,
			Timeline.IPostprocessorsFactory timelinePostprocessorsFactory,
			SequenceDiagram.IPostprocessorsFactory sequenceDiagramPostprocessorsFactory
		)
		{
			Func<string, string, IUserDefinedFactory> findFormat = (company, formatName) =>
			{
				var ret = userDefinedFormatsManager.Items.FirstOrDefault(
					f => f.CompanyName == company && f.FormatName == formatName);
				if (ret == null)
					throw new Exception(string.Format("Log format {0} is not registered in LogJoint", formatName));
				return ret;
			};

			this.symRtcLogFormat = findFormat("Symphony", "RTC log");
			this.springServiceLogFormat = findFormat("Symphony", "RTC Java Spring Service log");

			var correlatorPostprocessorType = correlatorPostprocessorsFactory.CreatePostprocessor(this);
			postprocessorsManager.RegisterCrossLogSourcePostprocessor(correlatorPostprocessorType);

			this.springServiceLogMeta = new LogSourceMetadata(
				springServiceLogFormat,
				sequenceDiagramPostprocessorsFactory.CreateSpringServiceLogPostprocessor()
			);
			postprocessorsManager.RegisterLogType(this.springServiceLogMeta);

			this.symRtcLogMeta = new LogSourceMetadata(
				symRtcLogFormat,
				stateInspectorPostprocessorsFactory.CreateSymphonyRtcPostprocessor(),
				timeSeriesPostprocessorsFactory.CreateSymphonyRtcPostprocessor(),
				timelinePostprocessorsFactory.CreateSymRtcPostprocessor()
			);
			postprocessorsManager.RegisterLogType(this.symRtcLogMeta);
		}

		LogSourceMetadata IPostprocessorsRegistry.SpringServiceLog => springServiceLogMeta;

		LogSourceMetadata IPostprocessorsRegistry.RtcLog => symRtcLogMeta;
	};
}

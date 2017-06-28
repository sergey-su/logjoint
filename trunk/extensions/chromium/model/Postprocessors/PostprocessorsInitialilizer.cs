using System.Linq;
using LogJoint.Postprocessing;
using System;
using UDF = LogJoint.RegularGrammar.UserDefinedFormatFactory;

namespace LogJoint.Chromium
{
	public interface IPostprocessorsRegistry
	{
		LogSourceMetadata ChromeDebugLog { get; }
	};

	public class PostprocessorsInitialilizer : IPostprocessorsRegistry
	{
		private readonly UDF chromeDebugLogFormat;
		private readonly LogSourceMetadata chromeDebugLogMeta;


		public PostprocessorsInitialilizer(
			IPostprocessorsManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			StateInspector.IPostprocessorsFactory stateInspectorPostprocessorsFactory,
			TimeSeries.IPostprocessorsFactory timeSeriesPostprocessorsFactory
		)
		{
			Func<string, UDF> findFormat = formatName =>
			{
				var ret = userDefinedFormatsManager.Items.FirstOrDefault(
					f => f.CompanyName == "Google" && f.FormatName == formatName) as UDF;
				if (ret == null)
					throw new Exception(string.Format("Log format {0} is not registered in LogJoint", formatName));
				return ret;
			};

			this.chromeDebugLogFormat = findFormat("Chrome debug log");


			this.chromeDebugLogMeta = new LogSourceMetadata(
				chromeDebugLogFormat,
				stateInspectorPostprocessorsFactory.CreateChromeDebugPostprocessor(),
				timeSeriesPostprocessorsFactory.CreateChromeDebugPostprocessor()
			);
			postprocessorsManager.RegisterLogType(this.chromeDebugLogMeta);
		}

		LogSourceMetadata IPostprocessorsRegistry.ChromeDebugLog
		{
			get { return chromeDebugLogMeta; }
		}
	};
}

using System.Linq;
using LogJoint.Postprocessing;
using System;
using UDF = LogJoint.UserDefinedFactoryBase;

namespace LogJoint.PacketAnalysis
{
	public interface IPostprocessorsRegistry
	{
		LogSourceMetadata WiresharkPdml { get; }
	};

	public class PostprocessorsInitializer : IPostprocessorsRegistry
	{
		private readonly UDF wiresharkPdmlFormat;
		private readonly LogSourceMetadata wiresharkPdml;


		public PostprocessorsInitializer(
			IPostprocessorsManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			Timeline.IPostprocessorsFactory timelinePostprocessorsFactory
		)
		{
			UDF findFormat(string company, string formatName)
			{
				var ret = userDefinedFormatsManager.Items.FirstOrDefault(
					f => f.CompanyName == company && f.FormatName == formatName) as UDF;
				if (ret == null)
					throw new Exception(string.Format("Log format {0} is not registered in LogJoint", formatName));
				return ret;
			}

			this.wiresharkPdmlFormat = findFormat("Wireshark", "pdml");

			this.wiresharkPdml = new LogSourceMetadata(
				wiresharkPdmlFormat,
				timelinePostprocessorsFactory.CreateWiresharkDpmlPostprocessor()
			);
			postprocessorsManager.RegisterLogType(this.wiresharkPdml);
		}

		LogSourceMetadata IPostprocessorsRegistry.WiresharkPdml
		{
			get { return wiresharkPdml; }
		}
	};
}

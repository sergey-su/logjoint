using System.Linq;
using LogJoint.Postprocessing;
using System;

namespace LogJoint.PacketAnalysis
{
	public interface IPostprocessorsRegistry
	{
		LogSourceMetadata WiresharkPdml { get; }
	};

	public class PostprocessorsInitializer : IPostprocessorsRegistry
	{
		private readonly IUserDefinedFactory wiresharkPdmlFormat;
		private readonly LogSourceMetadata wiresharkPdml;


		public PostprocessorsInitializer(
			IManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			Timeline.IPostprocessorsFactory timelinePostprocessorsFactory
		)
		{
			IUserDefinedFactory findFormat(string company, string formatName)
			{
				var ret = userDefinedFormatsManager.Items.FirstOrDefault(
					f => f.CompanyName == company && f.FormatName == formatName);
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

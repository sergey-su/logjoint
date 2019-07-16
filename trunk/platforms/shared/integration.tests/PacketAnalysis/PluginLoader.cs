using PA = LogJoint.PacketAnalysis;

namespace LogJoint.Tests.Integration.PacketAnalysis
{
	public class PluginLoader
	{
		public readonly IPluginManifest Manifest =
			new Extensibility.PluginManifest(PluginUtils.GetPluginDirectory("packet-analysis"));

		public PluginLoader()
		{
			Manifest.ValidateFilesExist();
			PluginUtils.LoadPluginAssemblies(Manifest, () =>
			{
				typeof(PA.Factory).ToString();
				typeof(PA.UI.Presenters.Factory).ToString();
			});
		}
	};
}

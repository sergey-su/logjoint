
using CR = LogJoint.Chromium;

namespace LogJoint.Tests.Integration.Chromium
{
	public class PluginLoader
	{
		public readonly IPluginManifest Manifest =
			new Extensibility.PluginManifest(PluginUtils.GetPluginDirectory("chromium"));

		public PluginLoader()
		{
			Manifest.ValidateFilesExist();
			PluginUtils.LoadPluginAssemblies(Manifest, () =>
			{
				typeof(CR.Factory).ToString();
			});
		}
	};
}

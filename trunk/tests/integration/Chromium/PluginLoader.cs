
using CR = LogJoint.Chromium;
using LogJoint.Extensibility;

namespace LogJoint.Tests.Integration.Chromium
{
	public class PluginLoader
	{
		public readonly IPluginManifest Manifest =
			new PluginManifest(PluginUtils.GetPluginDirectory("chromium"));

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

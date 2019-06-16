using LogJoint.PacketAnalysis;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IApplication app)
		{
			PluginInitializer.Init(
				app,
				new PacketAnalysis.UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage.WiresharkPageUI(),
				null
			);
		}
	}
}

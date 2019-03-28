using LogJoint.Extensibility;
using LogJoint.PacketAnalysis;

namespace LogJoint
{
	public class Plugin : PluginBase
	{
		public Plugin()
		{
		}

		public override void Init(IApplication app)
		{
			PluginInitializer.Init(
				app,
				new PacketAnalysis.UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage.WiresharkPageUI()
			);
		}

		public override void Dispose()
		{
		}
	}
}

using NSubstitute;

namespace LogJoint
{
    public class Plugin
    {
        public Plugin(
            Tests.Integration.IContext context
        )
        {
            var viewFactory = Substitute.For<PacketAnalysis.UI.Presenters.Factory.IViewsFactory>();
            PacketAnalysis.PluginInitializer.Init(context.Model, context.Presentation, viewFactory);
            context.Registry.Set(viewFactory);
        }
    }
}

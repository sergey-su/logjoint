using AppKit;
using LogJoint;
using LogJoint.UI.Mac;
using LogJoint.UI.Reactive;

namespace LogJoint
{
	public class Application: IApplication, IView, IReactive
	{
		private readonly Telemetry.ITelemetryCollector telemetryCollector;

		public Application (
			IModel model,
			UI.Presenters.IPresentation presentation,
			Telemetry.ITelemetryCollector telemetryCollector
		)
		{
			Model = model;
			Presentation = presentation;
		}

		public IModel Model { get; private set; }
		public UI.Presenters.IPresentation Presentation { get; private set; }
		public IView View => this;

		IReactive IView.Reactive => this;

		INSOutlineViewController IReactive.CreateOutlineViewController (NSOutlineView outlineView)
		{
			return new NSOutlineViewController (outlineView, telemetryCollector);
		}
	}
}

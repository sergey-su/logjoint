using AppKit;
using LogJoint;
using LogJoint.UI.Mac;

namespace LogJoint
{
	public class Application: IApplication, IView
	{
		private readonly IReactive reactive;

		public Application (
			IModel model,
			UI.Presenters.IPresentation presentation,
			IReactive reactive
		)
		{
			this.Model = model;
			this.Presentation = presentation;
			this.reactive = reactive;
		}

		public IModel Model { get; private set; }
		public UI.Presenters.IPresentation Presentation { get; private set; }
		public IView View => this;

		IReactive IView.Reactive => reactive;
	}
}

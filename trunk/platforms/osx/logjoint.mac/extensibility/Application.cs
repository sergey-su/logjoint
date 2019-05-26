using LogJoint;

namespace LogJoint
{
	public class Application: IApplication
	{
		public Application (
			IModel model,
			UI.Presenters.IPresentation presentation
		)
		{
			Model = model;
			Presentation = presentation;
		}

		public IModel Model { get; private set; }
		public UI.Presenters.IPresentation Presentation { get; private set; }
	}
}

namespace LogJoint.Extensibility
{
	class Application: IApplication
	{
		public Application(
			IModel model,
			IPresentation presentation,
			IView view
		)
		{
			this.presentation = presentation;
			this.model = model;
			this.view = view;
		}

		IPresentation IApplication.Presentation { get { return presentation; } }

		IModel IApplication.Model { get { return model; } }

		IView IApplication.View { get { return view; } }

		readonly IModel model;
		readonly IPresentation presentation;
		readonly IView view;
	}
}
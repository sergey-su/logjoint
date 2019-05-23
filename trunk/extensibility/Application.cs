namespace LogJoint.Extensibility
{
	class Application: IApplication
	{
		public Application(
			IModel model,
			UI.Presenters.IPresentation presentation,
			UI.Windows.IView view
		)
		{
			this.presentation = presentation;
			this.model = model;
			this.view = view;
		}

		UI.Presenters.IPresentation IApplication.Presentation { get { return presentation; } }

		IModel IApplication.Model { get { return model; } }

		UI.Windows.IView IApplication.View { get { return view; } }

		readonly IModel model;
		readonly UI.Presenters.IPresentation presentation;
		readonly UI.Windows.IView view;
	}
}
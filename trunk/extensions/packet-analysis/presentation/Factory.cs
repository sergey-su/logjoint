namespace LogJoint.PacketAnalysis.UI.Presenters
{
	public class PresentationObjects
	{
	};

	public class Factory
	{
		public interface IViewsFactory
		{
			NewLogSourceDialog.Pages.WiresharkPage.IView CreateWiresharkPageView();
			MessagePropertiesDialog.IView CreateMessageContentView();
		};

		public static PresentationObjects Create(
			ModelObjects model,
			LogJoint.UI.Presenters.IPresentation appPresentation,
			IModel appModel,
			IViewsFactory viewsFactory)
		{
			appPresentation.NewLogSourceDialog.PagesRegistry.RegisterPagePresenterFactory(
				"wireshark",
				f => new NewLogSourceDialog.Pages.WiresharkPage.Presenter(
					viewsFactory.CreateWiresharkPageView(),
					appModel.Preprocessing.Manager,
					appModel.Preprocessing.StepsFactory,
					model.TShark
				)
			);

			appPresentation.MessagePropertiesDialog.ExtensionsRegistry.Register(
				new MessagePropertiesDialog.Extension(model.PostprocessorsRegistry, viewsFactory.CreateMessageContentView, appPresentation.ClipboardAccess)
			);

			return new PresentationObjects
			{
			};
		}
	};
};
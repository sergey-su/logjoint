using System;
using System.Linq;
using LogJoint;
using MPD = LogJoint.UI.Presenters.MessagePropertiesDialog;

namespace LogJoint.PacketAnalysis
{
	public class PluginInitializer
	{
		public static void Init(
			IApplication app,
			UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage.IView wiresharkPageView,
			Func<UI.Presenters.MessagePropertiesDialog.IView> messageContentViewFactory
		)
		{
			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.Manager, 
				app.Model.UserDefinedFormatsManager, 
				new Timeline.PostprocessorsFactory(app.Model.Postprocessing)
			);

			var tshark = new Wireshark.Dpml.TShark();
			var wiresharkPreprocessingStepsFactory = new LogJoint.Wireshark.Dpml.PreprocessingStepsFactory(
				app.Model.PreprocessingStepsFactory, tshark
			);

			app.Model.PreprocessingManagerExtensionsRegistry.Register(
				new LogJoint.Wireshark.Dpml.PreprocessingManagerExtension(wiresharkPreprocessingStepsFactory, tshark)
			);

			app.Presentation.NewLogSourceDialog.PagesRegistry.RegisterPagePresenterFactory(
				"wireshark",
				f => new UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage.Presenter(
					wiresharkPageView,
					app.Model.LogSourcesPreprocessingManager,
					app.Model.PreprocessingStepsFactory,
					tshark
				)
			);

			if (messageContentViewFactory != null)
			{
				app.Presentation.MessagePropertiesDialog.ExtensionsRegistry.Register(
					new MessagePropertiesDialogExtension
					{
						postprocessorsRegistry = postprocessorsRegistry,
						viewFactory = messageContentViewFactory,
						clipboardAccess = app.Presentation.ClipboardAccess
					}
				);
			}
		}

		class MessagePropertiesDialogExtension : MPD.IExtension
		{
			internal IPostprocessorsRegistry postprocessorsRegistry;
			internal Func<UI.Presenters.MessagePropertiesDialog.IView> viewFactory;
			internal LogJoint.UI.Presenters.IClipboardAccess clipboardAccess;
			private UI.Presenters.MessagePropertiesDialog.IPresenter presenter;

			MPD.IMessageContentPresenter MPD.IExtension.CreateContentPresenter(MPD.ContentPresenterParams @params)
			{
				UI.Presenters.MessagePropertiesDialog.IPresenter result = null;
				if (@params.Message.GetLogSource()?.Provider?.Factory ==
					postprocessorsRegistry.WiresharkPdml.LogProviderFactory)
				{
					if (presenter == null)
					{
						presenter = new UI.Presenters.MessagePropertiesDialog.Presenter(
							viewFactory(),
							@params.ChangeNotification,
							clipboardAccess
						);
					}
					result = presenter;
					result.SetMessage(@params.Message);
				}
				return result;
			}
		};
	}
}

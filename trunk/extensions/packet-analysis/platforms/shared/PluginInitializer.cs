using System;
using System.Linq;
using LogJoint.Extensibility;

namespace LogJoint.PacketAnalysis
{
	public class PluginInitializer
	{
		public static void Init(
			IApplication app,
			UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage.IView wiresharkPageView
		)
		{
			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.PostprocessorsManager, 
				app.Model.UserDefinedFormatsManager, 
				new Timeline.PostprocessorsFactory(app.Model.TempFilesManager)
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
					wiresharkPreprocessingStepsFactory,
					tshark
				)
			);
		}
	}
}

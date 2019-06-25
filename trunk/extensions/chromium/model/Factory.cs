
namespace LogJoint.Chromium
{
	public class ModelObjects
	{
		public IPostprocessorsRegistry PostprocessorsRegistry { get; internal set; }
	};

	public class Factory
	{
		public static ModelObjects Create(LogJoint.IModel appModel)
		{
			appModel.Postprocessing.TimeSeries.RegisterTimeSeriesTypesAssembly(typeof(TimeSeries.PostprocessorsFactory).Assembly);

			var pluginModel = new PluginModel();
			appModel.PluginsManager.Register<IPluginModel>(pluginModel);

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				appModel.Postprocessing.Manager,
				appModel.UserDefinedFormatsManager,
				new StateInspector.PostprocessorsFactory(appModel.Postprocessing, pluginModel),
				new TimeSeries.PostprocessorsFactory(appModel.Postprocessing, pluginModel),
				new Correlator.PostprocessorsFactory(appModel),
				new Timeline.PostprocessorsFactory(appModel.Postprocessing, pluginModel),
				new SequenceDiagram.PostprocessorsFactory(appModel.Postprocessing)
			);

			appModel.PreprocessingManagerExtensionsRegistry.Register(
				new WebrtcInternalsDump.PreprocessingManagerExtension(appModel.PreprocessingStepsFactory)
			);
			appModel.PreprocessingManagerExtensionsRegistry.Register(
				new ChromeDriver.PreprocessingManagerExtension(appModel.PreprocessingStepsFactory, postprocessorsRegistry.ChromeDriver.LogProviderFactory, appModel.Postprocessing.TextLogParser)
			);
			appModel.PreprocessingManagerExtensionsRegistry.Register(
				new HttpArchive.PreprocessingManagerExtension(appModel.PreprocessingStepsFactory, postprocessorsRegistry.HttpArchive.LogProviderFactory)
			);

			return new ModelObjects
			{
				PostprocessorsRegistry = postprocessorsRegistry
			};
		}
	};
}

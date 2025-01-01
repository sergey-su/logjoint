
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
                new Correlator.PostprocessorsFactory(appModel, pluginModel),
                new Timeline.PostprocessorsFactory(appModel.Postprocessing, pluginModel),
                new SequenceDiagram.PostprocessorsFactory(appModel.Postprocessing, pluginModel)
            );

            appModel.Preprocessing.ExtensionsRegistry.Register(
                new WebrtcInternalsDump.PreprocessingManagerExtension(appModel.Preprocessing.StepsFactory)
            );
            appModel.Preprocessing.ExtensionsRegistry.Register(
                new ChromeDriver.PreprocessingManagerExtension(appModel.Preprocessing.StepsFactory, postprocessorsRegistry.ChromeDriver.LogProviderFactory, appModel.Postprocessing.TextLogParser)
            );
            appModel.Preprocessing.ExtensionsRegistry.Register(
                new HttpArchive.PreprocessingManagerExtension(appModel.Preprocessing.StepsFactory, postprocessorsRegistry.HttpArchive.LogProviderFactory)
            );

            return new ModelObjects
            {
                PostprocessorsRegistry = postprocessorsRegistry
            };
        }
    };
}

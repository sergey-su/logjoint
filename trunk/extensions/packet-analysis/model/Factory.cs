
namespace LogJoint.PacketAnalysis
{
    public class ModelObjects
    {
        public IPostprocessorsRegistry PostprocessorsRegistry { get; internal set; }
        public Wireshark.Dpml.ITShark TShark { get; internal set; }
    };

    public static class Factory
    {
        public static ModelObjects Create(LogJoint.IModel appModel)
        {
            IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
                appModel.Postprocessing.Manager,
                appModel.UserDefinedFormatsManager,
                new Timeline.PostprocessorsFactory(appModel.Postprocessing)
            );

            var tshark = new Wireshark.Dpml.TShark();
            var wiresharkPreprocessingStepsFactory = new LogJoint.Wireshark.Dpml.PreprocessingStepsFactory(
                appModel.Preprocessing.StepsFactory, tshark
            );

            appModel.Preprocessing.ExtensionsRegistry.Register(
                new LogJoint.Wireshark.Dpml.PreprocessingManagerExtension(wiresharkPreprocessingStepsFactory, tshark)
            );

            return new ModelObjects
            {
                PostprocessorsRegistry = postprocessorsRegistry,
                TShark = tshark
            };
        }
    };
}

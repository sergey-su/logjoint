namespace LogJoint.Preprocessing
{
	public class Model : IModel
	{
		public Model(
			IManager manager,
			IStepsFactory stepsFactory,
			IExtensionsRegistry extentionsRegistry
		)
		{
			this.Manager = manager;
			this.StepsFactory = stepsFactory;
			this.ExtensionsRegistry = extentionsRegistry;
		}

		public IManager Manager { get; private set; }
		public IStepsFactory StepsFactory { get; private set; }
		public IExtensionsRegistry ExtensionsRegistry { get; private set; }
	}
}

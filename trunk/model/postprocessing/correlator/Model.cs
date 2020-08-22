namespace LogJoint.Postprocessing.Correlation
{
	class Model : IModel
	{
		readonly ITempFilesManager tempFiles;
		readonly ILogPartTokenFactories logPartTokenFactories;
		readonly ISameNodeDetectionTokenFactories nodeDetectionTokenFactories;

		public Model(ITempFilesManager tempFiles,
			ILogPartTokenFactories logPartTokenFactories, ISameNodeDetectionTokenFactories nodeDetectionTokenFactories)
		{
			this.tempFiles = tempFiles;
			this.logPartTokenFactories = logPartTokenFactories;
			this.nodeDetectionTokenFactories = nodeDetectionTokenFactories;
		}

		PostprocessorOutputBuilder IModel.CreatePostprocessorOutputBuilder()
		{
			return new PostprocessorOutputBuilder
			{
				build = (postprocessorInput, builder) => PostprocessorOutput.SerializePostprocessorOutput(
					builder.logPart,
					logPartTokenFactories,
					builder.events,
					builder.sameNodeDetectionToken,
					nodeDetectionTokenFactories,
					builder.triggersConverter,
					postprocessorInput.InputContentsEtag,
					postprocessorInput.openOutputFile,
					tempFiles,
					postprocessorInput.CancellationToken
				)
			};
		}
	}
}

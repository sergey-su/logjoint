using System;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public enum ViewControlId
	{
		StateInspector,
		Timeline,
		Sequence,
		TimeSeries,
		Correlate,
		AllPostprocessors,
		LogsCollectionControl1,
		LogsCollectionControl2,
		LogsCollectionControl3,
	};

	
	public interface IPostprocessorOutputFormFactory
	{
		IPostprocessorOutputForm GetPostprocessorOutputForm(ViewControlId id);

		/// <summary>
		/// Plugins can use it to override default postprocessor view with custom one.
		/// </summary>
		void OverrideFormFactory(ViewControlId id, Func<IPostprocessorOutputForm> factory);
		event EventHandler<PostprocessorOutputFormCreatedEventArgs> FormCreated;
	};

	public class PostprocessorOutputFormCreatedEventArgs: EventArgs
	{
		public ViewControlId Id { get; private set; }
		public IPostprocessorOutputForm Form { get; private set; }
		public object Presenter { get; private set; }

		public PostprocessorOutputFormCreatedEventArgs(ViewControlId id, IPostprocessorOutputForm form, object presenter)
		{
			this.Id = id;
			this.Form = form;
			this.Presenter = presenter;
		}
	};
}

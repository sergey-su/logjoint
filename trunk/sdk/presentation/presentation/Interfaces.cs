namespace LogJoint.UI.Presenters
{
	public interface IPresentation
	{
		NewLogSourceDialog.IPresenter NewLogSourceDialog { get; }
		UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputFormFactory PostprocessorsFormFactory { get; }
	}
}

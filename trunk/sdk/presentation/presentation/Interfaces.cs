namespace LogJoint.UI.Presenters
{
	public interface IPresentation
	{
		NewLogSourceDialog.IPresenter NewLogSourceDialog { get; }
		Postprocessing.MainWindowTabPage.IPostprocessorOutputFormFactory PostprocessorsFormFactory { get; }
		IColorTheme Theme { get; }
		MessagePropertiesDialog.IPresenter MessagePropertiesDialog { get; }
	}
}

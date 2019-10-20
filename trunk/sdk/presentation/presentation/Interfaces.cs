namespace LogJoint.UI.Presenters
{
	public interface IPresentation
	{
		NewLogSourceDialog.IPresenter NewLogSourceDialog { get; }
		IColorTheme Theme { get; }
		MessagePropertiesDialog.IPresenter MessagePropertiesDialog { get; }
		IClipboardAccess ClipboardAccess { get; }
		IPromptDialog PromptDialog { get; }
		Postprocessing.IPresentation Postprocessing { get; }
	}
}

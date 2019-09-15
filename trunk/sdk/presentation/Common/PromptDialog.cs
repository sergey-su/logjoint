namespace LogJoint.UI.Presenters
{
	public interface IPromptDialog
	{
		string ExecuteDialog(string caption, string prompt, string defaultValue);
	};
};
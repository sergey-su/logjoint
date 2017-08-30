using System;

namespace LogJoint.UI.Presenters
{
	public interface IFileDialogs
	{
		string[] OpenFileDialog(OpenFileDialogParams p);
		string SaveFileDialog(SaveFileDialogParams p);
	}

	public struct OpenFileDialogParams
	{
		public bool CanChooseFiles;
		public bool AllowsMultipleSelection;
		public bool CanChooseDirectories;
		public string Filter;
	};

	public struct SaveFileDialogParams
	{
		public string Title;
		public string SuggestedFileName;
	};
}


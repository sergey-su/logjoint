using System;

namespace LogJoint.UI.Presenters
{
	public interface IShellOpen
	{
		void OpenInWebBrowser(Uri uri);
		void OpenFileBrowser(string filePath);
		void OpenInTextEditor(string filePath);
	}
}


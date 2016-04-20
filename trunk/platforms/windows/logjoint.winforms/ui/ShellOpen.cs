using LogJoint.UI.Presenters;
using System;
using System.Diagnostics;

namespace LogJoint.UI
{
	class ShellOpen: IShellOpen
	{
		void IShellOpen.OpenInWebBrowser(Uri uri)
		{
			Process.Start(uri.ToString());
		}

		void IShellOpen.OpenFileBrowser(string filePath)
		{
			Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
		}

		void IShellOpen.OpenInTextEditor(string filePath)
		{
			Process.Start("notepad.exe", filePath);
		}
	}
}

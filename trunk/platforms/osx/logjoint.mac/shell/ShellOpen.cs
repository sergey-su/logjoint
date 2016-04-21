using System;
using LogJoint.UI.Presenters;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace LogJoint
{
	public class ShellOpen: IShellOpen
	{
		void IShellOpen.OpenInWebBrowser(Uri uri)
		{
			throw new NotImplementedException();
		}

		void IShellOpen.OpenFileBrowser(string filePath)
		{
			NSWorkspace.SharedWorkspace.ActivateFileViewer(new [] { NSUrl.FromFilename(filePath) });
		}

		void IShellOpen.OpenInTextEditor(string filePath)
		{
			throw new NotImplementedException();
		}
	}
}


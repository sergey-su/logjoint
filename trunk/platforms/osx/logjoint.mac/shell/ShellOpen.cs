using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LogJoint.UI.Presenters;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace LogJoint.UI
{
	public class ShellOpen: IShellOpen
	{
		void IShellOpen.OpenInWebBrowser(Uri uri)
		{
			NSWorkspace.SharedWorkspace.OpenUrl(uri);
		}

		void IShellOpen.OpenFileBrowser(string filePath)
		{
			NSWorkspace.SharedWorkspace.ActivateFileViewer(new [] { NSUrl.FromFilename(filePath) });
		}

		void IShellOpen.OpenInTextEditor(string filePath)
		{
			Process.Start(filePath);
		}

		Task IShellOpen.EditFile (string filePath, CancellationToken cancel)
		{
			throw new NotImplementedException ();
		}
	}
}


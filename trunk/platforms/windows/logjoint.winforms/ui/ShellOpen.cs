using LogJoint.UI.Presenters;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI
{
    class ShellOpen : IShellOpen
    {
        void IShellOpen.OpenInWebBrowser(Uri uri)
        {
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true } );
        }

        void IShellOpen.OpenFileBrowser(string filePath)
        {
            Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
        }

        void IShellOpen.OpenInTextEditor(string filePath)
        {
            Process.Start("notepad.exe", filePath);
        }

        async Task IShellOpen.EditFile(string filePath, CancellationToken cancel)
        {
            using (var process = Process.Start("notepad.exe", filePath))
            using (cancel.Register(() => process.Kill()))
            {
                await process.GetExitCodeAsync(Timeout.InfiniteTimeSpan);
            }
        }
    }
}

using LogJoint.UI.Presenters;
using LogJoint.Wasm.UI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class ShellOpen : IShellOpen
    {
        public ShellOpen(BrowserInterop browser)
        {
            this.browserInterop = browser;
        }

        public void SetFileEditor(LogJoint.UI.Presenters.FileEditor.IPresenter fileEditor)
        {
            this.fileEditor = fileEditor;
        }

        Task IShellOpen.EditFile(string filePath, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        void IShellOpen.OpenFileBrowser(string filePath)
        {
            throw new NotImplementedException();
        }

        void IShellOpen.OpenInTextEditor(string filePath)
        {
            fileEditor.ShowDialog(filePath, readOnly: true);
        }

        async void IShellOpen.OpenInWebBrowser(Uri uri)
        {
            await browserInterop.OpenUrl(uri);
        }

        LogJoint.UI.Presenters.FileEditor.IPresenter fileEditor;
        readonly BrowserInterop browserInterop;
    }
}

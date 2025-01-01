using LogJoint.UI.Presenters;
using LogJoint.Wasm.UI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class ShellOpen : IShellOpen
    {
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

        void IShellOpen.OpenInWebBrowser(Uri uri)
        {
            throw new NotImplementedException();
        }

        LogJoint.UI.Presenters.FileEditor.IPresenter fileEditor;
    }
}

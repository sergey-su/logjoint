using LogJoint.UI.Presenters;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class FileDialogs : IFileDialogs
    {
        public FileDialogs(JsInterop jsInterop)
        {
            this.jsInterop = jsInterop;
        }

        string[] IFileDialogs.OpenFileDialog(OpenFileDialogParams p)
        {
            throw new NotImplementedException();
        }

        string IFileDialogs.SaveFileDialog(SaveFileDialogParams p)
        {
            throw new NotImplementedException();
        }

        async Task IFileDialogs.SaveOrDownloadFile(Func<Stream, Task> saver, SaveFileDialogParams p)
        {
            using var stream = new MemoryStream();
            await saver(stream);
            await stream.FlushAsync();
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            await jsInterop.SaveAs.SaveAs(await reader.ReadToEndAsync(), p.SuggestedFileName);
        }

        readonly JsInterop jsInterop;
    }
}

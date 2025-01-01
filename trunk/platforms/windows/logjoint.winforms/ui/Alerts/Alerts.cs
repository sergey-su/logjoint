using LogJoint.UI.Presenters;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace LogJoint.UI
{
    public class Alerts : IAlertPopup, IFileDialogs
    {
        string[] IFileDialogs.OpenFileDialog(OpenFileDialogParams p)
        {
            if (p.CanChooseDirectories)
            {
                var folderBrowserDialog = new FolderBrowserDialog();
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    return new[] { folderBrowserDialog.SelectedPath };
            }
            else if (p.CanChooseFiles)
            {
                var browseFileDialog = new OpenFileDialog();
                browseFileDialog.Filter = p.Filter ?? "";
                browseFileDialog.Multiselect = p.AllowsMultipleSelection;
                browseFileDialog.ShowReadOnly = false;
                if (browseFileDialog.ShowDialog() == DialogResult.OK)
                    return browseFileDialog.FileNames;
            }
            return null;
        }

        string IFileDialogs.SaveFileDialog(SaveFileDialogParams p)
        {
            var browseFileDialog = new SaveFileDialog();
            if (p.Title != null)
                browseFileDialog.Title = p.Title;
            browseFileDialog.FileName = p.SuggestedFileName ?? "";
            if (browseFileDialog.ShowDialog() == DialogResult.OK)
                return browseFileDialog.FileName;
            return null;
        }

        async Task IFileDialogs.SaveOrDownloadFile(Func<Stream, Task> saver, SaveFileDialogParams p)
        {
            var fileName = ((IFileDialogs)this).SaveFileDialog(p);
            if (fileName != null)
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                using (var fs = new FileStream(fileName, FileMode.Create))
                    await saver(fs);
            }
        }

        Task<AlertFlags> IAlertPopup.ShowPopupAsync(string caption, string text, AlertFlags flags)
        {
            var taskSource = new TaskCompletionSource<AlertFlags>();
            SynchronizationContext.Current.Post(_ => taskSource.SetResult(
                ((IAlertPopup)this).ShowPopup(caption, text, flags)), null);
            return taskSource.Task;
        }

        AlertFlags IAlertPopup.ShowPopup(string caption, string text, AlertFlags flags)
        {
            MessageBoxButtons btns;
            switch (flags & AlertFlags.ButtonsMask)
            {
                case AlertFlags.Yes | AlertFlags.No:
                    btns = MessageBoxButtons.YesNo;
                    break;
                case AlertFlags.Yes | AlertFlags.No | AlertFlags.Cancel:
                    btns = MessageBoxButtons.YesNoCancel;
                    break;
                case AlertFlags.Ok | AlertFlags.Cancel:
                    btns = MessageBoxButtons.OKCancel;
                    break;
                default:
                    btns = MessageBoxButtons.OK;
                    break;
            }

            MessageBoxIcon icon;
            switch (flags & AlertFlags.IconsMask)
            {
                case AlertFlags.QuestionIcon:
                    icon = MessageBoxIcon.Question;
                    break;
                case AlertFlags.WarningIcon:
                    icon = MessageBoxIcon.Warning;
                    break;
                default:
                    icon = MessageBoxIcon.None;
                    break;
            }

            switch (MessageBox.Show(text, caption, btns, icon))
            {
                case DialogResult.OK:
                    return AlertFlags.Ok;
                case DialogResult.Cancel:
                    return AlertFlags.Cancel;
                case DialogResult.Yes:
                    return AlertFlags.Yes;
                case DialogResult.No:
                    return AlertFlags.No;
                default:
                    return AlertFlags.None;
            }
        }
    }
}

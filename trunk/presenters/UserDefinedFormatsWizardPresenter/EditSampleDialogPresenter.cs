using System.Text;
using System.IO;
using System;
using System.Linq;

namespace LogJoint.UI.Presenters.FormatsWizard.EditSampleDialog
{
    internal class Presenter : IPresenter, IDisposable, IViewEvents
    {
        readonly IView view;
        readonly IAlertPopup alerts;
        readonly IFileDialogs fileDialogs;
        ISampleLogAccess sampleLog;

        public Presenter(
            IView view,
            IFileDialogs fileDialogs,
            IAlertPopup alerts
        )
        {
            this.view = view;
            this.fileDialogs = fileDialogs;
            this.alerts = alerts;
            this.view.SetEventsHandler(this);
        }

        void IDisposable.Dispose()
        {
            view.Dispose();
        }

        void IViewEvents.OnCloseButtonClicked(bool accepted)
        {
            if (accepted)
                sampleLog.SampleLog = view.SampleLogTextBoxValue;
            view.Close();
        }

        void IViewEvents.OnLoadSampleButtonClicked()
        {
            var fileName = fileDialogs.OpenFileDialog(new OpenFileDialogParams()
            {
                CanChooseFiles = true
            })?.FirstOrDefault();
            if (fileName == null)
                return;
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader r = new StreamReader(fs, Encoding.ASCII, true))
                {
                    char[] buf = new char[1024 * 4];
                    var sample = new string(buf, 0, r.Read(buf, 0, buf.Length));
                    view.SampleLogTextBoxValue = sample;
                }
            }
            catch (Exception ex)
            {
                alerts.ShowPopup("Error", "Failed to read the file: " + ex.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
            }
        }

        void IPresenter.ShowDialog(ISampleLogAccess sampleLog)
        {
            this.sampleLog = sampleLog;
            view.SampleLogTextBoxValue = sampleLog.SampleLog;
            view.Show();
        }
    };
};
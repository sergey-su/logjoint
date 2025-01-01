using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat
{
    public interface IView
    {
        void SetEventsHandler(IViewEvents eventsHandler);
        object PageView { get; }
        object ReadControlValue(ControlId id);
        void WriteControlValue(ControlId id, object value);
        void SetEnabled(ControlId id, bool value);
    };

    public enum ControlId
    {
        IndependentLogsModeButton,
        RotatedLogModeButton,
        FileSelector,
        FolderSelector,
        PatternsSelector,
    };

    public interface IViewEvents
    {
        void OnSelectedModeChanged();
        void OnBrowseFilesButtonClicked();
        void OnBrowseFolderButtonClicked();
    };

    public class Presenter : IPagePresenter, IViewEvents
    {
        readonly IView view;
        readonly IFileBasedLogProviderFactory factory;
        readonly ILogSourcesManager model;
        readonly IAlertPopup alerts;
        readonly IFileDialogs fileDialogs;

        public Presenter(
            IView view,
            IFileBasedLogProviderFactory factory,
            ILogSourcesManager model,
            IAlertPopup alerts,
            IFileDialogs fileDialogs
        )
        {
            this.view = view;
            this.factory = factory;
            this.model = model;
            this.alerts = alerts;
            this.fileDialogs = fileDialogs;

            view.SetEventsHandler(this);
        }

        async void IPagePresenter.Apply()
        {
            if ((bool)view.ReadControlValue(ControlId.IndependentLogsModeButton))
                await ApplyIndependentLogsMode();
            else if ((bool)view.ReadControlValue(ControlId.RotatedLogModeButton))
                await ApplyRotatedLogMode();
        }

        void IPagePresenter.Activate()
        {
            UpdateView(initialUpdate: true);
        }

        void IPagePresenter.Deactivate()
        {
        }

        object IPagePresenter.View
        {
            get { return view.PageView; }
        }

        void IDisposable.Dispose()
        {
        }

        void IViewEvents.OnSelectedModeChanged()
        {
            UpdateView(initialUpdate: false);
        }

        void IViewEvents.OnBrowseFilesButtonClicked()
        {
            char[] wildcardsChars = { '*', '?' };

            var concretePatterns = new StringBuilder();
            var wildcardsPatterns = new StringBuilder();

            foreach (string s in factory.SupportedPatterns)
            {
                StringBuilder buf = null;
                if (s.IndexOfAny(wildcardsChars) >= 0)
                {
                    if (s != "*.*" && s != "*")
                        buf = wildcardsPatterns;
                }
                else
                {
                    buf = concretePatterns;
                }
                if (buf != null)
                {
                    buf.AppendFormat("{0}{1}", buf.Length == 0 ? "" : "; ", s);
                }
            }

            StringBuilder filter = new StringBuilder();
            if (concretePatterns.Length > 0)
                filter.AppendFormat("{0}|{0}|", concretePatterns.ToString());

            if (wildcardsPatterns.Length > 0)
                filter.AppendFormat("{0}|{0}|", wildcardsPatterns.ToString());

            filter.Append("*.*|*.*");

            var fnames = fileDialogs.OpenFileDialog(new OpenFileDialogParams()
            {
                Filter = filter.ToString(),
                CanChooseFiles = true,
                AllowsMultipleSelection = true,
                CanChooseDirectories = false,
            });

            if (fnames != null)
            {
                view.WriteControlValue(ControlId.FileSelector, FileListUtils.MakeFileList(fnames).ToString());
            }
        }

        void IViewEvents.OnBrowseFolderButtonClicked()
        {
            var folder = fileDialogs.OpenFileDialog(new OpenFileDialogParams()
            {
                CanChooseDirectories = true,
                CanChooseFiles = false,
            });
            if (folder != null && folder.Length > 0)
                view.WriteControlValue(ControlId.FolderSelector, folder[0]);
        }

        void UpdateView(bool initialUpdate)
        {
            bool supportsRotation = (factory.Flags & LogProviderFactoryFlag.SupportsRotation) != 0;
            view.SetEnabled(ControlId.RotatedLogModeButton, supportsRotation);
            if (!supportsRotation)
                view.WriteControlValue(ControlId.IndependentLogsModeButton, true);
            view.SetEnabled(ControlId.FileSelector, (bool)view.ReadControlValue(ControlId.IndependentLogsModeButton));
            view.SetEnabled(ControlId.FolderSelector, (bool)view.ReadControlValue(ControlId.RotatedLogModeButton));
            view.SetEnabled(ControlId.PatternsSelector, (bool)view.ReadControlValue(ControlId.RotatedLogModeButton));
            if (initialUpdate && supportsRotation)
            {
                view.WriteControlValue(ControlId.PatternsSelector, string.Join(";", factory.SupportedPatterns));
            }
        }

        async Task ApplyIndependentLogsMode()
        {
            string tmp = ((string)view.ReadControlValue(ControlId.FileSelector)).Trim();
            if (tmp == "")
                return;
            view.WriteControlValue(ControlId.FileSelector, "");
            foreach (string fname in FileListUtils.ParseFileList(tmp))
            {
                try
                {
                    await model.Create(factory, factory.CreateParams(fname));
                }
                catch (Exception e)
                {
                    alerts.ShowPopup("Error",
                        string.Format("Failed to create log source for '{0}': {1}", fname, e.Message),
                        AlertFlags.Ok | AlertFlags.WarningIcon);
                    break;
                }
            }
        }

        async Task ApplyRotatedLogMode()
        {
            var folder = ((string)view.ReadControlValue(ControlId.FolderSelector)).Trim();
            if (folder == "")
                return;
            if (!System.IO.Directory.Exists(folder))
            {
                alerts.ShowPopup("Error", "Specified folder does not exist", AlertFlags.Ok | AlertFlags.WarningIcon);
                return;
            }

            view.WriteControlValue(ControlId.FolderSelector, "");

            folder = folder.TrimEnd(System.IO.Path.DirectorySeparatorChar);

            var patters = (string)view.ReadControlValue(ControlId.PatternsSelector);

            IConnectionParams connectParams = factory.CreateRotatedLogParams(folder,
                patters.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)));

            try
            {
                await model.Create(factory, connectParams);
            }
            catch (Exception e)
            {
                alerts.ShowPopup("Error", e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
            }
        }
    };
};
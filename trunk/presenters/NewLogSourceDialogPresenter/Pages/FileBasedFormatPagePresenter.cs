using System;
using System.Text;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat
{
	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);
		object PageView { get; }
		object ReadControlValue(ControlId id);
		void WriteControlValue(ControlId id, object value);
		void SetEnabled(ControlId id, bool value);
		string[] ShowFilesSelectorDialog(string filters);
		string ShowFolderSelectorDialog();
	};

	public enum ControlId
	{
		IndependentLogsModeButton,
		RotatedLogModeButton,
		FileSelector,
		FolderSelector,
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
		readonly IModel model;
		readonly IAlertPopup alerts;

		public Presenter(IView view, IFileBasedLogProviderFactory factory, IModel model, IAlertPopup alerts)
		{
			this.view = view;
			this.factory = factory;
			this.model = model;
			this.alerts = alerts;

			view.SetEventsHandler(this);
		}

		void IPagePresenter.Apply()
		{
			if ((bool)view.ReadControlValue(ControlId.IndependentLogsModeButton))
				ApplyIndependentLogsMode();
			else if ((bool)view.ReadControlValue(ControlId.RotatedLogModeButton))
				ApplyRotatedLogMode();
		}

		void IPagePresenter.Activate()
		{
			UpdateView();
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
			UpdateView();
		}

		void IViewEvents.OnBrowseFilesButtonClicked()
		{
			char[] wildcardsChars = new char[] { '*', '?' };

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

			var fnames = view.ShowFilesSelectorDialog(filter.ToString());

			if (fnames != null)
			{
				view.WriteControlValue(ControlId.FileSelector, FileListUtils.MakeFileList(fnames).ToString());
			}
		}

		void IViewEvents.OnBrowseFolderButtonClicked()
		{
			var folder = view.ShowFolderSelectorDialog();
			if (folder != null)
				view.WriteControlValue(ControlId.FolderSelector, folder);
		}

		void UpdateView()
		{
			bool supportsRotation = (factory.Flags & LogProviderFactoryFlag.SupportsRotation) != 0;
			view.SetEnabled(ControlId.RotatedLogModeButton, supportsRotation);
			if (!supportsRotation)
				view.WriteControlValue(ControlId.IndependentLogsModeButton, true);
			view.SetEnabled(ControlId.FileSelector, (bool)view.ReadControlValue(ControlId.IndependentLogsModeButton));
			view.SetEnabled(ControlId.FolderSelector, (bool)view.ReadControlValue(ControlId.RotatedLogModeButton));
		}

		void ApplyIndependentLogsMode()
		{
			string tmp = ((string)view.ReadControlValue(ControlId.FileSelector)).Trim();
			if (tmp == "")
				return;
			view.WriteControlValue(ControlId.FileSelector, "");
			foreach (string fname in FileListUtils.ParseFileList(tmp))
			{
				try
				{
					model.CreateLogSource(factory, factory.CreateParams(fname));
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

		void ApplyRotatedLogMode()
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

			folder = folder.TrimEnd('\\');

			IConnectionParams connectParams = factory.CreateRotatedLogParams(folder);

			try
			{
				model.CreateLogSource(factory, connectParams);
			}
			catch (Exception e)
			{
				alerts.ShowPopup("Error", e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
			}
		}
	};
};
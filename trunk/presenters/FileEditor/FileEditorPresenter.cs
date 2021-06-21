using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace LogJoint.UI.Presenters.FileEditor
{
	public class Presenter : IPresenter, IViewModel
	{
		IView view;
		readonly IChangeNotification changeNotification;
		readonly ITempFilesManager tempFiles;

		bool visible = false;
		bool readOnly = true;
		string fileName;
		string caption;
		string contents = "";

		public Presenter(
			IChangeNotification changeNotification,
			ITempFilesManager tempFiles
		)
		{
			this.changeNotification = changeNotification;
			this.tempFiles = tempFiles;
		}


		void IPresenter.ShowDialog(string file, bool readOnly)
		{
			if (!visible)
			{
				fileName = file;
				caption = tempFiles.IsTemporaryFile(file) ? Path.GetFileName(file) : file;
				contents = File.ReadAllText(file);
				this.readOnly = readOnly;
				visible = true;
				changeNotification.Post();
			}
		}

		void IViewModel.SetView(IView view) {}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;
		bool IViewModel.IsVisible => visible;
		bool IViewModel.IsReadOnly => readOnly;
		string IViewModel.Contents => contents;
		string IViewModel.Caption => caption;
		bool IViewModel.IsSaveButtonVisible => !readOnly;
		bool IViewModel.IsDownloadButtonVisible => false; // TODO: show in web

		void IViewModel.OnClose()
		{
			if (visible)
			{
				visible = false;
				changeNotification.Post();
			}
		}

		void IViewModel.OnSave()
		{
			// TODO
		}

		void IViewModel.OnDownload()
		{
			// TODO
		}

		void IViewModel.OnChange(string value)
		{
			if (visible && !readOnly)
			{
				contents = value;
				changeNotification.Post();
			}
		}
	};
};
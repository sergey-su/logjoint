using System;
using LogJoint.UI.Presenters.MainForm;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FormatDetection
{
	public interface IView
	{
		object PageView { get; }
		string InputValue { get; set; }
	};

	public class Presenter : IPagePresenter
	{
		readonly IView view;
		readonly ICommandLineHandler commandLineHandler;

		public Presenter(IView view, ICommandLineHandler commandLineHandler)
		{
			this.view = view;
			this.commandLineHandler = commandLineHandler;
		}

		void IPagePresenter.Apply()
		{
			string tmp = view.InputValue.Trim();
			if (tmp == "")
				return;
			view.InputValue = "";

			foreach (string fnameOrUrl in FileListUtils.ParseFileList(tmp))
			{
				commandLineHandler.HandleCommandLineArgs(new[] { fnameOrUrl });
			}

		}

		void IPagePresenter.Activate()
		{
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
	};
};
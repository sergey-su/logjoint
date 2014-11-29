using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint.UI.Presenters.LogViewer;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public class PresentationModel : LogViewer.IModel
	{
		IModel model;
		LazyUpdateFlag pendingUpdateFlag;

		public PresentationModel(IModel model, LazyUpdateFlag pendingUpdateFlag)
		{
			this.model = model;
			this.pendingUpdateFlag = pendingUpdateFlag;
			this.model.OnMessagesChanged += delegate(object sender, MessagesChangedEventArgs e)
			{
				if (OnMessagesChanged != null)
					OnMessagesChanged(sender, e);
			};
		}

		public IMessagesCollection Messages
		{
			get { return model.LoadedMessages; }
		}

		public IModelThreads Threads
		{
			get { return model.Threads; }
		}

		public IFiltersList DisplayFilters
		{
			get { return model.DisplayFilters; }
		}

		public IFiltersList HighlightFilters
		{
			get { return model.HighlightFilters; }
		}

		public IBookmarks Bookmarks
		{
			get { return model.Bookmarks; }
		}

		public LJTraceSource Tracer
		{
			get { return model.Tracer; }
		}

		public string MessageToDisplayWhenMessagesCollectionIsEmpty
		{
			get
			{
				if (model.SourcesManager.Items.Any(s => s.Visible))
					return null;
				return "No log sources open. To add new log source:\n  - Press Add... button on Log Sources tab\n  - or drag&&drop (possibly zipped) log file from Windows Explorer\n  - or drag&&drop URL from a browser to download (possibly zipped) log file";
			}
		}

		public void ShiftUp()
		{
			model.SourcesManager.ShiftUp();
		}

		public bool IsShiftableUp
		{
			get { return model.SourcesManager.IsShiftableUp; }
		}

		public void ShiftDown()
		{
			model.SourcesManager.ShiftDown();
		}

		public bool IsShiftableDown
		{
			get { return model.SourcesManager.IsShiftableDown; }
		}

		public void ShiftAt(DateTime t)
		{
			model.SourcesManager.ShiftAt(t);
		}

		public void ShiftHome()
		{
			model.SourcesManager.ShiftHome();
		}

		public void ShiftToEnd()
		{
			model.SourcesManager.ShiftToEnd();
		}

		public bool GetAndResetPendingUpdateFlag()
		{
			return pendingUpdateFlag.Validate();
		}

		public Settings.IGlobalSettingsAccessor GlobalSettings
		{
			get { return model.GlobalSettings; }
		}

		public event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
	};
};
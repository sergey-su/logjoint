using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class LogsPreprocessorCredentialsCache : Preprocessing.ICredentialsCache
	{
		readonly Persistence.IStorageEntry credentialsCacheStorage;
		readonly object credentialCacheLock = new object();
		readonly IInvokeSynchronization uiInvokeSynchronization;
		readonly Form appWindow;
		NetworkCredentialsStorage credentialCache = null;

		public LogsPreprocessorCredentialsCache(IInvokeSynchronization uiInvokeSynchronization, Persistence.IStorageEntry credentialsCacheStorage, Form appWindow)
		{
			this.credentialsCacheStorage = credentialsCacheStorage;
			this.uiInvokeSynchronization = uiInvokeSynchronization;
			this.appWindow = appWindow;
		}

		NetworkCredential Preprocessing.ICredentialsCache.QueryCredentials(Uri uri, string authType)
		{
			lock (credentialCacheLock)
			{
				if (credentialCache == null)
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage);
				var cred = credentialCache.GetCredential(uri);
				if (cred != null)
					return cred;
				var ret = uiInvokeSynchronization.Invoke<NetworkCredential>(() =>
					CredUIUtils.ShowCredentialsDialog(appWindow.Handle,
						NetworkCredentialsStorage.GetRelevantPart(uri).ToString(), 
						authType == "protected-archive")).Result;
				if (ret == null)
					return null;
				credentialCache.Add(uri, ret);
				credentialCache.StoreSecurely();
				return ret;
			}
		}

		void Preprocessing.ICredentialsCache.InvalidateCredentialsCache(Uri site, string authType)
		{
			lock (credentialCacheLock)
			{
				if (credentialCache == null)
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage);
				if (credentialCache.Remove(site))
					credentialCache.StoreSecurely();
			}
		}

	};

	class LogsPreprocessorUI : Preprocessing.IPreprocessingUserRequests
	{
		readonly Form appWindow;
		Presenters.StatusReports.IPresenter statusReports;

		public LogsPreprocessorUI(Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings,
			Form appWindow, Presenters.StatusReports.IPresenter statusReports)
		{
			this.appWindow = appWindow;
			this.statusReports = statusReports;
			logSourcesPreprocessings.SetUserRequestsHandler(this);
		}

		bool[] Preprocessing.IPreprocessingUserRequests.SelectItems(string prompt, string[] items)
		{
			appWindow.BringToFront();
			using (var dlg = new FilesSelectionDialog())
				return dlg.Execute(prompt, items);
		}

		void Preprocessing.IPreprocessingUserRequests.NotifyUserAboutIneffectivePreprocessing(string notificationSource)
		{
			statusReports.CreateNewStatusReport().ShowStatusPopup(
				notificationSource ?? "Log preprocessor",
				"No log of known format is detected",
				true);
		}

		void Preprocessing.IPreprocessingUserRequests.NotifyUserAboutPreprocessingFailure(string notificationSource, string message)
		{
			statusReports.CreateNewStatusReport().ShowStatusPopup(
				notificationSource ?? "Log preprocessor",
				message,
				true);
		}
	}
}

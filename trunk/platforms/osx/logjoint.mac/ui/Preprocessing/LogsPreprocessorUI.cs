using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using AppKit;

namespace LogJoint.UI
{
	class PreprocessingCredentialsCache: Preprocessing.ICredentialsCache
	{
		readonly Persistence.IStorageEntry credentialsCacheStorage;
		readonly object credentialCacheLock = new object();
		readonly IInvokeSynchronization uiInvoke;
		NetworkCredentialsStorage credentialCache = null;
		NSWindow parentWindow;

		public PreprocessingCredentialsCache(
			NSWindow parentWindow,
			Persistence.IStorageEntry credentialsCacheStorage,
			IInvokeSynchronization uiInvoke)
		{
			this.credentialsCacheStorage = credentialsCacheStorage;
			this.parentWindow = parentWindow;
			this.uiInvoke = uiInvoke;
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
				var ret = uiInvoke.Invoke<NetworkCredential>(() =>
					NetworkCredentialsDialogController.ShowSheet(parentWindow,
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
		Presenters.StatusReports.IPresenter statusReports;

		public LogsPreprocessorUI(
			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings, 
			Presenters.StatusReports.IPresenter statusReports)
		{
			this.statusReports = statusReports;
			logSourcesPreprocessings.SetUserRequestsHandler(this);
		}
			

		bool[] Preprocessing.IPreprocessingUserRequests.SelectItems(string prompt, string[] items)
		{
			return FilesSelectionDialogController.Execute(prompt, items);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class LogsPreprocessorUI : Preprocessing.IPreprocessingUserRequests
	{
		readonly Form appWindow;
		readonly Persistence.IStorageEntry credentialsCacheStorage;
		readonly object credentialCacheLock = new object();
		NetworkCredentialsStorage credentialCache = null;

		public LogsPreprocessorUI(Form appWindow, Persistence.IStorageEntry credentialsCacheStorage)
		{
			this.appWindow = appWindow;
			this.credentialsCacheStorage = credentialsCacheStorage;
		}

		NetworkCredential Preprocessing.IPreprocessingUserRequests.QueryCredentials(Uri uri, string authType)
		{
			lock (credentialCacheLock)
			{
				if (credentialCache == null)
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage);
				var cred = credentialCache.GetCredential(uri);
				if (cred != null)
					return cred;
				using (var dlg = new CredentialsDialog())
				{
					var ret = CredUIUtils.ShowCredentialsDialog(appWindow.Handle,
						NetworkCredentialsStorage.StripToPrefix(uri).ToString());
					if (ret == null)
						return null;
					//if (!dlg.Execute(NetworkCredentialsStorage.StripToPrefix(uri).ToString()))
					//	return null;
					//var ret = new System.Net.NetworkCredential(dlg.UserName, dlg.Password);
					credentialCache.Add(uri, ret);
					credentialCache.StoreSecurely();
					return ret;
				}
			}
		}

		void Preprocessing.IPreprocessingUserRequests.InvalidateCredentialsCache(Uri site, string authType)
		{
			lock (credentialCacheLock)
			{
				if (credentialCache == null)
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage);
				if (credentialCache.Remove(site))
					credentialCache.StoreSecurely();
			}
		}

		bool[] Preprocessing.IPreprocessingUserRequests.SelectItems(string prompt, string[] items)
		{
			appWindow.BringToFront();
			using (var dlg = new FilesSelectionDialog())
				return dlg.Execute(prompt, items);
		}
	}
}

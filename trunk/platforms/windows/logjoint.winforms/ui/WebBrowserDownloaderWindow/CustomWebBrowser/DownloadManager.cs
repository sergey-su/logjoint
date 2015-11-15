using LogJoint.UI.Presenters.WebBrowserDownloader;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace LogJoint.Skype.WebBrowserDownloader
{
	class DownloadManager : IDownloadManager
	{
		private readonly IViewEvents owner;

		public DownloadManager(IViewEvents owner)
		{
			this.owner = owner;
		}

		IntPtr IDownloadManager.Download(IMoniker pmk, IBindCtx pbc, uint dwBindVerb, int grfBINDF, IntPtr pBindInfo, string pszHeaders, string pszRedir, uint uiCP)
		{
			// write some status
			string monikerName;
			pmk.GetDisplayName(pbc, null, out monikerName);

			Uri monikerUri = new Uri(monikerName);

			RegisterCallback(pbc, monikerUri);
			BindMonikerToStream(pmk, pbc);

			return HResults.S_OK;
		}

		private void BindMonikerToStream(IMoniker pmk, IBindCtx pbc)
		{
			Guid iid = Guids.IID_IStream;
			object pStream = null;
			try
			{
				pmk.BindToStorage(pbc, null, ref iid, out pStream);

			}
			catch (COMException)
			{
				//s_log.InfoFormat ("COM Exception occured in DownloadManagerImplementation.BindMonikerToStream: Message: {}", ex.Message);
			}
			if (pStream != null)
				Marshal.ReleaseComObject(pStream); // don't need the stream, we get asynchronous callbacks
		}

		private void RegisterCallback(IBindCtx pbc, Uri monikerUri)
		{
			var callback = new BindStatusCallback(monikerUri, owner);
			IBindStatusCallback previous;
			IntPtr result = RegisterBindStatusCallback(pbc, callback, out previous, 0);

			// The call to RegisterBindStatusCallback will fail if the default calback "_BSCB_Holder_" is registered.
			// Remove it and try again. (This trick has been taken from 
			// <http://www.codeproject.com/KB/atl/vbmhwb.aspx?fid=180355&df=90&mpp=25&noise=3&sort=Position&view=Quick&select=2211230>.)
			if (HResults.Equals(result, HResults.E_FAIL) && previous != null)
			{
				pbc.RevokeObjectParam("_BSCB_Holder_");
				callback.PreviousCallback = previous;
				result = RegisterBindStatusCallback(pbc, callback, out previous, 0);
			}

			if (!HResults.Equals(result, HResults.S_OK))
				throw new InvalidOperationException("Could not register custom bind status callback.");
		}

		[DllImport("urlmon.dll")]
		[PreserveSig]
		static extern IntPtr RegisterBindStatusCallback(IBindCtx pbc, IBindStatusCallback pbsc, out IBindStatusCallback ppbscPrevious, uint reserved);
	}
}

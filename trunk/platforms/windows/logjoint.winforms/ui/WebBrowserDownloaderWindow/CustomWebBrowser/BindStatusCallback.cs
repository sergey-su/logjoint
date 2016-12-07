using LogJoint.UI.Presenters.WebBrowserDownloader;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace LogJoint.Skype.WebBrowserDownloader
{
	class BindStatusCallback : IBindStatusCallback
	{
		const int bufferSize = 1024 * 1024;
		readonly Uri uri;
		readonly IViewEvents viewEvents;
		IBinding binding;
		IBindStatusCallback previousCallback;

		public BindStatusCallback(Uri uri, IViewEvents viewEvents)
		{
			this.uri = uri;
			this.viewEvents = viewEvents;
		}

		public IBindStatusCallback PreviousCallback
		{
			get { return previousCallback; }
			set { previousCallback = value; }
		}

		IntPtr IBindStatusCallback.OnStartBinding(uint dwReserved, IBinding pib)
		{
			// If we have a "_BSCB_Holder_" callback that was previously registered (ie. IE's download manager), we'll tell it to stop binding to this
			// download.
			if (previousCallback != null)
				previousCallback.OnStopBinding(new IntPtr(200), null);

			bool shouldContinue = viewEvents.OnStartDownload(uri);
			if (!shouldContinue)
				return HResults.E_FAIL;

			binding = pib;
			return HResults.S_OK;
		}

		IntPtr IBindStatusCallback.GetPriority(out int pnPriority)
		{
			pnPriority = 0;
			return HResults.E_NOTIMPL;
		}

		IntPtr IBindStatusCallback.OnLowResource(uint reserved)
		{
			return HResults.E_NOTIMPL;
		}

		IntPtr IBindStatusCallback.OnProgress(uint ulProgress, uint ulProgressMax, uint ulStatusCode, string szStatusText)
		{
			bool shouldContinue = viewEvents.OnProgress((int)ulProgress, (int)ulProgressMax, szStatusText);
			if (!shouldContinue)
				Abort();

			return HResults.S_OK;
		}

		IntPtr IBindStatusCallback.OnStopBinding(IntPtr hresult, string szError)
		{
			// Give a callback async way to avoid failures connected to the fact that 
			// while being in the method the browser may be still "busy" and therefore not ready for next navigation.
			PosrCompletionCallback(HResults.Equals(hresult, HResults.S_OK), szError);
			return HResults.S_OK;
		}

		IntPtr IBindStatusCallback.GetBindInfo(out uint grfBINDF, ref IntPtr pbindinfo)
		{
			grfBINDF = 0; // TODO: Here, we could return flags to control how the file is downloaded
			return HResults.S_OK;
		}

		IntPtr IBindStatusCallback.OnDataAvailable(uint grfBSCF, uint dwSize, ref FORMATETC pformatetc, ref STGMEDIUM pstgmed)
		{
			if (pstgmed.tymed != TYMED.TYMED_ISTREAM)
				throw new InvalidOperationException("This callback handler only supports IStreams.");

			// assume an IStream has been requested in BindMonikerToObject
			IStream stream = (IStream)Marshal.GetObjectForIUnknown(pstgmed.unionmember);

			IntPtr hresult;
			bool shouldContinue = true;
			do
			{
				byte[] buffer = new byte[bufferSize];
				IntPtr pBytesRead = Marshal.AllocHGlobal(sizeof(uint));
				hresult = stream.Read(buffer, buffer.Length, pBytesRead);
				uint bytesRead = (uint)Marshal.PtrToStructure(pBytesRead, typeof(uint));
				Marshal.FreeHGlobal(pBytesRead);

				if (bytesRead > 0)
					shouldContinue = viewEvents.OnDataAvailable(buffer, (int)bytesRead);

			} while (shouldContinue && HResults.Succeeded(hresult) && !HResults.Equals(hresult, HResults.S_FALSE));

			Marshal.ReleaseComObject(stream);

			if (!shouldContinue)
				Abort();

			if (!HResults.Succeeded(hresult))
				return hresult;

			return HResults.S_OK;
		}

		IntPtr IBindStatusCallback.OnObjectAvailable(ref Guid riid, object punk)
		{
			return HResults.E_NOTIMPL;
		}

		void Abort()
		{
			binding.Abort();
			Marshal.ReleaseComObject(binding);
			viewEvents.OnAborted();
			binding = null;
		}

		async void PosrCompletionCallback(bool success, string error)
		{
			await Task.Yield();
			viewEvents.OnDownloadCompleted(success, error);
		}
	}
}

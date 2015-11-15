using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Skype.WebBrowserDownloader
{
	[ComImport]
	[Guid("79eac9c0-baf9-11ce-8c82-00aa004ba90b")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IBinding
	{
		[PreserveSig]
		IntPtr Abort();
		void Suspend();
		void Resume();
		void SetPriority(int nPriority);
		void GetPriority(out int pnPriority);
		void GetBindResult(out Guid pclsidProtocol, out uint pdwResult, [MarshalAs(UnmanagedType.LPWStr)] out string pszResult, ref uint pdwReserved);
	}

	[ComImport]
	[Guid("79eac9c1-baf9-11ce-8c82-00aa004ba90b")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IBindStatusCallback
	{
		[PreserveSig]
		IntPtr OnStartBinding(uint dwReserved, IBinding pib);

		[PreserveSig]
		IntPtr GetPriority(out int pnPriority);

		[PreserveSig]
		IntPtr OnLowResource(uint reserved);

		[PreserveSig]
		IntPtr OnProgress(uint ulProgress, uint ulProgressMax, uint ulStatusCode, [MarshalAs(UnmanagedType.LPWStr)] string szStatusText);

		[PreserveSig]
		IntPtr OnStopBinding(IntPtr hresult, [MarshalAs(UnmanagedType.LPWStr)] string szError);

		[PreserveSig]
		IntPtr GetBindInfo(out uint grfBINDF, ref IntPtr pbindinfo);

		[PreserveSig]
		IntPtr OnDataAvailable(uint grfBSCF, uint dwSize, ref FORMATETC pformatetc, ref STGMEDIUM pstgmed);

		[PreserveSig]
		IntPtr OnObjectAvailable(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] object punk);
	}

	[ComImport]
	[Guid("988934A4-064B-11D3-BB80-00104B35E7F9")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IDownloadManager
	{
		[PreserveSig]
		IntPtr Download(IMoniker pmk, IBindCtx pbc, uint dwBindVerb, int grfBINDF, IntPtr pBindInfo, [MarshalAs(UnmanagedType.LPWStr)] string pszHeaders,
		   [MarshalAs(UnmanagedType.LPWStr)] string pszRedir, uint uiCP);

	}

	[ComImport]
	[Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IServiceProvider
	{
		[PreserveSig]
		IntPtr QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject);
	}

	[ComImport]
	[Guid("0000000c-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IStream
	{
		[PreserveSig]
		IntPtr Read(
			[Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pv,
			int cb,
			IntPtr pcbRead);

		void Write([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pv, int cb, IntPtr pcbWritten);
		void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition);
		void SetSize(long libNewSize);
		void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten);
		void Commit(int grfCommitFlags);
		void Revert();
		void LockRegion(long libOffset, long cb, int dwLockType);
		void UnlockRegion(long libOffset, long cb, int dwLockType);
		void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag);
		void Clone(out IStream ppstm);
	}

	static class Guids
	{
		public static Guid SID_SDownloadManager = new Guid("988934a4-064b-11d3-bb80-00104b35e7f9");
		public static Guid IID_IDownloadManager = new Guid("988934A4-064B-11D3-BB80-00104B35E7F9");
		public static Guid IID_IStream = new Guid("0000000c-0000-0000-C000-000000000046");
	}

	public static class HResults
	{
		public static IntPtr S_OK = MakeIntPtr(0);
		public static IntPtr S_FALSE = MakeIntPtr(1);
		public static IntPtr E_NOINTERFACE = MakeIntPtr(0x80004002L);
		public static IntPtr E_FAIL = MakeIntPtr(0x80004005L);
		public static IntPtr E_NOTIMPL = MakeIntPtr(0x80004001L);
		public static IntPtr E_PENDING = MakeIntPtr(0x8000000AL);

		public static bool Succeeded(IntPtr hresult)
		{
			return unchecked((int)hresult.ToInt64()) >= 0;
		}

		public static bool Equals(IntPtr one, IntPtr two)
		{
			return unchecked((int)one.ToInt64()) == unchecked((int)two.ToInt64());
		}

		public static IntPtr MakeIntPtr(long hresult)
		{
			if (IntPtr.Size == 4)
				return new IntPtr(unchecked((int)hresult));
			else
				return new IntPtr(hresult);
		}
	}
}

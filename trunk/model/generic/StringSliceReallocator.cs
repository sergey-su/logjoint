using System;
using System.Runtime.InteropServices;

namespace LogJoint
{
	public interface IStringSliceReallocator : IDisposable
	{
		StringSlice Reallocate(StringSlice value);
	};

	unsafe public class StringSliceReallocator : IStringSliceReallocator, IDisposable
	{
		char* bufPtr;
		string buf;
		int bufPosition;
		GCHandle bufHandle;

		public StringSliceReallocator()
		{
			AllocateNewBuffer();
		}

		void IDisposable.Dispose()
		{
			FreeBuffer();
		}

#if MONO
		unsafe static char* memmove(char* dest, char* src, UIntPtr count)
		{
			Buffer.MemoryCopy((void*)src, (void*)dest, count, count);
			return dest;
		}
#else
		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern char* memmove(char* dest, char* src, UIntPtr count);
#endif

		StringSlice IStringSliceReallocator.Reallocate(StringSlice value)
		{
			if (value.Length == 0)
			{
				return value;
			}
			if (bufPosition + value.Length > buf.Length)
			{
				FreeBuffer();
				AllocateNewBuffer();
			}
			var ret = new StringSlice(buf, bufPosition, value.Length);
			fixed (char* src = value.Buffer)
			{
				memmove(bufPtr + bufPosition, src + value.StartIndex, new UIntPtr((uint)value.Length * sizeof(char)));
				bufPosition += value.Length;
			}
			return ret;
		}

		void AllocateNewBuffer()
		{
			buf = new string('\0', 64 * 1024);
			bufHandle = GCHandle.Alloc(buf, GCHandleType.Pinned);
			bufPtr = (char*)bufHandle.AddrOfPinnedObject();
			bufPosition = 0;
		}

		void FreeBuffer()
		{
			bufHandle.Free();
		}
	};
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public static class Hashing
	{
		/// <summary>
		/// Return subsrting's hash code that doesn't depend on .Net version 
		/// </summary>
		public static int GetStableHashCode(string str, int index, int length)
		{
			unsafe
			{
				fixed (char* src = str)
				{
					int hash1 = 5381;
					int hash2 = hash1;
					int c;
					char* s = src + index;
					int len = length;
					for (; ; )
					{
						if (len == 0)
							break;
						c = s[0];
						hash1 = ((hash1 << 5) + hash1) ^ c;
						--len;

						if (len == 0)
							break;
						c = s[1];
						hash2 = ((hash2 << 5) + hash2) ^ c;
						--len;

						s += 2;
					}
					return hash1 + (hash2 * 1566083941);
				}
			}
		}

		/// <summary>
		/// Return srting's hash code that doesn't depend on .Net version 
		/// </summary>
		public static int GetStableHashCode(string str)
		{
			return GetStableHashCode(str, 0, str.Length);
		}

		public static int GetStableHashCode(long value)
		{
			return (int)value ^ (int)(value >> 32);
		}

		public static int GetStableHashCode(DateTime value)
		{
			return GetStableHashCode(value.Ticks);
		}
	}
}

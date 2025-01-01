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

        public static int GetStableHashCode(byte[] value)
        {
            return GetStableHashCode(value, 0, value.Length);
        }

        public static int GetStableHashCode(byte[] value, int offset, int len)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = offset; i < len; i++)
                    hash = (hash ^ value[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        public static int GetHashCode(IEnumerable<int> hashes, int seed = 4663)
        {
            int hash = seed;
            foreach (var h in hashes)
                hash = ((hash << 5) + hash) ^ h;
            return hash;
        }

        public static int GetHashCode(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }

        public static int GetHashCode(int h)
        {
            return GetHashCode(4663, h);
        }

        public static Int16 GetShortHashCode(int val)
        {
            return unchecked((Int16)((UInt16)(val & 0xffff) ^ (UInt16)(val >> 16)));
        }
    }
}

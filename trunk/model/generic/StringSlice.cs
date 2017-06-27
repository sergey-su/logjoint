using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public struct StringSlice: IEnumerable<char>, IEquatable<StringSlice>
	{
		public static readonly StringSlice Empty = new StringSlice("", 0, 0);

		public StringSlice(string str, int index, int length)
		{
			this.str = str;
			this.index = index;
			this.length = length;
			this.cachedValue = null;

			ValidateInDebug();
		}
		public StringSlice(string str)
		{
			this.str = str;
			this.index = 0;
			this.length = str.Length;
			this.cachedValue = str;
		}
		public StringSlice(StringSlice str)
		{
			this.str = str.str;
			this.index = str.index;
			this.length = str.length;
			this.cachedValue = str.cachedValue;
		}
		public StringSlice(string buffer, Group group)
		{
			this.str = buffer;
			this.index = group.Index;
			this.length = group.Length;
			this.cachedValue = null;
		}

		public static implicit operator string(StringSlice stringSlice)
		{
			return stringSlice.Value;
		}

		public string Buffer
		{
			get { return str; }
		}

		public int StartIndex
		{
			get { return index; }
		}

		public int EndIndex
		{
			get { return index + length; }
		}

		public int Length
		{
			get { return length; }
		}

		public int GetStableHashCode()
		{
			return Hashing.GetStableHashCode(str, index, length);
		}

		public override int GetHashCode()
		{
#if !SILVERLIGHT
			return Hashing.GetStableHashCode(str, index, length);
#else
			return Value.GetHashCode();
#endif
		}

		public bool IsEmpty
		{
			get { return length == 0; }
		}

		public bool IsInitialized
		{
			get { return str != null; }
		}

		public char this[int idx]
		{
			get { return str[index + idx]; }
		}

		public string Value
		{
			get
			{
				if (cachedValue == null)
					cachedValue = str.Substring(index, length);
				return cachedValue;
			}
		}

		public bool ValusIsCached
		{
			get { return cachedValue != null; }
		}

		public bool IsWhiteSpace(int idx)
		{
			return char.IsWhiteSpace(str, index + idx);
		}

		public override string ToString()
		{
			return Value;
		}

		public StringSlice SubString(int index, int length)
		{
			return new StringSlice(str, this.index + index, length);
		}

		public StringSlice SubString(int index)
		{
			return SubString(index, Length - index);
		}

		public StringSlice Slice(int beginIndex, int endIndex)
		{
			return new StringSlice(str, this.index + beginIndex, endIndex - beginIndex);
		}

		public bool StartsWith(string value)
		{
			if (this.Length >= value.Length)
			{
				if (StringSlice.Compare(this.SubString(0, value.Length), new StringSlice(value)) == 0)
					return true;
			}
			return false;
		}

		public StringSlice Trim()
		{
			int begin = index;
			int end = index + length;
			while (begin < end && char.IsWhiteSpace(str, begin))
				++begin;
			while (end > begin && char.IsWhiteSpace(str, end - 1))
				--end;
			return new StringSlice(str, begin, end - begin);
		}

		public int IndexOf(string value, int startIndex, StringComparison cmp)
		{
			return str.IndexOf(value, this.index + startIndex, length - startIndex, cmp) - this.index;
		}

		public int LastIndexOf(string value, int startIndex, StringComparison cmp)
		{
			return str.LastIndexOf(value, this.index + startIndex, startIndex + 1, cmp) - this.index;
		}

		public int LastIndexOfAny(char[] chars)
		{
			return str.LastIndexOfAny(chars, this.index + this.length - 1, this.length) - this.index;
		}

		public int IndexOfAny(char[] chars)
		{
			return str.IndexOfAny(chars, this.index, this.length) - this.index;
		}

		public StringBuilder Append(StringBuilder sb)
		{
			sb.Append(str, index, length);
			return sb;
		}

		public IEnumerator<char> GetEnumerator()
		{
			int begin = index;
			int end = index + length;
			for (; begin != end; ++begin)
				yield return str[begin];
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			int begin = index;
			int end = index + length;
			for (; begin != end; ++begin)
				yield return str[begin];
		}

		public bool Equals(StringSlice other)
		{
			return Compare(this, other) == 0;
		}

		public override bool Equals(object other)
		{
			if (!(other is StringSlice))
				return false;
			return Equals((StringSlice)other);
		}

		public static int Compare(StringSlice s1, StringSlice s2)
		{
			int ret = Math.Sign(s1.length - s2.length);
			if (ret != 0)
				return ret;
			return string.Compare(s1.str, s1.index, s2.str, s2.index, s1.length);
		}

		public static StringSlice Concat(StringSlice s1, StringSlice s2)
		{
			// Optimization when contacting adjacent slices
			if (object.ReferenceEquals(s1.Buffer, s2.Buffer)
			 && (s1.StartIndex + s1.Length == s2.StartIndex))
			{
				return new StringSlice(s1.Buffer, s1.StartIndex, s1.Length + s2.Length);
			}

			StringBuilder tmp = new StringBuilder(s1.Length + s2.Length);
			tmp.Append(s1.Buffer, s1.StartIndex, s1.Length);
			tmp.Append(s2.Buffer, s2.StartIndex, s2.Length);
			return new StringSlice(tmp.ToString());
		}

		public static StringSlice Concat(StringSlice s1, StringSlice s2, StringSlice s3)
		{
			// Optimization when contacting adjacent slices
			if (object.ReferenceEquals(s1.Buffer, s2.Buffer)
			 && object.ReferenceEquals(s1.Buffer, s3.Buffer)
			 && (s1.StartIndex + s1.Length == s2.StartIndex)
			 && (s2.StartIndex + s2.Length == s3.StartIndex))
			{
				return new StringSlice(s1.Buffer, s1.StartIndex, s1.Length + s2.Length + s3.Length);
			}

			StringBuilder tmp = new StringBuilder(s1.Length + s2.Length + s3.Length);
			tmp.Append(s1.Buffer, s1.StartIndex, s1.Length);
			tmp.Append(s2.Buffer, s2.StartIndex, s2.Length);
			tmp.Append(s3.Buffer, s3.StartIndex, s3.Length);
			return new StringSlice(tmp.ToString());
		}

		public static bool operator != (StringSlice stringSlice1, StringSlice stringSlice2)
		{
			return Compare(stringSlice1, stringSlice2) != 0;
		}

		public static bool operator ==(StringSlice stringSlice1, StringSlice stringSlice2)
		{
			return Compare(stringSlice1, stringSlice2) == 0;
		}

		public static bool operator !=(StringSlice stringSlice, string str)
		{
			return Compare(stringSlice, new StringSlice(str)) != 0;
		}

		public static bool operator ==(StringSlice stringSlice, string str)
		{
			return Compare(stringSlice, new StringSlice(str)) == 0;
		}

		public static bool operator !=(string str, StringSlice stringSlice)
		{
			return Compare(stringSlice, new StringSlice(str)) != 0;
		}

		public static bool operator ==(string str, StringSlice stringSlice)
		{
			return Compare(stringSlice, new StringSlice(str)) == 0;
		}

		public static StringSlice operator +(StringSlice stringSlice1, StringSlice stringSlice2)
		{
			return Concat(stringSlice1, stringSlice2);
		}

		public static StringSlice operator +(StringSlice stringSlice, string str)
		{
			return Concat(stringSlice, new StringSlice(str));
		}

		public static StringSlice operator +(string str, StringSlice stringSlice)
		{
			return Concat(new StringSlice(str), stringSlice);
		}

		#region Implementation

		internal void Validate()
		{
			if (str == null)
				throw new ArgumentNullException("str");
			if (index < 0 || index > str.Length)
				throw new IndexOutOfRangeException();
			int end = index + length;
			if (end < 0 || end > str.Length)
				throw new ArgumentException("length");
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void ValidateInDebug()
		{
			Validate();
		}

		#endregion


		#region Fields

		readonly string str;
		readonly int index;
		readonly int length;
		
		string cachedValue;


		#endregion
	}
}

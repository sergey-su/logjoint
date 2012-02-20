using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
			if (length != other.length)
				return false;
			return string.Compare(str, index, other.str, other.index, length) == 0;
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

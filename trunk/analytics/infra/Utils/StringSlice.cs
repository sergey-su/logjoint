using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.Analytics
{
	// todo: consider reusing lj.model's one
	public struct StringSlice
	{
		string buffer;
		int index;
		int length;
		string value;

		public static StringSlice Empty = new StringSlice("");

		public StringSlice(string buffer, int index, int length)
		{
			this.buffer = buffer;
			this.index = index;
			this.length = length;
			this.value = null;
		}

		public StringSlice(string buffer)
		{
			this.buffer = buffer;
			this.index = 0;
			this.length = buffer.Length;
			this.value = buffer;
		}

		public StringSlice(string buffer, Group group)
		{
			this.buffer = buffer;
			this.index = group.Index;
			this.length = group.Length;
			this.value = null;
		}

		public string Value
		{
			get
			{
				if (value == null)
					value = buffer.Substring(index, length);
				return value;
			}
		}

		public int Length { get { return length; } }

		public int Index { get { return index; } }


		public override string ToString()
		{
			return this.Value;
		}

		public static int Compare(StringSlice s1, StringSlice s2)
		{
			int i = Math.Sign(s1.length - s2.length);
			if (i != 0)
				return i;
			return string.Compare(s1.buffer, s1.index, s2.buffer, s2.index, s1.length);
		}

		public StringSlice Unique()
		{
			if (buffer == null)
				return StringSlice.Empty;
			return new StringSlice(this.Value);
		}
	}
}

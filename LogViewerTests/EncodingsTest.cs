using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace LogJointTests
{
	[TestClass]
	public class EncodingsTest
	{
		[TestMethod]
		public void EncodingTest()
		{
			// utf8 bytes
			byte[] bytes = { 
				0x20, 0xe2, 0x98, 0xa3, 0x20, 0x20, 0x20, 0x20,
				0xe2, 0x98, 0xa3 // biohazard symbol
			};
			char[] chars = new char[10];
			Decoder d = Encoding.UTF8.GetDecoder();

			// Decode biohazard symbol
			d.Reset();
			Debug.Assert(d.GetChars(bytes, 8, 3, chars, 0) == 1 && chars[0] == '\u2623');

			// Decoding the biohazard symbol from the midde must produce 
			// two simbols. Biohazard won't be recognized of-course.
			d.Reset();
			Debug.Assert(d.GetChars(bytes, 9, 2, chars, 0) == 2);

			d.Reset();
			// Read 9 bytes. The last bytes is the beginning of biohazard symbol
			Debug.Assert(d.GetChars(bytes, 0, 9, chars, 0) == 6);
			// Read the biohazard symbol from the rest of the buffer
			Debug.Assert(d.GetChars(bytes, 9, 2, chars, 0) == 1 && chars[0] == '\u2623');

			d.Reset();
			// Read 7 bytes starting from 4-th one. 4-th byte is the middle of the biohazard symbol.
			// This 4-th byte causes fallback and produces substitution simbol.
			Debug.Assert(d.GetChars(bytes, 3, 7, chars, 0) == 5);
			// Read the biohazard symbol from the rest of the buffer
			Debug.Assert(d.GetChars(bytes, 10, 1, chars, 0) == 1 && chars[0] == '\u2623');
		}
	}
}

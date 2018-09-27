using System;

namespace LogJoint
{
	class RollingHash
	{
		UInt32 value;
		readonly UInt32[] randomNums = new UInt32[1 << (sizeof(char) * 8)];
		readonly UInt32 n1 = 43;
		readonly UInt32 n2;

		public RollingHash(int length)
		{
			n2 = 1;
			for (int i = 0; i < length; ++i)
				n2 *= n1;

			var random = new Random();
			for (var k = 0; k < randomNums.Length; ++k)
			{
				UInt32 thirtyBits = (UInt32)random.Next(1 << 30);
				UInt32 twoBits = (UInt32)random.Next(1 << 2);
				randomNums[k] = unchecked((thirtyBits << 2) | twoBits);
			}
		}

		public UInt32 Value { get { return value; } }

		public void Reset()
		{
			value = 0;
		}

		public void Update(char @in)
		{
			value = unchecked(n1 * value + randomNums[@in]);
		}

		public void Update(char @out, char @in)
		{
			value = unchecked(n1 * value + randomNums[@in] - n2 * randomNums[@out]);
		}
	};
}

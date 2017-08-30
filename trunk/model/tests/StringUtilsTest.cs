using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class StringUtilsTest
	{
		[Test]
		public void NormalizeSingleLF()
		{
			Assert.AreEqual("\r\n11\r\n22\r\n34\r\n", StringUtils.NormalizeLinebreakes("\n11\n22\n34\n"));
		}

		[Test]
		public void NormalizeSingleCR()
		{
			Assert.AreEqual("\r\n11\r\n22\r\n34\r\n", StringUtils.NormalizeLinebreakes("\r11\r22\r34\r"));
		}

		[Test]
		public void NormalizeLFCR()
		{
			Assert.AreEqual("11\r\n\r\n22", StringUtils.NormalizeLinebreakes("11\n\r22"));
		}

		[Test]
		public void NormalizeMultipleCRsAndLFs()
		{
			Assert.AreEqual("11\r\n\r\n22\r\n\r\n33", StringUtils.NormalizeLinebreakes("11\n\n22\r\r33"));
		}
	}
}

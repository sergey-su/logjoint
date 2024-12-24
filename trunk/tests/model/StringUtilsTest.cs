using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class StringUtilsTest
	{
		[Test]
		public void NormalizeSingleLF()
		{
			Assert.That("\r\n11\r\n22\r\n34\r\n", Is.EqualTo(StringUtils.NormalizeLinebreakes("\n11\n22\n34\n")));
		}

		[Test]
		public void NormalizeSingleCR()
		{
			Assert.That("\r\n11\r\n22\r\n34\r\n", Is.EqualTo(StringUtils.NormalizeLinebreakes("\r11\r22\r34\r")));
		}

		[Test]
		public void NormalizeLFCR()
		{
			Assert.That("11\r\n\r\n22", Is.EqualTo(StringUtils.NormalizeLinebreakes("11\n\r22")));
		}

		[Test]
		public void NormalizeMultipleCRsAndLFs()
		{
			Assert.That("11\r\n\r\n22\r\n\r\n33", Is.EqualTo(StringUtils.NormalizeLinebreakes("11\n\n22\r\r33")));
		}
	}
}

using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace logjoint.model.tests
{
	[TestClass()]
	public class StringUtilsTest
	{
		[TestMethod()]
		public void NormalizeSingleLF()
		{
			Assert.AreEqual("\r\n11\r\n22\r\n34\r\n", StringUtils.NormalizeLinebreakes("\n11\n22\n34\n"));
		}

		[TestMethod()]
		public void NormalizeSingleCR()
		{
			Assert.AreEqual("\r\n11\r\n22\r\n34\r\n", StringUtils.NormalizeLinebreakes("\r11\r22\r34\r"));
		}

		[TestMethod()]
		public void NormalizeLFCR()
		{
			Assert.AreEqual("11\r\n\r\n22", StringUtils.NormalizeLinebreakes("11\n\r22"));
		}

		[TestMethod()]
		public void NormalizeMultipleCRsAndLFs()
		{
			Assert.AreEqual("11\r\n\r\n22\r\n\r\n33", StringUtils.NormalizeLinebreakes("11\n\n22\r\r33"));
		}

	}
}

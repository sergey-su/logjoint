using System.Security.Cryptography;

namespace LogJoint.Persistence
{
	public class SystemDataProtection : ICredentialsProtection
	{
		byte[] ICredentialsProtection.Protect(byte[] userData)
		{
			return ProtectedData.Protect(userData, additionalEntropy, DataProtectionScope.CurrentUser);
		}

		byte[] ICredentialsProtection.Unprotect(byte[] encryptedData)
		{
			return ProtectedData.Unprotect(encryptedData, additionalEntropy, DataProtectionScope.CurrentUser);
		}

		readonly static byte[] additionalEntropy = { 19, 22, 43, 127, 128, 63, 221 };
	};
}

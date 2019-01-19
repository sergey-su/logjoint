using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint.Analytics
{
	public static class LogRotationExtensions
	{
		public static ILogPartToken SafeDeserializeLogPartToken(this ILogPartTokenFactory rotatedLogPartFactory, XElement element)
		{
			if (rotatedLogPartFactory == null || element == null)
				return new NullLogPartToken();
			var tokenElt = element.Element(rotatedLogPartTokenEltName);
			if (tokenElt == null)
				return new NullLogPartToken();
			return rotatedLogPartFactory.Deserialize(tokenElt) ?? new NullLogPartToken();
		}

		public static XElement SafeSerializeLogPartToken(this ILogPartToken logPartToken, XElement element)
		{
			if (logPartToken == null)
				return null;
			var tokenElt = new XElement(rotatedLogPartTokenEltName);
			logPartToken.Serialize(tokenElt);
			element?.Add(tokenElt);
			return tokenElt;
		}

		const string rotatedLogPartTokenEltName = "rotatedLogPartToken";
	};

	public class PartsOfSameLogEqualityComparer : IEqualityComparer<ILogPartToken>
	{
		bool IEqualityComparer<ILogPartToken>.Equals(ILogPartToken x, ILogPartToken y)
		{
			return x.CompareTo(y) != 0;
		}

		int IEqualityComparer<ILogPartToken>.GetHashCode(ILogPartToken obj)
		{
			return 0; // all tokens will have same hash code to force slow comparision via Equals(x, y)
		}
	};

	public class NullLogPartToken : ILogPartToken
	{
		int ILogPartToken.CompareTo(ILogPartToken otherToken)
		{
			return 0;
		}

		void ILogPartToken.Serialize(XElement to)
		{
		}
	};
}

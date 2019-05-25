using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
	public static class LogRotationExtensions
	{
		public static bool TryReadLogPartToken(this ILogPartTokenFactory rotatedLogPartFactory, XElement element, out ILogPartToken token)
		{
			token = null;
			if (rotatedLogPartFactory != null && element != null && element.Name.LocalName == rotatedLogPartTokenEltName )
			{
				token = rotatedLogPartFactory.TryDeserialize(element);
			}
			return token != null;
		}

		public static void SafeWriteTo(this ILogPartToken logPartToken, XmlWriter writer)
		{
			if (logPartToken == null)
				return;
			var tokenElt = new XElement(rotatedLogPartTokenEltName);
			logPartToken.Serialize(tokenElt);
			tokenElt.WriteTo(writer);
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

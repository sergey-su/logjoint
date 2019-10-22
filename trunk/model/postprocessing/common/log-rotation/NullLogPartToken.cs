using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
	class NullLogPartToken : ILogPartToken, ILogPartTokenFactory
	{
		ILogPartTokenFactory ILogPartToken.Factory => this;

		int ILogPartToken.CompareTo(ILogPartToken otherToken) => 0;

		void ILogPartToken.Serialize(XElement to)
		{
		}

		string ILogPartTokenFactory.Id => "null-factory";
		ILogPartToken ILogPartTokenFactory.Deserialize(XElement element) => this;
	};
}

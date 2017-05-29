using System.Xml.Linq;

namespace LogJoint.Analytics
{
	public interface ILogPartToken
	{
		int CompareTo(ILogPartToken otherToken);
		void Serialize(XElement to);
	};

	public interface ILogPartTokenFactory
	{
		ILogPartToken Deserialize(XElement element);
	};
}

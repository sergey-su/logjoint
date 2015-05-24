using System.Xml.Linq;

namespace LogJoint
{
	public struct UserDefinedFactoryParams
	{
		public ILogProviderFactoryRegistry FactoryRegistry;
		public IFormatDefinitionRepositoryEntry Entry;
		public XElement RootNode;
		public XElement FormatSpecificNode;
	};
}

using System.Xml.Linq;

namespace LogJoint
{
	public struct UserDefinedFactoryParams
	{
		public ILogProviderFactoryRegistry FactoryRegistry;
		public string Location;
		public XElement RootNode;
		public XElement FormatSpecificNode;

		public FieldsProcessor.IFactory FieldsProcessorFactory;
	};
}

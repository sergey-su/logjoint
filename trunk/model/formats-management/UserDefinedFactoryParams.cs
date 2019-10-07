using System.Xml.Linq;

namespace LogJoint
{
	public struct UserDefinedFactoryParams
	{
		public ILogProviderFactoryRegistry FactoryRegistry;
		public string Location;
		public XElement RootNode;
		public XElement FormatSpecificNode;

		public ITempFilesManager TempFilesManager;
		public ITraceSourceFactory TraceSourceFactory;
		public RegularExpressions.IRegexFactory RegexFactory;
		public IFieldsProcessorFactory FieldsProcessorFactory;
	};
}

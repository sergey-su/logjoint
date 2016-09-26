using LogJoint.MRU;
using System;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface IModel
	{
		ILogSource CreateLogSource(ILogProviderFactory factory, IConnectionParams connectionParams);
		bool ContainsEnumerableLogSources { get; }
		void SaveJointAndFilteredLog(IJointLogWriter writer);
		IFiltersList HighlightFilters { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		ITempFilesManager TempFilesManager { get; }
	};

	public interface IJointLogWriter
	{
		void WriteMessage(IMessage msg);
	};

	public class InvalidFormatException : Exception
	{
		public InvalidFormatException()
			: base("Unable to parse the stream. The data seems to have incorrect format.")
		{ }
	};
}

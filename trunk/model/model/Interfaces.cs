using LogJoint.MRU;
using System;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface IModel
	{
		IFiltersList HighlightFilters { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		ITempFilesManager TempFilesManager { get; }
	};

	public class InvalidFormatException : Exception
	{
		public InvalidFormatException()
			: base("Unable to parse the stream. The data seems to have incorrect format.")
		{ }
	};
}

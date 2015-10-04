using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.AutoUpdate
{
	class ConfiguredAzureUpdateDownloader : AzureUpdateDownloader
	{
		public ConfiguredAzureUpdateDownloader(): base(LogJoint.Properties.Settings.Default.AutoUpdateUrl)
		{
		}
	}
}

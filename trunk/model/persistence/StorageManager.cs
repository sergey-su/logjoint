using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.IO.IsolatedStorage;

namespace LogJoint.Persistence
{
	public class StorageManager: IStorageManager, IDisposable
	{
		public StorageManager()
		{
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}
		public void Dispose()
		{
		}
		public IStorageEntry GetEntry(string entryKey)
		{
			return null;
		}

		//interface 
	};
}

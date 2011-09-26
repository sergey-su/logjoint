using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogJoint
{
	public class TempFilesManager: ITempFilesManager
	{
		public static TempFilesManager GetInstance(Source tracer)
		{
			if (instance == null)
				instance = new TempFilesManager(tracer);
			return instance;
		}

		private TempFilesManager(Source tracer)
		{
			using (tracer.NewFrame)
			{
				this.tracer = tracer;
				
				folder = Environment.ExpandEnvironmentVariables("%TEMP%\\LogJoint");
				tracer.Info("Temp directory: {0}", folder);
				
				if (!Directory.Exists(folder))
				{
					tracer.Info("Temp directory doesn't exist. Creating it.");
					Directory.CreateDirectory(folder);
				}
				else
				{
					tracer.Info("Temp directory exists. Deleting it first.");
					try
					{
						Directory.Delete(folder, true);
					}
					catch (Exception e)
					{
						tracer.Error(e, "Failed to delete tempdir");
					}
					tracer.Info("Creating temp directory.");
					Directory.CreateDirectory(folder);
				}
			}
		}

		public string GenerateNewName()
		{
			string fname = string.Format(@"{0}\{1}.tmp", folder, Guid.NewGuid());
			return fname;
		}

		public string CreateEmptyFile()
		{
			string fname = GenerateNewName();
			File.Create(fname).Close();
			return fname;
		}

		readonly Source tracer;
		readonly string folder;
		static TempFilesManager instance;
	}
}
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace LogJoint
{
	public interface ITempFilesManager
	{
		string GenerateNewName();
		bool IsTemporaryFile(string filePath);
	};

	public class TempFilesManager: ITempFilesManager
	{
		public static TempFilesManager GetInstance()
		{
			if (instance == null)
				instance = new TempFilesManager(LJTraceSource.EmptyTracer);
			return instance;
		}

		private TempFilesManager(LJTraceSource tracer)
		{
			using (tracer.NewFrame)
			{
				this.tracer = tracer;
				
#if !SILVERLIGHT
				folder = Path.Combine(Path.GetTempPath(), "LogJoint");
#else
#endif
				bool thisIsTheOnlyInstance = false;
				runningInstanceMutex = new Mutex(true, "LogJoint/TempFilesManager", out thisIsTheOnlyInstance);

				tracer.Info("Temp directory: {0}", folder);
				
				if (!Directory.Exists(folder))
				{
					tracer.Info("Temp directory doesn't exist. Creating it.");
					Directory.CreateDirectory(folder);
				}
				else
				{
					if (!thisIsTheOnlyInstance)
					{
						tracer.Info("Temp directory exists and I am NOT the only instance in the system. Skipping temp cleanup.");
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
					}
					tracer.Info("Creating temp directory.");
					Directory.CreateDirectory(folder);
				}
			}
		}

		public string GenerateNewName()
		{
			string fname = Path.Combine(folder, Guid.NewGuid().ToString() + ".tmp");
			return fname;
		}

		public bool IsTemporaryFile(string filePath)
		{
			return string.Compare(Path.GetDirectoryName(filePath), folder, true) == 0;
		}

		public string CreateEmptyFile()
		{
			string fname = GenerateNewName();
			File.Create(fname).Close();
			return fname;
		}

		readonly LJTraceSource tracer;
		readonly string folder;
		readonly Mutex runningInstanceMutex;
		static TempFilesManager instance;
	}
}
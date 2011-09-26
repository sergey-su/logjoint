using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace FunctionalTests
{
	class FileSystemWatching: ITest
	{
		#region ITest Members

		public string Name
		{
			get { return "FSWatching"; }
		}

		public string ArgsHelp
		{
			get { return "<existing folder>"; }
		}

		public string Description
		{
			get { return "Checks is FileSystemWatcher works as expected. Call this test against folders on different filesystems (NTFS, FAT, Network shares)"; }
		}

		public void Run(string[] args)
		{
			bool testOk = true;
			string folder = Path.GetFullPath(args[0]);
			string testFile = folder + "\\test";
			using (FileSystemWatcher fsw = new FileSystemWatcher())
			{
				if (File.Exists(testFile))
					File.Delete(testFile);

				fsw.Path = folder;
				fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
				fsw.Created += new FileSystemEventHandler(fsw_Created);
				fsw.Deleted += new FileSystemEventHandler(fsw_Deleted);
				fsw.Changed += new FileSystemEventHandler(fsw_Changed);
				fsw.Renamed += new RenamedEventHandler(fsw_Renamed);
				fsw.EnableRaisingEvents = true;

				BeginWaitingFor(WatcherChangeTypes.Created, "test");
				using (FileStream fs = new FileStream(testFile, FileMode.Create, FileAccess.ReadWrite,
					FileShare.ReadWrite | FileShare.Delete))
				{
					try
					{
						EndWaitingFor();

						BeginWaitingFor(WatcherChangeTypes.Changed, "test");
						fs.WriteByte(123);
						fs.Flush();
						try
						{
							EndWaitingFor();
						}
						catch (Exception)
						{
							testOk = false;
							Console.WriteLine("Warninig! No change notification");
						}
					}
					finally
					{
						File.Delete(testFile);
					}
				}

				string renamedTestFile = testFile + ".renamed";
				File.WriteAllBytes(testFile, new byte[] { 1, 2, 3 });
				try
				{
					BeginWaitingFor(WatcherChangeTypes.Renamed, "test.renamed");
					File.Move(testFile, renamedTestFile);
					try
					{
						EndWaitingFor();
					}
					catch (Exception)
					{
						testOk = false;
						Console.WriteLine("Warning! No renaming notification");
					}
				}
				finally
				{
					File.Delete(renamedTestFile);
				}
			}
			if (!testOk)
			{
				throw new Exception("Some expecpectations were not met");
			}
		}

		#endregion

		void fsw_Deleted(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine("Deleted: {0}", e.FullPath);
			RegisterChange(e);
		}

		void fsw_Changed(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine("Changed: {0}", e.FullPath);
			RegisterChange(e);
		}

		void fsw_Renamed(object sender, RenamedEventArgs e)
		{
			Console.WriteLine("Renamed: {0}", e.FullPath);
			RegisterChange(e);
		}

		void fsw_Created(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine("Created: {0}", e.FullPath);
			RegisterChange(e);
		}

		void BeginWaitingFor(WatcherChangeTypes types, string fileName)
		{
			typeToWaitFor = types;
			fileToWaitFor = fileName;
			sw.Start();
		}

		void EndWaitingFor()
		{
			if (!waitEvt.WaitOne(TimeSpan.FromSeconds(20)))
				throw new TimeoutException(string.Format("No notification received. Expected {0}", typeToWaitFor));
			sw.Stop();
			Console.WriteLine("Notification {0} received OK. Delay: {1}", typeToWaitFor, sw.ElapsedMilliseconds);
			fileToWaitFor = null;
			typeToWaitFor = null;
		}

		void RegisterChange(FileSystemEventArgs e)
		{
			if (typeToWaitFor == null)
				return;
			if (e.ChangeType == typeToWaitFor.Value && string.Compare(e.Name, fileToWaitFor, true) == 0)
				waitEvt.Set();
		}

		WatcherChangeTypes? typeToWaitFor;
		string fileToWaitFor;
		AutoResetEvent waitEvt = new AutoResetEvent(false);
		Stopwatch sw = new Stopwatch();
	}
}

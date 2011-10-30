using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace LogJoint.Persistence
{
	internal abstract class StorageSectionBase : IStorageSection
	{
		public StorageSectionBase(StorageManager manager, StorageEntry entry, string key, ulong additionalNumericKey, string pathPrefix, StorageSectionOpenFlag openFlags)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Wrong key");
			this.manager = manager;
			this.entry = entry;
			this.key = key;
			this.path = entry.Path + System.IO.Path.DirectorySeparatorChar + StorageManager.NormalizeKey(key, additionalNumericKey, pathPrefix);
			this.openFlags = openFlags;
			this.commitOnDispose = (openFlags & StorageSectionOpenFlag.AccessMask) == StorageSectionOpenFlag.ReadWrite;
		}

		public StorageSectionOpenFlag OpenFlags { get { return openFlags; } }
		public string Key { get { CheckNotDisposed(); return key; } }
		public string Path { get { CheckNotDisposed(); return path; } }
		public StorageManager Manager { get { CheckNotDisposed(); return manager; } }
		public string AbsolutePath { get { CheckNotDisposed(); return manager.Implementation.AbsoluteRootPath + path; } }

		public void Dispose()
		{
			if (disposed)
				return;
			if (commitOnDispose)
			{
				entry.EnsureCreated();
				Commit();
			}
			disposed = true;
		}

		protected abstract void Commit();

		protected void CheckNotDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("StogareSection "+key);
		}

		readonly StorageManager manager;
		readonly StorageEntry entry;
		readonly string key;
		readonly StorageSectionOpenFlag openFlags;
		readonly string path;
		bool disposed;
		bool commitOnDispose;
	};

	internal class XmlStorageSection : StorageSectionBase, IXMLStorageSection
	{
		static readonly string KeyPrefix = "x";

		public XmlStorageSection(StorageManager manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, additionalNumericKey, KeyPrefix, openFlags)
		{
			if ((openFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				using (var s = manager.Implementation.OpenFile(Path, true))
				{
					try
					{
						if (s != null)
							data = XDocument.Load(s);
					}
					catch (System.Xml.XmlException e)
					{
						data = null;
					}
				}
			}
			if (data == null)
				data = new XDocument();
		}

		protected override void Commit()
		{
			using (var s = Manager.Implementation.OpenFile(Path, false))
			{
				s.SetLength(0);
				s.Position = 0;
				data.Save(s);
			}
		}

		public XDocument Data
		{
			get { CheckNotDisposed(); return data; }
		}

		readonly XDocument data;
	};

	internal class BinaryStorageSection : StorageSectionBase, IRawStreamStorageSection
	{
		static readonly string KeyPrefix = "b";

		public BinaryStorageSection(StorageManager manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, additionalNumericKey, KeyPrefix, openFlags)
		{
			if ((openFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				using (var s = manager.Implementation.OpenFile(Path, true))
					if (s != null)
					{
						s.CopyTo(data);
						data.Position = 0;
					}
			}
		}

		public Stream Data { get { CheckNotDisposed(); return data; } }

		protected override void Commit()
		{
			using (var s = Manager.Implementation.OpenFile(Path, false))
			{
				s.SetLength(0);
				s.Position = 0;
				data.Position = 0;
				data.CopyTo(s);
			}
		}

		readonly MemoryStream data = new MemoryStream();
	};
}

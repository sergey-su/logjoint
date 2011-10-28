using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace LogJoint.Persistence
{
	internal abstract class StorageSectionBase : IStorageSection
	{
		public StorageSectionBase(StorageManager manager, StorageEntry entry, string key, string pathPrefix, StorageSectionOpenFlag openFlags)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Wrong key");
			this.manager = manager;
			this.entry = entry;
			this.key = key;
			this.path = entry.Path + System.IO.Path.DirectorySeparatorChar + StorageManager.NormalizeKey(key, pathPrefix);
			this.openFlags = openFlags;
			this.commitOnDispose = openFlags == StorageSectionOpenFlag.ReadWrite;
		}

		public abstract StorageSectionType Type { get; }

		public string Key { get { CheckNotDisposed(); return key; } }
		public string Path { get { CheckNotDisposed(); return path; } }
		public StorageManager Manager { get { CheckNotDisposed(); return manager; } }

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

		public XmlStorageSection(StorageManager manager, StorageEntry entry, string key, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, KeyPrefix, openFlags)
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
				if (data == null)
					data = new XDocument();
			}
		}

		public override StorageSectionType Type { get { CheckNotDisposed(); return StorageSectionType.XML; } }

		protected override void Commit()
		{
			using (var s = Manager.Implementation.OpenFile(Path, false))
				data.Save(s);
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

		public BinaryStorageSection(StorageManager manager, StorageEntry entry, string key, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, KeyPrefix, openFlags)
		{
			using (var s = manager.Implementation.OpenFile(Path, true))
				if (s != null)
					s.CopyTo(data);
		}

		public override StorageSectionType Type { get { CheckNotDisposed(); return StorageSectionType.XML; } }

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

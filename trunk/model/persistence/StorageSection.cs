using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;

namespace LogJoint.Persistence.Implementation
{
	internal abstract class StorageSectionBase : IStorageSection
	{
		public StorageSectionBase(StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, string pathPrefix, StorageSectionOpenFlag openFlags)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Wrong key");
			this.manager = manager;
			this.entry = entry;
			this.key = key;
			this.path = entry.Path + System.IO.Path.DirectorySeparatorChar + StorageManagerImplementation.NormalizeKey(key, additionalNumericKey, pathPrefix);
			this.openFlags = openFlags;
			this.commitOnDispose = (openFlags & StorageSectionOpenFlag.AccessMask) == StorageSectionOpenFlag.ReadWrite;
		}

		public StorageSectionOpenFlag OpenFlags { get { return openFlags; } }
		public string Key { get { CheckNotDisposed(); return key; } }
		public string Path { get { CheckNotDisposed(); return path; } }
		public StorageManagerImplementation Manager { get { CheckNotDisposed(); return manager; } }
		public string AbsolutePath { get { CheckNotDisposed(); return manager.FileSystem.AbsoluteRootPath + path; } }

		public virtual void Dispose()
		{
			if (disposed || disposing)
				return;
			disposing = true;
			try
			{
				if (commitOnDispose)
				{
					entry.EnsureCreated();
					Commit();
				}
			}
			finally
			{
				disposed = true;
				disposing = false;
			}
		}

		protected abstract void Commit();

		protected void CheckNotDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("StogareSection "+key);
		}

		readonly StorageManagerImplementation manager;
		readonly StorageEntry entry;
		readonly string key;
		readonly StorageSectionOpenFlag openFlags;
		readonly string path;
		bool disposed;
		bool disposing;
		bool commitOnDispose;
	};

	internal class XmlStorageSection : StorageSectionBase, IXMLStorageSection
	{
		public const string KeyPrefix = "x";

		public XmlStorageSection(StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, additionalNumericKey, KeyPrefix, openFlags)
		{
			if ((openFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				using (var s = manager.FileSystem.OpenFile(Path, true))
				{
					try
					{
						if (s != null)
							data = XDocument.Load(s);
					}
					catch (XmlException)
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
			try
			{
				using (var s = Manager.FileSystem.OpenFile(Path, false))
				{
					s.SetLength(0);
					s.Position = 0;
					data.Save(s);
				}
			}
			catch (Exception e)
			{
				if ((OpenFlags & StorageSectionOpenFlag.IgnoreStorageExceptions) != 0)
				{
					try
					{
						Manager.FileSystem.ConvertException(e);
					}
					catch (Persistence.StorageFullException)
					{
					}
				}
				else
				{
					Manager.FileSystem.ConvertException(e);
				}
			}
		}

		public XDocument Data
		{
			get { CheckNotDisposed(); return data; }
		}

		readonly XDocument data;
	};

	internal class SaxXmlStorageSection : StorageSectionBase, ISaxXMLStorageSection, IDisposable
	{
		public SaxXmlStorageSection(StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, additionalNumericKey, XmlStorageSection.KeyPrefix, openFlags)
		{
			if ((openFlags & StorageSectionOpenFlag.ReadWrite) == 0)
			{
				throw new NotSupportedException("Sax xml section can be open for writing");
			}
			if ((openFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				fileSystemStream = manager.FileSystem.OpenFile(Path, true);
				if (fileSystemStream != null)
				{
					reader = XmlReader.Create(fileSystemStream);
					streamLen = fileSystemStream.Length != 0 ? fileSystemStream.Length : new double?();
				}
			}
		}

		protected override void Commit()
		{
			throw new NotSupportedException("can not modify XML section open with SAX flag");
		}

		public XmlReader Reader
		{
			get { CheckNotDisposed(); return reader; }
		}

		public double ReadProgress
		{
			get
			{
				CheckNotDisposed();
				return streamLen.HasValue ? ((double)fileSystemStream.Position / streamLen.Value) : 0d;
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			reader?.Dispose();
			fileSystemStream?.Dispose();
		}

		readonly Stream fileSystemStream;
		readonly XmlReader reader;
		readonly double? streamLen;
	};

	internal class BinaryStorageSection : StorageSectionBase, IRawStreamStorageSection, IStorageSectionInternal
	{
		public const string KeyPrefix = "b";

		public BinaryStorageSection(StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags, string keyPrefix = null) :
			base(manager, entry, key, additionalNumericKey, keyPrefix ?? KeyPrefix, openFlags)
		{
			if ((openFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				using (var s = manager.FileSystem.OpenFile(Path, true))
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
			try
			{
				using (var s = Manager.FileSystem.OpenFile(Path, false))
				{
					s.SetLength(0);
					s.Position = 0;
					data.Position = 0;
					data.CopyTo(s);
				}
			}
			catch (Exception e)
			{
				Manager.FileSystem.ConvertException(e);
			}
		}

		bool IStorageSectionInternal.ExistsInFileSystem
		{
			get 
			{
				using (var s = Manager.FileSystem.OpenFile(Path, true))
					return s != null;
			} 
		}

		readonly MemoryStream data = new MemoryStream();
	};
}

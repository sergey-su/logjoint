using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

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

		public virtual async ValueTask DisposeAsync()
		{
			if (disposed || disposing)
				return;
			disposing = true;
			try
			{
				if (commitOnDispose)
				{
					await entry.EnsureCreated();
					await Commit();
				}
			}
			finally
			{
				disposed = true;
				disposing = false;
			}
		}

		protected abstract ValueTask Commit();

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
		readonly bool commitOnDispose;
	};

	internal class XmlStorageSection : StorageSectionBase, IXMLStorageSection
	{
		public const string KeyPrefix = "x";

		public static async Task<IXMLStorageSection> Create(
			StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags)
		{
			var result = new XmlStorageSection(manager, entry, key, additionalNumericKey, openFlags);
			await result.Init();
			return result;
		}

		private XmlStorageSection(StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, additionalNumericKey, KeyPrefix, openFlags)
		{
		}

		private async ValueTask Init()
		{
			if ((OpenFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				using var s = await Manager.FileSystem.OpenFile(Path, readOnly: true);
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
			if (data == null)
				data = new XDocument();
		}

		protected override async ValueTask Commit()
		{
			try
			{
				using var s = await Manager.FileSystem.OpenFile(Path, readOnly: false);
				s.SetLength(0);
				s.Position = 0;
				data.Save(s);
				await s.FlushAsync();
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

		XDocument data;
	};

	internal class SaxXmlStorageSection : StorageSectionBase, ISaxXMLStorageSection, IAsyncDisposable
	{
		public static async Task<ISaxXMLStorageSection> Create(StorageManagerImplementation manager,
			StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags)
		{
			SaxXmlStorageSection result = new SaxXmlStorageSection(manager, entry, key, additionalNumericKey, openFlags);
			await result.Init();
			return result;
		}

		private SaxXmlStorageSection(StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags) :
			base(manager, entry, key, additionalNumericKey, XmlStorageSection.KeyPrefix, openFlags)
		{ }

		private async ValueTask Init()
		{
			if ((OpenFlags & StorageSectionOpenFlag.ReadWrite) == 0)
			{
				throw new NotSupportedException("Sax xml section can be open for writing");
			}
			if ((OpenFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				fileSystemStream = await Manager.FileSystem.OpenFile(Path, readOnly: true);
				if (fileSystemStream != null)
				{
					reader = XmlReader.Create(fileSystemStream);
					streamLen = fileSystemStream.Length != 0 ? fileSystemStream.Length : new double?();
				}
			}
		}

		protected override ValueTask Commit()
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

		public override async ValueTask DisposeAsync()
		{
			await base.DisposeAsync();
			reader?.Dispose();
			fileSystemStream?.Dispose();
		}

		Stream fileSystemStream;
		XmlReader reader;
		double? streamLen;
	};

	internal class BinaryStorageSection : StorageSectionBase, IRawStreamStorageSection, IStorageSectionInternal
	{
		public const string KeyPrefix = "b";

		public static async Task<IRawStreamStorageSection> Create(StorageManagerImplementation manager,
			StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags, string keyPrefix = null)
		{
			var result = new BinaryStorageSection(manager, entry, key, additionalNumericKey, openFlags, keyPrefix);
			await result.Init();
			return result;
		}

		private BinaryStorageSection(StorageManagerImplementation manager, StorageEntry entry, string key, ulong additionalNumericKey, StorageSectionOpenFlag openFlags, string keyPrefix) :
			base(manager, entry, key, additionalNumericKey, keyPrefix ?? KeyPrefix, openFlags)
		{ }

		private async ValueTask Init()
		{
			if ((OpenFlags & StorageSectionOpenFlag.ClearOnOpen) == 0)
			{
				using var s = await Manager.FileSystem.OpenFile(Path, readOnly: true);
				if (s != null)
				{
					await s.CopyToAsync(data);
					data.Position = 0;
				}
			}
		}

		public Stream Data { get { CheckNotDisposed(); return data; } }

		protected override async ValueTask Commit()
		{
			try
			{
				using var s = await Manager.FileSystem.OpenFile(Path, readOnly: false);
				s.SetLength(0);
				s.Position = 0;
				data.Position = 0;
				await data.CopyToAsync(s);
				await s.FlushAsync();
			}
			catch (Exception e)
			{
				Manager.FileSystem.ConvertException(e);
			}
		}

		async ValueTask<bool> IStorageSectionInternal.ExistsInFileSystem()
		{
			using var s = await Manager.FileSystem.OpenFile(Path, readOnly: true);
			return s != null;
		}

		readonly MemoryStream data = new MemoryStream();
	};
}

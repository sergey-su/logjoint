using System;
using System.Net;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint
{
	public class NetworkCredentialsStorage
	{
		public static async Task<NetworkCredentialsStorage> Create(
			Persistence.IStorageEntry settingsEntry, Persistence.ICredentialsProtection credentialsProtection)
		{
			var storage = new NetworkCredentialsStorage(settingsEntry, credentialsProtection);
			await storage.Load();
			return storage;
		}

		private NetworkCredentialsStorage(Persistence.IStorageEntry settingsEntry, Persistence.ICredentialsProtection credentialsProtection)
		{
			this.settingsEntry = settingsEntry;
			this.credentialsProtection = credentialsProtection;
		}

		public System.Net.NetworkCredential GetCredential(Uri uri)
		{
			var uriPrefix = GetRelevantPart(uri);
			return entries.Find(e => e.UriPrefix == uriPrefix).Cred;
		}

		public static Uri GetRelevantPart(Uri uri)
		{
			return uri.IsFile ? uri : new Uri(uri.Scheme + "://" + uri.Host);
		}

		public bool Remove(Uri uri)
		{
			Uri uriPrefix = GetRelevantPart(uri);
			return entries.RemoveAll(e => e.UriPrefix == uriPrefix) > 0;
		}

		public void Add(Uri uri, System.Net.NetworkCredential cred)
		{
			Uri uriPrefix = GetRelevantPart(uri);
			entries.RemoveAll(e => e.UriPrefix == uriPrefix);
			entries.Add(new Entry() { UriPrefix = uriPrefix, Cred = cred });
		}

		public async Task StoreSecurely()
		{
			var doc = new XDocument(
				new XElement("credentials", 
					entries.Select(e => new XElement("cred",
						new XAttribute("uri", e.UriPrefix.ToString()),
						new XAttribute("user", e.Cred.UserName),
						new XAttribute("domain", e.Cred.Domain),
						new XAttribute("pwd", e.Cred.Password))
					).ToArray()
				)
			);
			MemoryStream ms = new MemoryStream();
			doc.Save(ms);
			var protectedData = credentialsProtection.Protect(ms.ToArray());
			using (var sect = settingsEntry.OpenRawStreamSection("network-auth", Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.IgnoreStorageExceptions))
			{
				sect.Data.SetLength(0);
				await sect.Data.WriteAsync(protectedData, 0, protectedData.Length);
			}
		}

		private async Task Load()
		{
			byte[] protectedData;
			using (var sect = settingsEntry.OpenRawStreamSection("network-auth", Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				protectedData = new byte[sect.Data.Length];
				await sect.Data.ReadAsync(protectedData, 0, protectedData.Length);
			}
			byte[] unprotectedData;
			try
			{
				unprotectedData = credentialsProtection.Unprotect(protectedData);
			}
			catch (System.Security.Cryptography.CryptographicException)
			{
				return;
			}
			var doc = XDocument.Load(new MemoryStream(unprotectedData, false));
			Clear();
			entries.AddRange(
				doc.Element("credentials").Elements("cred").Select(e => 
					new Entry() { 
						UriPrefix = new Uri(e.Attribute("uri").Value),
						Cred = new System.Net.NetworkCredential(
							e.Attribute("user").Value,
							e.Attribute("pwd").Value,
							e.Attribute("domain").Value
						)
					}
				)
			);
		}

		void Clear()
		{
			entries.Clear();
		}

		readonly Persistence.IStorageEntry settingsEntry;
		readonly Persistence.ICredentialsProtection credentialsProtection;

		struct Entry
		{
			public Uri UriPrefix;
			public System.Net.NetworkCredential Cred;
		};
		List<Entry> entries = new List<Entry>();
	}
}
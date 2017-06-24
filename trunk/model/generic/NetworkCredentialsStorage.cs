using System;
using System.Net;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
	public class NetworkCredentialsStorage
	{
		public NetworkCredentialsStorage(Persistence.IStorageEntry settingsEntry)
		{
			this.settingsEntry = settingsEntry;

			Load();
		}

		public System.Net.NetworkCredential GetCredential(Uri uri)
		{
			var uriPrefix = StripToPrefix(uri);
			return entries.Find(e => e.UriPrefix == uriPrefix).Cred;
		}

		public static Uri StripToPrefix(Uri uri)
		{
			return new Uri(uri.Scheme + "://" + uri.Host);
		}

		public bool Remove(Uri uri)
		{
			Uri uriPrefix = StripToPrefix(uri);
			return entries.RemoveAll(e => e.UriPrefix == uriPrefix) > 0;
		}

		public void Add(Uri uri, System.Net.NetworkCredential cred)
		{
			Uri uriPrefix = StripToPrefix(uri);
			entries.RemoveAll(e => e.UriPrefix == uriPrefix);
			entries.Add(new Entry() { UriPrefix = uriPrefix, Cred = cred });
		}

		public void StoreSecurely()
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
			var protectedData = System.Security.Cryptography.ProtectedData.Protect(ms.ToArray(), additionalEntropy, 
				System.Security.Cryptography.DataProtectionScope.CurrentUser);
			using (var sect = settingsEntry.OpenRawStreamSection("network-auth", Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.IgnoreStorageExceptions))
			{
				sect.Data.SetLength(0);
				sect.Data.Write(protectedData, 0, protectedData.Length);
			}
		}

		public void Load()
		{
			byte[] protectedData;
			using (var sect = settingsEntry.OpenRawStreamSection("network-auth", Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				protectedData = new byte[sect.Data.Length];
				sect.Data.Read(protectedData, 0, protectedData.Length);
			}
			byte[] unprotectedData;
			try
			{
				unprotectedData = System.Security.Cryptography.ProtectedData.Unprotect(protectedData, additionalEntropy,
					System.Security.Cryptography.DataProtectionScope.CurrentUser);
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
		struct Entry
		{
			public Uri UriPrefix;
			public System.Net.NetworkCredential Cred;
		};
		List<Entry> entries = new List<Entry>();

		static byte[] additionalEntropy = { 19, 22, 43, 127, 128, 63, 221 };
	}
}
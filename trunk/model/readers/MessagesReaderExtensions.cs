using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint
{
	public class MessagesReaderExtensions: IDisposable
	{
		public class XmlInitializationParams
		{
			public static readonly XmlInitializationParams Empty = new XmlInitializationParams(null);

			public XmlInitializationParams(XElement extensionsNode)
			{
				if (extensionsNode == null)
					return;
				items.Clear();
				items.AddRange(
					from e in extensionsNode.Elements("extension")
					let name = e.Attribute("name")
					let parsedClassName = ParseFullClassName(e.Attribute("class-name").Value)
					where name != null && parsedClassName.Item1 != null
					select new InitializationDataItem(name.Value, parsedClassName.Item2, parsedClassName.Item1)
				);
			}

			static Tuple<string, string> ParseFullClassName(string fullClassName)
			{
				if (fullClassName != null)
				{
					var i = fullClassName.IndexOf(',');
					if (i > 0)
					{
						return new Tuple<string, string>(fullClassName.Substring(0, i).Trim(), 
							fullClassName.Substring(i + 1).Trim());
					}
				}
				return new Tuple<string, string>(null, null);
			}

			internal void InitializeInstance(MessagesReaderExtensions instance)
			{
				foreach (InitializationDataItem initDataItem in this.items)
				{
					ExtensionDataInternal extData = new ExtensionDataInternal();
					instance.items.Add(extData);
					extData.initData = initDataItem;
				}
			}

			List<InitializationDataItem> items = new List<InitializationDataItem>();
		};

		public MessagesReaderExtensions(IPositionedMessagesReader owner, XmlInitializationParams initializationData = null)
		{
			this.owner = owner;
			if (initializationData != null)
			{
				try
				{
					initializationData.InitializeInstance(this);
				}
				catch
				{
					Dispose();
					throw;
				}
			}
		}

		public void AttachExtensions()
		{
			if (attached)
				throw new InvalidOperationException("Extensions are already attached to messages reader");
			CheckOwnerSpecified();
			attached = true;
			foreach (var extIntf in EnumExtensionsImplementingTheInterface())
				extIntf.Attach(this.owner);
		}

		public void NotifyExtensionsAboutUpdatedAvailableBounds(AvailableBoundsUpdateNotificationArgs param)
		{
			foreach (var extIntf in EnumExtensionsImplementingTheInterface())
				extIntf.OnAvailableBoundsUpdated(param);
		}

		public struct ExtensionData
		{
			public string AssemblyName;
			public string ClassName;
			public string Name;
			public Func<object> Instance;
		};

		public IEnumerable<ExtensionData> Items
		{
			get
			{
				foreach (ExtensionDataInternal extData in items)
				{
					ExtensionData ret;
					ret.ClassName = extData.initData.ClassName;
					ret.Name = extData.initData.ExtensionName;
					ret.AssemblyName = extData.initData.AssemblyName;
					ret.Instance = extData.instanceGetter;
					yield return ret;
				}
			}
		}


		#region IDisposable Members

		public void Dispose()
		{
			foreach (ExtensionDataInternal extData in items)
				extData.Dispose();
		}

		#endregion

		#region Implementation

		struct InitializationDataItem
		{
			public readonly string ExtensionName;
			public readonly string AssemblyName;
			public readonly string ClassName;
			public InitializationDataItem(string fieldName, string assemblyName, string className)
			{
				ExtensionName = fieldName;
				AssemblyName = assemblyName;
				ClassName = className;
			}
		};

		class ExtensionDataInternal
		{
			public InitializationDataItem initData;
			public Func<object> instanceGetter;
			
			object instance;
			IMessagesReaderExtension instanceIntf;

			public ExtensionDataInternal()
			{
				instanceGetter = GetInstance;
			}

			public object GetInstance()
			{
				if (instance != null)
					return instance;
				string fullTypeName = initData.ClassName + ", " + initData.AssemblyName;
				Type extType = Type.GetType(fullTypeName);
				if (extType == null)
					throw new TypeLoadException("Extension type not found: " + fullTypeName);
				instance = Activator.CreateInstance(extType);
				instanceIntf = instance as IMessagesReaderExtension;
				return instance;
			}

			public IMessagesReaderExtension GetInstanceIntf()
			{
				GetInstance();
				return instanceIntf;
			}

			public void Dispose()
			{
				IDisposable dispIntf = instance as IDisposable;
				if (dispIntf != null)
					dispIntf.Dispose();
			}
		};

		IEnumerable<IMessagesReaderExtension> EnumExtensionsImplementingTheInterface()
		{
			return from ext in items
				   let intf = ext.GetInstanceIntf()
				   where intf != null 
				   select intf;
		}

		void CheckOwnerSpecified()
		{
			if (owner == null)
				throw new InvalidOperationException("Operation is not allowed for extensions collection that is not connected to a messages reader");
		}

		readonly IPositionedMessagesReader owner;
		readonly List<ExtensionDataInternal> items = new List<ExtensionDataInternal>();
		bool attached;

		#endregion
	}
}

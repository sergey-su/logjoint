using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint
{
	public class SharedModelData
	{
		public SharedModelData(SharedMemory sharedMemory)
		{
			this.sharedMemory = sharedMemory;
		}

		public event EventHandler OnChange;

		public class ViewerInstance
		{
			public string InstanceID { get { return id; } }

			public class LogSource
			{
				//public ConnectionParams ConnectParams { get; }
			};

			//public IEnumerable<LogSource> LogSources { get; }

			internal void Update(XElement e)
			{
			}

			string id;
		};

		public IEnumerable<ViewerInstance> Instances { get { return instances; } }

		public ViewerInstance RegisterInstance(string id)
		{
			return null;
			//new ViewerInstance 
		}

		void Update()
		{
			XDocument doc;
			using (var stm = sharedMemory.ReadAll())
			{
				stm.Position = 0;
				try
				{
					doc = XDocument.Load(stm);
				}
				catch (Exception e)
				{
					// todo: log e
					return;
				}
			}
			if (doc.Root == null)
			{
				var stm2 = sharedMemory.BeginWrite();
				(new XDocument(new XElement("root"))).Save(stm2);
				sharedMemory.EndWrite();

				Reset();
			}
			else
			{
				foreach (var tmp in doc.Root.Elements("instance").GroupJoin(instances, 
					e => e.AttributeValue("id"), obj => obj.InstanceID, (e, objs) => new { Elt = e, Obj = objs.FirstOrDefault() }))
				{
					(tmp.Obj ?? new ViewerInstance()).Update(tmp.Elt);
				}
			}
		}

		void Reset()
		{
			instances.Clear();
		}

		readonly SharedMemory sharedMemory;
		readonly List<ViewerInstance> instances = new List<ViewerInstance>();
	}
}

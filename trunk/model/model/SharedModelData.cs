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
			this.sharedMemory.OnChanged += (s, e) =>
			{
				Update();
				if (OnChange != null)
					OnChange(this, EventArgs.Empty);
			};
		}

		public event EventHandler OnChange;

		public class ViewerInstance
		{
			public string ID { get { return id; } }

			public class LogSource
			{
				public string ID { get { return factoryAndParams; } }
				
				public RecentLogEntry GetRecentLogEntry()
				{
					return new RecentLogEntry(factoryAndParams);
				}

				internal LogSource(XElement e)
				{
					Update(e);
				}

				internal void Update(XElement e)
				{
					factoryAndParams = ReadId(e);
				}

				static internal string ReadId(XElement e)
				{
					return e.AttributeValue("factory-and-params");
				}

				string factoryAndParams;
			};

			public IEnumerable<LogSource> LogSources { get { return logSources; } }

			internal ViewerInstance(XElement e)
			{
				Update(e);
			}

			internal void Update(XElement instanceElt)
			{
				foreach (var i in FullOuterJoin(instanceElt.Elements("log-source"), logSources, e => LogSource.ReadId(e), obj => obj.ID,
					(e, obj) => new { Elt = e, Obj = obj }))
				{
					if (i.Elt == null)
						logSources.Remove(i.Obj);
					else if (i.Obj == null)
						logSources.Add(new LogSource(i.Elt));
					else
						i.Obj.Update(i.Elt);
				}
			}

			string id;
			List<LogSource> logSources = new List<LogSource>();
		};

		public IEnumerable<ViewerInstance> Instances { get { return instances; } }

		public ViewerInstance RegisterInstance(string id)
		{
			if (instances.Find(inst => inst.ID == id) != null)
				throw new ArgumentException("Instance already registered");
			return null;
			//new ViewerInstance 
		}

		XDocument BeginWrite()
		{
			var stm = sharedMemory.BeginWrite();
			XDocument doc = ReadDocument(stm);
			if (doc.Root == null)
			{
				doc.Add(new XElement("root"));
			}
			return doc;
		}

		void EndWrite()
		{
		}

		void Update()
		{
			XDocument doc;
			using (var stm = sharedMemory.ReadAll())
			{
				doc = ReadDocument(stm);
			}
			if (doc.Root == null)
			{
				var stm2 = sharedMemory.BeginWrite();
				doc.Add(new XElement("root"));
				doc.Save(stm2);
				sharedMemory.EndWrite();
			}
			foreach (var i in FullOuterJoin(doc.Root.Elements("instance"), instances, e => e.AttributeValue("id"), obj => obj.ID,
				(e, obj) => new {Elt=e, Obj=obj}))
			{
				if (i.Elt == null)
					instances.Remove(i.Obj);
				else if (i.Obj == null)
					instances.Add(new ViewerInstance(i.Elt));
				else
					i.Obj.Update(i.Elt);
			}
		}

		private static XDocument ReadDocument(System.IO.Stream stm)
		{
			XDocument doc;
			stm.Position = 0;
			try
			{
				doc = XDocument.Load(stm);
			}
			catch (Exception e)
			{
				// todo: log e
				doc = new XDocument();
			}
			return doc;
		}

		public static IEnumerable<TResult> FullOuterJoin<TOuter, TInner, TKey, TResult>(
			IEnumerable<TOuter> outer, IEnumerable<TInner> inner, 
			Func<TOuter, TKey> outerKeySelector, 
			Func<TInner, TKey> innerKeySelector, 
			Func<TOuter, TInner, TResult> resultSelector)
			where TInner : class
			where TOuter : class
		{
			var innerLookup = inner.ToLookup(innerKeySelector);
			var outerLookup = outer.ToLookup(outerKeySelector);
			return outer.Select(outerKeySelector).Union(inner.Select(innerKeySelector)).Distinct().Select(
				k => resultSelector(outerLookup[k].FirstOrDefault(), innerLookup[k].FirstOrDefault()));
		}


		readonly SharedMemory sharedMemory;
		readonly List<ViewerInstance> instances = new List<ViewerInstance>();
	}
}

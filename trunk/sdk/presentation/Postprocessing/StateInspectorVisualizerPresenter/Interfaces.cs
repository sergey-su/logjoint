using LogJoint.Postprocessing;
using LogJoint.Postprocessing.StateInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer
{
	public interface IPresenter
	{
		bool IsObjectEventPresented(ILogSource source, TextLogEventTrigger eventTrigger);
		bool TrySelectObject(ILogSource source, TextLogEventTrigger objectEvent, Func<IVisualizerNode, int> disambiguationFunction);
		void Show();
		IVisualizerNode SelectedObject { get; }
		event EventHandler<MenuData> OnMenu;
		event EventHandler<NodeCreatedEventArgs> OnNodeCreated;
	};

	public class MenuData
	{
		public class Item
		{
			public string Text;
			public Action Click;
		};
		public List<Item> Items;
	};

	public class NodeCreatedEventArgs
	{
		public IVisualizerNode NodeObject { get; internal set; }
		public bool? CreateCollapsed { get; set; }
		public bool? CreateLazilyLoaded { get; set; }
	};

	public interface IVisualizerNode
	{
		string Id { get; }
		Event CreationEvent { get; }
		IVisualizerNode Parent { get; }
		bool BelongsToSource(ILogSource logSource);
		IEnumerable<PropertyChange> ChangeHistory { get; }
	};
}

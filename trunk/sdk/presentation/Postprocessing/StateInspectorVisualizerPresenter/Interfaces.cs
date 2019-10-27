using LogJoint.Postprocessing;
using LogJoint.Postprocessing.StateInspector;
using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer
{
	public interface IPresenter: IPostprocessorVisualizerPresenter
	{
		IVisualizerNode SelectedObject { get; }
		IEnumerableAsync<IVisualizerNode> Roots { get; }
		event EventHandler<MenuData> OnMenu;
		event EventHandler<NodeCreatedEventArgs> OnNodeCreated;
	};

	public class MenuData
	{
		public class Item
		{
			public string Text { get; private set; }
			public Action Click { get; private set; }

			public Item(string text, Action click)
			{
				this.Text = text;
				this.Click = click;
			}
		};
		public List<Item> Items { get; internal set; }
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
		IEnumerableAsync<IVisualizerNode> Children { get; }
	};
}

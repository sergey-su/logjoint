using System.Collections.Generic;
using System.Linq;
using LogJoint.Drawing;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.SourcesList
{
	abstract class ViewItem : IViewItem
	{
		public bool? Checked;
		public string Description;
		public Color ItemColor;
		public bool IsFailed;
		public bool IsSelected;
		public SourcesContainerViewItem Parent;

		string Reactive.ITreeNode.Key => GetKey();
		bool Reactive.ITreeNode.IsExpanded => GetIsExpanded();
		bool Reactive.ITreeNode.IsSelected => IsSelected;
		IReadOnlyList<Reactive.ITreeNode> Reactive.ITreeNode.Children => GetChildren();
		bool Reactive.ITreeNode.IsExpandable => true;

		bool? IViewItem.Checked => Checked;
		(Color, bool) IViewItem.Color => (ItemColor, IsFailed);
		IViewItem IViewItem.Parent => Parent;

		public abstract string GetKey();
		public abstract IReadOnlyList<Reactive.ITreeNode> GetChildren();
		public virtual bool GetIsExpanded() => false;

		public override string ToString() => Description;

		public static IEnumerable<Reactive.ITreeNode> Flatten(Reactive.ITreeNode root)
		{
			return Enumerable.Union(
				new[] { root },
				root.Children.SelectMany(Flatten)
			);
		}
	};

	class LogSourceViewItem : ViewItem
	{
		public ILogSource LogSource;
		public string ContainerName;

		public override string GetKey() => $"{Parent?.GetKey()}-{LogSource.GetHashCode()}";
		public override IReadOnlyList<Reactive.ITreeNode> GetChildren() => ImmutableArray<Reactive.ITreeNode>.Empty;
	};

	class PreprocessingViewItem : ViewItem
	{
		public Preprocessing.ILogSourcePreprocessing Preprocessing;

		public override string GetKey() => $"{Parent?.GetKey()}-{Preprocessing.GetHashCode()}";
		public override IReadOnlyList<Reactive.ITreeNode> GetChildren() => ImmutableArray<Reactive.ITreeNode>.Empty;
	};

	class SourcesContainerViewItem : ViewItem
	{
		public string ContainerName;
		public IReadOnlyList<LogSourceViewItem> LogSources;
		public bool IsExpanded;

		public override string GetKey() => $"container-{ContainerName}";
		public override IReadOnlyList<Reactive.ITreeNode> GetChildren() => LogSources;
		public override bool GetIsExpanded() => IsExpanded;
	};

	class RootViewItem : IViewItem
	{
		public IReadOnlyList<Reactive.ITreeNode> Items;

		bool? IViewItem.Checked => false;
		(Color value, bool isFailureColor) IViewItem.Color => (Color.Black, false);
		string Reactive.ITreeNode.Key => "root";
		IReadOnlyList<Reactive.ITreeNode> Reactive.ITreeNode.Children => Items;
		bool Reactive.ITreeNode.IsExpanded => true;
		bool Reactive.ITreeNode.IsSelected => false;
		bool Reactive.ITreeNode.IsExpandable => true;
		IViewItem IViewItem.Parent => null;
	};
};
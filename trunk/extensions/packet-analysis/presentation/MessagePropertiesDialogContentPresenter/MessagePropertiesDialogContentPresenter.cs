using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LogJoint.UI.Presenters;
using LogJoint.UI.Presenters.MessagePropertiesDialog;
using LogJoint.Wireshark.Dpml;

namespace LogJoint.PacketAnalysis.UI.Presenters.MessagePropertiesDialog
{
	public class Presenter : IMessageContentPresenter, IPresenter, IViewModel
	{
		readonly IView view;
		readonly IChangeNotification changeNotification;
		readonly IClipboardAccess clipboardAccess;
		readonly Func<Node> getRoot;
		ImmutableHashSet<string> lastSetExpanded = ImmutableHashSet<string>.Empty;
		string lastSetSelected = "";
		IMessage message;

		public Presenter(
			IView view,
			IChangeNotification changeNotification,
			IClipboardAccess clipboardAccess
		)
		{
			this.view = view;
			this.changeNotification = changeNotification;
			this.clipboardAccess = clipboardAccess;

			this.getRoot = Selectors.Create(
				() => message,
				() => lastSetExpanded,
				() => lastSetSelected,
				(msg, setExpanded, setSelected) =>
				{
					XElement packetElement = null;
					try
					{
						if (msg != null)
							packetElement = XElement.Parse(msg.RawText.Value);
					}
					catch (XmlException)
					{
					}
					packetElement = packetElement ?? new XElement("packet");

					Node toNode(XElement element, string parentId, bool isHidden, HashSet<string> usedKeys)
					{
						// todo: handle "response in" by doing binary search inside the file
						var show =
							element.Attribute("showname")?.Value ?? element.Attribute("show")?.Value;
						var name = element.Attribute("name")?.Value;
						var hide = element.Attribute("hide")?.Value;
						if (show == null || name == null || hide == "yes")
							return null;
						var key = name;
						while (!usedKeys.Add(key))
							key += "+";
						var id = $"{parentId}/{key}";
						var isExpanded = setExpanded.Contains(id);
						return new Node
						{
							Text = show,
							Key = key,
							Id = id,
							Children = toNodes(element.Elements(), id, isHidden || !isExpanded),
							IsExpanded = isExpanded,
							IsSelected = !isHidden && id == setSelected
						};
					}

					ImmutableArray<Node> toNodes(IEnumerable<XElement> elements,
						string parentId, bool isHidden)
					{
						var usedKeys = new HashSet<string>();
						return ImmutableArray.CreateRange(elements.Select(
							e => toNode(e, parentId, isHidden, usedKeys)).Where(n => n != null));
					}

					return new Node
					{
						Text = "",
						Id = "",
						Key = "",
						IsExpanded = true,
						Children = toNodes(
							packetElement.Elements("proto")
							.Where(p => p.Attribute("name")?.Value != "geninfo"),
							"",
							false
						),
					};
				}
			);

			view.SetViewModel(this);
		}

		object IMessageContentPresenter.View => view.OSView;
		string IMessageContentPresenter.ContentViewModeName => "Packet protocols";

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IViewTreeNode IViewModel.Root => getRoot();

		void IViewModel.OnExpand(IViewTreeNode node)
		{
			if (node is Node impl)
			{
				lastSetExpanded = lastSetExpanded.Add(impl.Id);
				changeNotification.Post();
			}
		}

		void IViewModel.OnCollapse(IViewTreeNode node)
		{
			if (node is Node impl)
			{
				lastSetExpanded = lastSetExpanded.Remove(impl.Id);
				changeNotification.Post();
			}
		}

		void IViewModel.OnSelect(IViewTreeNode node)
		{
			if (node is Node impl)
			{
				lastSetSelected = impl.Id;
				changeNotification.Post();
			}
		}

		void IViewModel.OnCopy()
		{
			var textBuilder = new StringBuilder();
			void Traverse(Node node, string outerCopyIndent)
			{
				var copyIndent =
					outerCopyIndent != null ? outerCopyIndent + "    " :
					node.IsSelected ? "" : null;
				if (copyIndent != null)
					textBuilder.AppendLine($"{copyIndent}{node.Text}");
				foreach (var n in node.Children)
					Traverse((Node)n, copyIndent);
			}
			Traverse(getRoot(), null);
			var text = textBuilder.ToString();
			clipboardAccess.SetClipboard(text, $"<pre>{text}</pre>");
		}

		void IPresenter.SetMessage(IMessage message)
		{
			this.message = message;
			changeNotification.Post();
		}

		class Node : IViewTreeNode
		{
			public string Key { get; set; }

			public string Text { get; internal set; }

			public bool IsSelected { get; internal set; }

			public IReadOnlyList<LogJoint.UI.Presenters.Reactive.ITreeNode> Children { get; internal set; }

			public bool IsExpanded { get; internal set; }

			internal string Id { get; set; }

			public override string ToString() => Text;
		};
	};
};
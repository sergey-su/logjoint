using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using LogJoint.UI.Presenters.MessagePropertiesDialog;
using LogJoint.Wireshark.Dpml;

namespace LogJoint.PacketAnalysis.UI.Presenters.MessagePropertiesDialog
{
	public class Presenter : IMessageContentPresenter, IPresenter, IViewModel
	{
		readonly IView view;
		readonly IChangeNotification changeNotification;
		readonly Func<Node> getRoot;
		ImmutableHashSet<string> lastSetExpanded = ImmutableHashSet<string>.Empty;
		string lastSetSelected = "";
		IMessage message;

		public Presenter(
			IView view,
			IChangeNotification changeNotification
		)
		{
			this.view = view;
			this.changeNotification = changeNotification;

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

					Node toNode(XElement element, string parentId, bool isHidden)
					{
						// todo: hide attr
						var show = element.Attribute("showname")?.Value;
						var name = element.Attribute("name")?.Value;
						if (show == null || name == null)
							return null;
						var id = $"{parentId}/{name}";
						var isExpanded = setExpanded.Contains(id);
						return new Node
						{
							Text = show,
							Id = id,
							Children = toNodes(element.Elements(), id, isHidden || !isExpanded),
							IsExpanded = isExpanded,
							IsSelected = !isHidden && id == setSelected
						};
					}

					ImmutableArray<Node> toNodes(IEnumerable<XElement> elements,
						string parentId, bool isHidden)
					{
						return ImmutableArray.CreateRange(elements.Select(
							e => toNode(e, parentId, isHidden)).Where(n => n != null));
					}

					return new Node
					{
						Text = "",
						Id = "",
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
				lastSetExpanded = lastSetExpanded.Add(impl.Id); // todo: sanitize the set
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

		void IPresenter.SetMessage(IMessage message)
		{
			this.message = message;
			changeNotification.Post();
		}

		class Node : IViewTreeNode
		{
			public string Text { get; internal set; }

			public bool IsSelected { get; internal set; }

			public IReadOnlyList<IViewTreeNode> Children { get; internal set; }

			public bool IsExpanded { get; internal set; }

			internal string Id { get; set; }
		};
	};
};
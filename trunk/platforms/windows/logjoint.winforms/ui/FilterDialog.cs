using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace LogJoint.UI
{
	public partial class FilterDialog : Form
	{
		IFilterDialogHost host;
		bool clickLock;
		Filter tempFilter;

		public FilterDialog(IFilterDialogHost host)
		{
			this.host = host;
			InitializeComponent();
			if (host.IsHighlightFilter)
				Text = "Highlight Filter";
			else
				Text = "Display Filter";
		}

		public bool Execute(Filter filter)
		{
			using (tempFilter = filter.Clone(filter.InitialName))
			{
				Read(filter);
				if (ShowDialog() != DialogResult.OK)
					return false;
				Write(filter);
				return true;
			}
		}


		void Read(Filter filter)
		{
			nameTextBox.Text = filter.Name;
			enabledCheckBox.Checked = filter.Enabled;
			templateTextBox.Text = filter.Template;
			matchCaseCheckbox.Checked = filter.MatchCase;
			regExpCheckBox.Checked = filter.Regexp;
			wholeWordCheckbox.Checked = filter.WholeWord;
			actionComboBox.Items.Clear();
			if (host.IsHighlightFilter)
			{
				actionComboBox.Items.Add("Highlight");
				actionComboBox.Items.Add("Exclude from highlighting");
			}
			else
			{
				actionComboBox.Items.Add("Show");
				actionComboBox.Items.Add("Hide");
			}
			actionComboBox.SelectedIndex = (int)filter.Action;
			
			ReadTarget(filter.Target);

			ReadTypes(filter);
		}

		static readonly MessageBase.MessageFlag[] typeFlagsList = {
			MessageBase.MessageFlag.Error | MessageBase.MessageFlag.Content,
			MessageBase.MessageFlag.Warning | MessageBase.MessageFlag.Content,
			MessageBase.MessageFlag.Info | MessageBase.MessageFlag.Content,
			MessageBase.MessageFlag.EndFrame | MessageBase.MessageFlag.StartFrame
		};

		void ReadTypes(Filter filter)
		{
			for (int i = 0; i < typeFlagsList.Length; ++i)
			{
				messagesTypesCheckedListBox.SetItemChecked(i, (typeFlagsList[i] & filter.Types) == typeFlagsList[i]);
			}
			matchFrameContentCheckBox.Checked = filter.MatchFrameContent;
			matchFrameContentCheckBox.Enabled = (filter.Types & MessageBase.MessageFlag.StartFrame) != 0;
		}

		abstract class Node
		{
			public Node(int idx)
			{
				this.Index = idx;
			}
			public abstract void Click(CheckedListBox list);
			public readonly int Index;
			public static readonly int TabSize = 4;
		};

		class AllSources : Node
		{
			public AllSources(int idx) : base(idx) { }
			public override void Click(CheckedListBox list)
			{
				bool f = !list.GetItemChecked(Index);
				for (int i = 0; i < list.Items.Count; ++i)
					list.SetItemChecked(i, f);
			}
			public override string ToString()
			{
				return "All threads from all sources";
			}
		};

		class SourceNode : Node
		{
			public readonly ILogSource Source;

			public SourceNode(int idx, ILogSource src) : base(idx) 
			{
				this.Source = src;
			}
			public override void Click(CheckedListBox list)
			{
				bool f = !list.GetItemChecked(Index);
				for (int i = 0; i < list.Items.Count; ++i)
				{
					object item = list.Items[i];
					if (object.ReferenceEquals(item, this))
					{
						list.SetItemChecked(i, f);
					}
					else if (item is AllSources)
					{
						if (!f)
							list.SetItemChecked(i, false);
					}
					else if (item is ThreadNode)
					{
						if (((ThreadNode)item).Thread.LogSource == Source)
							list.SetItemChecked(i, f);
					}
				}
			}
			public override string ToString()
			{
				return new string(' ', 1 * Node.TabSize) + "All threads from " + Source.DisplayName;
			}
		};

		class ThreadNode : Node
		{
			public readonly IThread Thread;

			public ThreadNode(int idx, IThread t)
				: base(idx) 
			{
				this.Thread = t;
			}
			public override void Click(CheckedListBox list)
			{
				bool f = !list.GetItemChecked(Index);
				for (int i = 0; i < list.Items.Count; ++i)
				{
					object item = list.Items[i];
					if (object.ReferenceEquals(item, this))
					{
						list.SetItemChecked(i, f);
					}
					else if (item is AllSources)
					{
						if (!f)
							list.SetItemChecked(i, false);
					}
					else if (item is SourceNode)
					{
						if (!f && ((SourceNode)item).Source == Thread.LogSource)
							list.SetItemChecked(i, false);
					}
				}
			}
			public override string ToString()
			{
				return new string(' ', 2 * Node.TabSize) + Thread.DisplayName;
			}
		};

		void ReadTarget(FilterTarget target)
		{
			CheckedListBox.ObjectCollection items = threadsCheckedListBox.Items;
			

			items.Clear();

			bool matchesAllSources = target.MatchesAllSources;
			items.Add(new AllSources(items.Count), matchesAllSources);

			foreach (ILogSource s in host.LogSources)
			{
				bool matchesSource = matchesAllSources || target.MatchesSource(s);
				items.Add(new SourceNode(items.Count, s), matchesSource);

				foreach (IThread t in s.Threads.Items)
				{
					bool matchesThread = matchesSource || target.MatchesThread(t);
					items.Add(new ThreadNode(items.Count, t), matchesThread);
				}
			}
		}

		void Write(Filter filter)
		{
			filter.SetUserDefinedName(nameTextBox.Text);
			filter.Action = (FilterAction)actionComboBox.SelectedIndex;
			filter.Enabled = enabledCheckBox.Checked;
			filter.Template = templateTextBox.Text;
			filter.MatchCase = matchCaseCheckbox.Checked;
			filter.Regexp = regExpCheckBox.Checked;
			filter.WholeWord = wholeWordCheckbox.Checked;
			WriteTarget(filter);
			WriteTypes(filter);
		}

		void WriteTypes(Filter filter)
		{
			MessageBase.MessageFlag f = MessageBase.MessageFlag.None;
			for (int i = 0; i < typeFlagsList.Length; ++i)
			{
				if (messagesTypesCheckedListBox.GetItemChecked(i))
					f |= typeFlagsList[i];
			}
			filter.Types = f;
			filter.MatchFrameContent = matchFrameContentCheckBox.Checked;
		}

		void WriteTarget(Filter filter)
		{
			CheckedListBox list = threadsCheckedListBox;

			List<ILogSource> sources = new List<ILogSource>();
			List<IThread> threads = new List<IThread>();

			for (int i = 0; i < list.Items.Count;)
			{
				object item = list.Items[i];
				bool isChecked = list.GetItemChecked(i);

				if (item is AllSources)
				{
					if (isChecked)
					{
						filter.Target = FilterTarget.Default;
						return;
					}
					 ++i;
					continue;
				}

				if (item is SourceNode)
				{
					if (isChecked)
					{
						sources.Add(((SourceNode)item).Source);
						++i;
						while (i < list.Items.Count && list.Items[i] is ThreadNode)
							++i;
					}
					else
					{
						++i;
					}
					continue;
				}

				if (item is ThreadNode)
				{
					if (isChecked)
					{
						threads.Add(((ThreadNode)item).Thread);
					}
					++i;
					continue;
				}

				throw new InvalidOperationException("Unknown node type");
			}

			filter.Target = new FilterTarget(sources, threads);
		}

		private void threadsCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (clickLock)
				return;
			clickLock = true;
			try
			{
				Node n = threadsCheckedListBox.Items[e.Index] as Node;
				if (n != null)
				{
					n.Click(threadsCheckedListBox);
				}
			}
			finally
			{
				clickLock = false;
			}
		}

		private void messagesTypesCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (e.Index == 3)
				matchFrameContentCheckBox.Enabled = e.NewValue == CheckState.Checked;
			SynchronizationContext.Current.Post(state => RefreshNameTextBox(), null);
		}

		private void FilterDialog_Shown(object sender, EventArgs e)
		{
			templateTextBox.Focus();
		}

		private void criteriaInputChanged(object sender, EventArgs e)
		{
			RefreshNameTextBox();
		}

		void RefreshNameTextBox()
		{
			Write(tempFilter);
			if (tempFilter.Name != nameTextBox.Text)
				nameTextBox.Text = tempFilter.Name;
		}
	}

}
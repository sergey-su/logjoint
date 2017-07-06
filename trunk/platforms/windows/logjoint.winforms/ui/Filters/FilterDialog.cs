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
		readonly IFiltersFactory factory;
		IEnumerable<ILogSource> allSources;
		bool isHighlightDialog;
		bool clickLock;
		IFilter tempFilter;

		public FilterDialog(IEnumerable<ILogSource> allSources, bool isHighlightDialog, IFiltersFactory factory)
		{
			this.factory = factory;
			this.allSources = allSources;
			this.isHighlightDialog = isHighlightDialog;
			InitializeComponent();
			if (isHighlightDialog)
				Text = "Highlight Filter";
			else
				Text = "Display Filter";
		}

		public bool Execute(IFilter filter)
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


		void Read(IFilter filter)
		{
			nameTextBox.Text = filter.Name;
			enabledCheckBox.Checked = filter.Enabled;
			templateTextBox.Text = filter.Template;
			matchCaseCheckbox.Checked = filter.MatchCase;
			regExpCheckBox.Checked = filter.Regexp;
			wholeWordCheckbox.Checked = filter.WholeWord;
			actionComboBox.Items.Clear();
			if (isHighlightDialog)
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

		static readonly MessageFlag[] typeFlagsList = {
			MessageFlag.Error | MessageFlag.Content,
			MessageFlag.Warning | MessageFlag.Content,
			MessageFlag.Info | MessageFlag.Content,
			MessageFlag.EndFrame | MessageFlag.StartFrame
		};

		void ReadTypes(IFilter filter)
		{
			for (int i = 0; i < typeFlagsList.Length; ++i)
			{
				messagesTypesCheckedListBox.SetItemChecked(i, (typeFlagsList[i] & filter.Types) == typeFlagsList[i]);
			}
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

		void ReadTarget(IFilterTarget target)
		{
			CheckedListBox.ObjectCollection items = threadsCheckedListBox.Items;
			

			items.Clear();

			bool matchesAllSources = target.MatchesAllSources;
			items.Add(new AllSources(items.Count), matchesAllSources);

			foreach (ILogSource s in allSources)
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

		void Write(IFilter filter)
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

		void WriteTypes(IFilter filter)
		{
			MessageFlag f = MessageFlag.None;
			for (int i = 0; i < typeFlagsList.Length; ++i)
			{
				if (messagesTypesCheckedListBox.GetItemChecked(i))
					f |= typeFlagsList[i];
			}
			filter.Types = f;
		}

		void WriteTarget(IFilter filter)
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
						filter.Target = filter.Factory.CreateFilterTarget();
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

			filter.Target = factory.CreateFilterTarget(sources, threads);
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

	public class FilterDialogView : Presenters.FilterDialog.IView
	{
		IFiltersFactory factory;

		public FilterDialogView(IFiltersFactory factory)
		{
			this.factory = factory;
		}

		bool Presenters.FilterDialog.IView.ShowTheDialog(IFilter forFilter, IEnumerable<ILogSource> allSources, bool isHighlightDialog)
		{
			using (FilterDialog dlg = new FilterDialog(allSources, isHighlightDialog, factory))
			{
				return dlg.Execute(forFilter);
			}
		}
	};
}
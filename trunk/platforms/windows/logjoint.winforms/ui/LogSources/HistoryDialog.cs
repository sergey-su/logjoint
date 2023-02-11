using LogJoint.UI.Presenters.HistoryDialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class HistoryDialog : Form, IView
	{
		readonly IViewModel viewModel;
		bool refreshColumnHeaderPosted;

		public HistoryDialog(IViewModel viewModel)
		{
			InitializeComponent();
			this.viewModel = viewModel;
			viewModel.SetView(this);

			var updateItems = Updaters.Create(() => viewModel.ItemsIgnoringTreeState, UpdateItems);
			var dialogConroller = new ModalDialogController(this);
			var updateVisible = Updaters.Create(() => viewModel.IsVisible, dialogConroller.SetVisibility);
			var updateOpenButton = Updaters.Create(() => viewModel.OpenButtonEnabled, value => openButton.Enabled = value);

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateItems();
				updateVisible();
				updateOpenButton();
			});

			quickSearchTextBox.InnerTextBox.SetViewModel(viewModel.QuickSearchTextBox);
		}

		IViewItem[] SelectedItems
		{
			get
			{
				return
					listView.SelectedItems.OfType<ListViewItem>()
					.Select(i => i.Tag)
					.OfType<TagData>()
					.Select(i => i.PresentationObject)
					.ToArray(); 
			}
			set
			{
				var lookup = value.ToLookup(i => i);
				listView.SelectedIndices.Clear();
				foreach (ListViewItem i in listView.Items)
					if (lookup.Contains(i.Tag is TagData ? ((TagData)i.Tag).PresentationObject : null))
						listView.SelectedIndices.Add(i.Index);
			}
		}

		void UpdateItems(IReadOnlyList<IViewItem> items)
		{
			listView.BeginUpdate();
			listView.Items.Clear();
			listView.Items.AddRange(MakeListViewItems(items, null).ToArray());
			listView.EndUpdate();
		}

		void IView.PutInputFocusToItemsList()
		{
			if (listView.CanFocus)
			{
				listView.Focus();
			}
		}

		static string GetTreeControlText(bool? collapsed)
		{
			return collapsed.HasValue ? (collapsed.Value ? "\u25B6" : "\u25BC") : "";
		}

		private IEnumerable<ListViewItem> MakeListViewItems(IEnumerable<IViewItem> viewItems, TagData parent)
		{
			foreach (var presentationItem in viewItems)
			{
				var tag = new TagData()
				{
					PresentationObject = presentationItem,
					Collapsed = presentationItem.Type == ViewItemType.ItemsContainer ? true : new bool?(),
					Parent = parent,
					Children = new List<TagData>()
				};

				bool isHidden = false;
				bool isCollapsibleChild = false;

				if (parent != null)
				{
					parent.Children.Add(tag);
					isHidden = parent.Collapsed.GetValueOrDefault(false);
					isCollapsibleChild = parent.Collapsed != null;
				}

				var li = new ListViewItem()
				{
					Text = GetTreeControlText(tag.Collapsed),
					ForeColor = presentationItem.Type == ViewItemType.Comment ? Color.Gray : Color.Black,
					Tag = tag
				};
				li.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = (isCollapsibleChild ? "  " : "") + presentationItem.Text });
				li.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = presentationItem.Annotation });
				tag.ViewObject = li;

				if (!isHidden)
				{
					yield return li;
				}

				if (presentationItem.Children != null)
				{
					foreach (var child in MakeListViewItems(presentationItem.Children.OfType<IViewItem>(), tag))
						yield return child;
				}
			}
		}

		private async void RefreshHeaders()
		{
			if (refreshColumnHeaderPosted)
				return;
			refreshColumnHeaderPosted = true;
			await Task.Yield();
			entryColumnHeader.Width = listView.ClientSize.Width - annotationColumnHeader.Width - treeControlColumnHeader.Width - 10;
			refreshColumnHeaderPosted = false;
		}


		private void listView_Layout(object sender, LayoutEventArgs e)
		{
			RefreshHeaders();
		}

		private void listView_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
		{
			RefreshHeaders();
		}

		private void openButton_Click(object sender, EventArgs e)
		{
			viewModel.OnOpenClicked();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			viewModel.OnCancelClicked();
		}

		private void listView_DoubleClick(object sender, EventArgs e)
		{
			viewModel.OnDoubleClick();
		}

		private void HistoryDialog_Shown(object sender, EventArgs e)
		{
			viewModel.OnDialogShown();
		}

		private void HistoryDialog_Closing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			viewModel.OnCancelClicked();
		}

		private void HistoryDialog_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.F)
				viewModel.OnFindShortcutPressed();
		}

		private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			viewModel.OnSelect(SelectedItems);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			viewModel.OnClearHistoryButtonClicked();
		}

		private void listView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			if (e.ColumnIndex == treeControlColumnHeader.Index)
			{
				e.NewWidth = treeControlColumnHeader.Width;
			}
			e.Cancel = true;
		}

		void listView_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			var item = listView.GetItemAt(e.X, e.Y);
			if (item == null)
				return;
			if (e.X > treeControlColumnHeader.Width)
				return;
			var tag = item.Tag as TagData;
			if (tag == null || tag.Collapsed == null)
				return;
			ChangeCollapsedState(tag, !tag.Collapsed.Value);
		}

		private void ChangeCollapsedState(TagData tag, bool targetState)
		{
			if (tag.Collapsed.Value == targetState)
				return;
			listView.BeginUpdate();
			if (tag.Collapsed.Value)
			{
				tag.Collapsed = false;
				tag.ViewObject.Text = GetTreeControlText(tag.Collapsed);
				foreach (var c in tag.Children.ZipWithIndex())
					listView.Items.Insert(tag.ViewObject.Index + c.Key + 1, c.Value.ViewObject);
			}
			else
			{
				tag.Collapsed = true;
				tag.ViewObject.Text = GetTreeControlText(tag.Collapsed);
				foreach (var c in tag.Children)
					c.ViewObject.Remove();
			}
			listView.EndUpdate();
		}

		void listView_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			bool? collapse = null;
			if (e.KeyCode == Keys.Left)
				collapse = true;
			else if (e.KeyCode == Keys.Right)
				collapse = false;
			if (collapse == null)
				return;
			if (listView.SelectedItems.Count != 1)
				return;
			var tag = listView.SelectedItems[0].Tag as TagData;
			if (tag == null || tag.Collapsed == null)
				return;
			ChangeCollapsedState(tag, collapse.Value);
		}

		class TagData
		{
			public ListViewItem ViewObject;
			public IViewItem PresentationObject;
			public bool? Collapsed;
			public TagData Parent;
			public List<TagData> Children;
		};
	}
}

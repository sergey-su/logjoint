using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.TagsList
{
	public class TagsListPresenter: IPresenter, IViewEvents
	{
		readonly IView view;
		readonly Dictionary<string, bool> tags = new Dictionary<string, bool>();

		public TagsListPresenter(IView view)
		{
			this.view = view;
			view.SetEventsHandler (this);
		}

		public event EventHandler SelectedTagsChanged;

		void IPresenter.Edit(string focusedTag)
		{
			this.Edit(focusedTag);
		}

		void IPresenter.SetIsSingleLine (bool value)
		{
			view.SetSingleLine (value);
		}

		void IPresenter.SetTags (IEnumerable<string> tags, ISet<string> selectedTags)
		{
			this.tags.Clear();
			foreach (var t in tags)
				this.tags[t] = selectedTags.Contains(t);
			UpdateTagsLabel();
		}

		IEnumerable<string> IPresenter.SelectedTags 
		{
			get { return tags.Where(t => t.Value).Select(t => t.Key); }
		}

		void IViewEvents.OnEditLinkClicked()
		{
			Edit(null);
		}

		void Edit(string focusedTag)
		{
			var selected = view.RunEditDialog(tags, focusedTag);
			if (selected == null)
				return;
			bool selectionChanged = false;
			var selectedLookup = selected.ToLookup(t => t);
			foreach (var t in tags.Keys.ToArray())
			{
				var value = selectedLookup.Contains(t);
				if (value != tags[t])
				{
					selectionChanged = true;
					tags[t] = value;
				}
			}
			if (selectionChanged)
			{
				UpdateTagsLabel();
				SelectedTagsChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		void UpdateTagsLabel()
		{
			var tagsString = string.Join(", ", this.tags.Where(t => t.Value).Select(t => t.Key));
			var clickablePrefix = "tags";
			var selectedCount = tags.Count(t => t.Value);
			if (tags.Count > selectedCount)
				clickablePrefix += string.Format(" ({0} out of {1})", selectedCount, tags.Count);
			view.SetText(
				string.Format("{0}: {1}", clickablePrefix, tagsString == "" ? "<none selected>" : tagsString),
				0,
				clickablePrefix.Length
			);
		}
	}
}

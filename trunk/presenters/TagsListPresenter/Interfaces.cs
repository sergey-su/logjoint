using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.TagsList
{
	public interface IPresenter
	{
		void SetIsSingleLine (bool value);
		void SetTags (IEnumerable<string> tags, ISet<string> selectedTags);
		IEnumerable<string> SelectedTags { get; }
		void Edit(string focusedTag = null);
		event EventHandler SelectedTagsChanged;
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);

		void SetText(string value, int clickablePartBegin, int clickablePartLength);
		HashSet<string> RunEditDialog(Dictionary<string, bool> tags, string focusedTag);
		void SetSingleLine (bool value);
	};

	public interface IViewEvents
	{
		void OnEditLinkClicked();
	};
}

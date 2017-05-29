using LogJoint.UI.Presenters.TagsList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class TagsListControl : UserControl, IView
	{
		IViewEvents eventsHandler;

		public TagsListControl()
		{
			InitializeComponent();
		}

		private void allTagsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnEditLinkClicked();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetText(string value, int clickablePartBegin, int clickablePartLength)
		{
			allTagsLinkLabel.Text = value;
			allTagsLinkLabel.LinkArea = new LinkArea(clickablePartBegin, clickablePartLength);
		}

		void IView.SetSingleLine(bool value)
		{
		}
		HashSet<string> IView.RunEditDialog(Dictionary<string, bool> tags)
		{
			using (var dlg = new AllTagsDialog())
				return dlg.SelectTags(tags);
		}
	}
}

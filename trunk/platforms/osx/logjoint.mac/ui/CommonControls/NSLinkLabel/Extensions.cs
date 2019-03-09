using System.Linq;

namespace LogJoint.UI
{
	public static class NSLinkLabelExtensions
	{
		public static void SetAttributedContents(this NSLinkLabel lbl, string value)
		{
			var parsed = Presenters.LinkLabelUtils.ParseLinkLabelString (value);
			lbl.StringValue = parsed.Text;
			lbl.Links = parsed.Links.Select (
				l => new NSLinkLabel.Link (l.Item1, l.Item2, l.Item3)).ToList ();
		}
	}
}
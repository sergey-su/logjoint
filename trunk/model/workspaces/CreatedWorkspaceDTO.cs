using System.Xml.Serialization;

namespace LogJoint.Workspaces
{
	[XmlRoot("workspace")]
	public class CreatedWorkspaceDTO
	{
		public string id { get; set; }
		public IdAlterationReason? idAlterationReason { get; set; }
		public string selfUrl { get; set; }
		public string entriesArchiveUrl { get; set; }
		public string selfWebUrl { get; set; }
	}

	public enum IdAlterationReason
	{
		conflict,
		validation
	};
}

namespace LogJoint.Drawing
{
	public partial class StringFormat
	{
		internal System.Drawing.StringFormat format;

		partial void Init(System.Drawing.StringFormat format)
		{
			this.format = format;
		}
	};
}
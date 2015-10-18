using System.Drawing;

namespace LogJoint.Drawing
{
	partial class StringFormat
	{
		internal StringAlignment horizontalAlignment;
		internal StringAlignment verticalAlignment;

		partial void Init(StringAlignment horizontalAlignment, StringAlignment verticalAlignment)
		{
			this.horizontalAlignment = horizontalAlignment;
			this.verticalAlignment = verticalAlignment;
		}
	};
}
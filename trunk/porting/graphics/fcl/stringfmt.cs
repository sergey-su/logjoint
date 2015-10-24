using System.Drawing;

namespace LogJoint.Drawing
{
	public partial class StringFormat
	{
		internal System.Drawing.StringFormat format;

		partial void Init(StringAlignment horizontalAlignment, StringAlignment verticalAlignment, StringTrimming trimming)
		{
			format = new System.Drawing.StringFormat()
			{
				Alignment = horizontalAlignment,
				LineAlignment = verticalAlignment,
				Trimming = trimming,
				FormatFlags = StringFormatFlags.LineLimit
			};
		}

		partial void Init(System.Drawing.StringFormat format)
		{
			this.format = format;
		}
	};
}
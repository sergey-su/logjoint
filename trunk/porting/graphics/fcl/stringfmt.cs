using System.Drawing;

namespace LogJoint.Drawing
{
	public partial class StringFormat
	{
		internal System.Drawing.StringFormat format;

		partial void Init(StringAlignment horizontalAlignment, StringAlignment verticalAlignment, LineBreakMode lineBreakMode)
		{
			format = new System.Drawing.StringFormat()
			{
				Alignment = horizontalAlignment,
				LineAlignment = verticalAlignment,
				FormatFlags = StringFormatFlags.LineLimit | StringFormatFlags.NoFontFallback
			};
			if (lineBreakMode == LineBreakMode.WrapChars)
				format.Trimming = StringTrimming.Character;
			else if (lineBreakMode == LineBreakMode.WrapWords)
				format.Trimming = StringTrimming.Word;
			else if (lineBreakMode == LineBreakMode.SingleLineEndEllipsis)
				format.Trimming = StringTrimming.EllipsisCharacter;
		}

		partial void Init(System.Drawing.StringFormat format)
		{
			this.format = format;
		}
	};
}
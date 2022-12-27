using SD = System.Drawing;

namespace LogJoint.Drawing
{
	public partial class StringFormat
	{
		internal SD.StringFormat format;

		partial void Init(StringAlignment horizontalAlignment, StringAlignment verticalAlignment, LineBreakMode lineBreakMode)
		{
			format = new SD.StringFormat()
			{
				Alignment = (SD.StringAlignment)horizontalAlignment,
				LineAlignment = (SD.StringAlignment)verticalAlignment,
				FormatFlags = SD.StringFormatFlags.LineLimit | SD.StringFormatFlags.NoFontFallback
			};
			if (lineBreakMode == LineBreakMode.WrapChars)
				format.Trimming = SD.StringTrimming.Character;
			else if (lineBreakMode == LineBreakMode.WrapWords)
				format.Trimming = SD.StringTrimming.Word;
			else if (lineBreakMode == LineBreakMode.SingleLineEndEllipsis)
				format.Trimming = SD.StringTrimming.EllipsisCharacter;
		}

		partial void Init(SD.StringFormat f)
		{
			this.format = f;
		}
	};
}
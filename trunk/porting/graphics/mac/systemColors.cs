using AppKit;

namespace LogJoint.Drawing
{
	partial class SystemColorsImpl
	{
		partial void Init()
		{
			text = NSColor.Text.ToColor;
			textBackground = NSColor.TextBackground.ToColor;
		}
	};
}
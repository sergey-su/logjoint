using System.Drawing;

namespace LogJoint.Drawing
{
	partial class SystemColorsImpl
	{
		partial void Init()
		{
			text = () => System.Drawing.SystemColors.ControlText;
			textBackground = () => Color.White;
		}
	};
}
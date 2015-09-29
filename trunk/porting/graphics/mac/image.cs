using System.Drawing;
using MonoMac.CoreImage;
using MonoMac.CoreGraphics;

namespace LogJoint.Drawing
{
	partial class Image
	{
		internal CGImage image;

		public void Dispose()
		{
		}

		partial void Init(MonoMac.CoreGraphics.CGImage img)
		{
			this.image = img;
		}

		partial void SizeImp(ref Size ret)
		{
			ret = new Size(image.Width, image.Height);
		}
	};
}
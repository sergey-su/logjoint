using System.Drawing;
using CoreImage;
using CoreGraphics;

namespace LogJoint.Drawing
{
	partial class Image
	{
		internal CGImage image;

		public void Dispose()
		{
		}

		partial void Init(CoreGraphics.CGImage img)
		{
			this.image = img;
		}

		partial void SizeImp(ref Size ret)
		{
			ret = new Size((int)image.Width, (int)image.Height);
		}
	};
}

namespace LogJoint.Drawing
{
    public partial class Image
    {
        internal System.Drawing.Image image;

        public void Dispose()
        {
            image.Dispose();
        }

        partial void Init(System.Drawing.Image img)
        {
            this.image = img;
        }

        partial void SizeImp(ref Size ret)
        {
            ret = image.Size.ToSize();
        }
    };
}
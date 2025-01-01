
namespace LogJoint.Drawing
{
    public static class ColorExtensions
    {
        public static Color MakeDarker(this Color cl, byte delta)
        {
            return Color.FromArgb(cl.A, Dec(cl.R, delta), Dec(cl.G, delta), Dec(cl.B, delta));
        }

        public static Color MakeLighter(this Color cl, byte delta)
        {
            return Color.FromArgb(cl.A, Inc(cl.R, delta), Inc(cl.G, delta), Inc(cl.B, delta));
        }

        static byte Dec(byte v, byte delta)
        {
            if (v <= delta)
                return 0;
            return (byte)(v - delta);
        }

        static byte Inc(byte v, byte delta)
        {
            if (0xff - v <= delta)
                return 0xff;
            return (byte)(v + delta);
        }

        public static string ToHtmlColor(this Color cl)
        {
            return string.Format("#{0:x6}", cl.ToArgb() & 0xffffff);
        }
    };
}

using System;

namespace LogJoint.Drawing
{
    partial class Brush
    {
        internal System.Drawing.Brush b;

        partial void Init(ColorRef color)
        {
            b = new System.Drawing.SolidBrush(color.Value.ToSystemDrawingObject());
        }

        public void Dispose()
        {
            b.Dispose();
        }
    };
}
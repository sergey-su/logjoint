namespace LogJoint.Drawing
{
	public partial class Matrix
	{
		CoreGraphics.CGAffineTransform t;

		partial void Init(Matrix other)
		{
			t = other != null ? other.t : CoreGraphics.CGAffineTransform.MakeIdentity();
		}

		partial void TranslateImpl (float dx, float dy, MatrixOrder order = MatrixOrder.Prepend)
		{
			if (order == MatrixOrder.Append) 
				t = t * CoreGraphics.CGAffineTransform.MakeTranslation (dx, dy);
			else
				t = CoreGraphics.CGAffineTransform.MakeTranslation (dx, dy) * t;
		}

		partial void ScaleImpl (float sx, float sy)
		{
			t.Scale (sx, sy);
		}

		partial void TransformVectorsImpl (PointF [] pts)
		{
			var tmp = t;
			tmp.x0 = 0;
			tmp.y0 = 0;
			TransformPoints(tmp, pts);
		}

		partial void TransformPointsImpl (PointF [] pts)
		{
			TransformPoints(t, pts);
		}

		partial void InvertImpl ()
		{
			t = t.Invert ();
		}

		static void TransformPoints (CoreGraphics.CGAffineTransform t, PointF [] pts)
		{
			for (int i = 0; i < pts.Length; ++i) {
				var p = pts [i];
				var cgp = t.TransformPoint (new CoreGraphics.CGPoint (p.X, p.Y));
				pts [i] = new PointF ((float)cgp.X, (float)cgp.Y);
			}
		}
	};
}

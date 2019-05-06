using SDD2 = System.Drawing.Drawing2D;
using SDD = System.Drawing;

namespace LogJoint.Drawing
{
	public partial class Matrix
	{
		SDD2.Matrix t;

		partial void Init(Matrix other)
		{
			t = other != null ? other.t.Clone() : new SDD2.Matrix();
		}

		partial void TranslateImpl (float dx, float dy, MatrixOrder order)
		{
			t.Translate(dx, dy, order == MatrixOrder.Append ? SDD2.MatrixOrder.Append : SDD2.MatrixOrder.Prepend);
		}

		partial void ScaleImpl (float sx, float sy)
		{
			t.Scale (sx, sy);
		}

		partial void TransformVectorsImpl(PointF [] pts)
		{
			var tmp = ToSDDPoints(pts);
			t.TransformVectors(tmp);
			Copy(tmp, pts);
		}

		partial void TransformPointsImpl(PointF [] pts)
		{
			var tmp = ToSDDPoints(pts);
			t.TransformPoints(tmp);
			Copy(tmp, pts);
		}

		partial void InvertImpl ()
		{
			t.Invert();
		}

		partial void DisposeImpl()
		{
			t.Dispose();
		}

		static SDD.PointF[] ToSDDPoints(PointF [] pts)
		{
			var ret = new SDD.PointF[pts.Length];
			for (var i = 0; i < pts.Length; ++i)
				ret[i] = new SDD.PointF(pts[i].X, pts[i].Y);
			return ret;
		}

		static void Copy(SDD.PointF[] src, PointF[] dest)
		{
			for (var i = 0; i < src.Length; ++i)
				dest[i] = new PointF(src[i].X, src[i].Y);
		}
	};
}

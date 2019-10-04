using SDD2 = System.Drawing.Drawing2D;
using SDD = System.Drawing;
using System;

namespace LogJoint.Drawing
{
	public class Matrix : IMatrix, IDisposable
	{
		private readonly SDD2.Matrix t;

		public Matrix(Matrix other)
		{
			t = other != null ? other.t.Clone() : new SDD2.Matrix();
		}

		void IMatrix.Translate(float dx, float dy, MatrixOrder order)
		{
			t.Translate(dx, dy, order == MatrixOrder.Append ? SDD2.MatrixOrder.Append : SDD2.MatrixOrder.Prepend);
		}

		void IMatrix.Scale(float sx, float sy)
		{
			t.Scale(sx, sy);
		}

		void IMatrix.TransformVectors(PointF[] pts)
		{
			var tmp = ToSDDPoints(pts);
			t.TransformVectors(tmp);
			Copy(tmp, pts);
		}

		void IMatrix.TransformPoints(PointF[] pts)
		{
			var tmp = ToSDDPoints(pts);
			t.TransformPoints(tmp);
			Copy(tmp, pts);
		}

		void IMatrix.Invert()
		{
			t.Invert();
		}

		IMatrix IMatrix.Clone()
		{
			return new Matrix(this);
		}

		void IDisposable.Dispose()
		{
			t.Dispose();
		}

		static SDD.PointF[] ToSDDPoints(PointF[] pts)
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

		public class Factory : IMatrixFactory
		{
			IMatrix IMatrixFactory.CreateIdentity()
			{
				return new Matrix(null);
			}
		};
	};
}

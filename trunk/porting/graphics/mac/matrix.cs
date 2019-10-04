using System;

namespace LogJoint.Drawing
{
	public class Matrix: IMatrix, IDisposable
	{
		CoreGraphics.CGAffineTransform t;

		public Matrix(Matrix other)
		{
			t = other != null ? other.t : CoreGraphics.CGAffineTransform.MakeIdentity();
		}

		void IMatrix.Translate (float dx, float dy, MatrixOrder order)
		{
			if (order == MatrixOrder.Append) 
				t = t * CoreGraphics.CGAffineTransform.MakeTranslation (dx, dy);
			else
				t = CoreGraphics.CGAffineTransform.MakeTranslation (dx, dy) * t;
		}

		void IMatrix.Scale (float sx, float sy)
		{
			t.Scale (sx, sy);
		}

		void IMatrix.TransformVectors (PointF [] pts)
		{
			var tmp = t;
			tmp.x0 = 0;
			tmp.y0 = 0;
			TransformPoints(tmp, pts);
		}

		void IMatrix.TransformPoints (PointF [] pts)
		{
			TransformPoints(t, pts);
		}

		void IMatrix.Invert ()
		{
			t = t.Invert ();
		}

		IMatrix IMatrix.Clone()
		{
			return new Matrix(this);
		}

		void IDisposable.Dispose()
		{
		}


		void TransformPoints (CoreGraphics.CGAffineTransform t, PointF [] pts)
		{
			for (int i = 0; i < pts.Length; ++i) {
				var p = pts [i];
				var cgp = t.TransformPoint (new CoreGraphics.CGPoint (p.X, p.Y));
				pts [i] = new PointF ((float)cgp.X, (float)cgp.Y);
			}
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

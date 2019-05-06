using System;

namespace LogJoint.Drawing
{
	public enum MatrixOrder
	{
		Prepend,
		Append
	};

	public partial class Matrix: IDisposable
	{
		public Matrix()
		{
			Init(null);
		}

		public void Dispose()
		{
			DisposeImpl();
		}

		public void Translate(float dx, float dy, MatrixOrder order = MatrixOrder.Prepend)
		{
			TranslateImpl(dx, dy, order);
		}

		public void Scale (float sx, float sy)
		{
			ScaleImpl(sx, sy);
		}

		public void TransformVectors (PointF [] pts)
		{
			TransformVectorsImpl(pts);
		}

		public void TransformPoints (PointF [] pts)
		{
			TransformPointsImpl(pts);
		}

		public void Invert ()
		{
			InvertImpl();
		}

		public Matrix Clone ()
		{
			var m = new Matrix();
			m.Init(this);
			return m;
		}


		partial void Init(Matrix other);
		partial void TranslateImpl(float dx, float dy, MatrixOrder order = MatrixOrder.Prepend);
		partial void ScaleImpl(float sx, float sy);
		partial void TransformVectorsImpl(PointF[] pts);
		partial void TransformPointsImpl(PointF[] pts);
		partial void InvertImpl();
		partial void DisposeImpl();
	};
}

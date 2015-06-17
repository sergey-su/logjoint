using System;

namespace LogJoint
{
	public class ConnectionParamsReadOnlyView : IConnectionParams
	{
		public ConnectionParamsReadOnlyView(IConnectionParams underlyingParams)
		{
			if (underlyingParams == null)
				throw new ArgumentNullException("underlyingParams");
			this.underlyingParams = underlyingParams;
		}

		IConnectionParams underlyingParams;

		public string this[string key]
		{
			get
			{
				return underlyingParams[key];
			}
			set
			{
				AssertOnWrite();
			}
		}

		public void AssignFrom(IConnectionParams other)
		{
			AssertOnWrite();
		}

		public bool AreEqual(IConnectionParams other)
		{
			return underlyingParams.AreEqual(other);
		}

		public IConnectionParams Clone(bool makeWritebleCopyIfReadonly)
		{
			var tmp = underlyingParams.Clone();
			if (makeWritebleCopyIfReadonly)
				return tmp;
			else
				return new ConnectionParamsReadOnlyView(tmp);
		}

		public string ToNormalizedString()
		{
			return underlyingParams.ToNormalizedString();
		}

		public bool IsReadOnly { get { return true; } }

		void AssertOnWrite()
		{
			throw new InvalidOperationException("Cannot change readonly connection params");
		}

		public override string ToString()
		{
			return underlyingParams.ToString();
		}
	};
}

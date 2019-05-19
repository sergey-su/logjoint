using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LogJoint
{
	public interface IConnectionParams
	{
		string this[string key] { get; set; }
		void AssignFrom(IConnectionParams other);
		bool AreEqual(IConnectionParams other);
		IConnectionParams Clone(bool makeWritebleCopyIfReadonly = false);
		string ToNormalizedString();
		bool IsReadOnly { get; }
	};

	public class InvalidConnectionParamsException : Exception
	{
		public InvalidConnectionParamsException(string msg) : base(msg) { }
	};
}

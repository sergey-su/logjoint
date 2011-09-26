using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public class BadUserCodeException : Exception
	{
		public BadUserCodeException(string message, string fullCode, string errorMessage, string allErrors, BadFieldDescription badField) :
			base(message)
		{
			this.FullCode = fullCode;
			this.ErrorMessage = errorMessage;
			this.AllErrors = allErrors;
			this.BadField = badField;
		}

		public readonly string FullCode;
		public readonly string ErrorMessage;
		public readonly string AllErrors;

		public BadFieldDescription BadField;

		public class BadFieldDescription
		{
			public readonly string FieldName;
			public readonly int ErrorPosition;
			public BadFieldDescription(string fieldName, int errorPos)
			{
				this.FieldName = fieldName;
				this.ErrorPosition = errorPos;
			}
		};
	};
}

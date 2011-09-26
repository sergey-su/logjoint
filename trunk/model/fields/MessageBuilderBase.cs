using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Internal
{
	public abstract class __MessageBuilder : UserCodeHelperFunctions
	{
		internal DateTime __sourceTime;
		internal long __position;

		protected virtual int INPUT_FIELDS_COUNT()
		{
			return 0;
		}

		protected virtual StringSlice INPUT_FIELD_VALUE(int idx)
		{
			return new StringSlice();
		}

		protected virtual string INPUT_FIELD_NAME(int idx)
		{
			return "";
		}

		protected override DateTime SOURCE_TIME()
		{
			return __sourceTime;
		}

		protected long POSITION()
		{
			return __position;
		}

		public enum Severity
		{
			Info = MessageBase.MessageFlag.Info,
			Warning = MessageBase.MessageFlag.Warning,
			Error = MessageBase.MessageFlag.Error,
		};

		public enum EntryType
		{
			Content,
			FrameBegin,
			FrameEnd,
		};

		public abstract MessageBase MakeMessage(IMessagesBuilderCallback callback, MakeMessageFlags flags);
		public abstract __MessageBuilder Clone();
		public abstract void SetExtensionByName(string name, object ext);

		public abstract void SetInputFieldByIndex(int __index, StringSlice __value);
		public abstract void ResetFieldValues();
	};
}

using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Internal
{
	public abstract class __MessageBuilder : StringSliceAwareUserCodeHelperFunctions 
	{
		internal DateTime __sourceTime;
		internal long __position;
		internal ITimeOffsets __timeOffsets = TimeOffsets.Empty;

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

		protected DateTime __ApplyTimeOffset(DateTime d)
		{
			return __timeOffsets.Get(d);
		}

		public enum Severity
		{
			Info = MessageFlag.Info,
			Warning = MessageFlag.Warning,
			Error = MessageFlag.Error,
		};

		public enum EntryType
		{
			Content,
			FrameBegin,
			FrameEnd,
		};

		public abstract IMessage MakeMessage(IMessagesBuilderCallback callback, MakeMessageFlags flags);
		public abstract __MessageBuilder Clone();
		public abstract void SetExtensionByName(string name, object ext);

		public abstract void SetInputFieldByIndex(int __index, StringSlice __value);
		public abstract void ResetFieldValues();
	};
}

using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Internal
{
	public abstract class __MessageBuilder : StringSliceAwareUserCodeHelperFunctions 
	{
		internal DateTime __sourceTime;
		internal long __position;
		internal TimeSpan __timeOffset;

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

		protected TimeSpan TIME_OFFSET()
		{
			return __timeOffset;
		}

		protected DateTime __ApplyTimeOffset(DateTime d)
		{
			try
			{
				return d + __timeOffset;
			}
			catch (ArgumentOutOfRangeException)
			{
				if (__timeOffset.Ticks < 0)
				{
					if ((d - DateTime.MinValue) < -__timeOffset)
						return DateTime.MinValue;
				}
				if (__timeOffset.Ticks > 0)
				{
					if ((DateTime.MaxValue - d) < __timeOffset)
						return DateTime.MaxValue;
				}
				throw new ArgumentOutOfRangeException(
					string.Format("Time offset {0} can not be applied to DateTime {1}", __timeOffset, d));
			}
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

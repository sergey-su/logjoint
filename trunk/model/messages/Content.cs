
namespace LogJoint
{
	public class Content : MessageBase, IContent
	{
		public Content(long position, IThread t, MessageTimestamp time, StringSlice msg, SeverityFlag s)
			:
			base(position, t, time)
		{
			this.message = msg;
			this.flags = MessageFlag.Content | (MessageFlag)s;
		}

		SeverityFlag IContent.Severity
		{
			get
			{
				return (SeverityFlag)(flags & MessageFlag.ContentTypeMask);
			}
		}

		#region Protected overrides

		protected override void DoVisit(IMessageBaseVisitor visitor)
		{
			visitor.Visit(this);
		}

		protected override StringSlice DoGetText()
		{
			return message;
		}
		protected override int DoReallocateTextBuffer(string newBuffer, int positionWithinBuffer)
		{
			message = new StringSlice(newBuffer, positionWithinBuffer, message.Length);
			return positionWithinBuffer + message.Length;
		}

		#endregion


		StringSlice message;
	};

}


namespace LogJoint
{
	public class Content : MessageBase, IContent
	{
		public Content(long position, IThread t, MessageTimestamp time, StringSlice msg, SeverityFlag s, StringSlice rawText = new StringSlice())
			:
			base(position, t, time, rawText)
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

		IMessage IMessage.Clone()
		{
			IContent intf = this;
			return new Content(intf.Position, intf.Thread, intf.Time, message, intf.Severity, this.DoGetRawText());
		}

		#region Protected overrides

		protected override void DoVisit(IMessageVisitor visitor)
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


namespace LogJoint
{
	public sealed class FrameEnd : MessageBase, IFrameEnd
	{
		public FrameEnd(long position, long endPosition, IThread thread, MessageTimestamp time, StringSlice rawText = new StringSlice())
			:
			base(position, endPosition, thread, time, rawText)
		{
			this.flags = MessageFlag.EndFrame;
		}


		IMessage IMessage.Clone()
		{
			IMessage intf = this;
			return new FrameEnd(intf.Position, intf.EndPosition, intf.Thread, intf.Time, DoGetRawText());
		}

		protected override void DoVisit(IMessageVisitor visitor)
		{
			visitor.Visit(this);
		}


		IFrameBegin IFrameEnd.Start { get { return start; } }

		void IFrameEnd.SetCollapsed(bool value) { SetCollapsedFlag(value); }

		void IFrameEnd.SetStart(IFrameBegin start) { this.start = start; }


		#region Protected overrides

		protected override StringSlice DoGetText()
		{
			return start != null ? start.Name : StringSlice.Empty;
		}
		protected override StringSlice DoGetRawText()
		{
			return start != null ? start.RawText : base.DoGetRawText();
		}
		#endregion

		IFrameBegin start;
	};

}

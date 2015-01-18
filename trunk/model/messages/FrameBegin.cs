
namespace LogJoint
{
	public sealed class FrameBegin : MessageBase, IFrameBegin
	{
		public FrameBegin(long position, IThread t, MessageTimestamp time, StringSlice name)
			:
			base(position, t, time)
		{
			this.name = name;
			this.flags = MessageFlag.StartFrame;
		}

		IMessage IMessage.Clone()
		{
			IMessage intf = this;
			return new FrameBegin(intf.Position, intf.Thread, intf.Time, name);
		}

		void IFrameBegin.SetEnd(IFrameEnd e)
		{
			end = e;
		}
		StringSlice IFrameBegin.Name { get { return name; } }
		bool IFrameBegin.Collapsed
		{
			get
			{
				return (flags & MessageFlag.Collapsed) != 0;
			}
			set
			{
				SetCollapsedFlag(value);
				if (end != null)
					end.SetCollapsed(value);
			}
		}
		IFrameEnd IFrameBegin.End { get { return end; } }


		#region Pretected overrides

		protected override void DoVisit(IMessageVisitor visitor) { visitor.Visit(this); }

		protected override StringSlice DoGetText() { return name; }
		protected override int DoReallocateTextBuffer(string newBuffer, int positionWithinBuffer)
		{
			name = new StringSlice(newBuffer, positionWithinBuffer, name.Length);
			return positionWithinBuffer + name.Length;
		}

		#endregion

		public static string GetCollapseMark(bool collapsed)
		{
			return collapsed ? "{...}" : "{";
		}

		StringSlice name;
		IFrameEnd end;
	};
}

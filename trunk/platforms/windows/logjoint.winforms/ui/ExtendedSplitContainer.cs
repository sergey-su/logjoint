using System.Reflection;

namespace  System.Windows.Forms
{
	public class ExtendedSplitContainer : SplitContainer
	{
		public void BeginSplitting()
		{
			var pos = this.PointToClient(Cursor.Position);
			try
			{
				// hack: invoke private inherited methods to start moving the splitter
				var type = typeof(SplitContainer);
				type.InvokeMember("SplitBegin", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
					null, this, new object[] { pos.X, pos.Y });
				type.InvokeMember("_splitterClick", BindingFlags.Instance | BindingFlags.SetField | BindingFlags.NonPublic,
					null, this, new object[] { true });
			}
			catch (Exception)
			{
				// hack does not work anymore :(
				// catch it not to crash the whole process.
			}
			Cursor.Current = GetSplitCursor();
		}

		protected override void WndProc(ref Message m)
		{
			int WM_SETCURSOR = 0x0020;

			if (m.Msg == WM_SETCURSOR && m.WParam == this.Handle)
			{
				Cursor.Current = GetSplitCursor();
			}
			else
			{
				base.WndProc(ref m);
			}
		}

		Cursor GetSplitCursor()
		{
			if (Orientation == Forms.Orientation.Horizontal)
				return Cursors.SizeNS;
			else
				return Cursors.SizeWE;
		}
	}
}

using System.Reflection;

namespace  System.Windows.Forms
{
	class ExtendedSplitContainer : SplitContainer
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
				type.InvokeMember("splitterClick", BindingFlags.Instance | BindingFlags.SetField | BindingFlags.NonPublic,
					null, this, new object[] { true });
			}
			catch (Exception)
			{
				// hack does not work anymore :(
				// catch it not to crash the whole process.
			}
			Cursor.Current = Cursors.HSplit;
		}

		protected override void WndProc(ref Message m)
		{
			int WM_SETCURSOR = 0x0020;

			if (m.Msg == WM_SETCURSOR && m.WParam == this.Handle)
			{
				Cursor.Current = Cursors.HSplit;
			}
			else
			{
				base.WndProc(ref m);
			}
		}
	}
}

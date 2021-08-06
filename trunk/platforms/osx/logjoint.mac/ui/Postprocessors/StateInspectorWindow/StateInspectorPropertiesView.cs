using System;
using Foundation;
using AppKit;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	[Register("StateInspectorPropertiesView")]
	class StateInspectorPropertiesView: NSTableView
	{
		StateInspectorWindowController owner;

		public StateInspectorPropertiesView (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public StateInspectorPropertiesView (NSCoder coder) : base (coder)
		{
		}

		public void Init(StateInspectorWindowController owner)
		{
			this.owner = owner;
		}

		[Export ("validateMenuItem:")]
		bool OnValidateMenuItem (NSMenuItem item)
		{
			return SelectedRow >= 0;
		}

		[Export ("copy:")]
		void OnCopy (NSObject theEvent)
		{
			if (SelectedRow >= 0)
				owner.ViewModel.OnPropertyCellCopyShortcutPressed ();
		}

		public override void MouseDown (NSEvent e)
		{
			base.MouseDown (e);

			if (e.ClickCount == 2 && SelectedRow >= 0)
				owner.ViewModel.OnPropertiesRowDoubleClicked ((int)SelectedRow);
		}
	};
}
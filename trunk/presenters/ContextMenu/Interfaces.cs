using LogJoint.Drawing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
	public class ContextMenuItem
	{
		public string Text { get; internal set; }
		public Action Click { get; internal set; }

		public ContextMenuItem(string text, Action click)
		{
			Text = text;
			Click = click;
		}
	};

	public interface IContextMenu
	{
		Task ShowMenu(IReadOnlyList<ContextMenuItem> items, PointF? screenLocation);
	}

	namespace ContextMenu
	{
		public interface IViewModel
		{
			IChangeNotification ChangeNotification { get; }
			IViewState ViewState { get; } // if null, the menu is not visible
			void OnClickOutside();
			void OnItemHovered(int index);
			void OnItemUnhovered(int index);
			void OnItemClicked();
			void OnKeyPressed(KeyCode key);
		};

		public interface IViewState
		{
			IReadOnlyList<string> Items { get; }
			int? SelectedItem { get; }
			PointF? ScreenLocation { get; }
		};

		public enum KeyCode
		{
			None, Up, Down, Enter
		};
	}
}


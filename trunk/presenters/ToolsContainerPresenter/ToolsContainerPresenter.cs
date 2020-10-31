using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.ToolsContainer
{
	class Presenter : IViewModel
	{
		readonly IChangeNotification changeNotification;
		bool isVisible = true;
		double? size = null;
		int selectedToolIndex = 0;
		IReadOnlyList<ToolKind> availableTools = new[] { ToolKind.StateInspector, ToolKind.MessageProperties };
		readonly Func<IReadOnlyList<ToolInfo>> availableToolsInfo;

		public Presenter(IChangeNotification changeNotification)
		{
			this.changeNotification = changeNotification;
			this.availableToolsInfo = Selectors.Create(
				() => availableTools,
				kinds => ImmutableList.CreateRange(kinds.Select(ToToolInfo))
			);
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		bool IViewModel.IsVisible => isVisible;

		IReadOnlyList<ToolInfo> IViewModel.AvailableTools => availableToolsInfo();

		int IViewModel.SelectedToolIndex => selectedToolIndex;

		double? IViewModel.Size => size;

		void IViewModel.OnHideButtonClicked()
		{
			if (isVisible)
			{
				isVisible = false;
				changeNotification.Post();
			}
		}

		void IViewModel.OnResize(double size)
		{
			if (isVisible)
			{
				this.size = Math.Max(0, size);
				changeNotification.Post();
			}
		}

		void IViewModel.OnSelectTool(int index)
		{
			selectedToolIndex = index;
			changeNotification.Post();
		}

		void IViewModel.OnShowButtonClicked()
		{
			if (!isVisible)
			{
				isVisible = true;
				changeNotification.Post();
			}
		}

		string IViewModel.HideButtonTooltip => "Hide tools panel";

		string IViewModel.ShowButtonTooltip => "Show tools panel";

		string IViewModel.ResizerTooltip => "Resize tools panel";

		static ToolInfo ToToolInfo(ToolKind kind)
		{
			switch (kind)
			{
				case ToolKind.StateInspector:
					return new ToolInfo { Kind = kind, Name = "StateInspector", Tooltip = null };
				case ToolKind.MessageProperties:
					return new ToolInfo { Kind = kind, Name = "Message properties", Tooltip = null };
				default:
					return new ToolInfo { Kind = kind, Name = "?", Tooltip = "?" };
			}
		}
	}
}

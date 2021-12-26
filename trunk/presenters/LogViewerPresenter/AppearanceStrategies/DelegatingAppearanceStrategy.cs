using LogJoint.Settings;

namespace LogJoint.UI.Presenters.LogViewer
{
	class DelegatingAppearanceStrategy : IAppearanceStrategy
	{
		readonly IAppearanceStrategy referenceStrategy;

		public DelegatingAppearanceStrategy(
			IAppearanceStrategy referenceStrategy
		)
		{
			this.referenceStrategy = referenceStrategy;
		}

		Appearance.ColoringMode IAppearanceStrategy.Coloring => referenceStrategy.Coloring;

		void IAppearanceStrategy.SetColoring(Appearance.ColoringMode value) => referenceStrategy.SetColoring(value);

		FontData IAppearanceStrategy.Font => referenceStrategy.Font;

		void IAppearanceStrategy.SetFont(FontData font) => referenceStrategy.SetFont(font);
	};
};

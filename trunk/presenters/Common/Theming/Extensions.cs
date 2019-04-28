using System.Collections.Immutable;

namespace LogJoint.UI.Presenters
{
	public static class ColorTableExtensions
	{
		public static ModelColor GetByIndex(this IColorTable table, int index)
		{
			return GetByIndex(table.Items, index);
		}

		public static ModelColor GetByIndex(this ImmutableArray<ModelColor> table, int index)
		{
			return table[index % table.Length];
		}

		public static ModelColor? GetByIndex(this ImmutableArray<ModelColor> table, int? index)
		{
			return index != null ? table[index.Value % table.Length] : new ModelColor?();
		}
	};
}
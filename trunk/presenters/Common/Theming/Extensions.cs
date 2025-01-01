using LogJoint.Drawing;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters
{
    public static class ColorTableExtensions
    {
        public static Color GetByIndex(this IColorTable table, int index)
        {
            return GetByIndex(table.Items, index);
        }

        public static Color GetByIndex(this ImmutableArray<Color> table, int index)
        {
            return table[index % table.Length];
        }

        public static Color? GetByIndex(this ImmutableArray<Color> table, int? index)
        {
            return index != null ? table[index.Value % table.Length] : new Color?();
        }
    };
}
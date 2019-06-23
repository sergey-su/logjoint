using System;
using System.Collections.Generic;

namespace LogJoint
{
	public static class EditDistance
	{
		/// <summary>
		/// Finds the cheapest sequence of actions that needs to be taken
		/// to convert list <paramref name="s1"/> to list <paramref name="s2"/>.
		/// The cost of actions is defined by <paramref name="cost"/> function.
		/// </summary>
		/// <returns>Tuple with total cost of cheapest actions sequence and the sequence itself.
		/// An action is represented by a tuple with fields cost, i, j - the cost of the action
		/// and nullable positions in s1 and s2 respectively.
		/// If i or j is null, the action represents the insertion of s2[j] or deletion of s1[i] respectively.
		/// If both i and j are not null, the action represents conversion of s1[i] into s2[j].</returns>
		/// <remarks>
		/// The <paramref name="cost"/> function accepts two arguments or type
		/// <typeparamref name="U"/> and <typeparamref name="V"/>. It can be called in 3 modes.
		/// If first argument is default(U) the cost of insertion needs to be computed.
		/// The value to be inserted is passed in second argument.
		/// If second argument is default(V), the cost of deletion needs to be computed.
		/// The value to be deleted is passed in first argument.
		/// If both arguments are not default(U) or default(V), the cost of conversion need to be computed.
		/// Minimal cost value is 0, that cost function can return when, for example,
		/// two passed values are equal.
		/// <paramref name="cost"/> function can return int.MaxValue to disallow certain operation.
		/// </remarks>
		public static (int totalCost, List<(int? i, int? j, int cost)> edits) GetEditDistance<U, V>(
			IReadOnlyList<U> s1,
			IReadOnlyList<V> s2,
			Func<U, V, int> cost
		)
		{
			int INF = int.MaxValue;
			var memo = new (int totalCost, int di, int dj, int cost)[(s1.Count + 1) * (s2.Count + 1)];
			ref (int totalCost, int di, int dj, int cost) getMemo(int i, int j) =>
				ref memo[i * (s2.Count + 1) + j];
			(int, int, int, int) minCell((int totalCost, int, int, int) t1, (int totalCost, int, int, int) t2) =>
				t1.totalCost < t2.totalCost ? t1 : t2;
			int validateCostRetval(int value) =>
				value < 0 ? throw new InvalidOperationException("cost can not be negative") : value;
			(int, int, int, int) makeCell(int di, int dj, int cellCost, int restCost) =>
				(validateCostRetval(cellCost) == INF || restCost == INF ? INF : cellCost + restCost, di, dj, cellCost);
			var infCell = makeCell(0, 0, 0, INF);
			var zeroCell = makeCell(0, 0, 0, 0);
			for (int i = s1.Count; i >= 0; --i)
			{
				for (int j = s2.Count; j >= 0; --j)
				{
					bool eos1 = i == s1.Count;
					bool eos2 = j == s2.Count;
					var deletion = !eos1 ? makeCell(+1, 0, cost(s1[i], default), getMemo(i + 1, j).totalCost) : infCell;
					var insertion = !eos2 ? makeCell(0, +1, cost(default, s2[j]), getMemo(i, j + 1).totalCost) : infCell;
					var replacement = (!eos1 && !eos2) ? makeCell(+1, +1, cost(s1[i], s2[j]), getMemo(i + 1, j + 1).totalCost) : infCell;
					var eos = eos1 && eos2 ? zeroCell : infCell;
					getMemo(i, j) = minCell(
						minCell(deletion, insertion),
						minCell(replacement, eos)
					);
				}
			}
			var edits = new List<(int?, int?, int)>(s1.Count + s2.Count);
			for ((int i, int j) curr = (0, 0); ;)
			{
				var c = getMemo(curr.i, curr.j);
				if (c.di == 0 && c.dj == 0)
					break;
				edits.Add((
					c.di > 0 ? curr.i : new int?(), c.dj > 0 ? curr.j : new int?(),
					c.cost
				));
				curr = (curr.i + c.di, curr.j + c.dj);
			}
			return (getMemo(0, 0).totalCost, edits);
		}
	}
}

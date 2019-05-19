using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint
{
	public enum ValueBound
	{
		/// <summary>
		/// Finds the index of the FIRST element that has a value GREATER than OR EQUIVALENT to a specified value
		/// </summary>
		Lower,
		/// <summary>
		/// Finds the index of the FIRST element that has a value that is GREATER than a specified value
		/// </summary>
		Upper,
		/// <summary>
		/// Finds the index of the LAST element that has a value LESS than OR EQUIVALENT to a specified value
		/// </summary>
		LowerReversed,
		/// <summary>
		/// Finds the index of the LAST element that has a value LESS than a specified value
		/// </summary>
		UpperReversed
	};

}

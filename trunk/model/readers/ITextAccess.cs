using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint
{
	public enum TextAccessDirection
	{
		Forward,
		Backward
	};

	public interface ITextAccessIterator: IDisposable
	{
		string CurrentBuffer { get; }
		long CharIndexToPosition(int idx);
		int PositionToCharIndex(long position);
		TextAccessDirection AdvanceDirection { get; }
		ValueTask<bool> Advance(int charsToDiscard);
	};

	public interface ITextAccess
	{
		Task<ITextAccessIterator> OpenIterator(long initialPosition, TextAccessDirection direction);
		int AverageBufferLength { get; }
		int MaximumSequentialAdvancesAllowed { get; }
	};
}

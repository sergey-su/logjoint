namespace LogJoint
{
	// An interface a log provider factory can implement to export its possibility
	// to precompile format's user code into an assembly.
	public interface IPrecompilingLogProviderFactory: ILogProviderFactory
	{
		byte[] Precompile(LJTraceSource trace);
	}
}

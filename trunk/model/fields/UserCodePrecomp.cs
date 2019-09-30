using System;

namespace LogJoint
{
	public interface IUserCodePrecompile
	{
		Type CompileUserCodeToType(Func<string, string> assemblyLocationResolver);
	};
}

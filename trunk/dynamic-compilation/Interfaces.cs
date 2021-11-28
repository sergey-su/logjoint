using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public interface IMetadataReferencesProvider
	{
		IReadOnlyList<Microsoft.CodeAnalysis.MetadataReference> GetMetadataReferences();
	};
}

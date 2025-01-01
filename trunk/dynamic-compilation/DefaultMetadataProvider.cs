using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LogJoint
{
    public class DefaultMetadataReferencesProvider : IMetadataReferencesProvider
    {
        IReadOnlyList<MetadataReference> IMetadataReferencesProvider.GetMetadataReferences()
        {
            MetadataReference assemblyLocationResolver(string asmName) => MetadataReference.CreateFromFile(Assembly.Load(asmName).Location);

            var metadataReferences = new List<MetadataReference>();
            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            metadataReferences.Add(assemblyLocationResolver("System.Runtime"));
            metadataReferences.Add(assemblyLocationResolver("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"));
            metadataReferences.Add(assemblyLocationResolver(Assembly.GetExecutingAssembly().FullName));
            metadataReferences.Add(assemblyLocationResolver(typeof(StringSlice).Assembly.FullName));
            metadataReferences.Add(assemblyLocationResolver(typeof(Message).Assembly.FullName));

            return metadataReferences;
        }
    };
}

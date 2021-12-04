using System.IO;
using System.Reflection;
using LogJoint.FieldsProcessor;

namespace LogJoint.Wasm
{
    class AssemblyLoader : IAssemblyLoader
    {
        Assembly IAssemblyLoader.Load(byte[] image)
        {
            var context = System.Runtime.Loader.AssemblyLoadContext.Default;
            using var ms = new MemoryStream(image);
            return context.LoadFromStream(ms);
        }
    };
}
